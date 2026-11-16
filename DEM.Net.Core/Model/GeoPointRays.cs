using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace DEM.Net.Core
{
    /// <summary>A point with its normal and the distances to the walls around it (cave surveys)</summary>
    public class GeoPointRays
    {
        public GeoPoint Point { get; }
        public double Latitude => Point.Latitude;
        public double Longitude => Point.Longitude;
        public double? Elevation => Point.Elevation;

        public Vector3 Normal { get; set; }
        public float Left { get; }
        public float Right { get; }
        public float Down { get; }
        public float Up { get; }

        public GeoPointRays(double latitude, double longitude, double? altitude, Vector3 normal, float left, float right, float up, float down)
        {
            this.Point = new GeoPoint(latitude, longitude, altitude);
            this.Normal = normal;
            this.Left = left;
            this.Right = right;
            this.Down = down;
            this.Up = up;
        }

        public static implicit operator GeoPoint(GeoPointRays rays) => rays.Point;
    }
}
