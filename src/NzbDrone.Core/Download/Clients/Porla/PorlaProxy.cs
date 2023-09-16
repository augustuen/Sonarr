using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;

namespace NzbDrone.Core.Download.Clients.Porla
{
    public interface IPorlaProxy
    {
        string GetVersions(PorlaSettings settings);
        PorlaPreferences GetConfig(PorlaSettings settings);
        PorlaTorrent[] GetTorrents(PorlaSettings settings);
        PorlaTorrentProperties GetTorrentProperties(string hash, PorlaSettings settings);
        List<PorlaTorrentFile> GetTorrentFiles(string hash, PorlaSettings settings);
        string AddTorrentFromUrl(string torrentUrl, TorrentSeedConfiguration seedConfiguration, PorlaSettings settings);
        string AddTorrentFromFile(string fileName, byte[] fileContent, TorrentSeedConfiguration seedConfiguration, PorlaSettings settings);

        void RemoveTorrent(string hash, bool removeData, PorlaSettings settings);
        void SetTorrentSeedingConfiguration(string hash, TorrentSeedConfiguration seedConfiguration, PorlaSettings settings);
        void MoveTorrentToTopInQueue(string hash, PorlaSettings settings);
    }

    public class PorlaProxy : IPorlaProxy
    {
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public PorlaProxy(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public string GetVersions(PorlaSettings settings)
        {
            var filter = new Dictionary<string, object>();
            var result = ProcessRequest<PorlaResponse>(settings, "sys.versions", filter);
            return result.Porla.Version;
        }

        public PorlaPreferences GetConfig(PorlaSettings settings)
        {
            var result = ProcessRequest<PorlaPreferences>(settings, "session.settings.list");
            return result;
        }

        public string AddTorrentFromFile(string fileName, byte[] fileContent, TorrentSeedConfiguration seedConfiguration, PorlaSettings settings)
        {
            var parameters = new
            {
                name = fileName,
                save_path = settings.SavePath,
                category = settings.TvCategory ?? "",
                ti = Convert.ToBase64String(fileContent)
            };

            // ProcessRequest<PorlaResponse>(settings, "torrents.add", parameters);
            var result = ProcessRequest<PorlaResponse>(settings, "torrents.add", parameters);
            return result.InfoHash;
        }

        public string AddTorrentFromUrl(string torrentUrl, TorrentSeedConfiguration seedConfiguration, PorlaSettings settings)
        {
            var parameters = new
            {
                magnet_uri = torrentUrl,
                save_path = settings.SavePath,
                category = settings.TvCategory ?? ""
            };

            var result = ProcessRequest<PorlaResponse>(settings, "torrents.add", parameters);
            return result.InfoHash;
        }

        public List<PorlaTorrentFile> GetTorrentFiles(string hash, PorlaSettings settings)
        {
            var parameters = new
            {
                info_hash = hash
            };
            var result = ProcessRequest<PorlaResponse>(settings, "torrents.files.list", parameters);
            return result.Files.ToList<PorlaTorrentFile>();
        }

        public PorlaTorrentProperties GetTorrentProperties(string hash, PorlaSettings settings)
        {
            throw new NotImplementedException();
        }

        public PorlaTorrent[] GetTorrents(PorlaSettings settings)
        {
            var parameters = new
            {
                filters = new
                {
                    category = settings.TvCategory.ToString() ?? ""
                }
            };
            var result = ProcessRequest<PorlaResponse>(settings, "torrents.list", parameters);

            return result.Torrents;
        }

        public void MoveTorrentToTopInQueue(string hash, PorlaSettings settings)
        {
            throw new System.NotImplementedException();
        }

        public void PauseTorrent(string hash, PorlaSettings settings)
        {
            var parameters = new
            {
                info_hash = hash
            };
            var result = ProcessRequest<PorlaTorrentsResponse>(settings, "torrrents.pause", parameters);
            var test = result.ToString();
        }

        public void RemoveTorrent(string hash, bool removeData, PorlaSettings settings)
        {
            var parameters = new Dictionary<string, object>();
            parameters.Add("info_hashes", new string[] { hash, null });
            parameters.Add("remove_data", removeData);
            ProcessRequest<object>(settings, "torrents.remove", parameters);
        }

        public void ResumeTorrent(string hash, PorlaSettings settings)
        {
            throw new System.NotImplementedException();
        }

        public void SetTorrentSeedingConfiguration(string hash, TorrentSeedConfiguration seedConfiguration, PorlaSettings settings)
        {
            throw new System.NotImplementedException();
        }

        private void AddTorrentSeedingFormParameters(JsonRpcRequestBuilder request, TorrentSeedConfiguration seedConfiguration, bool always = false)
        {
            return; // TODO: Porla doesn't have a built-in ratio manager. Consider making our own
        }

        private JsonRpcRequestBuilder BuildRequest(PorlaSettings settings)
        {
            var url = HttpRequestBuilder.BuildBaseUrl(settings.UseSsl, settings.Host, settings.Port, settings.UrlBase);

            var requestBuilder = new JsonRpcRequestBuilder(url, JsonRpcRequestBuilder.ParameterStructure.ByName);
            requestBuilder.LogResponseContent = true;

            requestBuilder.Resource("/api/v1/jsonrpc");
            requestBuilder.SetHeader("Authorization", "Bearer " + settings.Token);
            requestBuilder.SetHeader("Accept", "application/json");
            requestBuilder.PostProcess += r => r.RequestTimeout = TimeSpan.FromSeconds(15); // WTF does this do?

            return requestBuilder;
        }

        protected TResult ProcessRequest<TResult>(PorlaSettings settings, string method, params object[] arguments)
        {
            var requestBuilder = BuildRequest(settings);

            var response = ExecuteRequest<TResult>(requestBuilder, method, arguments);

            // TODO: Implement error handling
            if (response.Error != null)
            {
                var error = response.Error.ToObject<PorlaError>();
                if (error.Code == -5)
                {
                    error = response.Error.ToObject<PorlaError>();
                    throw new DownloadClientAuthenticationException(error.Message);
                }
            }

            return response.Result;
        }

        protected TResult ProcessRequest<TResult>(PorlaSettings settings, string method)
        {
            return ProcessRequest<TResult>(settings, method, new Dictionary<string, object>());
        }

        private JsonRpcResponse<TResult> ExecuteRequest<TResult>(JsonRpcRequestBuilder requestBuilder, string method, params object[] arguments)
        {
            var request = requestBuilder.Call(method, arguments).Build();

            HttpResponse response;
            try
            {
                response = _httpClient.Execute(request);

                return Json.Deserialize<JsonRpcResponse<TResult>>(response.Content);
            }
            catch (HttpException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.RequestTimeout)
                {
                    _logger.Debug("Porla timeout during request, daemon connection may have been broken. Attempting to reconnect.");
                    return new JsonRpcResponse<TResult>()
                    {
                        Error = JToken.Parse("{ Code = 2 }")
                    };
                }
                else
                {
                    throw new DownloadClientException("Unable to connect to Porla, please check your settings", ex);
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.TrustFailure)
                {
                    throw new DownloadClientUnavailableException("Unable to connect to Porla, certificate validation failed.", ex);
                }

                throw new DownloadClientUnavailableException("Unable to connect to Porla, please check your settings", ex);
            }
        }
    }
}
