using DEM.Net.Core.Configuration;
using DEM.Net.Core.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace DEM.Net.Core.EarthData
{
    // Connector class for downloading a resource from an Earthdata Login enabled service
    // https://wiki.earthdata.nasa.gov/pages/viewpage.action?pageId=76519934
    //
    public class EarthdataLoginConnector
    {
        // Ideally the cookie container will be persisted to/from file
        private CookieContainer cookieContainer = new CookieContainer();
        private CredentialCache credentialCache = new CredentialCache();
        bool isInitialized = false;
        private readonly ILogger<EarthdataLoginConnector> logger;
        private readonly AppSecrets secretOptions;
        private readonly IHttpClientFactory httpClientFactory;

        public EarthdataLoginConnector(IHttpClientFactory httpClientFactory, IOptions<AppSecrets> secretOptions, ILogger<EarthdataLoginConnector> logger)
        {
            this.logger = logger;
            this.secretOptions = secretOptions.Value;
            this.httpClientFactory = httpClientFactory;

        }
        public void Setup()
        {
            if (string.IsNullOrWhiteSpace(secretOptions.NasaEarthDataLogin))
            {
                throw new InvalidOperationException($"{nameof(EarthdataLoginConnector)} cannot download data because login and password are not set in user secrets.");
            }
            logger.LogInformation($"Setup {nameof(EarthdataLoginConnector)}");

            string urs = "https://urs.earthdata.nasa.gov";

            // Create a credential cache for authenticating when redirected to Earthdata Login
            credentialCache = new CredentialCache();
            credentialCache.Add(new Uri(urs), "Basic", new NetworkCredential(secretOptions.NasaEarthDataLogin, secretOptions.NasaEarthDataPassword));

            isInitialized = true;
        }

        public async Task DownloadAsync(string url, string localFileName)
        {
            try
            {
                logger.LogInformation($"Downloading {url}...");

                if (!isInitialized)
                    this.Setup();

                var dirName = Path.GetDirectoryName(localFileName);
                if (!Directory.Exists(dirName))
                {
                    Directory.CreateDirectory(dirName);
                }
                // Execute the request
                var baseAddress = new Uri(url);
                using (var handler = new HttpClientHandler()
                {
                    CookieContainer = cookieContainer,
                    Credentials = credentialCache,
                    PreAuthenticate = false,
                    AllowAutoRedirect = true
                })
                {
                    var httpClient = new HttpClient(handler) { BaseAddress = baseAddress };
                    var contentbytes = await httpClient.GetByteArrayAsync(url);
                    using (FileStream fs = new FileStream(localFileName, FileMode.Create, FileAccess.Write))
                    {
                        await fs.WriteAsync(contentbytes, 0, contentbytes.Length);
                    }


                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error while downloading DEM file: {ex.Message}");
                throw;
            }
        }

    }
}
