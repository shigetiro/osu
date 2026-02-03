using System;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects.Drawables;
#if !SERVER
using osu.Game.Rulesets.Space.Objects.Drawables;
#endif

namespace osu.Game.Rulesets.Space.Mods
{
    public class SpaceModFullGhost : ModHidden, IApplicableToDrawableHitObject
    {
        public override string Name => "FullGhost";
        public override string Acronym => "HD";
        public override LocalisableString Description => "Notes fade out before you hit them!";
        public override double ScoreMultiplier => 1.06;

        protected override void ApplyNormalVisibilityState(DrawableHitObject hitObject, ArmedState state)
        {
        }

        protected override void ApplyIncreasedVisibilityState(DrawableHitObject hitObject, ArmedState state)
        {
        }

        public override void ApplyToDrawableHitObject(DrawableHitObject drawable)
        {
#if !SERVER
            if (drawable is DrawableSpaceHitObject spaceDrawable)
            {
                spaceDrawable.FullGhost = true;
            }
#endif
        }
    }
}
