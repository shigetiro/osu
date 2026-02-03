// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.IO.Network;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using osu.Game.Scoring;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Rooms;

namespace osu.Game.Online.Solo
{
    public class SubmitRhythiaScoreRequest : APIRequest<MultiplayerScore>
    {
        private readonly int beatmapId;
        private readonly SoloScoreInfo score;
        private readonly long scoreId;

        public SubmitRhythiaScoreRequest(ScoreInfo scoreInfo, long scoreId, int beatmapId)
        {
            this.beatmapId = beatmapId;
            this.score = SoloScoreInfo.ForSubmission(scoreInfo);
            this.scoreId = scoreId;
        }

        protected override string Target => $@"rhythia/maps/{beatmapId}/scores";

        protected override WebRequest CreateWebRequest()
        {
            var req = base.CreateWebRequest();
            req.Method = HttpMethod.Post;
            req.ContentType = "application/json";

            var submission = new RhythiaSubmissionDTO
            {
                UserId = score.UserID,
                Score = score.TotalScore,
                MaxCombo = score.MaxCombo,
                Accuracy = score.Accuracy,
                Rank = score.Rank.ToString(),
                Mode = "osuspaceruleset", // Always use osuspaceruleset for Rhythia/SSPM maps
                ModeInt = 727, // Always use 727 for Rhythia/SSPM maps
                Mods = score.Mods.Select(m => m.Acronym).ToList(),
                Statistics = score.Statistics.ToDictionary(k => k.Key.ToString(), v => v.Value),
                Pp = score.PP ?? 0 // Ensure PP is never null for submission
            };

            req.AddRaw(JsonConvert.SerializeObject(submission));

            return req;
        }

        private class RhythiaSubmissionDTO
        {
            [JsonProperty("user_id")]
            public int UserId { get; set; }

            [JsonProperty("score")]
            public long Score { get; set; }

            [JsonProperty("max_combo")]
            public int MaxCombo { get; set; }

            [JsonProperty("accuracy")]
            public double Accuracy { get; set; }

            [JsonProperty("rank")]
            public string Rank { get; set; }

            [JsonProperty("mode")]
            public string Mode { get; set; }

            [JsonProperty("mode_int")]
            public int ModeInt { get; set; }

            [JsonProperty("mods")]
            public System.Collections.Generic.List<string> Mods { get; set; }

            [JsonProperty("statistics")]
            public System.Collections.Generic.Dictionary<string, int> Statistics { get; set; }

            [JsonProperty("pp")]
            public double? Pp { get; set; }
        }
    }
}
