using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using MirrorAceNet.Exceptions;
using MirrorAceNet.Helpers;
using MirrorAceNet.ResponseJson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MirrorAceNet
{
    /// <summary>
    /// Library used to interact with MirrorAce's API
    /// </summary>
    public class MirrorAceApi
    {
        private const string ApiBase = "https://mirrorace.com/api/v1/";
        private const int RequestRetryDelay = 3000;
        
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Current API token
        /// </summary>
        public string ApiToken { get; }
        
        /// <summary>
        /// Current API key
        /// </summary>
        public string ApiKey { get; }

        private int _requestRetires = 3;
        
        /// <summary>
        /// Number of retries per request
        /// </summary>
        public int RequestRetires
        {
            get => _requestRetires;
            set
            {
                if (value < 1 || value > 5)
                {
                    value = 3;
                }

                _requestRetires = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="apiKey">API Key from https://mirrorace.com/api</param>
        /// <param name="apiToken">API Token from https://mirrorace.com/api</param>
        public MirrorAceApi(string apiKey, string apiToken)
        {
            _httpClient = new HttpClient();
            ApiToken = apiToken;
            ApiKey = apiKey;
        }

        /// <summary>
        /// Fetches upload variables required for <seealso cref="UploadAsync"/> and <seealso cref="RemoteUploadAsync"/> serialised
        /// </summary>
        /// <returns>Upload variables response serialised</returns>
        public async Task<MirrorAceResponseBase<UploadVariablesResult>?> GetUploadVariablesAsync()
        {
            using var request = CreateRequest("file/upload");
            
            using HttpResponseMessage? response = await MakeRequest(request);

            if (response == null)
            {
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseJson = JObject.Parse(responseContent);

            if (!responseJson.ResponseHasError())
            {
                return JsonConvert.DeserializeObject<MirrorAceResponseBase<UploadVariablesResult>>(responseContent);
            }
            
            responseJson.PrintResponseError();
            return null;

        }

        /// <summary>
        /// Fetches info about slugs passed
        /// </summary>
        /// <param name="fileSlugs"></param>
        /// <returns>File info response serialised</returns>
        public async Task<MirrorAceResponseBase<Dictionary<string, MirrorAceFileInfo>>?> GetFileInfoAsync(IEnumerable<string> fileSlugs)
        {
            var fileSlugsSeparated = string.Join(',', fileSlugs);

            using var request = CreateRequest("file/info", new Dictionary<string, string>
            {
                {"files", fileSlugsSeparated}
            });
            
            using HttpResponseMessage? response = await MakeRequest(request);

            if (response == null)
            {
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseJson = JObject.Parse(responseContent);

            if (!responseJson.ResponseHasError())
            {
                return JsonConvert.DeserializeObject<MirrorAceResponseBase<Dictionary<string, MirrorAceFileInfo>>>(
                    responseContent);
                
            }
            
            responseJson.PrintResponseError();
            return null;

        }

        /// <summary>
        /// Uploads supplied file to API and subsequent mirrors
        /// </summary>
        /// <param name="uploadVariablesResult">Result received from <see cref="GetUploadVariablesAsync"/></param>
        /// <param name="uploadFilePath">Path of the file to upload</param>
        /// <param name="mirrors">List of mirrors to upload to, can be retrieved from <see cref="GetUploadVariablesAsync"/></param>
        /// <param name="filePassword"></param>
        /// <returns>Final response message serialised</returns>
        /// <exception cref="FileTooLargeException">Thrown if supplied file exceeds the maximum upload size for a single file</exception>
        public async Task<MirrorAceResponseBase<UploadResponseCompleteResult>?> UploadAsync(UploadVariablesResult uploadVariablesResult,
            string uploadFilePath,
            IReadOnlyCollection<string> mirrors,
            string? filePassword = null)
        {
            var uploadFileLength = new FileInfo(uploadFilePath).Length;
            var uploadFilename = new FileInfo(uploadFilePath).Name;

            if (uploadFileLength >= uploadVariablesResult.MaxFileSize)
            {
                throw new FileTooLargeException($"Upload file supplied was too large, expected less than {uploadVariablesResult.MaxFileSize} got {uploadFileLength}");
            }

            var uploadFileBytes = await File.ReadAllBytesAsync(uploadFilePath);
            var numberOfChunks = Math.Ceiling((double) uploadFileLength / uploadVariablesResult.MaxChunkSize);

            for (int i = 0; i < numberOfChunks; i++)
            {
                var rangeStart = i * uploadVariablesResult.MaxChunkSize;
                var rangeEnd = (i + 1) * uploadVariablesResult.MaxChunkSize;

                var isLastChunk = rangeEnd > uploadFileLength;
                
                if (isLastChunk)
                {
                    rangeEnd = uploadFileLength;
                }
                
                var range = rangeEnd - rangeStart;

                byte[] chunk = new byte[range];

                Buffer.BlockCopy(uploadFileBytes, (int) rangeStart, chunk, 0, (int) range);

                using var request = CreateFileUploadRequest(uploadVariablesResult, mirrors, chunk, uploadFilename, filePassword);
                var rangeValue = $"bytes {rangeStart}-{rangeEnd}/{uploadFileLength}";
                
                if (isLastChunk)
                {
                    request.Content.Headers.TryAddWithoutValidation("Content-Range", rangeValue);
                }
                else
                {
                    request.Content.Headers.Add("Content-Range", rangeValue);
                }

                var chunkSizeMegabytes = Math.Round(range / 1024d / 1024d, 2);
                Console.WriteLine($"Uploading chunk {i + 1} of {numberOfChunks} with a size of {chunkSizeMegabytes}MB");
                
                using HttpResponseMessage? uploadResponse = await MakeRequest(request);

                if (uploadResponse == null)
                {
                    return null;
                }
                
                var uploadResponseContent = await uploadResponse.Content.ReadAsStringAsync();
                var uploadResponseJson = JObject.Parse(uploadResponseContent);

                if (uploadResponseJson.ResponseHasError())
                {
                    Console.WriteLine($"Upload failure: {uploadResponseJson?["result"]?.Value<string>() ?? ""}");
                    return null;
                }

                if (isLastChunk)
                {
                    var uploadResponseParsed =
                        JsonConvert.DeserializeObject<MirrorAceResponseBase<UploadResponseCompleteResult>>(
                            uploadResponseContent);

                    return uploadResponseParsed;
                }
                else
                {
                    var uploadResponseParsed =
                        JsonConvert.DeserializeObject<MirrorAceResponseBase<UploadResponseChunkResult>>(
                            uploadResponseContent);

                    if (uploadResponseParsed?.Result?.Info == "continue")
                    {
                        continue;
                    }
                    
                    // not sure what happens here
                    Console.WriteLine($"Chunk info failure: {uploadResponseParsed?.Result?.Info ?? ""}");
                    return null;
                }

            }

            return null;
        }

        /// <summary>
        /// Uploads remote file to API and subsequent mirrors
        /// </summary>
        /// <param name="uploadVariablesResult">Result received from <see cref="GetUploadVariablesAsync"/></param>
        /// <param name="url">Remote file URL to upload</param>
        /// <param name="mirrors">List of mirrors to upload to, can be retrieved from <see cref="GetUploadVariablesAsync"/></param>
        /// <param name="filePassword"></param>
        /// <returns>Response message serialised</returns>
        public async Task<MirrorAceResponseBase<UploadResponseCompleteResult>?> RemoteUploadAsync(
            UploadVariablesResult uploadVariablesResult,
            string url,
            IEnumerable<string> mirrors,
            string? filePassword = null)
        {
            using var remoteUploadRequest = CreateRemoteFileUploadRequest(uploadVariablesResult, mirrors, url, filePassword);

            HttpResponseMessage? remoteUploadResponse = await MakeRequest(remoteUploadRequest);

            if (remoteUploadResponse == null)
            {
                return null;
            }
            
            var responseContent = await remoteUploadResponse.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<MirrorAceResponseBase<UploadResponseCompleteResult>>(responseContent);
        }

        private async Task<HttpResponseMessage?> MakeRequest(HttpRequestMessage request)
        {
            for (int i = 0; i < RequestRetires; i++)
            {
                HttpResponseMessage response;

                try
                {
                    response = await _httpClient.SendAsync(request);
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"Request Exception: {ex.Message}");
                    request = await request.CloneAsync();
                    await Task.Delay(RequestRetryDelay);
                    continue;
                }

                if (response.IsSuccessStatusCode)
                {
                    return response;
                }
                
                Console.WriteLine($"Request Failure: {response.ReasonPhrase}");
                request = await request.CloneAsync();
                await Task.Delay(RequestRetryDelay);
            }

            return null;
        }

        private HttpRequestMessage CreateRequest(string path, Dictionary<string, string>? extraParameters = null)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, ApiBase + path);

            var requestContent = new Dictionary<string, string>
            {
                {"api_key", ApiKey},
                {"api_token", ApiToken}
            };

            if (extraParameters == null)
            {

                request.Content = new FormUrlEncodedContent(requestContent);
                return request;
            }
            
            foreach ((string key, string value) in extraParameters)
            {
                requestContent.Add(key, value);
            }

            request.Content = new FormUrlEncodedContent(requestContent);

            return request;
        }

        private HttpRequestMessage CreateFileUploadRequest(UploadVariablesResult uploadVariablesResult,
            IEnumerable<string> mirrors,
            byte[] chunkContents,
            string filename,
            string? password)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uploadVariablesResult.ServerFile);

            var multipartData = CreateBaseUploadContent(uploadVariablesResult, mirrors, password);
            
            multipartData.Add(new ByteArrayContent(chunkContents), "files[]", filename);

            request.Content = multipartData;
            return request;
        }

        private HttpRequestMessage CreateRemoteFileUploadRequest(UploadVariablesResult uploadVariablesResult,
            IEnumerable<string> mirrors,
            string url,
            string? password)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uploadVariablesResult.ServerRemote);

            var multipartData = CreateBaseUploadContent(uploadVariablesResult, mirrors, password);
            
            multipartData.Add(new StringContent(url), "url");

            request.Content = multipartData;
            return request;
        }

        private MultipartFormDataContent CreateBaseUploadContent(UploadVariablesResult uploadVariablesResult,
            IEnumerable<string> mirrors,
            string? password)
        {
            var multipartData = new MultipartFormDataContent
            {
                {
                    new StringContent(ApiKey), "api_key"
                },
                {
                    new StringContent(ApiToken), "api_token"
                },
                {
                    new StringContent(uploadVariablesResult.CTracker ?? ""), "cTracker"
                },
                {
                    new StringContent(uploadVariablesResult.UploadKey ?? ""), "upload_key"
                }
            };


            if (!string.IsNullOrWhiteSpace(password))
            {
                multipartData.Add(new StringContent(password), "file_password");
            }
            
            foreach (string mirror in mirrors)
            {
                multipartData.Add(new StringContent(mirror), "mirrors[]");
            }

            return multipartData;
        }
    }
}