using Newtonsoft.Json;
#pragma warning disable 1591

namespace MirrorAceNet.ResponseJson
{
    public class UploadResponseChunkResult
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }

        [JsonProperty("info")]
        public string? Info { get; set; }
    }
}