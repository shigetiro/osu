using System.Collections.Generic;
using Newtonsoft.Json;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Space.Difficulty
{
    public class SpaceDifficultyAttributes : DifficultyAttributes
    {
        [JsonProperty("aim_difficulty")]
        public double AimDifficulty { get; set; }

        [JsonProperty("reading_difficulty")]
        public double ReadingDifficulty { get; set; }

        public SpaceDifficultyAttributes()
        {
        }

        public SpaceDifficultyAttributes(Mod[] mods, double starRating)
            : base(mods, starRating)
        {
        }

        public override IEnumerable<(int attributeId, object value)> ToDatabaseAttributes()
        {
            foreach (var v in base.ToDatabaseAttributes())
                yield return v;

            yield return (11, AimDifficulty); // IDs should be unique per ruleset? Osu uses 1, 3 etc. I'll use arbitary for now or check if there's a convention.
            yield return (13, ReadingDifficulty);
        }

        public override void FromDatabaseAttributes(IReadOnlyDictionary<int, double> values, osu.Game.Beatmaps.IBeatmapOnlineInfo onlineInfo)
        {
            base.FromDatabaseAttributes(values, onlineInfo);

            AimDifficulty = values.GetValueOrDefault(11);
            ReadingDifficulty = values.GetValueOrDefault(13);
        }
    }
}
