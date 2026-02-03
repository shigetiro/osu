#nullable enable
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Space.Objects;

namespace osu.Game.Rulesets.Space.Edit.Blueprints
{
    public partial class SpaceSelectionBlueprint : HitObjectSelectionBlueprint<SpaceHitObject>
    {
        public SpaceSelectionBlueprint(SpaceHitObject item)
            : base(item)
        {
        }
    }
}

