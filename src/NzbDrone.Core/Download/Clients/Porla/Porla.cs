using System;
using System.Collections.Generic;
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
            throw new NotImplementedException();
        }

        public override DownloadClientInfo GetStatus()
        {
            throw new NotImplementedException();
        }

        public override void RemoveItem(DownloadClientItem item, bool deleteData)
        {
            throw new NotImplementedException();
        }

        protected override string AddFromMagnetLink(RemoteEpisode remoteEpisode, string hash, string magnetLink)
        {
            throw new NotImplementedException();
        }

        protected override string AddFromTorrentFile(RemoteEpisode remoteEpisode, string hash, string filename, byte[] fileContent)
        {
            throw new NotImplementedException();
        }
    }
}
