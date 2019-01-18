using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Imagery
{
    public class TextureInfo
    {

        public string FilePath { get; }
        public string FileName { get; }
        public ImageFormat ImageFormat { get; }
        public int Width { get; }
        public int Height { get; }

        public TextureInfo(string filePath, ImageFormat imageFormat, int width, int height)
        {
            this.FilePath = filePath;
            this.FileName = Path.GetFileName(filePath);
            this.ImageFormat = imageFormat;
            this.Width = width;
            this.Height = height;
        }

       
    }
}
