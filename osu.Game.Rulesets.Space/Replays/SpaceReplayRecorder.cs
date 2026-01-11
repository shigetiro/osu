using System.Collections.Generic;
using osu.Game.Rulesets.Replays;
using osu.Game.Rulesets.Space.Replays;
using osu.Game.Rulesets.UI;
using osu.Game.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Space.Replays
{
    public class SpaceReplayRecorder : ReplayRecorder<SpaceAction>
    {
        public SpaceReplayRecorder(Score score, SpaceRuleset ruleset)
            : base(score)
        {
        }

        protected override ReplayFrame HandleFrame(Vector2 mousePosition, List<SpaceAction> actions, ReplayFrame previousFrame)
        {
            return new SpaceReplayFrame
            {
                Time = Time.Current,
                Position = mousePosition,
            };
        }
    }
}
