using System;
using Newtonsoft.Json;
#pragma warning disable 1591

namespace MirrorAceNet.ResponseJson
{
    public class MirrorAceFileInfo
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("slug")]
        public string? Slug { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }

        [JsonProperty("url")]
        public Uri? Url { get; set; }

        [JsonProperty("status")]
        public string? Status { get; set; }
    }
}