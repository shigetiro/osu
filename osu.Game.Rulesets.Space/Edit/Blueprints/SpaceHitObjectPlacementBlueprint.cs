#nullable enable
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Space.Objects;
using osu.Game.Rulesets.Space.UI;
using osu.Game.Rulesets.UI;
using osuTK;
using osuTK.Input;
using osu.Framework.Allocation;

namespace osu.Game.Rulesets.Space.Edit.Blueprints
{
    public partial class SpaceHitObjectPlacementBlueprint : HitObjectPlacementBlueprint
    {
        private SpacePlayfield playfield = null!;

        public new SpaceHitObject HitObject => (SpaceHitObject)base.HitObject;

        public SpaceHitObjectPlacementBlueprint()
            : base(new SpaceHitObject())
        {
            RelativeSizeAxes = Axes.Both;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            BeginPlacement();
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (e.Button == MouseButton.Left)
            {
                EndPlacement(true);
                return true;
            }

            return base.OnMouseDown(e);
        }

        protected override bool OnKeyDown(osu.Framework.Input.Events.KeyDownEvent e)
        {
            float cell = SpacePlayfield.BASE_SIZE / 3f;

            bool handled = true;
            int? ix = null;
            int? iy = null;

            switch (e.Key)
            {
                case Key.Q: ix = 0; iy = 0; break;
                case Key.W: ix = 1; iy = 0; break;
                case Key.E: ix = 2; iy = 0; break;
                case Key.A: ix = 0; iy = 1; break;
                case Key.S: ix = 1; iy = 1; break;
                case Key.D: ix = 2; iy = 1; break;
                case Key.Z: ix = 0; iy = 2; break;
                case Key.X: ix = 1; iy = 2; break;
                case Key.C: ix = 2; iy = 2; break;
                default:
                    handled = false;
                    break;
            }

            if (ix.HasValue && iy.HasValue)
            {
                HitObject.oX = ix.Value;
                HitObject.oY = iy.Value;
                HitObject.X = ix.Value * cell + cell / 2f;
                HitObject.Y = iy.Value * cell + cell / 2f;
            }

            return handled || base.OnKeyDown(e);
        }

        [BackgroundDependencyLoader]
        private void load(Playfield pf)
        {
            playfield = (SpacePlayfield)pf;
        }

        public override SnapResult UpdateTimeAndPosition(Vector2 screenSpacePosition, double fallbackTime)
        {
            var result = base.UpdateTimeAndPosition(screenSpacePosition, fallbackTime);

            var gamePos = playfield.ScreenSpaceToGamefield(result.ScreenSpacePosition);
            float gx = gamePos.X / SpacePlayfield.BASE_SIZE;
            float gy = gamePos.Y / SpacePlayfield.BASE_SIZE;

            float cell = SpacePlayfield.BASE_SIZE / 3f;

            if (playfield.EnableQuantum.Value)
            {
                HitObject.X = gamePos.X;
                HitObject.Y = gamePos.Y;
                HitObject.oX = gamePos.X / cell - 0.5f;
                HitObject.oY = gamePos.Y / cell - 0.5f;
            }
            else
            {
                int ix = (int)System.Math.Clamp(System.Math.Round(gx * 3 - 0.5), 0, 2);
                int iy = (int)System.Math.Clamp(System.Math.Round(gy * 3 - 0.5), 0, 2);

                float cx = ix * cell + cell / 2f;
                float cy = iy * cell + cell / 2f;

                HitObject.X = cx;
                HitObject.Y = cy;
                HitObject.oX = ix;
                HitObject.oY = iy;
            }

            return result;
        }
    }
}
