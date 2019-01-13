namespace DEM.Net.Lib.Imagery
{
    public class UrlModel
    {
        public string UrlFormat;
        public string[] Servers;

        public UrlModel(string urlFormat, string[] servers)
        {
            this.UrlFormat = urlFormat;
            this.Servers = servers;
        }
    }
}