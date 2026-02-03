using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Space.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Space.Difficulty.Skills
{
    public class Flow : StrainSkill
    {
        protected virtual double SkillMultiplier => 1.5;
        protected virtual double StrainDecayBase => 0.15;

        private double currentStrain;

        public Flow(Mod[] mods) : base(mods) { }

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

            double angleBonus = 1.0;
            if (spaceObject.Angle != null)
            {
                double angle = spaceObject.Angle.Value;
                double degrees = angle * (180.0 / Math.PI);

                // Reward wide angles (flow)
                if (degrees < 90)
                    angleBonus = 1.2; // Sharper turns are harder flow? Or breaks flow? 
                                      // User said "adjust your cursor movement with ease".
                                      // Usually Flow implies continuous movement.
                                      // I'll assume standard osu! flow logic: wider angles are more flow-y, acute are snap-y.
                                      // But "difficulty" usually rewards harder things.
                                      // If Flow is "ability to adjust cursor movement with ease", maybe it measures how hard it is to maintain flow?

                // For now, I'll just use a simple angle scalar.
                angleBonus += angle / Math.PI;
            }

            double time = Math.Max(current.DeltaTime, 50);
            double velocity = spaceObject.JumpDistance / time;

            return velocity * angleBonus;
        }

        private double strainDecay(double ms) => Math.Pow(StrainDecayBase, ms / 1000);
    }
}
