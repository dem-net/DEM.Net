// GeoPoint.cs
//
// Author:
//       Xavier Fischer 
//
// Copyright (c) 2019 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Globalization;

namespace DEM.Net.Core
{
    /// <summary>
    /// A location with an optional elevation (32 bytes, immutable: use <c>with</c> to change a coordinate,
    /// eg: <c>point with { Elevation = 120 }</c>).
    /// </summary>
    /// <remarks>
    /// Since v11 this is a value type: copies are independent, and a point stored in a list is changed
    /// by storing a new value (<c>list[i] = list[i] with { ... }</c>).
    /// <c>default(GeoPoint)</c> is (0, 0) without elevation.
    /// </remarks>
    public readonly struct GeoPoint : IEquatable<GeoPoint>
    {
        // Optional values are stored as the bits of the double xor those of NaN: 0 (the default) means no value,
        // so the struct needs no HasValue flags (32 bytes instead of 48), and NaN is no value as before.
        private static readonly long NoValueBits = BitConverter.DoubleToInt64Bits(double.NaN);
        private readonly long _elevation;
        private readonly long _distanceFromOrigin;

        private static long Encode(double? value) => value is double v && !double.IsNaN(v) ? BitConverter.DoubleToInt64Bits(v) ^ NoValueBits : 0;
        private static double? Decode(long bits) => bits == 0 ? null : BitConverter.Int64BitsToDouble(bits ^ NoValueBits);

        public double Latitude { get; init; }
        public double Longitude { get; init; }

        /// <summary>Elevation in meters, null when unknown (NaN is stored as null)</summary>
        public double? Elevation
        {
            get => Decode(_elevation);
            init => _elevation = Encode(value);
        }

        /// <summary>
        /// For points of a line profile: distance from the first point of the line in meters
        /// (see <see cref="PointsExtensions.ComputeMetrics"/>), null otherwise.
        /// </summary>
        public double? DistanceFromOriginMeters
        {
            get => Decode(_distanceFromOrigin);
            init => _distanceFromOrigin = Encode(value);
        }

        public GeoPoint(double latitude, double longitude, double? altitude)
        {
            Latitude = latitude;
            Longitude = longitude;
            _elevation = Encode(altitude);
            _distanceFromOrigin = 0;
        }

        public GeoPoint(double latitude, double longitude) : this(latitude, longitude, null) { }

        public double DistanceSquaredTo(GeoPoint pt)
        {
            return (pt.Longitude - Longitude) * (pt.Longitude - Longitude)
                    + (pt.Latitude - Latitude) * (pt.Latitude - Latitude);
        }

        public static GeoPoint Zero => new GeoPoint(0d, 0d, 0d);
        public static GeoPoint UnitX => new GeoPoint(1d, 0d, 0d);
        public static GeoPoint UnitY => new GeoPoint(0d, 1d, 0d);
        public static GeoPoint UnitZ => new GeoPoint(0d, 0d, 1d);

        public static GeoPoint operator *(GeoPoint left, float right)
        {
            return new GeoPoint(left.Latitude * right, left.Longitude * right, left.Elevation.GetValueOrDefault(0) * right);
        }

        public override string ToString()
        {
            return $"Lat/Lon: {Latitude} / {Longitude} "
                + (Elevation.HasValue ? $", Elevation: {Elevation.Value:F2}" : "")
                + ((DistanceFromOriginMeters ?? 0) > 0 ? $", DistanceFromOrigin: {DistanceFromOriginMeters:F2}" : "");
        }

        /// <summary>Points are equal when they have the same location (elevation and distance are ignored)</summary>
        public bool Equals(GeoPoint other) => Latitude.Equals(other.Latitude) && Longitude.Equals(other.Longitude);

        public override bool Equals(object? obj) => obj is GeoPoint other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Latitude, Longitude);
    }
}
