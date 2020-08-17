using System;
using System.Collections.Generic;
using Newtonsoft.Json;
#pragma warning disable 1591

namespace MirrorAceNet.ResponseJson
{
    public class UploadVariablesResult
    {
        [JsonProperty("server")]
        public Uri? Server { get; set; }

        [JsonProperty("server_file")]
        public Uri? ServerFile { get; set; }

        [JsonProperty("server_remote")]
        public Uri? ServerRemote { get; set; }

        [JsonProperty("cTracker")]
        public string? CTracker { get; set; }

        [JsonProperty("mirrors")]
        public Dictionary<string, bool>? Mirrors { get; set; }

        [JsonProperty("default_mirrors")]
        public List<string>? DefaultMirrors { get; set; }

        [JsonProperty("max_chunk_size")]
        public long MaxChunkSize { get; set; }

        [JsonProperty("max_file_size")]
        public long MaxFileSize { get; set; }

        [JsonProperty("max_mirrors")]
        public long MaxMirrors { get; set; }

        [JsonProperty("upload_key")]
        public string? UploadKey { get; set; }

        [JsonProperty("upload_key_expiry")]
        public DateTimeOffset UploadKeyExpiry { get; set; }
    }
}