using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib
{
    public class FileMetadata
    {
        public FileMetadata(string filename)
        {
            this.Filename = filename;
			
			
				}

        public string Filename { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public double PixelScaleX { get; set; }
        public double PixelScaleY { get; set; }
        public double OriginLatitude { get; set; }
        public double OriginLongitude { get; set; }
        public int BitsPerSample { get; set; }
        public string WorldUnits { get; set; }
        public string SampleFormat { get; set; }
        public string NoDataValue { get; set; }
        public int ScanlineSize { get; set; }
        public double StartLon { get; set; }
        public double StartLat { get; set; }
        public double pixelSizeX { get; set; }
        public double pixelSizeY { get; set; }

        public float MininumAltitude { get; set; }
        public float MaximumAltitude { get; set; }
    }
}
