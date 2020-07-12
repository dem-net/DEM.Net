using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace DEM.Net.Core
{
    public class GeoPointRays : GeoPoint
    {
        //public List<Vector3> Rays { get; set; } = new List<Vector3>();
        public Vector3 Normal { get; set; }
        public float Left { get; }
        public float Right { get; }
        public float Down { get; }
        public float Up { get; }

        public GeoPointRays(double latitude, double longitude, double? altitude, Vector3 normal, float left, float right, float up, float down)
            : base(latitude, longitude, altitude)
        {
            this.Normal = normal;
            this.Left = left;
            this.Right = right;
            this.Down = down;
            this.Up = up;
        }
    }
}
