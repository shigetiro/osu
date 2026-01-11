
// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Online.API.Requests
{
    public class GetChangelogBuildRequest : APIRequest<APIChangelogBuild>
    {
        private readonly string stream;
        private readonly string version;

        public GetChangelogBuildRequest(string stream, string version)
        {
            this.stream = stream;
            this.version = version;
        }

        protected override string Target => $"changelog/{stream}/{version}";
    }
}
