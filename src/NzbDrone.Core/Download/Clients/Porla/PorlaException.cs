namespace NzbDrone.Core.Download.Clients.Porla
{
    public class PorlaException : DownloadClientException
    {
        public PorlaException(string message)
            : base(message)
        {
        }
    }
}
