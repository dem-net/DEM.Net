// HeightMapExtensions.cs
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core
{
    public static class HeightMapExtensions
    {
        public static HeightMap CenterOnOrigin(this HeightMap heightMap)
        {
            Logger.Info("CenterOnOrigin...");
            var bbox = heightMap.BoundingBox;

            double xOriginOffset = bbox.xMax - (bbox.xMax - bbox.xMin) / 2d;
            double yOriginOffset = bbox.yMax - (bbox.yMax - bbox.yMin) / 2d;
            heightMap.Coordinates = heightMap.Coordinates.Offset(xOriginOffset, yOriginOffset);

            heightMap.BoundingBox = new BoundingBox(bbox.xMin - xOriginOffset, bbox.xMax - xOriginOffset
                                                    , bbox.yMin - yOriginOffset, bbox.yMax - yOriginOffset);
            return heightMap;
        }

        public static IEnumerable<GeoPoint> CenterOnOrigin(this IEnumerable<GeoPoint> points)
        {
            Logger.Info("CenterOnOrigin...");
            var bbox = points.GetBoundingBox();

            return points.CenterOnOrigin(bbox);
        }
        public static IEnumerable<GeoPoint> CenterOnOrigin(this IEnumerable<GeoPoint> points, BoundingBox bbox)
        {
            Logger.Info("CenterOnOrigin...");
            double xOriginOffset = bbox.xMax - (bbox.xMax - bbox.xMin) / 2d;
            double yOriginOffset = bbox.yMax - (bbox.yMax - bbox.yMin) / 2d;
            points = points.Offset(xOriginOffset, yOriginOffset);

            return points;
        }
        private static IEnumerable<GeoPoint> Offset(this IEnumerable<GeoPoint> points, double x, double y)
        {
            Logger.Info("Offset...");
            foreach (var pt in points)
            {
                var p = pt.Clone();
                p.Latitude -= y;
                p.Longitude -= x;
                yield return p;
            }
            Logger.Info("Offset done...");
        }

        /// <summary>
        /// Helper to get an in memory coordinate list
        /// useful to generate normal maps and let the same height map follow the pipeline (reproj, center, ...)
        /// </summary>
        /// <returns></returns>
        public static HeightMap BakeCoordinates(this HeightMap heightMap)
        {
            heightMap.Coordinates = heightMap.Coordinates.ToList();

            return heightMap;
        }

        public static HeightMap ZScale(this HeightMap heightMap, float zFactor = 1f)
        {
            heightMap.Coordinates = heightMap.Coordinates.ZScale(zFactor);
            heightMap.Minimum *= zFactor;
            heightMap.Maximum *= zFactor;

            return heightMap;
        }
        public static HeightMap Scale(this HeightMap heightMap, float x = 1f, float y = 1f, float z = 1f)
        {
            heightMap.Coordinates = heightMap.Coordinates.Scale(x, y, z);
            heightMap.BoundingBox = heightMap.BoundingBox.Scale(x, y); // z does not affect bbox
            heightMap.Minimum *= z;
            heightMap.Maximum *= z;

            return heightMap;
        }
        public static HeightMap FitInto(this HeightMap heightMap, float maxSize)
        {
            float scale = 1f;
            if (heightMap.BoundingBox.Width > heightMap.BoundingBox.Height)
            {
                scale = (float)(maxSize / heightMap.BoundingBox.Width);
            }
            else
            {
                scale = (float)(maxSize / heightMap.BoundingBox.Height);
            }
            heightMap.Coordinates = heightMap.Coordinates.Scale(scale, scale, scale);
            heightMap.BoundingBox = heightMap.BoundingBox.ScaleAbsolute(scale, scale);
            return heightMap;
        }
        public static IEnumerable<GeoPoint> ZScale(this IEnumerable<GeoPoint> points, float zFactor = 1f)
        {
            return points.Scale(1, 1, zFactor);
        }
        public static IEnumerable<GeoPoint> Scale(this IEnumerable<GeoPoint> points, float x = 1f, float y = 1f, float z = 1f)
        {
            Logger.Info("Scale...");
            foreach (var pt in points)
            {
                var pout = pt.Clone();
                pout.Longitude *= x;
                pout.Latitude *= y;
                pout.Elevation *= z;
                yield return pout;
            }
            Logger.Info("Scale done...");

        }
        public static HeightMap ZTranslate(this HeightMap heightMap, float distance)
        {
            heightMap.Coordinates = heightMap.Coordinates.ZTranslate(distance);
            heightMap.Minimum += distance;
            heightMap.Maximum += distance;

            return heightMap;
        }
        public static IEnumerable<GeoPoint> ZTranslate(this IEnumerable<GeoPoint> points, float distance)
        {
            Logger.Info("ZTranslate...");
            foreach (var pt in points)
            {
                var pout = pt.Clone();
                pout.Elevation += distance;
                yield return pout;
            }
            Logger.Info("ZTranslate done...");

        }

        public static HeightMap Sort(this HeightMap heightMap)
        {
            heightMap.Coordinates = heightMap.Coordinates.Sort();

            return heightMap;
        }
        public static IEnumerable<GeoPoint> Sort(this IEnumerable<GeoPoint> coords)
        {
            coords = coords.OrderByDescending(pt => pt.Latitude)
                           .ThenBy(pt => pt.Longitude);

            return coords;
        }

        public static HeightMap Downsample(this HeightMap heightMap, int step)
        {
            if (step == 0 || step % 2 != 0)
                throw new ArgumentOutOfRangeException("step", "Step must be a factor of 2");

            HeightMap hMap = new HeightMap(heightMap.Width / step, heightMap.Height / step);
            hMap.Maximum = heightMap.Maximum;
            hMap.Minimum = heightMap.Minimum;
            hMap.BoundingBox = heightMap.BoundingBox;
            hMap.Coordinates = DownsampleCoordinates(heightMap.Coordinates.ToList(), heightMap.Width, heightMap.Height, step).ToList();

            return hMap;
        }

        private static IEnumerable<GeoPoint> DownsampleCoordinates(List<GeoPoint> input, int w, int h, int step)
        {
            for (int lat = 0; lat <= h; lat += step)
            {
                for (int lon = 0; lon <= w; lon += step)
                {
                    yield return input[lon + lat * h];
                }
            }
        }

    }
}
