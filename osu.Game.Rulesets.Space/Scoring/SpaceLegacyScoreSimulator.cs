using System.Collections.Generic;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring.Legacy;

namespace osu.Game.Rulesets.Space.Scoring
{
    public class SpaceLegacyScoreSimulator : ILegacyScoreSimulator
    {
        public LegacyScoreAttributes Simulate(IWorkingBeatmap workingBeatmap, IBeatmap playableBeatmap)
        {
            return new LegacyScoreAttributes
            {
                ComboScore = 1000000,
                MaxCombo = playableBeatmap.HitObjects.Count
            };
        }

        public double GetLegacyScoreMultiplier(IReadOnlyList<Mod> mods, LegacyBeatmapConversionDifficultyInfo difficulty) => 1.0;
    }
}
