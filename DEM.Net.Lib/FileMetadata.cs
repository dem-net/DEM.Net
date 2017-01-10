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

		public string Filename { get; private set; }
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
		public int ScanlineSize { get; internal set; }
		public double StartLon { get; internal set; }
		public double StartLat { get; internal set; }
		public double pixelSizeX { get; internal set; }
		public double pixelSizeY { get; internal set; }

		public float MininumAltitude { get; internal set; }
		public float MaximumAltitude { get; internal set; }
	}
}
