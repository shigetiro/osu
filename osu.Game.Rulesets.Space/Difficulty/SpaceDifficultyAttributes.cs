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

        [JsonProperty("stamina_difficulty")]
        public double StaminaDifficulty { get; set; }

        [JsonProperty("control_difficulty")]
        public double ControlDifficulty { get; set; }

        [JsonProperty("flow_difficulty")]
        public double FlowDifficulty { get; set; }

        [JsonProperty("consistency_difficulty")]
        public double ConsistencyDifficulty { get; set; }

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

            yield return (11, AimDifficulty);
            yield return (13, ReadingDifficulty);
            yield return (15, StaminaDifficulty);
            yield return (17, ControlDifficulty);
            yield return (19, FlowDifficulty);
            yield return (21, ConsistencyDifficulty);
        }

        public override void FromDatabaseAttributes(IReadOnlyDictionary<int, double> values, osu.Game.Beatmaps.IBeatmapOnlineInfo onlineInfo)
        {
            base.FromDatabaseAttributes(values, onlineInfo);

            AimDifficulty = values.GetValueOrDefault(11);
            ReadingDifficulty = values.GetValueOrDefault(13);
            StaminaDifficulty = values.GetValueOrDefault(15);
            ControlDifficulty = values.GetValueOrDefault(17);
            FlowDifficulty = values.GetValueOrDefault(19);
            ConsistencyDifficulty = values.GetValueOrDefault(21);
        }
    }
}
