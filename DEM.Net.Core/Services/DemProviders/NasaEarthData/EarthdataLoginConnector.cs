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
        public void Connect()
        {
            string resource = "<url of resource>";
            string urs = "https://urs.earthdata.nasa.gov";
            string username = "<URS user ID>";
            string password = "<URS user password>";

            try
            {
                // Ideally the cookie container will be persisted to/from file
                CookieContainer cookieContainer = new CookieContainer();


                // Create a credential cache for authenticating when redirected to Earthdata Login
                CredentialCache cache = new CredentialCache();
                cache.Add(new Uri(urs), "Basic", new NetworkCredential(username, password));


                // Execute the request
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(resource);
                request.Method = "GET";
                request.Credentials = cache;
                request.CookieContainer = cookieContainer;
                request.PreAuthenticate = false;
                request.AllowAutoRedirect = true;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();


                // Now access the data
                long length = response.ContentLength;
                string type = response.ContentType;
                Stream stream = response.GetResponseStream();
                StreamReader reader = new StreamReader(stream);


                // Process the stream data (e.g. save to file)



                // Tidy up

                stream.Close();
                reader.Close();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("Error: " + ex.Message);
            }
        }
    }
}
