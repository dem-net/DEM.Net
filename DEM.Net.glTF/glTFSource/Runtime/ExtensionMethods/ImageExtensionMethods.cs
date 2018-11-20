using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetGenerator.Runtime.ExtensionMethods
{
    public static class ImageExtensionMethods
    {
        /// <summary>
        /// Function which determines if two Image objects have equal values
        /// </summary>
        /// <param name="i1"></param>
        /// <param name="i2"></param>
        /// <returns></returns>
        public static bool ImagesEqual(this glTFLoader.Schema.Image i1, glTFLoader.Schema.Image i2)
        {
            return ((i1.Name == i2.Name) && (i1.Uri == i2.Uri) && i1.MimeType == i2.MimeType);
        }
    }
}
