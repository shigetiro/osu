// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Online.API;
using osu.Game.Online.Rooms;

namespace osu.Game.Online.Solo
{
    public class CreateRhythiaScoreTokenRequest : APIRequest<APIScoreToken>
    {
        private readonly int mapId;

        public CreateRhythiaScoreTokenRequest(int mapId)
        {
            this.mapId = mapId;
        }

        protected override string Target => $@"rhythia/token";
    }
}
