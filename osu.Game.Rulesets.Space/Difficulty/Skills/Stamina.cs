using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Space.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Space.Difficulty.Skills
{
    public class Stamina : StrainSkill
    {
        protected virtual double SkillMultiplier => 1.5;
        protected virtual double StrainDecayBase => 0.15;

        private double currentStrain;

        public Stamina(Mod[] mods) : base(mods) { }

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
            // Simple density-based strain for stamina
            double duration = Math.Max(current.DeltaTime, 50);
            return 1000.0 / duration;
        }

        private double strainDecay(double ms) => Math.Pow(StrainDecayBase, ms / 1000);
    }
}
