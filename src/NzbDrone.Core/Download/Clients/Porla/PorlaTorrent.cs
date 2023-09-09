using Newtonsoft.Json;
using Org.BouncyCastle.Math;

namespace NzbDrone.Core.Download.Clients.Porla
{
    public class PorlaTorrent
    {
        public string Hash { get; set; } // Torrent hash

        public string Name { get; set; } // Torrent name

        public long Size { get; set; } // Torrent size (bytes)

        public double Progress { get; set; } // Torrent progress

        public BigInteger Eta { get; set; } // Torrent ETA (seconds)

        public string State { get; set; } // Torrent State.

        public string Label { get; set; } // TODO: Needed? Probably not

        public string Category { get; set; }

        [JsonProperty(PropertyName = "save_path")] // TODO: Fix property name
        public string SavePath { get; set; }

        [JsonProperty(PropertyName = "content_path")] // TODO: Fix property name
        public string ContentPath { get; set; }

        public float Ratio { get; set; }

        [JsonProperty(PropertyName = "seeding_time")] // TODO: Fix property name
        public long? SeedingTime { get; set; }
    }

    public class PorlaTorrentProperties
    {
        public string Hash { get; set; }

        [JsonProperty(PropertyName = "save_path")] // Save path of the torrent
        public string SavePath { get; set; }

        [JsonProperty(PropertyName = "ratio")]
        public float Ratio { get; set; }

        [JsonProperty(PropertyName = "seeding_time")] // Number of seconds the torrent has had *all* pieces available to peers
        public long SeedingTime { get; set; }

        [JsonProperty(PropertyName = "finished_time")] // Number of seconds the torrent has had all *selected* files and pieces available to peers.
        public long FinishedTime { get; set; }
    }

    public class PorlaTorrentFile
    {
        public string Name { get; set; }
    }
}
