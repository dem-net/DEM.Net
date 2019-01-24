using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib
{
    public static class HeightMapExtensions
    {
        public static HeightMap CenterOnOrigin(this HeightMap heightMap, float zFactor = 1f)
        {
            var bbox = heightMap.BoundingBox;

            double xOriginOffset = bbox.xMax - (bbox.xMax - bbox.xMin) / 2d;
            double yOriginOffset = bbox.yMax - (bbox.yMax - bbox.yMin) / 2d;
            heightMap.Coordinates = heightMap.Coordinates.Select(pt => new GeoPoint(pt.Latitude - yOriginOffset, pt.Longitude - xOriginOffset, (float)pt.Elevation.GetValueOrDefault(0) * zFactor, pt.XIndex, pt.YIndex));

            return heightMap;
        }
        public static IEnumerable<GeoPoint> CenterOnOrigin(this IEnumerable<GeoPoint> points, float zFactor = 1f)
        {
            var bbox = points.GetBoundingBox();

            return points.CenterOnOrigin(bbox, zFactor);
        }
        public static IEnumerable<GeoPoint> CenterOnOrigin(this IEnumerable<GeoPoint> points, BoundingBox bbox, float zFactor = 1f)
        {
            double xOriginOffset = bbox.xMax - (bbox.xMax - bbox.xMin) / 2d;
            double yOriginOffset = bbox.yMax - (bbox.yMax - bbox.yMin) / 2d;
            points = points.Select(pt => new GeoPoint(pt.Latitude - yOriginOffset, pt.Longitude - xOriginOffset, (float)pt.Elevation * zFactor, (int)pt.XIndex, (int)pt.YIndex));

            return points;
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
            hMap.Mininum = heightMap.Mininum;
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
