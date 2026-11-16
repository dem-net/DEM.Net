using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace DEM.Net.Core;

/// <summary>A point with its normal and the distances to the walls around it (cave surveys)</summary>
public class GeoPointRays(double latitude, double longitude, double? altitude, Vector3 normal, float left, float right, float up, float down)
{
    public GeoPoint Point { get; } = new GeoPoint(latitude, longitude, altitude);
    public double Latitude => Point.Latitude;
    public double Longitude => Point.Longitude;
    public double? Elevation => Point.Elevation;

    public Vector3 Normal { get; set; } = normal;
    public float Left { get; } = left;
    public float Right { get; } = right;
    public float Down { get; } = down;
    public float Up { get; } = up;

    public static implicit operator GeoPoint(GeoPointRays rays) => rays.Point;
}
