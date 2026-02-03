#nullable enable
using System;
using osu.Framework.Allocation;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Space.Objects;
using osu.Game.Rulesets.Space.UI;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Edit.Compose.Components;
using osuTK;

namespace osu.Game.Rulesets.Space.Edit
{
    public partial class SpaceSelectionHandler : EditorSelectionHandler
    {
        [Resolved]
        private Playfield playfield { get; set; } = null!;

        public override bool HandleMovement(MoveSelectionEvent<HitObject> moveEvent)
        {
            var spacePlayfield = (SpacePlayfield)playfield;
            var blueprint = moveEvent.Blueprint;

            Vector2 original = spacePlayfield.ScreenSpaceToGamefield(blueprint.ScreenSpaceSelectionPoint);
            Vector2 target = spacePlayfield.ScreenSpaceToGamefield(blueprint.ScreenSpaceSelectionPoint + moveEvent.ScreenSpaceDelta);
            Vector2 delta = target - original;

            if (delta == Vector2.Zero)
                return true;

            EditorBeatmap.PerformOnSelection(h =>
            {
                if (h is SpaceHitObject s)
                {
                    float cell = SpacePlayfield.BASE_SIZE / 3f;
                    if (spacePlayfield.EnableQuantum.Value)
                    {
                        s.X += delta.X;
                        s.Y += delta.Y;
                        s.oX = s.X / cell - 0.5f;
                        s.oY = s.Y / cell - 0.5f;
                    }
                    else
                    {
                        float newX = s.X + delta.X;
                        float newY = s.Y + delta.Y;
                        float nx = newX / (SpacePlayfield.BASE_SIZE);
                        float ny = newY / (SpacePlayfield.BASE_SIZE);
                        int ix = Math.Clamp((int)Math.Round(nx * 3 - 0.5f), 0, 2);
                        int iy = Math.Clamp((int)Math.Round(ny * 3 - 0.5f), 0, 2);
                        s.oX = ix;
                        s.oY = iy;
                        s.X = ix * cell + cell / 2f;
                        s.Y = iy * cell + cell / 2f;
                    }
                }
            });

            return true;
        }
    }
}
