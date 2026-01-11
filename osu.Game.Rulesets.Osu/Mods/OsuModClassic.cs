// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Rulesets.Osu.Scoring;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Play;
using osu.Game.Screens.Play.HUD.HitErrorMeters;
using osu.Framework.Graphics.Containers;
using System.Collections.Generic;

namespace osu.Game.Rulesets.Osu.Mods
{
    public class OsuModClassic : ModClassic, IApplicableToHitObject, IApplicableToDrawableHitObject, IApplicableToDrawableRuleset<OsuHitObject>, IApplicableHealthProcessor, IApplicableToHUD
    {
        public override Type[] IncompatibleMods => base.IncompatibleMods.Append(typeof(OsuModStrictTracking)).ToArray();

        [SettingSource("No slider head accuracy requirement", "Scores sliders proportionally to the number of ticks hit.")]
        public Bindable<bool> NoSliderHeadAccuracy { get; } = new BindableBool(true);

        [SettingSource("Apply classic note lock", "Applies note lock to the full hit window.")]
        public Bindable<bool> ClassicNoteLock { get; } = new BindableBool(true);

        [SettingSource("Always play a slider's tail sample", "Always plays a slider's tail sample regardless of whether it was hit or not.")]
        public Bindable<bool> AlwaysPlayTailSample { get; } = new BindableBool(true);

        [SettingSource("Fade out hit circles earlier", "Make hit circles fade out into a miss, rather than after it.")]
        public Bindable<bool> FadeHitCircleEarly { get; } = new Bindable<bool>(true);

        [SettingSource("Classic health", "More closely resembles the original HP drain mechanics.")]
        public Bindable<bool> ClassicHealth { get; } = new Bindable<bool>(true);

        private bool usingHiddenFading;

        public void ApplyToHitObject(HitObject hitObject)
        {
            // Replace hit windows with legacy stable hit windows for osu! hit objects
            if (hitObject is OsuHitObject)
            {
                // If hit windows are already set (from ApplyDefaults), extract the OD and apply it
                // Otherwise, SetDifficulty will be called later in ApplyDefaults
                var legacyHitWindows = new LegacyOsuHitWindows();

                if (hitObject.HitWindows != null && hitObject.HitWindows is OsuHitWindows existing)
                {
                    // Extract OD from existing hit windows by reverse-engineering the difficulty range
                    // We can estimate OD from the meh window: meh = 200 - (OD * 10) in stable
                    // But in Lazer: meh = DifficultyRange(OD, 200, 150, 100)
                    // For a rough estimate, we'll use the meh window to calculate what OD would produce it in stable
                    double meh = existing.WindowFor(HitResult.Meh);
                    // Reverse Lazer formula to get approximate OD, then use that for stable
                    // This is approximate but should be close enough
                    double estimatedOD = IBeatmapDifficultyInfo.InverseDifficultyRange(meh, 200, 150, 100);
                    legacyHitWindows.SetDifficulty(estimatedOD);
                }

                hitObject.HitWindows = legacyHitWindows;
            }

            switch (hitObject)
            {
                case Slider slider:
                    slider.ClassicSliderBehaviour = NoSliderHeadAccuracy.Value;
                    break;
            }
        }

        public void ApplyToDrawableRuleset(DrawableRuleset<OsuHitObject> drawableRuleset)
        {
            var osuRuleset = (DrawableOsuRuleset)drawableRuleset;

            if (ClassicNoteLock.Value)
            {
                double hittableRange = OsuHitWindows.MISS_WINDOW - (drawableRuleset.Mods.OfType<OsuModAutopilot>().Any() ? 200 : 0);
                osuRuleset.Playfield.HitPolicy = new LegacyHitPolicy(hittableRange);
            }

            usingHiddenFading = drawableRuleset.Mods.OfType<OsuModHidden>().SingleOrDefault()?.OnlyFadeApproachCircles.Value == false;
        }

        public void ApplyToHUD(HUDOverlay overlay)
        {
            foreach (var meter in findDescendants<BarHitErrorMeter>(overlay))
            {
                meter.CentreMarkerStyle.Value = BarHitErrorMeter.CentreMarkerStyles.Line;
                meter.ColourBarVisibility.Value = true;
            }
        }

        private static IEnumerable<T> findDescendants<T>(Container root)
        {
            var stack = new Stack<Drawable>(root.Children);
            while (stack.Count > 0)
            {
                var d = stack.Pop();
                if (d is T t)
                    yield return t;

                if (d is Container c)
                {
                    foreach (var child in c.Children)
                        stack.Push(child);
                }
            }
        }

        public void ApplyToDrawableHitObject(DrawableHitObject obj)
        {
            switch (obj)
            {
                case DrawableSliderHead head:
                    if (FadeHitCircleEarly.Value && !usingHiddenFading)
                        applyEarlyFading(head);

                    if (ClassicNoteLock.Value)
                        blockInputToObjectsUnderSliderHead(head);

                    break;

                case DrawableSliderTail tail:
                    tail.SamplePlaysOnlyOnHit = !AlwaysPlayTailSample.Value;
                    break;

                case DrawableHitCircle circle:
                    if (FadeHitCircleEarly.Value && !usingHiddenFading)
                        applyEarlyFading(circle);

                    break;
            }
        }

        /// <summary>
        /// On stable, slider heads that have already been hit block input from reaching objects that may be underneath them
        /// until the sliders they're part of have been fully judged.
        /// The purpose of this method is to restore that behaviour.
        /// In order to avoid introducing yet another confusing config option, this behaviour is roped into the general notion of "note lock".
        /// </summary>
        private static void blockInputToObjectsUnderSliderHead(DrawableSliderHead slider)
        {
            slider.HitArea.CanBeHit = () => !slider.DrawableSlider.AllJudged;
        }

        private void applyEarlyFading(DrawableHitCircle circle)
        {
            circle.ApplyCustomUpdateState += (dho, state) =>
            {
                using (dho.BeginAbsoluteSequence(dho.StateUpdateTime))
                {
                    if (state != ArmedState.Hit)
                    {
                        double okWindow = dho.HitObject.HitWindows.WindowFor(HitResult.Ok);
                        double lateMissFadeTime = dho.HitObject.HitWindows.WindowFor(HitResult.Meh) - okWindow;
                        dho.Delay(okWindow).FadeOut(lateMissFadeTime);
                    }
                }
            };
        }

        public HealthProcessor? CreateHealthProcessor(double drainStartTime) => ClassicHealth.Value ? new OsuLegacyHealthProcessor(drainStartTime) : null;
    }
}
