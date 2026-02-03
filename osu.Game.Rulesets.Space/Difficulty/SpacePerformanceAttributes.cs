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

        [JsonProperty("stamina")]
        public double Stamina { get; set; }

        [JsonProperty("control")]
        public double Control { get; set; }

        [JsonProperty("flow")]
        public double Flow { get; set; }

        [JsonProperty("consistency")]
        public double Consistency { get; set; }

        public override IEnumerable<PerformanceDisplayAttribute> GetAttributesForDisplay()
        {
            foreach (var attribute in base.GetAttributesForDisplay())
                yield return attribute;

            yield return new PerformanceDisplayAttribute(nameof(Aim), "Aim", Aim);
            yield return new PerformanceDisplayAttribute(nameof(Reading), "Reading", Reading);
            yield return new PerformanceDisplayAttribute(nameof(Stamina), "Stamina", Stamina);
            yield return new PerformanceDisplayAttribute(nameof(Control), "Control", Control);
            yield return new PerformanceDisplayAttribute(nameof(Flow), "Flow", Flow);
            yield return new PerformanceDisplayAttribute(nameof(Consistency), "Consistency", Consistency);
        }
    }
}
