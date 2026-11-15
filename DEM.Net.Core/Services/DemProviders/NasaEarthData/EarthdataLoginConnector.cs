using DEM.Net.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DEM.Net.Core.EarthData
{
    // Connector class for downloading a resource from an Earthdata Login enabled service
    // https://wiki.earthdata.nasa.gov/pages/viewpage.action?pageId=76519934
    //
    public class EarthdataLoginConnector
    {
        private const string Urs = "https://urs.earthdata.nasa.gov";
        private readonly ILogger<EarthdataLoginConnector> logger;
        private readonly AppSecrets secretOptions;

        // One client for the lifetime of the connector (singleton): the cookies of the Earthdata Login session are
        // kept across downloads. Created on first use, so that missing credentials only fail NASA downloads.
        private readonly Lazy<HttpClient> httpClient;

        public EarthdataLoginConnector(IOptions<AppSecrets> secretOptions, ILogger<EarthdataLoginConnector> logger)
        {
            this.logger = logger;
            this.secretOptions = secretOptions.Value;
            this.httpClient = new Lazy<HttpClient>(CreateClient, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        private HttpClient CreateClient()
        {
            if (string.IsNullOrWhiteSpace(secretOptions.NasaEarthDataLogin))
            {
                throw new InvalidOperationException($"{nameof(EarthdataLoginConnector)} cannot download data because login and password are not set in user secrets.");
            }
            logger.LogInformation($"Setup {nameof(EarthdataLoginConnector)}");

            // Credentials for the Earthdata Login redirect
            var credentialCache = new CredentialCache();
            credentialCache.Add(new Uri(Urs), "Basic", new NetworkCredential(secretOptions.NasaEarthDataLogin, secretOptions.NasaEarthDataPassword));

            // Same behavior as the former HttpWebRequest: cookies kept across requests, credentials sent
            // only when challenged (Earthdata Login redirect), redirects followed automatically.
            return new HttpClient(new HttpClientHandler
            {
                CookieContainer = new CookieContainer(),
                UseCookies = true,
                Credentials = credentialCache,
                PreAuthenticate = false,
                AllowAutoRedirect = true
            }, disposeHandler: true);
        }

        public async Task DownloadAsync(string url, string localFileName, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation($"Downloading {url}...");
                await HttpDownload.DownloadToFileAsync(httpClient.Value, url, localFileName, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, $"Error while downloading DEM file: {ex.Message}");
                throw;
            }
        }
    }
}
