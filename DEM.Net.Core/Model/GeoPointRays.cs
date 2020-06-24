using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace DEM.Net.Core
{
    public class GeoPointRays
    {
        public GeoPoint GeoPoint { get; set; }

        public List<Vector3> Rays { get; set; } = new List<Vector3>();
        public double? Elevation => GeoPoint?.Elevation;
        public double Longitude => GeoPoint.Longitude;
        public double Latitude => GeoPoint.Latitude;

        public GeoPointRays(GeoPoint geoPoint)
        {
            this.GeoPoint = geoPoint;
        }
        public GeoPointRays(GeoPoint geoPoint, float left, float right, float up, float down)
        {
            this.GeoPoint = geoPoint;
            this.Rays.Add(Vector3.UnitX * left * -1F);
            this.Rays.Add(Vector3.UnitX * right * 1F);
            this.Rays.Add(Vector3.UnitZ * down * -1F);
            this.Rays.Add(Vector3.UnitZ * up * 1F);
        }
    }
}
