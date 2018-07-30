using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeroFormatter;

namespace DEM.Net.Lib
{
	[ZeroFormattable]
	[Serializable]
	public class GeoPoint
	{
		[Index(0)]
		public virtual double Latitude { get; set; }
		[Index(1)]
		public virtual double Longitude { get; set; }
		[Index(2)]
		public virtual int? XIndex { get; set; }
		[Index(3)]
		public virtual int? YIndex { get; set; }
		[Index(4)]
		public virtual double? Elevation { get; set; }
		[Index(5)]
		public virtual string TileId { get; set; }

		/// <summary>
		/// When this point is part of a List and ComputePointsDistances is called, this field
		/// stores the distance from this point to origin point in meters.
		/// </summary>
		[Index(6)]
		public virtual double DistanceFromOriginMeters { get; set; }

		public GeoPoint(double latitude, double longitude, float altitude, int indexX, int indexY)
		{
			Latitude = latitude;
			Longitude = longitude;
			Elevation = altitude;
			XIndex = indexX;
			YIndex = indexY;
		}

		public GeoPoint(double latitude, double longitude)
		{
			Latitude = latitude;
			Longitude = longitude;
			Elevation = null;
			XIndex = null;
			YIndex = null;
		}
		public GeoPoint()
		{

		}

		public GeoPoint Clone()
		{
			return (GeoPoint)this.MemberwiseClone();
		}

		public double DistanceSquaredTo(GeoPoint pt)
		{
			return (pt.Longitude - Longitude) * (pt.Longitude - Longitude)
					+ (pt.Latitude - Latitude) * (pt.Latitude - Latitude);
		}
		public double DistanceTo(GeoPoint pt)
		{
			return Math.Sqrt(this.DistanceSquaredTo(pt));
		}

		public static GeoPoint Zero
		{
			get { return new GeoPoint(0, 0); }
		}
		public override string ToString()
		{
			return $"Lat/Lon: {Latitude} / {Longitude} "
				+ (Elevation.HasValue ? $", Elevation: {Elevation.Value}" : "")
				+ $", DistanceFromOrigin: {DistanceFromOriginMeters}";
		}
	}


}
