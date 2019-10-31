using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

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

        public EarthdataLoginConnector(ILogger<EarthdataLoginConnector> logger)
        {
            this.logger = logger;
        }
        public void Setup()
        {
            logger.LogInformation($"Setup {nameof(EarthdataLoginConnector)}");

            string urs = "https://urs.earthdata.nasa.gov";
            string username = "elevationapi";
            string password = "EBPp-zf!uzw5Jd8";

            // Create a credential cache for authenticating when redirected to Earthdata Login
            credentialCache = new CredentialCache();
            credentialCache.Add(new Uri(urs), "Basic", new NetworkCredential(username, password));

            isInitialized = true;
        }

        public void Download(string url, string localFileName)
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
                HttpWebRequest request = this.CreateWebRequest(url);
               
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    // Now access the data
                    long length = response.ContentLength;
                    string type = response.ContentType;
                    using (Stream stream = response.GetResponseStream())
                    {
                        // Process the stream data (e.g. save to file)
                        using (FileStream fs = new FileStream(localFileName, FileMode.Create, FileAccess.Write))
                        {
                            stream.CopyTo(fs);
                            fs.Close();
                        }
                        stream.Close();
                    }
                    response.Close();
                }

                
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error while downloading DEM file: {ex.Message}");
            }
        }

        private HttpWebRequest CreateWebRequest(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Credentials = credentialCache;
            request.CookieContainer = cookieContainer;
            request.PreAuthenticate = false;
            request.AllowAutoRedirect = true;
            return request;
        }
    }
}
