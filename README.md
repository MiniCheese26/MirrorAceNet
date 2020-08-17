# MirrorAceNet

### A simple library used to interact with MirrorAce's API allowing you to upload local and remote files to MirrorAce and retrieve infomation on uploaded files.

## Example Code

```
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MirrorAceNet;
using MirrorAceNet.ResponseJson;

namespace MirrorAceTool
{
    internal static class Program
    {
        private static async Task Main()
        {
            // Instantiate class with API key and API token
            var mirrorAceApi = new MirrorAceApi("API Key", "API Token");
            
            // Get required data for uploading
            MirrorAceResponseBase<UploadVariablesResult>?
                uploadVariables = await mirrorAceApi.GetUploadVariablesAsync();

            if (uploadVariables == null)
            {
                Console.WriteLine("Something went wrong"); // Errors are printed, will change in future to also return error string
                Environment.Exit(1);
            }
            
            // Upload local file to MirrorAce
            MirrorAceResponseBase<UploadResponseCompleteResult>? uploadResponse =
                await mirrorAceApi.UploadAsync(uploadVariables.Result, "path-to-file",
                    uploadVariables.Result.DefaultMirrors);
            
            // Get some inforation on that upload
            MirrorAceResponseBase<Dictionary<string, MirrorAceFileInfo>>? fileInfo =
                await mirrorAceApi.GetFileInfoAsync(new[] {uploadResponse.Result.Slug});
            
            // Upload a remote file to MirrorAce
            MirrorAceResponseBase<UploadResponseCompleteResult>? remoteUploadResponse =
                await mirrorAceApi.RemoteUploadAsync(uploadVariables.Result, "url",
                    uploadVariables.Result.DefaultMirrors);
        }
    }
}
```
