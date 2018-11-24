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
            heightMap.Coordinates = heightMap.Coordinates.Select(pt => new GeoPoint(pt.Latitude - yOriginOffset, pt.Longitude - xOriginOffset, (float)pt.Elevation * zFactor, (int)pt.XIndex, (int)pt.YIndex));

            return heightMap;
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

    }
}
