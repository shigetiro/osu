#nullable enable
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Tools;
using osu.Game.Rulesets.Space.Edit.Blueprints;

namespace osu.Game.Rulesets.Space.Edit.Tools
{
    public class SpaceNoteCompositionTool : CompositionTool
    {
        public SpaceNoteCompositionTool()
            : base("Note")
        {
        }

        public override PlacementBlueprint? CreatePlacementBlueprint() => new SpaceHitObjectPlacementBlueprint();
    }
}

