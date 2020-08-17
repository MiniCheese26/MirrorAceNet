using Newtonsoft.Json;
#pragma warning disable 1591

namespace MirrorAceNet.ResponseJson
{
    public class MirrorAceResponseBase<T> where T : class
    {
        [JsonProperty("status")]
        public string? Status { get; set; }

        [JsonProperty("result")]
        public T? Result { get; set; }
    }
}