#nullable enable
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Tools;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Space.Objects;
using osu.Game.Rulesets.Space.UI;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Edit.Components.TernaryButtons;
using osu.Game.Screens.Edit.Compose.Components;

namespace osu.Game.Rulesets.Space.Edit
{
    [Cached]
    public partial class SpaceHitObjectComposer : HitObjectComposer<SpaceHitObject>
    {
        private readonly Bindable<TernaryState> quantumToggle = new Bindable<TernaryState> { Description = "Quantum" };

        public SpaceHitObjectComposer(SpaceRuleset ruleset)
            : base(ruleset)
        {
        }

        protected override DrawableRuleset<SpaceHitObject> CreateDrawableRuleset(Ruleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod> mods)
            => new DrawableSpaceEditorRuleset((SpaceRuleset)ruleset, beatmap, mods);

        protected override ComposeBlueprintContainer CreateBlueprintContainer()
            => new SpaceBlueprintContainer(this);

        protected override IReadOnlyList<CompositionTool> CompositionTools => new CompositionTool[]
        {
            new Tools.SpaceNoteCompositionTool()
        };

        protected override IEnumerable<Drawable> CreateTernaryButtons()
        {
            foreach (var d in base.CreateTernaryButtons())
                yield return d;

            yield return new DrawableTernaryButton
            {
                Current = quantumToggle,
                Description = "Quantum",
                CreateIcon = () => new OsuSpriteText { Text = "Q", Font = OsuFont.Default.With(weight: FontWeight.Bold, size: 18), Anchor = Anchor.Centre, Origin = Anchor.Centre }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            if (EditorBeatmap.ControlPointInfo.TimingPointAt(EditorClock.CurrentTime) == TimingControlPoint.DEFAULT)
                EditorBeatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });

            quantumToggle.BindValueChanged(v =>
            {
                var pf = (SpacePlayfield)Playfield;
                pf.EnableQuantum.Value = v.NewValue == TernaryState.True;
            }, true);
        }
    }
}
