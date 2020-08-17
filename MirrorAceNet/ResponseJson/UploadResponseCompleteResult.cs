using System;
using Newtonsoft.Json;
#pragma warning disable 1591

namespace MirrorAceNet.ResponseJson
{
    public class UploadResponseCompleteResult
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }

        [JsonProperty("slug")]
        public string? Slug { get; set; }

        [JsonProperty("url")]
        public Uri? Url { get; set; }

        [JsonProperty("info")]
        public string? Info { get; set; }
    }
}