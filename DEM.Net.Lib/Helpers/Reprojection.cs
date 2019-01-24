using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotSpatial.Projections;

namespace DEM.Net.Lib
{
    public static class Reprojection
    {

        public static HeightMap ReprojectToCartesian(this HeightMap heightMap)
        {
            // Defines the starting coordiante system
            double[] bboxCenter = heightMap.BoundingBox.Center;
            GeoPoint center = new GeoPoint(bboxCenter[1], bboxCenter[0]);

            heightMap.Coordinates = heightMap.Coordinates.ReprojectToCartesian(center);

            return heightMap;
        }
        private static IEnumerable<GeoPoint> ReprojectToCartesian(this IEnumerable<GeoPoint> points, GeoPoint center)
        {
            foreach (var p in points)
            {
                var pSameLat = new GeoPoint(center.Latitude, p.Longitude);
                var pSameLon = new GeoPoint(p.Latitude, center.Longitude);
                double xSign = p.Longitude < center.Longitude ? -1 : 1;
                double ySign = p.Latitude < center.Latitude ? -1 : 1;

                yield return new GeoPoint(pSameLon.DistanceTo(center) * ySign, pSameLat.DistanceTo(center) * xSign, (float)p.Elevation.GetValueOrDefault(0), p.XIndex, p.YIndex);
            }
        }
        private static GeoPoint GeodeticToGeocentric(this GeoPoint sourcePoint, GeocentricGeodetic geocentricGeodetic)
        {

            double[] coords = new double[] { MathHelper.ToRadians(sourcePoint.Longitude),
                                             MathHelper.ToRadians(sourcePoint.Latitude)};

            geocentricGeodetic.GeodeticToGeocentric(coords, new double[] { sourcePoint.Elevation.GetValueOrDefault(0) }, 0, 1);

            return new GeoPoint(coords[1], coords[0], (float)sourcePoint.Elevation, sourcePoint.XIndex.GetValueOrDefault(), sourcePoint.YIndex.GetValueOrDefault());
        }
        private static IEnumerable<GeoPoint> GeodeticToGeocentric(this IEnumerable<GeoPoint> sourcePoint, GeocentricGeodetic geocentricGeodetic)
        {

            return sourcePoint.Select(p => p.GeodeticToGeocentric(geocentricGeodetic));
        }
        public static HeightMap ReprojectTo(this HeightMap heightMap, int sourceEpsgCode, int destinationEpsgCode)
        {
            if (sourceEpsgCode == destinationEpsgCode)
                return heightMap;


            heightMap.Coordinates = heightMap.Coordinates.ReprojectTo(sourceEpsgCode, destinationEpsgCode);

            return heightMap;
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


        private static GeoPoint ReprojectPoint(GeoPoint sourcePoint, ProjectionInfo sourceProj, ProjectionInfo destProj)
        {

            double[] coords = new double[] { sourcePoint.Longitude, sourcePoint.Latitude };
            // Calls the reproject function that will transform the input location to the output locaiton
            Reproject.ReprojectPoints(coords, new double[] { sourcePoint.Elevation.GetValueOrDefault(0) }, sourceProj, destProj, 0, 1);

            return new GeoPoint(coords[1], coords[0], (float)sourcePoint.Elevation, sourcePoint.XIndex.GetValueOrDefault(), sourcePoint.YIndex.GetValueOrDefault());
        }


    }


}
