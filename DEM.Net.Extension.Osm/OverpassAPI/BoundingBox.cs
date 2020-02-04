using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Extension.Osm.OverpassAPI
{
	public class BoundingBox
	{
		public BoundingBox(double p_YMin, double p_XMin, double p_YMax, double p_XMax)
		{
			XMin = p_XMin;
			XMax = p_XMax;
			YMin = p_YMin;
			YMax = p_YMax;
		}

		public double XMax { get; }
		public double XMin { get; }
		public double YMax { get; }
		public double YMin { get; }

		public override string ToString()
		{
			var str = String.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3}", YMin, XMin, YMax, XMax);
			return str;
		}
		
	}
}
