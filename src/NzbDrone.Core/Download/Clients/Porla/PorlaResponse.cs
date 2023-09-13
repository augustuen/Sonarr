namespace NzbDrone.Core.Download.Clients.Porla
{
    public class PorlaResponse
    {
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
