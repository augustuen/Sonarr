using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles.TorrentInfo;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RemotePathMappings;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.Porla
{
    public class Porla : TorrentClientBase<PorlaSettings>
    {
        private readonly IPorlaProxy _proxy;
        public Porla(IPorlaProxy proxy,
                     ITorrentFileInfoReader torrentFileInfoReader,
                     IHttpClient httpClient,
                     IConfigService configService,
                     IDiskProvider diskProvider,
                     IRemotePathMappingService remotePathMappingService,
                     Logger logger)
            : base(torrentFileInfoReader, httpClient, configService, diskProvider, remotePathMappingService, logger)
        {
            _proxy = proxy;
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestConnection());
            if (failures.HasErrors())
            {
                return;
            }

            // TODO: Add more connection tests
        }

        private ValidationFailure TestConnection()
        {
            try
            {
                _proxy.GetVersions(Settings);
            }
            catch (DownloadClientAuthenticationException ex)
            {
                _logger.Error(ex, ex.Message);

                return new NzbDroneValidationFailure("Token", "Authentication failed");
            }
            catch (WebException ex)
            {
                _logger.Error(ex, "Unable to test connection.");
                switch (ex.Status)
                {
                    case WebExceptionStatus.ConnectFailure:
                        return new NzbDroneValidationFailure("Host", "Unable to connect")
                        {
                            DetailedDescription = "Please verify the hostname and port."
                        };
                    case WebExceptionStatus.ConnectionClosed:
                        return new NzbDroneValidationFailure("UseSsl", "Verify SSL settings")
                        {
                            DetailedDescription = "Please verify your SSL configuration on both Porla and Sonarr."
                        };
                    case WebExceptionStatus.SecureChannelFailure:
                        return new NzbDroneValidationFailure("UseSsl", "Unable to connect through SSL")
                        {
                            DetailedDescription = "Sonarr is unable to connect to Porla using SSL. This problem could be computer related. Please try to configure both Sonarr and Porla to not use SSL."
                        };
                    default:
                        return new NzbDroneValidationFailure(string.Empty, "Unknown exception: " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to test connection.");
                return new NzbDroneValidationFailure("Host", "Unable to connect to Porla")
                {
                    DetailedDescription = ex.Message
                };
            }

            return null;
        }

        public override string Name => "Porla";

        public override IEnumerable<DownloadClientItem> GetItems()
        {
            IEnumerable<PorlaTorrent> torrents;
            torrents = _proxy.GetTorrents(Settings);

            var items = new List<DownloadClientItem>();

            foreach (var torrent in torrents)
            {
                if (torrent.Hash == null)
                {
                    continue;
                }

                var item = new DownloadClientItem();
                item.DownloadId = torrent.Hash[0].ToUpper() ?? torrent.Hash[1].ToUpper();
                item.Title = torrent.Name;
                item.Category = Settings.TvCategory;

                item.DownloadClientInfo = DownloadClientItemClientInfo.FromDownloadClient(this);

                var outputPath = _remotePathMappingService.RemapRemoteToLocal(Settings.Host, new OsPath(torrent.SavePath));
                item.OutputPath = outputPath + torrent.Name;
                item.SeedRatio = torrent.Ratio;

                try
                {
                    item.RemainingTime = TimeSpan.FromSeconds(torrent.Eta);
                }
                catch (OverflowException ex)
                {
                    _logger.Debug(ex, "ETA for {0} is too long: {1}", torrent.Name, torrent.Eta);
                    item.RemainingTime = TimeSpan.MaxValue;
                }

                item.TotalSize = torrent.Size;

                if (torrent.State == 4 || torrent.State == 5)
                {
                    item.Status = DownloadItemStatus.Completed;
                }
                else
                {
                    item.Status = DownloadItemStatus.Downloading;
                }

                items.Add(item);
            }

            return items;
        }

        public override object RequestAction(string action, IDictionary<string, string> query)
        {
            if (Settings.Token.IsNullOrWhiteSpace())
            {
                return new
                {
                    devices = new List<object>()
                };
            }

            if (action == "getPresets")
            {
                var presets = _proxy.GetPresets(Settings);
                Settings.PresetsList = presets;
                return new
                {
                    options = presets.OrderBy(d => d.Key)
                    .Select(d => new
                    {
                        value = d.Key,
                        name = d.Key
                    })
                };
            }

            return base.RequestAction(action, query);
        }

        public override DownloadClientInfo GetStatus()
        {
            throw new NotImplementedException();
        }

        public override void RemoveItem(DownloadClientItem item, bool deleteData)
        {
            _proxy.RemoveTorrent(item.DownloadId, deleteData, Settings);
        }

        protected override string AddFromMagnetLink(RemoteEpisode remoteEpisode, string hash, string magnetLink)
        {
            var infoHash = _proxy.AddTorrentFromUrl(magnetLink, null, Settings);
            return hash ?? infoHash;
        }

        protected override string AddFromTorrentFile(RemoteEpisode remoteEpisode, string hash, string filename, byte[] fileContent)
        {
            var infoHash = _proxy.AddTorrentFromFile(filename, fileContent, null, Settings);
            return hash ?? infoHash;
        }
    }
}
