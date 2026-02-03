// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Space.Mods
{
    public class SpaceModDoubleTime : ModDoubleTime, IApplicableToDifficulty
    {
        public void ApplyToDifficulty(BeatmapDifficulty difficulty)
        {
            // Calculate current Preempt
            double preempt = difficulty.ApproachRate < 5
                ? 1200 + 120 * (5 - difficulty.ApproachRate)
                : 1200 - 150 * (difficulty.ApproachRate - 5);

            // Adjust Preempt to compensate for speed increase (so it stays the same in real-time)
            preempt *= SpeedChange.Value;

            // Calculate new AR from new Preempt
            if (preempt > 1200)
            {
                difficulty.ApproachRate = (float)((1800 - preempt) / 120.0);
            }
            else
            {
                difficulty.ApproachRate = (float)(5 + (1200 - preempt) / 150.0);
            }
        }
    }
}
