
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Replays.Types;
using osu.Game.Replays.Legacy;
using osu.Game.Rulesets.Replays;
using osuTK;

namespace osu.Game.Rulesets.Space.Replays
{
    public class SpaceReplayFrame : ReplayFrame, IConvertibleReplayFrame
    {
        public Vector2 Position;

        public override bool IsEquivalentTo(ReplayFrame other)
            => other is SpaceReplayFrame spaceFrame && Time == spaceFrame.Time && Position == spaceFrame.Position;

        public void FromLegacy(LegacyReplayFrame currentFrame, IBeatmap beatmap, ReplayFrame? lastFrame = null)
        {
            Position = currentFrame.Position;
            // System.Diagnostics.Debug.WriteLine($"SpaceReplayFrame FromLegacy: Time={Time}, Pos={Position}, LegacyPos={currentFrame.Position}");
        }

        public LegacyReplayFrame ToLegacy(IBeatmap beatmap)
        {
            return new LegacyReplayFrame(Time, Position.X, Position.Y, ReplayButtonState.None);
        }
    }
}

