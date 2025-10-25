using System.Collections.Generic;
using System.IO;
using osu.Framework.Extensions;

namespace osu.Game.Rulesets
{
    public class RulesetHashCache
    {
        private readonly Dictionary<string, string> cache = new Dictionary<string, string>();

        public RulesetHashCache(RulesetStore store)
        {
            foreach (var rulesetInfo in store.AvailableRulesets)
            {
                Ruleset instance = rulesetInfo.CreateInstance();
                using var str = File.OpenRead(instance.GetType().Assembly.Location);
                cache[instance.ShortName] = str.ComputeMD5Hash();
            }
        }

        public string? GetHash(string shortName)
        {
            cache.TryGetValue(shortName, out string? hash);
            return hash;
        }

        public string? GetHash(RulesetInfo rulesetInfo)
        {
            return GetHash(rulesetInfo.ShortName);
        }

        public string? GetHash(Ruleset ruleset)
        {
            return GetHash(ruleset.ShortName);
        }
    }
}
