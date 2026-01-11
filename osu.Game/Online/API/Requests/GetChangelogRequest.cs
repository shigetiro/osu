// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Online.API.Requests
{
    public class GetChangelogRequest : APIRequest<APIChangelogIndex>
    {
        private readonly string? stream;
        private readonly string? from;
        private readonly string? to;

        public GetChangelogRequest(string? stream = null, string? from = null, string? to = null)
        {
            this.stream = stream;
            this.from = from;
            this.to = to;
        }

        protected override string Target
        {
            get
            {
                string url = "changelog";

                var queryParams = new List<string>();

                if (!string.IsNullOrEmpty(stream))
                    queryParams.Add($"stream={stream}");

                if (!string.IsNullOrEmpty(from))
                    queryParams.Add($"from={from}");

                if (!string.IsNullOrEmpty(to))
                    queryParams.Add($"to={to}");

                if (queryParams.Count > 0)
                    url += "?" + string.Join("&", queryParams);

                return url;
            }
        }
    }
}
