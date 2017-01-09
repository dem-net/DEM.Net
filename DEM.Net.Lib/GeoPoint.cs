using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib
{
	[Serializable]
	public class GeoPoint
	{
		public double Latitude;
		public double Longitude;
		public int XIndex;
		public int YIndex;
		public float Altitude;

		public GeoPoint(double latitude, double longitude, float altitude, int indexX, int indexY)
		{
			Latitude = latitude;
			Longitude = longitude;
			Altitude = altitude;
			XIndex = indexX;
			YIndex = indexY;
		}

		public override string ToString()
		{
			return string.Concat("Lat/Lon: ", Latitude, ", ", Longitude, ", Altitude: ", Altitude);
		}
	}
}
