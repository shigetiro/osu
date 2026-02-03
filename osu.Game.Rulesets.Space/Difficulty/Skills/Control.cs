using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Space.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Space.Difficulty.Skills
{
    public class Control : StrainSkill
    {
        protected virtual double SkillMultiplier => 2.0;
        protected virtual double StrainDecayBase => 0.15;

        private double currentStrain;

        public Control(Mod[] mods) : base(mods) { }

        protected override double StrainValueAt(DifficultyHitObject current)
        {
            currentStrain *= strainDecay(current.DeltaTime);
            currentStrain += StrainValueOf(current) * SkillMultiplier;

            return currentStrain;
        }

        protected override double CalculateInitialStrain(double time, DifficultyHitObject current)
        {
            return currentStrain * strainDecay(time - current.Previous(0).StartTime);
        }

        protected double StrainValueOf(DifficultyHitObject current)
        {
            var spaceObject = (SpaceDifficultyHitObject)current;

            // Penalize erratic speed changes or high precision requirements
            double time = Math.Max(current.DeltaTime, 50);
            double velocity = spaceObject.JumpDistance / time;

            return velocity * 0.5;
        }

        private double strainDecay(double ms) => Math.Pow(StrainDecayBase, ms / 1000);
    }
}
