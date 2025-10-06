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

            double currVel = osuCurrObj.LazyJumpDistance / osuCurrObj.StrainTime;

            if (osuLastObj.BaseObject is Slider && withSliderTravelDist)
            {
                double travelVel = osuLastObj.TravelDistance / osuLastObj.TravelTime;
                double movementVel = osuCurrObj.MinimumJumpDistance / osuCurrObj.MinimumJumpTime;
                currVel = Math.Max(currVel, movementVel + travelVel);
            }

            double prevVel = osuLastObj.LazyJumpDistance / osuLastObj.StrainTime;

            if (osuLastLastObj?.BaseObject is Slider && withSliderTravelDist)
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

            double aimStrain = currVel;

            double streamNerf = 0.0006 * osuCurrObj.LazyJumpDistance + 0.86;
            aimStrain *= Math.Clamp(streamNerf, 0.92, 0.98);

            if (Math.Max(osuCurrObj.StrainTime, osuLastObj.StrainTime) < 1.25 * Math.Min(osuCurrObj.StrainTime, osuLastObj.StrainTime))
            {
                if (osuCurrObj.Angle != null && osuLastObj.Angle != null)
                {
                    double angleBonus = Math.Min(currVel, prevVel);

                    wideAngleBonus = calcWideAngleBonus(osuCurrObj.Angle.Value);
                    acuteAngleBonus = calcAcuteAngleBonus(osuCurrObj.Angle.Value);

                    wideAngleBonus *= 1.0 - Math.Min(wideAngleBonus, Math.Pow(calcWideAngleBonus(osuLastObj.Angle.Value), 3.0));
                    acuteAngleBonus *= 0.08 + 0.92 * (1.0 - Math.Min(acuteAngleBonus, Math.Pow(calcAcuteAngleBonus(osuLastObj.Angle.Value), 3.0)));

                    double nerfBase = 1.07;
                    double nerfStrainTime = osuCurrObj.StrainTime * Math.Pow(nerfBase, DifficultyCalculationUtils.Smootherstep(60000 / (osuCurrObj.StrainTime / 2), 300, 400));

                    wideAngleBonus *= angleBonus * DifficultyCalculationUtils.Smootherstep(osuCurrObj.LazyJumpDistance, 0, diameter);
                    acuteAngleBonus *= angleBonus
                        * DifficultyCalculationUtils.Smootherstep(60000 / (nerfStrainTime / 2), 300, 400)
                        * DifficultyCalculationUtils.Smootherstep(osuCurrObj.LazyJumpDistance, diameter, diameter * 2);

                    double wideStreamNerf = osuCurrObj.LazyJumpDistance * 0.007 - 1.3;
                    wideAngleBonus *= Math.Clamp(wideStreamNerf, 0.0, 1.0);

                    double reverseLerp1 = DifficultyCalculationUtils.ReverseLerp(osuCurrObj.LazyJumpDistance, diameter * 3, diameter);
                    double reverseLerp2 = DifficultyCalculationUtils.ReverseLerp(osuLastObj.LazyJumpDistance, diameter * 3, diameter);

                    wiggleBonus = angleBonus
                        * DifficultyCalculationUtils.Smootherstep(osuCurrObj.LazyJumpDistance, radius, diameter)
                        * Math.Pow(reverseLerp1, 1.8)
                        * DifficultyCalculationUtils.Smootherstep(osuCurrObj.Angle.Value, Math.PI * 110.0 / 180.0, Math.PI * 60.0 / 180.0)
                        * DifficultyCalculationUtils.Smootherstep(osuLastObj.LazyJumpDistance, radius, diameter)
                        * Math.Pow(reverseLerp2, 1.8)
                        * DifficultyCalculationUtils.Smootherstep(osuLastObj.Angle.Value, Math.PI * 110.0 / 180.0, Math.PI * 60.0 / 180.0);
                }
            }

            if (!Precision.AlmostEquals(Math.Max(prevVel, currVel), 0.0))
            {
                prevVel = (osuLastObj.LazyJumpDistance + (osuLastLastObj?.TravelDistance ?? 0)) / osuLastObj.StrainTime;
                currVel = (osuCurrObj.LazyJumpDistance + (osuLastObj?.TravelDistance ?? 0)) / osuCurrObj.StrainTime;

                double distRatioBase = Math.Sin(Math.PI / 2 * Math.Abs(prevVel - currVel) / Math.Max(prevVel, currVel));
                double distRatio = Math.Pow(distRatioBase, 2.0);

                double overlapVelBuff = Math.Min(diameter * 1.25 / Math.Min(osuCurrObj.StrainTime, osuLastObj.StrainTime), Math.Abs(prevVel - currVel));

                velChangeBonus = overlapVelBuff * distRatio;

                double bonusBase = Math.Min(osuCurrObj.StrainTime, osuLastObj.StrainTime) / Math.Max(osuCurrObj.StrainTime, osuLastObj.StrainTime);
                velChangeBonus *= Math.Pow(bonusBase, 2.0);
            }

            if (osuLastObj.BaseObject is Slider)
            {
                sliderBonus = osuLastObj.TravelDistance / osuLastObj.TravelTime;
            }

            aimStrain += wiggleBonus * wiggle_multiplier;
            aimStrain += Math.Max(acuteAngleBonus * acute_angle_multiplier, wideAngleBonus * wide_angle_multiplier + velChangeBonus * velocity_change_multiplier);

            if (withSliderTravelDist)
            {
                aimStrain += sliderBonus * slider_multiplier;
            }

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
