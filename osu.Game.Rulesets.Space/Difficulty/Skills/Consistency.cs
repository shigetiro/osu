using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Space.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Space.Difficulty.Skills
{
    public class Consistency : StrainSkill
    {
        protected virtual double SkillMultiplier => 1.0;
        protected virtual double StrainDecayBase => 0.15;

        private double currentStrain;

        public Consistency(Mod[] mods) : base(mods) { }

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
            // Consistency often relates to maintaining performance over time.
            // We'll use a simple time-based accumulator or density check.
            return 1.0;
        }

        private double strainDecay(double ms) => Math.Pow(StrainDecayBase, ms / 1000);
    }
}
