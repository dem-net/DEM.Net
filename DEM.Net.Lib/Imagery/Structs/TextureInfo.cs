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
        public TextureImageFormat ImageFormat { get; }
        public int Width { get; }
        public int Height { get; }
        public int ProjectedZoom { get; }
        public BoundingBox ProjectedBounds { get; }

        public TextureInfo(string filePath, TextureImageFormat imageFormat, int width, int height, int zoom = 0, BoundingBox projectedBounds = null)
        {
            this.FilePath = filePath;
            this.FileName = Path.GetFileName(filePath);
            this.ImageFormat = imageFormat;
            this.Width = width;
            this.Height = height;
            this.ProjectedZoom = zoom;
            this.ProjectedBounds = projectedBounds;
        }

       
    }
}
