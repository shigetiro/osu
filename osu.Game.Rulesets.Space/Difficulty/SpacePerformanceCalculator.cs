using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Beatmaps;

namespace osu.Game.Rulesets.Space.Difficulty
{
    public class SpacePerformanceCalculator : PerformanceCalculator
    {
        public const double PERFORMANCE_BASE_MULTIPLIER = 1.12;

        public SpacePerformanceCalculator(Ruleset ruleset)
            : base(ruleset)
        {
        }

        protected override PerformanceAttributes CreatePerformanceAttributes(ScoreInfo score, DifficultyAttributes attributes)
        {
            var spaceAttributes = (SpaceDifficultyAttributes)attributes;

            // Rhythia / Sound Space is heavily based on Aim/Grid control.
            // We use a formula similar to osu! Relax mode, which emphasizes Aim and reduces penalties for accuracy timing (though we still scale by acc).

            double multiplier = PERFORMANCE_BASE_MULTIPLIER;

            if (score.Mods.Any(m => m is ModNoFail))
                multiplier *= 0.9;
            if (score.Mods.Any(m => m is ModEasy))
                multiplier *= 0.5;
            if (score.Mods.Any(m => m is ModHidden))
                multiplier *= 1.06;

            // Get map stats
            double approachRate = score.BeatmapInfo.Difficulty.ApproachRate;
            double circleSize = score.BeatmapInfo.Difficulty.CircleSize;
            double overallDifficulty = score.BeatmapInfo.Difficulty.OverallDifficulty;

            // Apply mod adjustments to stats if needed (usually handled by DifficultyCalculator, but attributes might not reflect modded stats if not stored)
            // Ideally we should use the attributes for modded difficulty, but we don't have AR/CS in attributes.
            // For now, we assume BeatmapInfo has base stats.
            // NOTE: Ideally we should calculate modded AR/CS here if mods change it (HR/DT).
            // But since we want "Simple", we'll stick to base or basic adjustment.

            // Mod adjustments for AR/CS/OD (Simplified)
            foreach (var mod in score.Mods)
            {
                if (mod is IApplicableToDifficulty applicableToDifficulty)
                {
                    var adjustedDiff = new BeatmapDifficulty(score.BeatmapInfo.Difficulty);
                    applicableToDifficulty.ApplyToDifficulty(adjustedDiff);
                    approachRate = adjustedDiff.ApproachRate;
                    circleSize = adjustedDiff.CircleSize;
                    overallDifficulty = adjustedDiff.OverallDifficulty;
                }
            }

            double aimPP = computeComponentValue(spaceAttributes.AimDifficulty, score, approachRate, circleSize, overallDifficulty);
            double readingPP = computeComponentValue(spaceAttributes.ReadingDifficulty, score, approachRate, circleSize, overallDifficulty);
            double staminaPP = computeComponentValue(spaceAttributes.StaminaDifficulty, score, approachRate, circleSize, overallDifficulty);
            double controlPP = computeComponentValue(spaceAttributes.ControlDifficulty, score, approachRate, circleSize, overallDifficulty);
            double flowPP = computeComponentValue(spaceAttributes.FlowDifficulty, score, approachRate, circleSize, overallDifficulty);
            double consistencyPP = computeComponentValue(spaceAttributes.ConsistencyDifficulty, score, approachRate, circleSize, overallDifficulty);

            double difficultySum = spaceAttributes.AimDifficulty + spaceAttributes.ReadingDifficulty + spaceAttributes.StaminaDifficulty + spaceAttributes.ControlDifficulty + spaceAttributes.FlowDifficulty + spaceAttributes.ConsistencyDifficulty;
            if (difficultySum > 0)
            {
                double aimShare = spaceAttributes.AimDifficulty / difficultySum;
                double speedEmphasis = 1.0 + 0.2 * aimShare;
                aimPP *= speedEmphasis;
            }

            double totalPP = Math.Pow(
                Math.Pow(aimPP, 1.1) +
                Math.Pow(readingPP, 1.1) +
                Math.Pow(staminaPP, 1.1) +
                Math.Pow(controlPP, 1.1) +
                Math.Pow(flowPP, 1.1) +
                Math.Pow(consistencyPP, 1.1),
                1.0 / 1.1
            ) * multiplier;

            return new SpacePerformanceAttributes
            {
                Aim = aimPP,
                Reading = readingPP,
                Stamina = staminaPP,
                Control = controlPP,
                Flow = flowPP,
                Consistency = consistencyPP,
                Total = totalPP
            };
        }

        private double computeComponentValue(double difficulty, ScoreInfo score, double ar, double cs, double od)
        {
            if (difficulty <= 0) return 0;

            // Standard osu! Difficulty to Performance formula
            double value = Math.Pow(5.0 * Math.Max(1.0, difficulty / 0.0675) - 4.0, 3.0) / 100000.0;

            int totalHits = score.Statistics.Values.Sum();
            double scaledHits = Math.Min(1.0, totalHits / 1800.0);
            double lengthBonus = 0.7 + 0.6 * scaledHits;
            if (totalHits > 1800)
                lengthBonus += 0.2 * Math.Log10(totalHits / 1800.0 + 1.0);
            value *= lengthBonus;

            // Relax-style Miss Penalty
            int missCount = score.Statistics.GetValueOrDefault(HitResult.Miss);
            if (missCount > 0)
            {
                // Simple exponential penalty as we don't have strain counts
                value *= Math.Pow(0.96, missCount);
            }

            // AR/CS Bonus (Relax style)
            // Precision buff for small circles (High CS)
            if (cs > 5.58)
            {
                value *= Math.Pow(Math.Pow(cs - 5.46, 1.8) + 1.0, 0.03);
            }

            // High AR bonus
            if (ar > 10.8)
            {
                value *= 1.0 + (ar - 10.8);
                value *= 1.0 + Math.Clamp(cs - 6.0, 0.0, 0.2);
            }

            // Accuracy Scaling
            // Even in Relax/Rhythia, accuracy matters.
            // osu! Relax logic: aimValue *= 0.98 + Math.Pow(Math.Max(0.0, overallDifficulty), 2) / 2500.0;
            // But we also need to account for the player's actual accuracy.

            // In osu!, Aim value is NOT multiplied by Accuracy directly in the main formula,
            // but it IS multiplied by an accuracy factor in computeAimValue:
            // "aimValue *= 0.98 + Math.Pow(Math.Max(0.0, overallDifficulty), 2) / 2500.0;" -> This is OD bonus, not player accuracy?
            // Wait, looking at OsuPerformanceCalculator again:
            // "aimValue *= 0.5 + accuracy / 2.0;" (Flashlight)
            // For Aim, it doesn't seem to multiply by score.Accuracy directly in the snippet I saw?
            // Ah, I missed it?
            // Actually, in standard osu!, Aim is NOT penalized by accuracy (only misses).
            // Speed IS penalized by accuracy.

            // However, Rhythia points usually scale with accuracy: RP = Diff * (Acc/100)^x.
            // So we SHOULD multiply by accuracy.

            value *= Math.Pow(score.Accuracy, 4); // Power of 4 is a strong accuracy punishment (common in some private servers / systems)

            // OD Bonus
            value *= 0.98 + Math.Pow(Math.Max(0.0, od), 2) / 2500.0;

            return value;
        }
    }
}
