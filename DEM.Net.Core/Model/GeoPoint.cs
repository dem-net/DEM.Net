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

using DEM.Net.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeroFormatter;

namespace DEM.Net.Core
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

        public GeoPoint(double latitude, double longitude, double? altitude, int? indexX, int? indexY)
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
            return new GeoPoint
            {
                DistanceFromOriginMeters = this.DistanceFromOriginMeters
                ,
                Elevation = this.Elevation
                ,
                Latitude = this.Latitude
                ,
                Longitude = this.Longitude
                ,
                TileId = this.TileId
                ,
                XIndex = this.XIndex
                ,
                YIndex = this.YIndex
            };

        }

        public double DistanceSquaredTo(GeoPoint pt)
        {
            return (pt.Longitude - Longitude) * (pt.Longitude - Longitude)
                    + (pt.Latitude - Latitude) * (pt.Latitude - Latitude);
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
