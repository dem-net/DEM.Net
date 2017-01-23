using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Types;
using System.Numerics;

namespace DEM.Net.Lib.Services
{
    public static class GeometryService
    {
        private const int WGS84_SRID = 4326;
        private const double EARTH_RADIUS = 6371008.0; // [m]
        private const double RADIAN = Math.PI / 180;

        public static SqlGeometry GetNativeGeometry(string geomWKT)
        {
            SqlGeometry geom = SqlGeometry.STGeomFromText(new System.Data.SqlTypes.SqlChars(new System.Data.SqlTypes.SqlString(geomWKT)), WGS84_SRID);
            return geom;
        }

        public static SqlGeography GetNativeGeography(string geomWKT)
        {
            SqlGeography geom = SqlGeography.STGeomFromText(new System.Data.SqlTypes.SqlChars(new System.Data.SqlTypes.SqlString(geomWKT)), WGS84_SRID);
            return geom;
        }

        public static BoundingBox GetBoundingBox(string geomWKT)
        {
            SqlGeometry geom = GetNativeGeometry(geomWKT);
            return geom.GetBoundingBox();
        }

        public static bool LineLineIntersection(out GeoPoint intersection, GeoSegment line1, GeoSegment line2)
        {
            const float Z_FIXED = 0f;
            Vector3 linePoint1V3 = new Vector3((float)line1.Start.Longitude, (float)line1.Start.Latitude, Z_FIXED);
            Vector3 linePoint2V3 = new Vector3((float)line2.Start.Longitude, (float)line2.Start.Latitude, Z_FIXED);

            Vector3 lineVec1V3 = new Vector3((float)(line1.End.Longitude - line1.Start.Longitude), (float)(line1.End.Latitude - line1.Start.Latitude), Z_FIXED);
            Vector3 lineVec2V3 = new Vector3((float)(line2.End.Longitude - line2.Start.Longitude), (float)(line2.End.Latitude - line2.Start.Latitude), Z_FIXED);

            Vector3 intersectionV3 = Vector3.Zero;
            if (LineLineIntersection_Internal(out intersectionV3, linePoint1V3, lineVec1V3, linePoint2V3, lineVec2V3))
            {
                intersection = new GeoPoint(intersectionV3.Y, intersectionV3.X);
                return true;
            }
            else
            {
                intersection = GeoPoint.Zero;
                return false;
            }
        }

        public static List<GeoPoint> ComputePointsDistances(List<GeoPoint> points)
        {
            double total = 0;
            if (points.Count > 1)
            {
                for (int i = 1; i < points.Count; i++)
                {
                    double v_dist = GetDistanceBetweenPoints(points[i], points[i - 1]);
                    total += v_dist;
                    points[i].DistanceFromOriginMeters = total;
                }
            }
            return points;
        }

        //Calculate the intersection point of two lines. Returns true if lines intersect, otherwise false.
        private static bool LineLineIntersection_Internal(out Vector3 intersection, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
        {

            Vector3 lineVec3 = linePoint2 - linePoint1;
            Vector3 crossVec1and2 = Vector3.Cross(lineVec1, lineVec2);
            Vector3 crossVec3and2 = Vector3.Cross(lineVec3, lineVec2);

            float planarFactor = Vector3.Dot(lineVec3, crossVec1and2);

            //is coplanar, and not parrallel
            if (Math.Abs(planarFactor) < 0.0001f && crossVec1and2.LengthSquared() > 0.0001f)
            {
                float s = Vector3.Dot(crossVec3and2, crossVec1and2) / crossVec1and2.LengthSquared();
                intersection = linePoint1 + (lineVec1 * s);
                return true;
            }
            else
            {
                intersection = Vector3.Zero;
                return false;
            }
        }

        public static double GetLength(string lineWKT)
        {
            return GeometryService.GetNativeGeography(lineWKT).STLength().Value;
        }

        public static double GetDistanceBetweenPoints(GeoPoint pt1, GeoPoint pt2)
        {
            if ((pt1 == null) || (pt2 == null))
                return 0;
            else
            {
                double v_thisLatitude = pt1.Latitude;
                double v_otherLatitude = pt2.Latitude;
                double v_thisLongitude = pt1.Longitude;
                double v_otherLongitude = pt2.Longitude;

                double v_deltaLatitude = Math.Abs(pt1.Latitude - pt2.Latitude);
                double v_deltaLongitude = Math.Abs(pt1.Longitude - pt2.Longitude);

                if (v_deltaLatitude == 0 && v_deltaLongitude == 0)
                    return 0;

                v_thisLatitude *= RADIAN;
                v_otherLatitude *= RADIAN;
                v_deltaLongitude *= RADIAN;

                double v_cos = Math.Cos(v_deltaLongitude) * Math.Cos(v_thisLatitude) * Math.Cos(v_otherLatitude) +
                                             Math.Sin(v_thisLatitude) * Math.Sin(v_otherLatitude);

                double v_ret = EARTH_RADIUS * Math.Acos(v_cos);
                if (double.IsNaN(v_ret)) // points nearly the same
                {
                    v_ret = 0d;
                }
                return v_ret;
            }
        }

        public static double GetLineLength_Meters(List<GeoPoint> points)
        {
            double total = 0;
            try
            {
                if (points.Count > 1)
                {
                    for (int v_i = 1; v_i < points.Count; v_i++)
                    {
                        double v_dist = GetDistanceBetweenPoints(points[v_i], points[v_i - 1]);
                        total += v_dist;
                    }
                }
            }
            catch
            {
                throw;
            }
            return total;
        }


    }
}
