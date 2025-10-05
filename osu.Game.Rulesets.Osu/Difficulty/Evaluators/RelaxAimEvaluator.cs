// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Utils;
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

        public static double EvaluateDifficultyOf(OsuDifficultyHitObject current, OsuDifficultyHitObject[] diffObjects, double hitWindow, bool withSliderTravelDist)
        {
            var osuCurrObj = current;

            if (current.BaseObject is Spinner || current.LastObject is Spinner)
                return 0.0;

            var osuLastObj = current.Previous(1) as OsuDifficultyHitObject;
            var osuLastLastObj = current.Previous(2) as OsuDifficultyHitObject;

            if (osuLastObj == null)
                return 0.0;

            const int radius = OsuDifficultyHitObject.NORMALISED_RADIUS;
            const int diameter = OsuDifficultyHitObject.NORMALISED_DIAMETER;

            // Calculate the velocity to the current hitobject, which starts
            // with a base distance / time assuming the last object is a hitcircle.
            double currVel = osuCurrObj.LazyJumpDistance / osuCurrObj.StrainTime;

            // But if the last object is a slider, then we extend the travel
            // velocity through the slider into the current object.
            if (osuLastObj.BaseObject is Slider slider && withSliderTravelDist)
            {
                // calculate the slider velocity from slider head to slider end.
                double travelVel = osuLastObj.TravelDistance / osuLastObj.TravelTime;
                // calculate the movement velocity from slider end to current object
                double movementVel = osuCurrObj.MinimumJumpDistance / osuCurrObj.MinimumJumpTime;

                // take the larger total combined velocity.
                currVel = Math.Max(currVel, movementVel + travelVel);
            }

            // As above, do the same for the previous hitobject.
            double prevVel = osuLastObj.LazyJumpDistance / osuLastObj.StrainTime;

            if (osuLastLastObj?.BaseObject is Slider slider2 && withSliderTravelDist)
            {
                double travelVel = osuLastLastObj.TravelDistance / osuLastLastObj.TravelTime;
                double movementVel = osuLastObj.MinimumJumpDistance / osuLastObj.MinimumJumpTime;

                prevVel = Math.Max(prevVel, movementVel + travelVel);
            }

            double wideAngleBonus = 0;
            double acuteAngleBonus = 0;
            double sliderBonus = 0;
            double velChangeBonus = 0;
            double wiggleBonus = 0;

            // Start strain with regular velocity.
            double aimStrain = currVel;

            // Penalize overall stream aim.
            // Fittings: [(100, 0.92), (300, 0.98)] linear function.
            double streamNerf = 0.0006 * osuCurrObj.LazyJumpDistance + 0.86;
            aimStrain *= Math.Clamp(streamNerf, 0.92, 0.98);

            // If rhythms are the same.
            if (Math.Max(osuCurrObj.StrainTime, osuLastObj.StrainTime) < 1.25 * Math.Min(osuCurrObj.StrainTime, osuLastObj.StrainTime))
            {
                if (osuCurrObj.Angle != null && osuLastObj.Angle != null)
                {
                    // Rewarding angles, take the smaller velocity as base.
                    double angleBonus = Math.Min(currVel, prevVel);

                    wideAngleBonus = calcWideAngleBonus(osuCurrObj.Angle.Value);
                    acuteAngleBonus = calcAcuteAngleBonus(osuCurrObj.Angle.Value);

                    // Penalize angle repetition.
                    wideAngleBonus *= 1.0 - Math.Min(wideAngleBonus, Math.Pow(calcWideAngleBonus(osuLastObj.Angle.Value), 3));
                    acuteAngleBonus *= 0.08 + 0.92 * (1.0 - Math.Min(acuteAngleBonus, Math.Pow(calcAcuteAngleBonus(osuLastObj.Angle.Value), 3)));

                    // Nerf strain time for above 300 1/2 fast objects smoothly.
                    double nerfBase = 1.07;
                    double halfTime = osuCurrObj.StrainTime / 2;
                    double bpm = 60000 / halfTime;
                    double nerfStrainTime = osuCurrObj.StrainTime * Math.Pow(nerfBase, DifficultyCalculationUtils.Smootherstep(bpm, 300, 400));

                    // Apply full wide angle bonus for distance more than one diameter
                    wideAngleBonus *= angleBonus * DifficultyCalculationUtils.Smootherstep(osuCurrObj.LazyJumpDistance, 0, diameter);

                    // Apply acute angle bonus for BPM above 300 1/2 and distance more than one diameter
                    double nerfHalfTime = nerfStrainTime / 2;
                    double nerfBpm = 60000 / nerfHalfTime;
                    acuteAngleBonus *= angleBonus
                                       * DifficultyCalculationUtils.Smootherstep(nerfBpm, 300, 400)
                                       * DifficultyCalculationUtils.Smootherstep(osuCurrObj.LazyJumpDistance, diameter, diameter * 2);

                    // Penalize wide angles if their distances are quite small (consider as wide angle stream).
                    // Only jump dist is considered here, not velocity.
                    // Fittings: [(200, 0), (250, 0.5), (300, 1), (350, 1)] linear function.
                    double wideStreamNerf = osuCurrObj.LazyJumpDistance * 0.007 - 1.3;
                    wideAngleBonus *= Math.Clamp(wideStreamNerf, 0.0, 1.0);

                    // Apply wiggle bonus for jumps that are [radius, 3*diameter] in distance, with < 110 angle
                    // https://www.desmos.com/calculator/dp0v0nvowc
                    double reverseLerp1 = DifficultyCalculationUtils.ReverseLerp(osuCurrObj.LazyJumpDistance, diameter * 3, diameter);
                    double reverseLerp2 = DifficultyCalculationUtils.ReverseLerp(osuLastObj.LazyJumpDistance, diameter * 3, diameter);
                    
                    wiggleBonus = angleBonus
                                  * DifficultyCalculationUtils.Smootherstep(osuCurrObj.LazyJumpDistance, radius, diameter)
                                  * Math.Pow(reverseLerp1, 1.8)
                                  * DifficultyCalculationUtils.Smootherstep(osuCurrObj.Angle.Value, double.DegreesToRadians(110), double.DegreesToRadians(60))
                                  * DifficultyCalculationUtils.Smootherstep(osuLastObj.LazyJumpDistance, radius, diameter)
                                  * Math.Pow(reverseLerp2, 1.8)
                                  * DifficultyCalculationUtils.Smootherstep(osuLastObj.Angle.Value, double.DegreesToRadians(110), double.DegreesToRadians(60));
                }
            }

            if (!Precision.AlmostEquals(Math.Max(prevVel, currVel), 0.0))
            {
                // We want to use the average velocity over the whole object when awarding
                // differences, not the individual jump and slider path velocities.
                prevVel = (osuLastObj.LazyJumpDistance + (osuLastLastObj?.TravelDistance ?? 0)) / osuLastObj.StrainTime;
                currVel = (osuCurrObj.LazyJumpDistance + (osuLastObj?.TravelDistance ?? 0)) / osuCurrObj.StrainTime;

                // Scale with ratio of difference compared to 0.5 * max dist.
                double distRatioBase = Math.Sin(Math.PI / 2 * Math.Abs(prevVel - currVel) / Math.Max(prevVel, currVel));
                double distRatio = Math.Pow(distRatioBase, 2);

                // Reward for % distance up to 125 / strainTime for overlaps where velocity is still changing.
                double overlapVelBuff = Math.Min(diameter * 1.25 / Math.Min(osuCurrObj.StrainTime, osuLastObj.StrainTime), Math.Abs(prevVel - currVel));

                velChangeBonus = overlapVelBuff * distRatio;

                // Penalize for rhythm changes.
                double bonusBase = Math.Min(osuCurrObj.StrainTime, osuLastObj.StrainTime) / Math.Max(osuCurrObj.StrainTime, osuLastObj.StrainTime);
                velChangeBonus *= Math.Pow(bonusBase, 2);
            }

            if (osuLastObj.BaseObject is Slider slider3)
            {
                // Reward sliders based on velocity.
                sliderBonus = osuLastObj.TravelDistance / osuLastObj.TravelTime;
            }

            aimStrain += wiggleBonus * wiggle_multiplier;

            // Add in acute angle bonus or wide angle bonus + velocity change bonus, whichever is larger.
            aimStrain += Math.Max(acuteAngleBonus * acute_angle_multiplier, wideAngleBonus * wide_angle_multiplier + velChangeBonus * velocity_change_multiplier);

            // Add in additional slider velocity bonus.
            if (withSliderTravelDist)
            {
                aimStrain += sliderBonus * slider_multiplier;
            }

            // If the distance is small enough, we want to buff the rhythm complexity.
            if (osuCurrObj.LazyJumpDistance < 350.0)
            {
                aimStrain *= RelaxRhythmEvaluator.EvaluateDifficultyOf(current, diffObjects, hitWindow);
            }

            return aimStrain;
        }

        private static double calcWideAngleBonus(double angle) => DifficultyCalculationUtils.Smoothstep(angle, double.DegreesToRadians(40), double.DegreesToRadians(140));
        private static double calcAcuteAngleBonus(double angle) => DifficultyCalculationUtils.Smoothstep(angle, double.DegreesToRadians(140), double.DegreesToRadians(40));
    }
}