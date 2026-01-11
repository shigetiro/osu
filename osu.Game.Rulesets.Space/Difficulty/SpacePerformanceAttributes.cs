using System.Collections.Generic;
using Newtonsoft.Json;
using osu.Game.Rulesets.Difficulty;

namespace osu.Game.Rulesets.Space.Difficulty
{
    public class SpacePerformanceAttributes : PerformanceAttributes
    {
        [JsonProperty("aim")]
        public double Aim { get; set; }

        [JsonProperty("reading")]
        public double Reading { get; set; }

        public override IEnumerable<PerformanceDisplayAttribute> GetAttributesForDisplay()
        {
            foreach (var attribute in base.GetAttributesForDisplay())
                yield return attribute;

            yield return new PerformanceDisplayAttribute(nameof(Aim), "Aim", Aim);
            yield return new PerformanceDisplayAttribute(nameof(Reading), "Reading", Reading);
        }
    }
}
