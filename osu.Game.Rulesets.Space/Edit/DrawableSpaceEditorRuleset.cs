using System.Collections.Generic;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Space.Objects;
using osu.Game.Rulesets.Space.Objects.Drawables;
using osu.Game.Rulesets.Space.UI;

namespace osu.Game.Rulesets.Space.Edit
{
    public partial class DrawableSpaceEditorRuleset : DrawableSpaceRuleset
    {
        public DrawableSpaceEditorRuleset(SpaceRuleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod> mods)
            : base(ruleset, beatmap, mods)
        {
        }

        public override DrawableHitObject<SpaceHitObject> CreateDrawableRepresentation(SpaceHitObject h) => new DrawableEditorSpaceHitObject(h);
    }
}
