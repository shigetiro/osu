// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Difficulty.Skills;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Scoring;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Osu.Difficulty
{
    public class OsuDifficultyCalculator : DifficultyCalculator
    {
        private const double difficulty_multiplier = 0.0675;

        public override int Version => 20250306;

        public OsuDifficultyCalculator(IRulesetInfo ruleset, IWorkingBeatmap beatmap)
            : base(ruleset, beatmap)
        {
        }

        protected override DifficultyAttributes CreateDifficultyAttributes(IBeatmap beatmap, Mod[] mods, Skill[] skills, double clockRate)
        {
            if (beatmap.HitObjects.Count == 0)
                return new OsuDifficultyAttributes { Mods = mods };

            var aim = skills.OfType<Aim>().Single(a => a.IncludeSliders);
            double aimRating = Math.Sqrt(aim.DifficultyValue()) * difficulty_multiplier;
            double aimDifficultyStrainCount = aim.CountTopWeightedStrains();
            double difficultSliders = aim.GetDifficultSliders();

            var aimWithoutSliders = skills.OfType<Aim>().Single(a => !a.IncludeSliders);
            double aimRatingNoSliders = Math.Sqrt(aimWithoutSliders.DifficultyValue()) * difficulty_multiplier;
            double sliderFactor = aimRating > 0 ? aimRatingNoSliders / aimRating : 1;

            var speed = skills.OfType<Speed>().Single();
            double speedRating = Math.Sqrt(speed.DifficultyValue()) * difficulty_multiplier;
            double speedNotes = speed.RelevantNoteCount();
            double speedDifficultyStrainCount = speed.CountTopWeightedStrains();

            var flashlight = skills.OfType<Flashlight>().SingleOrDefault();
            double flashlightRating = flashlight == null ? 0.0 : Math.Sqrt(flashlight.DifficultyValue()) * difficulty_multiplier;

            var relax = skills.OfType<Relax>().SingleOrDefault();

            if (mods.Any(m => m is OsuModTouchDevice))
            {
                aimRating = Math.Pow(aimRating, 0.8);
                flashlightRating = Math.Pow(flashlightRating, 0.8);
            }

            // 修正mod分支，严格对齐rosu-pp逻辑
            double baseAimPerformance = 0.0;
            double baseSpeedPerformance = 0.0;
            double baseFlashlightPerformance = 0.0;

            if (mods.Any(h => h is OsuModRelax) && relax != null)
            {
                aimRating = Math.Sqrt(relax.DifficultyValue()) * difficulty_multiplier;
                difficultSliders = relax.GetDifficultSliders();
                speedRating = 0.0;
                flashlightRating *= 0.7;
                baseAimPerformance = OsuStrainSkill.DifficultyToPerformance(aimRating);
                // Relax下speed和flashlight不计入performance
            }
            else if (mods.Any(h => h is OsuModAutopilot))
            {
                speedRating *= 0.5;
                aimRating = 0.0;
                flashlightRating *= 0.4;
                baseSpeedPerformance = OsuStrainSkill.DifficultyToPerformance(speedRating);
                // Autopilot下aim和flashlight不计入performance
            }
            else
            {
                baseAimPerformance = OsuStrainSkill.DifficultyToPerformance(aimRating);
                baseSpeedPerformance = OsuStrainSkill.DifficultyToPerformance(speedRating);
                if (mods.Any(h => h is OsuModFlashlight))
                    baseFlashlightPerformance = Flashlight.DifficultyToPerformance(flashlightRating);
            }

            double basePerformance;
            if (mods.Any(h => h is OsuModRelax) && relax != null)
            {
                basePerformance = baseAimPerformance;
            }
            else
            {
                basePerformance =
                    Math.Pow(
                        Math.Pow(baseAimPerformance, 1.1) +
                        Math.Pow(baseSpeedPerformance, 1.1) +
                        Math.Pow(baseFlashlightPerformance, 1.1), 1.0 / 1.1
                    );
            }

            double starRating = basePerformance > 0.00001
                ? Math.Cbrt(OsuPerformanceCalculator.PERFORMANCE_BASE_MULTIPLIER) * 0.027 * (Math.Cbrt(100000 / Math.Pow(2, 1 / 1.1) * basePerformance) + 4)
                : 0;

            double drainRate = beatmap.Difficulty.DrainRate;

            int hitCirclesCount = beatmap.HitObjects.Count(h => h is HitCircle);
            int sliderCount = beatmap.HitObjects.Count(h => h is Slider);
            int spinnerCount = beatmap.HitObjects.Count(h => h is Spinner);

            OsuDifficultyAttributes attributes = new OsuDifficultyAttributes
            {
                StarRating = starRating,
                Mods = mods,
                AimDifficulty = aimRating,
                AimDifficultSliderCount = difficultSliders,
                SpeedDifficulty = speedRating,
                SpeedNoteCount = speedNotes,
                FlashlightDifficulty = flashlightRating,
                SliderFactor = sliderFactor,
                AimDifficultStrainCount = aimDifficultyStrainCount,
                SpeedDifficultStrainCount = speedDifficultyStrainCount,
                DrainRate = drainRate,
                MaxCombo = beatmap.GetMaxCombo(),
                HitCircleCount = hitCirclesCount,
                SliderCount = sliderCount,
                SpinnerCount = spinnerCount,
            };

            return attributes;
        }

        protected override IEnumerable<DifficultyHitObject> CreateDifficultyHitObjects(IBeatmap beatmap, double clockRate)
        {
            List<DifficultyHitObject> objects = new List<DifficultyHitObject>();

            // The first jump is formed by the first two hitobjects of the map.
            // If the map has less than two OsuHitObjects, the enumerator will not return anything.
            var hitObjects = beatmap.HitObjects.ToList(); // 将 IReadOnlyList 转换为 List

            for (int i = 1; i < hitObjects.Count; i++)
            {
                var lastLast = i > 1 ? hitObjects[i - 2] : null; // 移除冗余的显式类型转换
                objects.Add(new OsuDifficultyHitObject(hitObjects[i], hitObjects[i - 1], lastLast, clockRate, objects, objects.Count));
            }

            return objects;
        }

        protected override Skill[] CreateSkills(IBeatmap beatmap, Mod[] mods, double clockRate)
        {
            // Calculate hit window for Relax skill
            HitWindows hitWindows = new OsuHitWindows();
            hitWindows.SetDifficulty(beatmap.Difficulty.OverallDifficulty);
            double hitWindow = hitWindows.WindowFor(HitResult.Great) / clockRate;

            var skills = new List<Skill>
            {
                new Aim(mods, true),
                new Aim(mods, false),
                new Speed(mods)
            };

            if (mods.Any(h => h is OsuModFlashlight))
                skills.Add(new Flashlight(mods));

            if (mods.Any(h => h is OsuModRelax))
                skills.Add(new Relax(mods, true, hitWindow));

            return skills.ToArray();
        }

        protected override Mod[] DifficultyAdjustmentMods => new Mod[]
        {
            new OsuModTouchDevice(),
            new OsuModDoubleTime(),
            new OsuModHalfTime(),
            new OsuModEasy(),
            new OsuModHardRock(),
            new OsuModFlashlight(),
            new MultiMod(new OsuModFlashlight(), new OsuModHidden())
        };
    }
}
