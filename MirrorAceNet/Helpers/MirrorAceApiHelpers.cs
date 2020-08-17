using System;
using Newtonsoft.Json.Linq;

namespace MirrorAceNet.Helpers
{
    internal static class MirrorAceApiHelpers
    {
        public static bool ResponseHasError(this JObject response) => response?["status"]?.Value<string>() != "success";

        public static void PrintResponseError(this JObject response) =>
            Console.Write(response["result"]?.Value<string>() ?? "");
    }
}