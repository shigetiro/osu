// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace osu.Game.Online.API.Requests.Responses
{
    public class RhythiaBeatmapSet
    {
        [JsonProperty("id")]
        public int OnlineID { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("artist")]
        public string Artist { get; set; }

        [JsonProperty("author")]
        public string Author { get; set; }

        [JsonProperty("bpm")]
        public float BPM { get; set; }

        [JsonProperty("preview")]
        public string Preview { get; set; }

        [JsonProperty("background")]
        public string Background { get; set; }

        [JsonProperty("storyboard")]
        public bool Storyboard { get; set; }

        [JsonProperty("tags")]
        public string Tags { get; set; }

        [JsonProperty("beatmaps")]
        public List<RhythiaBeatmap> Beatmaps { get; set; }
    }

    public class RhythiaBeatmap
    {
        [JsonProperty("id")]
        public int OnlineID { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("difficulty")]
        public float Difficulty { get; set; }

        [JsonProperty("cs")]
        public float CS { get; set; }

        [JsonProperty("ar")]
        public float AR { get; set; }

        [JsonProperty("od")]
        public float OD { get; set; }

        [JsonProperty("hp")]
        public float HP { get; set; }

        [JsonProperty("length")]
        public int Length { get; set; }
    }
}
