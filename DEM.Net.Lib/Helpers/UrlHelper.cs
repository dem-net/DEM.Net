using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib
{
    public static class UrlHelper
    {
        readonly static Uri SomeBaseUri = new Uri("http://foo.org");

        public static string GetFileNameFromUrl(string url)
        {

            Uri uri = null;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                uri = new Uri(SomeBaseUri, url);

            }

            return Path.GetFileName(uri.LocalPath);
        }
    }
}
