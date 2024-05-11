using Newtonsoft.Json;

namespace NzbDrone.Core.Download.Clients.Porla
{
    public class PorlaPreset
    {
        public string Name { get; set; }

        public int DownloadLimit { get; set; }

        [JsonProperty(PropertyName = "save_path")]
        public string SavePath { get; set; }

        public string Category { get; set; }
    }
}
