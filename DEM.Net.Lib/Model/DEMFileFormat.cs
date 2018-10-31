using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeroFormatter;

namespace DEM.Net.Lib
{
	[ZeroFormattable]
	public class DEMFileFormat
	{

		[Index(0)]
		public virtual string Name { get; set; }


		[Index(1)]
		public virtual string FileExtension { get; set; }

		public static readonly DEMFileFormat SRTM_HGT = new DEMFileFormat("Shuttle Radar Topography Mission (SRTM) Data file.", ".hgt");
		public static readonly DEMFileFormat GEOTIFF = new DEMFileFormat("GeoTIFF file", ".tif");

		private DEMFileFormat(string name, string fileExtension)
		{
			this.Name = name;
			this.FileExtension = fileExtension;
		}

		public DEMFileFormat()
		{

		}

		public override string ToString()
		{
			return Name;
		}

	}
}
