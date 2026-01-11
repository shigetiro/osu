// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Game.Users
{
    public class Medal
    {
        public string Name { get; set; }
        public string InternalName { get; set; }
        /// <summary>
        /// Local resource path for the medal texture. Falls back to URL if local resource is not found.
        /// TextureStore automatically prepends "Textures/" when looking for resources.
        /// </summary>
        public string ImageUrl => $@"Medals/{InternalName}@2x.png";
        /// <summary>
        /// Fallback URL for medal texture if local resource is not available.
        /// </summary>
        public string ImageUrlFallback => $@"Textures/Medals/{InternalName}@2x.png";
        public string Description { get; set; }
    }
}
