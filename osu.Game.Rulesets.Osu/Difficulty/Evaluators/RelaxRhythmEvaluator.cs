// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Utils;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Difficulty.Evaluators
{
    public static class RelaxRhythmEvaluator
    {
        private const int history_time_max = 5 * 1000; // 5 seconds
        private const int history_objects_max = 32;
        private const double rhythm_overall_multiplier = 0.95;
        private const double rhythm_ratio_multiplier = 12.0;

        public static double EvaluateDifficultyOf(OsuDifficultyHitObject current, OsuDifficultyHitObject[] diffObjects, double hitWindow)
        {
            if (current.BaseObject is Spinner)
                return 0.0;

            double rhythmComplexitySum = 0.0;

            double deltaDifferenceEps = hitWindow * 0.3;

            var island = new RhythmIsland(deltaDifferenceEps);
            var prevIsland = new RhythmIsland(deltaDifferenceEps);

            // we can't use dictionary here because we need to compare island with a tolerance
            // which is impossible to pass into the hash comparer
            var islandCounts = new List<IslandCount>();

            // store the ratio of the current start of an island to buff for tighter rhythms
            double startRatio = 0.0;

            bool firstDeltaSwitch = false;

            int historicalNoteCount = Math.Min(current.Index, history_objects_max);

            int rhythmStart = 0;

            while (rhythmStart < diffObjects.Length && current.Previous(rhythmStart) != null &&
                   rhythmStart + 2 < historicalNoteCount &&
                   current.StartTime - current.Previous(rhythmStart).StartTime < history_time_max)
            {
                rhythmStart++;
            }

            var prevObj = current.Previous(rhythmStart) as OsuDifficultyHitObject;
            var lastObj = current.Previous(rhythmStart + 1) as OsuDifficultyHitObject;

            if (prevObj != null && lastObj != null)
            {
                // we go from the furthest object back to the current one
                for (int i = rhythmStart; i >= 1; i--)
                {
                    var currObj = current.Previous(i - 1) as OsuDifficultyHitObject;
                    if (currObj == null)
                        break;

                    // scales note 0 to 1 from history to now
                    double timeDecay = (history_time_max - (current.StartTime - currObj.StartTime)) / history_time_max;
                    double noteDecay = (historicalNoteCount - i) / (double)historicalNoteCount;

                    // either we're limited by time or limited by object count.
                    double currHistoricalDecay = Math.Min(noteDecay, timeDecay);

                    double currDelta = currObj.StrainTime;
                    double prevDelta = prevObj.StrainTime;
                    double lastDelta = lastObj.StrainTime;

                    // calculate how much current delta difference deserves a rhythm bonus
                    // this function is meant to reduce rhythm bonus for deltas that are multiples of each other (i.e 100 and 200)
                    double deltaDifferenceRatio = Math.Min(prevDelta, currDelta) / Math.Max(prevDelta, currDelta);
                    double currRatio = 1.0 + rhythm_ratio_multiplier * Math.Pow(Math.Sin(Math.PI / deltaDifferenceRatio), 2);

                    // reduce ratio bonus if delta difference is too big
                    double fraction = Math.Max(prevDelta / currDelta, currDelta / prevDelta);
                    double fractionMultiplier = Math.Clamp(2.0 - fraction / 8.0, 0.0, 1.0);

                    double windowPenalty = Math.Min(Math.Max((Math.Abs(prevDelta - currDelta) - deltaDifferenceEps), 0.0) / deltaDifferenceEps, 1.0);

                    double effectiveRatio = windowPenalty * currRatio * fractionMultiplier;

                    if (firstDeltaSwitch)
                    {
                        // Keep in-sync with lazer
                        if (Math.Abs(prevDelta - currDelta) < deltaDifferenceEps)
                        {
                            // island is still progressing
                            island.AddDelta((int)currDelta);
                        }
                        else
                        {
                            // bpm change is into slider, this is easy acc window
                            if (currObj.BaseObject is Slider)
                                effectiveRatio *= 0.125;

                            // bpm change was from a slider, this is easier typically than circle -> circle
                            // unintentional side effect is that bursts with kicksliders at the ends might have lower difficulty than bursts without sliders
                            if (prevObj.BaseObject is Slider)
                                effectiveRatio *= 0.3;

                            // repeated island polarity (2 -> 4, 3 -> 5)
                            if (island.IsSimilarPolarity(prevIsland))
                                effectiveRatio *= 0.5;

                            // previous increase happened a note ago, 1/1->1/2-1/4, dont want to buff this.
                            if (lastDelta > prevDelta + deltaDifferenceEps && prevDelta > currDelta + deltaDifferenceEps)
                                effectiveRatio *= 0.125;

                            // repeated island size (ex: triplet -> triplet)
                            // TODO: remove this nerf since its staying here only for balancing purposes because of the flawed ratio calculation
                            if (prevIsland.DeltaCount == island.DeltaCount)
                                effectiveRatio *= 0.5;

                            var existingIslandCount = islandCounts.FirstOrDefault(entry => entry.Island.Equals(island));

                            if (existingIslandCount != null && !island.IsDefault())
                            {
                                // only add island to island counts if they're going one after another
                                if (prevIsland.Equals(island))
                                    existingIslandCount.Count++;

                                // repeated island (ex: triplet -> triplet)
                                double power = 1 / (1 + Math.Exp(-(island.Delta - 58.33) / 0.24)); // logistic function
                                effectiveRatio *= Math.Min(3.0 / existingIslandCount.Count, Math.Pow(1.0 / existingIslandCount.Count, power));
                            }
                            else if (!island.IsDefault())
                            {
                                islandCounts.Add(new IslandCount { Island = island, Count = 1 });
                            }

                            // scale down the difficulty if the object is doubletappable
                            double doubletapness = prevObj.GetDoubletapness(currObj);
                            effectiveRatio *= 1.0 - doubletapness * 0.75;

                            rhythmComplexitySum += Math.Sqrt(effectiveRatio * startRatio) * currHistoricalDecay;

                            startRatio = effectiveRatio;

                            prevIsland = island;

                            // we're slowing down, stop counting
                            if (prevDelta + deltaDifferenceEps < currDelta)
                            {
                                // if we're speeding up, this stays true and we keep counting island size.
                                firstDeltaSwitch = false;
                            }

                            island = new RhythmIsland((int)currDelta, deltaDifferenceEps);
                        }
                    }
                    else if (prevDelta > currDelta + deltaDifferenceEps)
                    {
                        // we're speeding up.
                        // Begin counting island until we change speed again.
                        firstDeltaSwitch = true;

                        // bpm change is into slider, this is easy acc window
                        if (currObj.BaseObject is Slider)
                            effectiveRatio *= 0.6;

                        // bpm change was from a slider, this is easier typically than circle -> circle
                        // unintentional side effect is that bursts with kicksliders at the ends might have lower difficulty than bursts without sliders
                        if (prevObj.BaseObject is Slider)
                            effectiveRatio *= 0.6;

                        startRatio = effectiveRatio;

                        island = new RhythmIsland((int)currDelta, deltaDifferenceEps);
                    }

                    lastObj = prevObj;
                    prevObj = currObj;
                }
            }

            // produces multiplier that can be applied to strain. range [1, infinity) (not really though)
            return Math.Sqrt(4.0 + rhythmComplexitySum * rhythm_overall_multiplier) / 2.0;
        }

        private class RhythmIsland
        {
            private readonly double deltaDifferenceEps;
            public int Delta { get; private set; }
            public int DeltaCount { get; private set; }

            public RhythmIsland(double deltaDifferenceEps)
            {
                this.deltaDifferenceEps = deltaDifferenceEps;
                Delta = int.MaxValue;
                DeltaCount = 0;
            }

            public RhythmIsland(int delta, double deltaDifferenceEps)
            {
                this.deltaDifferenceEps = deltaDifferenceEps;
                Delta = Math.Max(OsuDifficultyHitObject.MIN_DELTA_TIME, delta);
                DeltaCount = 1;
            }

            public void AddDelta(int delta)
            {
                // 只在 delta 未初始化时赋值
                if (Delta == int.MaxValue)
                    Delta = Math.Max(OsuDifficultyHitObject.MIN_DELTA_TIME, delta);
                DeltaCount++;
            }

            public bool IsSimilarPolarity(RhythmIsland other)
            {
                // 只比较奇偶性
                return DeltaCount % 2 == other.DeltaCount % 2;
            }

            public bool IsDefault()
            {
                return Precision.AlmostEquals(deltaDifferenceEps, 0.0) && Delta == int.MaxValue && DeltaCount == 0;
            }

            public bool Equals(RhythmIsland other)
            {
                if (other == null)
                    return false;
                return Math.Abs(Delta - other.Delta) < deltaDifferenceEps && DeltaCount == other.DeltaCount;
            }
        }

        private class IslandCount
        {
            public RhythmIsland Island;
            public int Count;
        }
    }
}
