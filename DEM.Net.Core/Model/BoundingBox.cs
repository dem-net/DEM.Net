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

        public BoundingBox(double xmin, double xmax, double ymin, double ymax)
        {
            _xMin = xmin;
            _xMax = xmax;
            _yMin = ymin;
            _yMax = ymax;
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

        /// <summary>
        /// Reorders min / max and returns a new BoundingBox
        /// </summary>
        /// <returns></returns>
        public BoundingBox ReorderMinMax()
        {
            return new BoundingBox(Math.Min(_xMin, _xMax), Math.Max(_xMin, _xMax), Math.Min(_yMin, _yMax), Math.Max(_yMin, _yMax));
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
            return new BoundingBox(xMin - Width * scaleX, xMax + Width * scaleX, yMax - Height * scaleY, yMin + Height * scaleY);
        }
        /// <summary>
        /// Add padding around bbox (bbox must be projected to cartesian first)
        /// </summary>
        /// <param name="paddingMeters"></param>
        /// <returns></returns>
        public BoundingBox Pad(double paddingMeters)
        {
            return new BoundingBox(xMin - paddingMeters, xMax + paddingMeters, yMax + paddingMeters, yMin - paddingMeters);
        }
        public BoundingBox ScaleAbsolute(double scaleX, double scaleY)
        {
            return new BoundingBox(xMin * scaleX, xMax * scaleX, yMin * scaleY, yMax * scaleY);
        }

        public double[] Center
        {
            get
            {
                return new double[] { (xMax - xMin) / 2d + xMin, (yMax - yMin) / 2 + yMin };
            }
        }

        public static bool Contains(BoundingBox bbox, double x, double y)
        {
            return bbox.xMin <= x && x <= bbox.xMax
                    && bbox.yMin <= y && y <= bbox.yMax;
        }

        public static BoundingBox AroundPoint(double lat, double lon, double size)
        {
            return new BoundingBox(lon - size, lon + size, lat - size, lat + size);
        }

        public override string ToString()
        {
            return $"Xmin: {xMin}, Xmax: {xMax}, Ymin: {yMin}, Ymax: {yMax}";
        }

        public bool Equals(BoundingBox other)
        {
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
