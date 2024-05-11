using System.Collections.Generic;
using Newtonsoft.Json;

namespace NzbDrone.Core.Download.Clients.Porla
{
    public class PorlaResponse
    {
        [JsonProperty(PropertyName = "jsonrpc")]
        public string JsonRPC { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public PorlaVersion Porla;
        public PorlaTorrent[] Torrents;
        public int TorrentsTotal { get; set; }
        public int TorrentsTotalUnfiltered { get; set; }

        public PorlaTorrentFile[] Files;

        public string InfoHash { get; set; }

        [JsonProperty(PropertyName = "result")]
        public Dictionary<string, object> Result;
        public PorlaPreset Default;
    }

    public class PorlaVersionsResponse
    {
        public PorlaVersion Porla;
    }

    public class PorlaVersion
    {
        public string Version { get; set; }
        public string Branch { get; set; }
        public string Commitish { get; set; }
    }

    public class PorlaTorrentsResponse
    {
        public PorlaTorrent[] Torrents;
    }
}
