#nullable enable
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Input.Events;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Space.Objects;
using osu.Game.Screens.Edit.Compose.Components;
using osuTK;

namespace osu.Game.Rulesets.Space.Edit
{
    public partial class SpaceBlueprintContainer : ComposeBlueprintContainer
    {
        public new SpaceHitObjectComposer Composer => (SpaceHitObjectComposer)base.Composer;

        public SpaceBlueprintContainer(SpaceHitObjectComposer composer)
            : base(composer)
        {
        }

        protected override SelectionHandler<HitObject> CreateSelectionHandler() => new SpaceSelectionHandler();

        public override HitObjectSelectionBlueprint? CreateHitObjectBlueprintFor(osu.Game.Rulesets.Objects.HitObject hitObject)
        {
            if (hitObject is SpaceHitObject s)
                return new Blueprints.SpaceSelectionBlueprint(s);

            return base.CreateHitObjectBlueprintFor(hitObject);
        }

        protected override bool TryMoveBlueprints(DragEvent e, IList<(SelectionBlueprint<HitObject> blueprint, Vector2[] originalSnapPositions)> blueprints)
        {
            Vector2 distanceTravelled = e.ScreenSpaceMousePosition - e.ScreenSpaceMouseDownPosition;

            var reference = blueprints.First().blueprint;
            Vector2 target = blueprints.First().originalSnapPositions.First() + distanceTravelled;
            Vector2 delta = target - reference.ScreenSpaceSelectionPoint;

            return SelectionHandler.HandleMovement(new MoveSelectionEvent<HitObject>(reference, delta));
        }
    }
}
