using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DEM.Net.Lib.Imagery;
using DotSpatial.Projections;

namespace DEM.Net.Lib
{
    public static class Reprojection
    {

        public const int SRID_GEODETIC = 4236;
        public const int SRID_PROJECTED_LAMBERT_93 = 2154;
        public const int SRID_PROJECTED_MERCATOR = 3857;

        public static HeightMap ReprojectGeodeticToCartesian(this HeightMap heightMap)
        {
            heightMap.Coordinates = heightMap.Coordinates.ReprojectTo(SRID_GEODETIC, SRID_PROJECTED_MERCATOR);
            heightMap.BoundingBox = heightMap.BoundingBox.ReprojectTo(SRID_GEODETIC, SRID_PROJECTED_MERCATOR);
            return heightMap;
        }

        public static HeightMap ReprojectTo(this HeightMap heightMap, int sourceEpsgCode, int destinationEpsgCode)
        {
            if (sourceEpsgCode == destinationEpsgCode)
                return heightMap;

            heightMap.Coordinates = heightMap.Coordinates.ReprojectTo(sourceEpsgCode, destinationEpsgCode);
            heightMap.BoundingBox = heightMap.BoundingBox.ReprojectTo(sourceEpsgCode, destinationEpsgCode);

            return heightMap;
        }
       
        public static IEnumerable<GeoPoint> ReprojectGeodeticToCartesian(this IEnumerable<GeoPoint> points)
        {
            return points.ReprojectTo(SRID_GEODETIC, SRID_PROJECTED_MERCATOR);
        }

        public static IEnumerable<GeoPoint> ReprojectTo(this IEnumerable<GeoPoint> points, int sourceEpsgCode, int destinationEpsgCode)
        {
            if (sourceEpsgCode == destinationEpsgCode)
                return points;


            // Defines the starting coordiante system
            ProjectionInfo pSource = ProjectionInfo.FromEpsgCode(sourceEpsgCode);
            // Defines the starting coordiante system
            ProjectionInfo pTarget = ProjectionInfo.FromEpsgCode(destinationEpsgCode);

            return points.Select(pt => ReprojectPoint(pt, pSource, pTarget));

        }
        public static BoundingBox ReprojectTo(this BoundingBox bbox, int sourceEpsgCode, int destinationEpsgCode)
        {
            if (sourceEpsgCode == destinationEpsgCode)
                return bbox;


            // Defines the starting coordiante system
            ProjectionInfo pSource = ProjectionInfo.FromEpsgCode(sourceEpsgCode);
            // Defines the starting coordiante system
            ProjectionInfo pTarget = ProjectionInfo.FromEpsgCode(destinationEpsgCode);

            var minmin = ReprojectPoint(new GeoPoint(bbox.xMin, bbox.yMin), pSource, pTarget);
            var minmax = ReprojectPoint(new GeoPoint(bbox.xMin, bbox.yMax), pSource, pTarget);
            var maxmax = ReprojectPoint(new GeoPoint(bbox.xMax, bbox.yMax), pSource, pTarget);
            var maxmin = ReprojectPoint(new GeoPoint(bbox.xMax, bbox.yMin), pSource, pTarget);

            return GeometryService.GetBoundingBox(new GeoPoint[] { minmin, minmax, maxmax, maxmin });

        }


        private static GeoPoint ReprojectPoint(GeoPoint sourcePoint, ProjectionInfo sourceProj, ProjectionInfo destProj)
        {

            double[] coords = new double[] { sourcePoint.Longitude, sourcePoint.Latitude };
            // Calls the reproject function that will transform the input location to the output locaiton
            Reproject.ReprojectPoints(coords, new double[] { sourcePoint.Elevation.GetValueOrDefault(0) }, sourceProj, destProj, 0, 1);

            return new GeoPoint(coords[1], coords[0], sourcePoint.Elevation, sourcePoint.XIndex.GetValueOrDefault(), sourcePoint.YIndex.GetValueOrDefault());
        }


    }


}
