using osu.Framework.Graphics;
using osuTK;

namespace osu.Game.Rulesets.Space.Objects.Drawables
{
    public partial class DrawableEditorSpaceHitObject : DrawableSpaceHitObject
    {
        public DrawableEditorSpaceHitObject(SpaceHitObject hitObject) : base(hitObject)
        {
        }

        protected override void Update()
        {
            base.Update();

            Vector2 targetRelative = new Vector2((HitObject.oX + 0.5f) / 3f, (HitObject.oY + 0.5f) / 3f);
            Vector2 center = new Vector2(0.5f, 0.5f);
            Vector2 offset = targetRelative - center;

            Position = center + offset;
            RelativePositionAxes = Axes.Both;
        }
    }
}
