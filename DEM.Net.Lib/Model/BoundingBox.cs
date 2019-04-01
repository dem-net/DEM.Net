using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib
{
    public class BoundingBox
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


        public BoundingBox Scale(double scale)
        {
            return Scale(scale, scale);
        }
        public BoundingBox Scale(double scaleX, double scaleY)
        {
            return new BoundingBox(xMax - Width * scaleX, xMin + Width * scaleX, yMax - Height * scaleY, yMin + Height * scaleY);
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

        public override string ToString()
        {
            return $"Xmin: {xMin}, Xmax: {xMax}, Ymin: {yMin}, Ymax: {yMax}";
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
