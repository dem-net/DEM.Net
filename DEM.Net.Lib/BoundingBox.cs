using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib
{
    public struct BoundingBox
    {
        private double _xMin;
        public double XMin
        {
            get { return _xMin; }
            set { _xMin = value; }
        }

        private double _xMax;
        public double XMax
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
            get {
                return _xMax - _xMin;
            }
        }

        public double Height
        {
            get {
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
            return new BoundingBox(XMin * scale, XMax * scale, yMin * scale, yMax * scale);
        }

        public BoundingBox Transform(System.Drawing.Drawing2D.Matrix matrix)
        {
            System.Drawing.PointF[] points = new System.Drawing.PointF[2];
            points[0] = new System.Drawing.PointF((float)XMin, (float)yMin);
            points[1] = new System.Drawing.PointF((float)XMax, (float)yMax);
            matrix.TransformPoints(points);
            return new BoundingBox(points[0].X, points[1].X, points[0].Y, points[1].Y);
        }

        public override string ToString()
        {
            return $"Xmin: {XMin}, Xmax: {XMax}, Ymin: {yMin}, Ymax: {yMax}";
        }
    }
}
