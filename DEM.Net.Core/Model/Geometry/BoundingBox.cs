// BoundingBox.cs
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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DEM.Net.Core.Gpx;

namespace DEM.Net.Core
{
    public class BoundingBox : IEquatable<BoundingBox>
    {
        public double xMin { get; set; }
        public double xMax { get; set; }
        public double yMin { get; set; }
        public double yMax { get; set; }
        public double zMin { get; set; }
        public double zMax { get; set; }

        public double Width
        {
            get
            {
                return xMax - xMin;
            }
        }

        public double Height
        {
            get
            {
                return yMax - yMin;
            }
        }

        public double Depth
        {
            get
            {
                return zMax - zMin;
            }
        }

        public BoundingBox() : this(double.MaxValue, double.MinValue
            , double.MaxValue, double.MinValue
            , 0, 0)
        {
        }
        public BoundingBox(double xmin, double xmax, double ymin, double ymax) : this(xmin, xmax, ymin, ymax, 0, 0)
        {
        }

        public BoundingBox(double xmin, double xmax, double ymin, double ymax, double zmin, double zmax)
        {
            xMin = xmin;
            xMax = xmax;
            yMin = ymin;
            yMax = ymax;
            zMin = zmin;
            zMax = zmax;
        }

        public bool IsValid()
        {
            return xMin < xMax
                    && yMin < yMax
                    && GpsLocation.IsValidLongitude(xMin)
                    && GpsLocation.IsValidLongitude(xMax)
                    && GpsLocation.IsValidLatitude(yMin)
                    && GpsLocation.IsValidLatitude(yMax);
        }
        public void UnionWith(double x, double y, double z)
        {
            xMin = Math.Min(xMin, x);
            xMax = Math.Max(xMax, x);
            yMin = Math.Min(yMin, y);
            yMax = Math.Max(yMax, y);
            zMin = Math.Min(zMin, z);
            zMax = Math.Max(zMax, z);
        }
        public void UnionWith(BoundingBox bbox)
        {
            xMin = Math.Min(xMin, bbox.xMin);
            xMax = Math.Max(xMax, bbox.xMax);
            yMin = Math.Min(yMin, bbox.yMin);
            yMax = Math.Max(yMax, bbox.yMax);
            zMin = Math.Min(zMin, bbox.zMin);
            zMax = Math.Max(zMax, bbox.zMax);
        }

        /// <summary>
        /// Reorders min / max and returns a new BoundingBox
        /// </summary>
        /// <returns></returns>
        public BoundingBox ReorderMinMax()
        {
            return new BoundingBox(Math.Min(xMin, xMax), Math.Max(xMin, xMax)
                , Math.Min(yMin, yMax), Math.Max(yMin, yMax)
                , Math.Min(zMin, zMax), Math.Max(zMin, zMax));
        }

        public override bool Equals(object obj)
        {
            BoundingBox objTyped = obj as BoundingBox;

            return this == objTyped;
        }
        public BoundingBox Scale(double scale)
        {
            return Scale(scale, scale);
        }
        public BoundingBox Scale(double scaleX, double scaleY)
        {
            return Scale(scaleX, scaleY, 1);
        }
        public BoundingBox Scale(double scaleX, double scaleY, double scaleZ)
        {
            return new BoundingBox(xMin - Width * scaleX, xMax + Width * scaleX
                , yMin - Height * scaleY, yMax + Height * scaleY
                , zMin - Depth * scaleZ, zMax + Depth * scaleZ)
            { SRID = this.SRID };
        }

        public BoundingBox Translate(double x, double y, double z)
        {
            return new BoundingBox(xMin + x, xMax + x
                , yMin + y, yMax + y
                , zMin + z, zMax + z)
            { SRID = this.SRID };
        }
        /// <summary>
        /// Add padding around bbox (in bbox units)
        /// </summary>
        /// <param name="padding"></param>
        /// <returns></returns>
        public BoundingBox Pad(double padding)
        {
            return Pad(padding, padding, padding);
        }
        /// <summary>
        /// Add padding around bbox (in bbox units)
        /// </summary>
        /// <param name="paddingX"></param>
        /// <param name="paddingY"></param>
        /// <param name="paddingZ"></param>
        /// <returns></returns>
        public BoundingBox Pad(double paddingX, double paddingY, double paddingZ)
        {
            return new BoundingBox(
                xMin - paddingX, xMax + paddingX,
                yMax + paddingY, yMin - paddingY,
                zMax + paddingZ, zMin - paddingZ)
            { SRID = this.SRID };
        }
        public BoundingBox ScaleAbsolute(double scaleX, double scaleY, double scaleZ = 1)
        {
            return new BoundingBox(xMin * scaleX, xMax * scaleX
                , yMin * scaleY, yMax * scaleY
                , zMin * scaleZ, zMax * scaleZ)
            { SRID = this.SRID };
        }

        public double[] Center
        {
            get
            {
                return new double[] {
                    (xMax - xMin) / 2d + xMin
                    , (yMax - yMin) / 2d + yMin
                    , (zMax - zMin) / 2d + zMin
                };
            }
        }

        public static bool Contains(BoundingBox bbox, double x, double y)
        {
            return bbox.xMin <= x && x <= bbox.xMax
                    && bbox.yMin <= y && y <= bbox.yMax;
        }
        public List<GeoPoint> AsPoints()
        {
            var pts = new List<GeoPoint>();
            pts.Add(new GeoPoint(yMax, xMin));
            pts.Add(new GeoPoint(yMax, xMax));
            pts.Add(new GeoPoint(yMin, xMax));
            pts.Add(new GeoPoint(yMin, xMin));
            pts.Add(new GeoPoint(yMax, xMin));
            return pts;
        }

        public static BoundingBox AroundPoint(double lat, double lon, double size)
        {
            return new BoundingBox(lon - size, lon + size, lat - size, lat + size, 0, 0);
        }
        public static BoundingBox AroundPoint(GeoPoint point, double size)
        {
            return BoundingBox.AroundPoint(point.Latitude, point.Longitude, size);
        }

        public override string ToString()
        {
            //  "Xmin: {xMin}, Xmax: {xMax}, Ymin: {yMin}, Ymax: {yMax}, Zmin: {zMin}, Zmax: {zMax}, Center: {this.Center[0]}, this.Center";
            return string.Format(CultureInfo.InvariantCulture, "Xmin: {0}, Xmax: {1}, Ymin: {2}, Ymax: {3}, Zmin: {4}, Zmax: {5}, Center: {6}, {7}, {8}"
                , xMin, xMax, yMin, yMax, zMin, zMax, Center[0], Center[1], Center[2]);
        }

        public bool Equals(BoundingBox other)
        {
            if (other == null) return false;
            return xMin == other.xMin
                    && xMax == other.xMax
                    && yMin == other.yMin
                    && yMax == other.yMax;
        }

        public override int GetHashCode()
        {
            var hashCode = -1820762126;
            hashCode = hashCode * -1521134295 + xMin.GetHashCode();
            hashCode = hashCode * -1521134295 + xMax.GetHashCode();
            hashCode = hashCode * -1521134295 + yMin.GetHashCode();
            hashCode = hashCode * -1521134295 + yMax.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(BoundingBox a, BoundingBox b)
        {
            if (Object.ReferenceEquals(a, null))
            {
                if (Object.ReferenceEquals(b, null))
                {
                    return true;
                }
                return false;
            }
            if (Object.ReferenceEquals(b, null))
            {
                return false;
            }

            return a.Equals(b);
        }
        public static bool operator !=(BoundingBox a, BoundingBox b)
        {
            return !(a == b);
        }
        public double Area
        {
            get
            {
                return (xMax - xMin) * (yMax - yMin);
            }
        }
        public string WKT
        {
            get
            {
                FormattableString fs = $"POLYGON(({xMin} {yMin}, {xMax} {yMin}, {xMax} {yMax}, {xMin} {yMax}, {xMin} {yMin}))";
                return fs.ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Spatial reference ID for the bounding box coordinates
        /// Useful when reprojecting / recentering model : the bbbox can be reprojected on the fly
        /// </summary>
        public int SRID { get; set; } = 4326;

    }
}
