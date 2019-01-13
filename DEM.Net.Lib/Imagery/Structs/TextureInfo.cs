using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Imagery
{
    public class TextureInfo
    {
        private string FileName;
        private ImageFormat ImageFormat;
        private int Width;
        private int Height;

        public TextureInfo(string fileName, ImageFormat imageFormat, int width, int height)
        {
            this.FileName = fileName;
            this.ImageFormat = imageFormat;
            this.Width = width;
            this.Height = height;
        }
    }
}
