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
        private double _xMin;
        public double xMin
        {
            get { return _xMin; }
            set { _xMin = value; }
        }

        private double _xMax;
        public double xMax
        {
            get { return _xMax; }
            set { _xMax = value; }
        }

        private double _yMin;
        public double yMin
        {
            get { return _yMin; }
            set { _yMin = value; }
        }

        private double _yMax;
        public double yMax
        {
            get { return _yMax; }
            set { _yMax = value; }
        }

        private double _zMin;
        public double zMin
        {
            get { return _zMin; }
            set { _zMin = value; }
        }

        private double _zMax;
        public double zMax
        {
            get { return _zMax; }
            set { _zMax = value; }
        }

        public double Width
        {
            get
            {
                return _xMax - _xMin;
            }
        }

        public double Height
        {
            get
            {
                return _yMax - _yMin;
            }
        }

        public double Depth
        {
            get
            {
                return _zMax - _zMin;
            }
        }

        public BoundingBox() : this(double.MaxValue, double.MinValue
            , double.MaxValue, double.MinValue
            , double.MaxValue, double.MinValue)
        {
        }
        public BoundingBox(double xmin, double xmax, double ymin, double ymax) : this(xmin, xmax, ymin, ymax, double.MaxValue, double.MinValue)
        {
        }

        public BoundingBox(double xmin, double xmax, double ymin, double ymax, double zmin, double zmax)
        {
            _xMin = xmin;
            _xMax = xmax;
            _yMin = ymin;
            _yMax = ymax;
            _zMin = zmin;
            _zMax = zmax;
        }

        public bool IsValid()
        {
            return _xMin < _xMax
                    && _yMin < _yMax
                    && GpsLocation.IsValidLongitude(_xMin)
                    && GpsLocation.IsValidLongitude(_xMax)
                    && GpsLocation.IsValidLatitude(_yMin)
                    && GpsLocation.IsValidLatitude(_yMax);
        }
        public void UnionWith(double x, double y, double z)
        {
            _xMin = Math.Min(_xMin, x);
            _xMax = Math.Max(_xMax, x);
            _yMin = Math.Min(_yMin, y);
            _yMax = Math.Max(_yMax, y);
            _zMin = Math.Min(_zMin, z);
            _zMax = Math.Max(_zMax, z);
        }

        /// <summary>
        /// Reorders min / max and returns a new BoundingBox
        /// </summary>
        /// <returns></returns>
        public BoundingBox ReorderMinMax()
        {
            return new BoundingBox(Math.Min(_xMin, _xMax), Math.Max(_xMin, _xMax)
                , Math.Min(_yMin, _yMax), Math.Max(_yMin, _yMax)
                , Math.Min(_zMin, _zMax), Math.Max(_zMin, _zMax));
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
                , zMin - Depth * scaleZ, zMax + Depth * scaleZ);
        }
        /// <summary>
        /// Add padding around bbox (bbox must be projected to cartesian first)
        /// </summary>
        /// <param name="paddingMeters"></param>
        /// <returns></returns>
        public BoundingBox Pad(double paddingMeters)
        {
            return new BoundingBox(xMin - paddingMeters, xMax + paddingMeters
                , yMax + paddingMeters, yMin - paddingMeters
                , zMax + paddingMeters, zMin - paddingMeters);
        }
        public BoundingBox ScaleAbsolute(double scaleX, double scaleY, double scaleZ = 1)
        {
            return new BoundingBox(xMin * scaleX, xMax * scaleX
                , yMin * scaleY, yMax * scaleY
                , zMin * scaleZ, zMax * scaleZ);
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
            return new BoundingBox(lon - size, lon + size, lat - size, lat + size);
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
            return _xMin == other.xMin
                    && _xMax == other.xMax
                    && _yMin == other.yMin
                    && _yMax == other.yMax;
        }

        public override int GetHashCode()
        {
            var hashCode = -1820762126;
            hashCode = hashCode * -1521134295 + _xMin.GetHashCode();
            hashCode = hashCode * -1521134295 + _xMax.GetHashCode();
            hashCode = hashCode * -1521134295 + _yMin.GetHashCode();
            hashCode = hashCode * -1521134295 + _yMax.GetHashCode();
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
    }
}
