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
		public int? XIndex;
		public int? YIndex;
		public float? Altitude;
		public string TileId;

		public GeoPoint(double latitude, double longitude, float altitude, int indexX, int indexY)
		{
			Latitude = latitude;
			Longitude = longitude;
			Altitude = altitude;
			XIndex = indexX;
			YIndex = indexY;
		}

		public GeoPoint(double latitude, double longitude)
		{
			Latitude = latitude;
			Longitude = longitude;
			Altitude = null;
			XIndex = null;
			YIndex = null;
		}

		public GeoPoint Clone()
		{
			return (GeoPoint)this.MemberwiseClone();
		}

		public static GeoPoint Zero
		{
			get { return new GeoPoint(0, 0); }
		}
		public override string ToString()
		{
			return $"Lat/Lon: {Latitude} / {Longitude} " + (Altitude.HasValue ? $", Altitude: {Altitude.Value}" : "");
		}
	}

	
}
