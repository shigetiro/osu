using System;
using osu.Game.Rulesets.Difficulty.Utils;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Difficulty.Evaluators
{
    public static class RelaxAimEvaluator
    {
        private const double wide_angle_multiplier = 1.5;
        private const double acute_angle_multiplier = 2.6;
        private const double slider_multiplier = 1.5;
        private const double velocity_change_multiplier = 1.2;
        private const double wiggle_multiplier = 1.02;

        public static double EvaluateDifficultyOf(OsuDifficultyHitObject current, bool withSliderTravelDistance)
        {
            if (current.BaseObject is Spinner || current.Index <= 1)
                return 0;

            if (current.Previous(0) is not OsuDifficultyHitObject last || current.Previous(1) is not OsuDifficultyHitObject lastLast || last.BaseObject is Spinner)
                return 0;

            const int radius = OsuDifficultyHitObject.NORMALISED_RADIUS;
            const int diameter = OsuDifficultyHitObject.NORMALISED_DIAMETER;

            // Calculate the velocity to the current hitobject, which starts
            // with a base distance / time assuming the last object is a hitcircle.
            double currVelocity = current.LazyJumpDistance / current.StrainTime;

            // But if the last object is a slider, then we extend the travel velocity through the slider into the current object.
            if (withSliderTravelDistance && last.BaseObject is Slider)
            {
                // Calculate the slider velocity from slider head to slider end.
                double travelVelocity = last.TravelDistance / last.TravelTime;
                double movementVelocity = current.MinimumJumpDistance / current.MinimumJumpTime;
                // Take the larger total combined velocity.
                currVelocity = Math.Max(currVelocity, movementVelocity + travelVelocity);
            }

            // As above, do the same for the previous hitobject.
            double prevVelocity = last.LazyJumpDistance / last.StrainTime;

            if (withSliderTravelDistance && lastLast.BaseObject is Slider)
            {
                double travelVelocity = lastLast.TravelDistance / lastLast.TravelTime;
                double movementVelocity = last.MinimumJumpDistance / last.MinimumJumpTime;
                prevVelocity = Math.Max(prevVelocity, movementVelocity + travelVelocity);
            }

            double wideAngleBonus = 0;
            double acuteAngleBonus = 0;
            double sliderBonus = 0;
            double velocityChangeBonus = 0;
            double wiggleBonus = 0;

            // Start strain with regular velocity.
            double aimStrain = currVelocity;

            // R* Penalize overall stream aim. Fittings: [(100, 0.92), (300, 0.98)] linear function.
            double streamNerf = current.LazyJumpDistance * 0.0006 + 0.86;
            aimStrain *= Math.Clamp(streamNerf, 0.92, 0.98);

            // If rhythms are the same.
            if (Math.Max(current.StrainTime, last.StrainTime) < 1.25 * Math.Min(current.StrainTime, last.StrainTime) && current.Angle is double currAngle && last.Angle is double lastAngle)
            {
                // Rewarding angles, take the smaller velocity as base.
                double angleBonus = Math.Min(currVelocity, prevVelocity);

                wideAngleBonus = calcWideAngleBonus(currAngle);
                acuteAngleBonus = calcAcuteAngleBonus(currAngle);

                // Penalize angle repetition.
                wideAngleBonus *= 1 - Math.Min(wideAngleBonus, Math.Pow(calcWideAngleBonus(lastAngle), 3));
                acuteAngleBonus *= 0.08 + 0.92 * (1 - Math.Min(acuteAngleBonus, Math.Pow(calcAcuteAngleBonus(lastAngle), 3)));

                // R* Nerf strain time for above 300 1/2 fast objects smoothly.
                const double nerf_base = 1.07;
                double nerfStrainTime = current.StrainTime
                                        * Math.Pow(nerf_base, DifficultyCalculationUtils.Smootherstep(DifficultyCalculationUtils.MillisecondsToBPM(current.StrainTime, 2), 300, 400));

                // Apply full wide angle bonus for distance more than one diameter.
                wideAngleBonus *= angleBonus * DifficultyCalculationUtils.Smootherstep(current.LazyJumpDistance, 0, diameter);

                // Apply acute angle bonus for BPM above 300 1/2 and distance more than one diameter.
                acuteAngleBonus *= angleBonus
                                   * DifficultyCalculationUtils.Smootherstep(DifficultyCalculationUtils.MillisecondsToBPM(nerfStrainTime, 2), 300, 400)
                                   * DifficultyCalculationUtils.Smootherstep(current.LazyJumpDistance, diameter, diameter * 2);

                // R* Penalize wide angles if their distances are quite small (consider as wide angle stream).
                // R* Only jump dist is considered here, not velocity.
                // R* Fittings: [(200, 0), (250, 0.5), (300, 1), (350, 1)] linear function.
                double wideStreamNerf = current.LazyJumpDistance * 0.007 - 1.3;
                wideAngleBonus *= Math.Clamp(wideStreamNerf, 0, 1);

                // Apply wiggle bonus for jumps that are [radius, 3*diameter] in distance, with < 110 angle.
                // * https://www.desmos.com/calculator/dp0v0nvowc
                wiggleBonus = angleBonus
                              * DifficultyCalculationUtils.Smootherstep(current.LazyJumpDistance, radius, diameter)
                              * Math.Pow(DifficultyCalculationUtils.ReverseLerp(current.LazyJumpDistance, diameter * 3, diameter), 1.8)
                              * DifficultyCalculationUtils.Smootherstep(currAngle, double.DegreesToRadians(110), double.DegreesToRadians(60))
                              * DifficultyCalculationUtils.Smootherstep(last.LazyJumpDistance, radius, diameter)
                              * Math.Pow(DifficultyCalculationUtils.ReverseLerp(last.LazyJumpDistance, diameter * 3, diameter), 1.8)
                              * DifficultyCalculationUtils.Smootherstep(lastAngle, double.DegreesToRadians(110), double.DegreesToRadians(60));
            }

            if (Math.Max(prevVelocity, currVelocity) != 0)
            {
                // We want to use the average velocity over the whole object when awarding differences, not the individual jump and slider path velocities.
                prevVelocity = (last.LazyJumpDistance + lastLast.TravelDistance) / last.StrainTime;
                currVelocity = (current.LazyJumpDistance + last.TravelDistance) / current.StrainTime;

                // Scale with ratio of difference compared to 0.5 * max dist.
                double distRatioBase = Math.Sin(Math.PI / 2 * Math.Abs(prevVelocity - currVelocity) / Math.Max(prevVelocity, currVelocity));
                double distRatio = Math.Pow(distRatioBase, 2);

                // Reward for % distance up to 125 / strainTime for overlaps where velocity is still changing.
                double overlapVelocityBuff = Math.Min(diameter * 1.25 / Math.Min(current.StrainTime, last.StrainTime), Math.Abs(prevVelocity - currVelocity));

                velocityChangeBonus = overlapVelocityBuff * distRatio;

                // Penalize for rhythm changes.
                double bonusBase = Math.Min(current.StrainTime, last.StrainTime) / Math.Max(current.StrainTime, last.StrainTime);
                velocityChangeBonus *= Math.Pow(bonusBase, 2);
            }

            if (last.BaseObject is Slider)
                // Reward sliders based on velocity.
                sliderBonus = last.TravelDistance / last.TravelTime;

            aimStrain += wiggleBonus * wiggle_multiplier;

            // Add in acute angle bonus or wide angle bonus + velocity change bonus, whichever is larger.
            aimStrain += Math.Max(acuteAngleBonus * acute_angle_multiplier, wideAngleBonus * wide_angle_multiplier + velocityChangeBonus * velocity_change_multiplier);

            if (withSliderTravelDistance)
                // Add in additional slider velocity bonus.
                aimStrain += sliderBonus * slider_multiplier;

            if (current.LazyJumpDistance < 350)
                // If the distance is small enough, we want to buff the rhythm complexity.
                aimStrain *= RelaxRhythmEvaluator.EvaluateDifficultyOf(current, current.HitWindowGreat);

            return aimStrain;
        }

        private static double calcWideAngleBonus(double angle) => DifficultyCalculationUtils.Smoothstep(angle, double.DegreesToRadians(40), double.DegreesToRadians(140));

        private static double calcAcuteAngleBonus(double angle) => DifficultyCalculationUtils.Smoothstep(angle, double.DegreesToRadians(140), double.DegreesToRadians(40));
    }
}
