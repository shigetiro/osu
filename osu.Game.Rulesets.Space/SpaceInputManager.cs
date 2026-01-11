
using System.ComponentModel;
using osu.Framework.Input.Bindings;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.Space
{
    public partial class SpaceInputManager : RulesetInputManager<SpaceAction>
    {
        public SpaceInputManager(RulesetInfo ruleset)
            : base(ruleset, 0, SimultaneousBindingMode.Unique)
        {
        }
    }

    public enum SpaceAction
    {
    }
}
