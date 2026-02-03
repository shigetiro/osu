// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Game.Beatmaps;
using osu.Game.Extensions;
using osu.Game.Online.API;
using osu.Game.Online.Rooms;
using osu.Game.Online.Solo;
using osu.Game.Scoring;
using osu.Game.Screens.Select.Leaderboards;

namespace osu.Game.Screens.Play
{
    public partial class SoloPlayer : SubmittingPlayer
    {
        [Cached(typeof(IGameplayLeaderboardProvider))]
        private readonly SoloGameplayLeaderboardProvider leaderboardProvider = new SoloGameplayLeaderboardProvider();

        public SoloPlayer([CanBeNull] PlayerConfiguration configuration = null)
            : base(configuration)
        {
            Configuration.ShowLeaderboard = true;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            AddInternal(leaderboardProvider);
        }

        protected override APIRequest<APIScoreToken> CreateTokenRequest()
        {
            int beatmapId = Beatmap.Value.BeatmapInfo.OnlineID;
            int rulesetId = Ruleset.Value.OnlineID;

            // Check for Rhythia/SSPM beatmap
            if (Beatmap.Value.BeatmapInfo.Metadata?.Tags?.Contains("sspm", StringComparison.OrdinalIgnoreCase) == true)
            {
                var match = Regex.Match(Beatmap.Value.BeatmapInfo.Metadata.Tags ?? "", @"sspm\s+(\d+)", RegexOptions.IgnoreCase);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int sspmId))
                {
                    return new CreateRhythiaScoreTokenRequest(sspmId);
                }
            }

            if (beatmapId <= 0)
                return null;

            if (Beatmap.Value.BeatmapInfo.Status == BeatmapOnlineStatus.LocallyModified)
                return null;

            if (!Ruleset.Value.IsLegacyRuleset())
                return null;

            return new CreateSoloScoreRequest(Beatmap.Value.BeatmapInfo, rulesetId, Game.VersionHash);
        }

        protected override bool ShouldExitOnTokenRetrievalFailure(Exception exception) => false;

        protected override APIRequest<MultiplayerScore> CreateSubmissionRequest(Score score, long token)
        {
            IBeatmapInfo beatmap = score.ScoreInfo.BeatmapInfo!;

            // Check for Rhythia/SSPM beatmap
            if (beatmap.Metadata?.Tags?.Contains("sspm", StringComparison.OrdinalIgnoreCase) == true)
            {
                var match = Regex.Match(beatmap.Metadata.Tags ?? "", @"sspm\s+(\d+)", RegexOptions.IgnoreCase);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int sspmId))
                {
                    return new SubmitRhythiaScoreRequest(score.ScoreInfo, token, sspmId);
                }
            }

            Debug.Assert(beatmap.OnlineID > 0);

            return new SubmitSoloScoreRequest(score.ScoreInfo, token, beatmap.OnlineID);
        }
    }
}
