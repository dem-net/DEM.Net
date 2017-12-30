using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Types;

namespace DEM.Net.Lib.Services
{
    public static class GeometryService
    {
        private const int WGS84_SRID = 4326;
        private const double EARTH_RADIUS = 6371008.0; // [m]
        private const double RADIAN = Math.PI / 180;

        public static SqlGeometry ParseWKTAsGeometry(string geomWKT)
        {
            SqlGeometry geom = SqlGeometry.STGeomFromText(new System.Data.SqlTypes.SqlChars(new System.Data.SqlTypes.SqlString(geomWKT)), WGS84_SRID);
			geom = geom.MakeValidIfInvalid();
			return geom;
        }

        public static SqlGeography ParseWKTAsGeography(string geomWKT)
        {
            SqlGeography geom = SqlGeography.STGeomFromText(new System.Data.SqlTypes.SqlChars(new System.Data.SqlTypes.SqlString(geomWKT)), WGS84_SRID);
            return geom;
        }

        public static BoundingBox GetBoundingBox(string geomWKT)
        {
            SqlGeometry geom = ParseWKTAsGeometry(geomWKT);
            return geom.GetBoundingBox();
        }

		public static SqlGeometry ParseGeoPointAsGeometryLine(IEnumerable<GeoPoint> points)
		{
			SqlGeometryBuilder gb = new SqlGeometryBuilder();
			gb.SetSrid(4326);
			gb.BeginGeometry(OpenGisGeometryType.LineString);
			bool first = true;
			foreach(var pt in points)
			{
				if (first)
				{
					gb.BeginFigure(pt.Longitude, pt.Latitude);
					first = false;
				}
				else
				{
					gb.AddLine(pt.Longitude, pt.Latitude);
				}
			}
			gb.EndFigure();
			gb.EndGeometry();
			return gb.ConstructedGeometry;
		}

        
        //Check if the lines are interescting in 2d space
        //Alternative version from http://thirdpartyninjas.com/blog/2008/10/07/line-segment-intersection/
        public static bool LineLineIntersection(out GeoPoint intersection, GeoSegment line1, GeoSegment line2)
        {
            bool isIntersecting = false;
            intersection = GeoPoint.Zero;

            //3d -> 2d
            double p1_x = line1.Start.Longitude;
            double p1_y = line1.Start.Latitude;
            double p2_x = line1.End.Longitude;
            double p2_y = line1.End.Latitude;
            double p3_x = line2.Start.Longitude;
            double p3_y = line2.Start.Latitude;
            double p4_x = line2.End.Longitude;
            double p4_y = line2.End.Latitude;
       

            double denominator = (p4_y - p3_y) * (p2_x - p1_x) - (p4_x - p3_x) * (p2_y - p1_y);

            //Make sure the denominator is > 0, if so the lines are parallel
            if (denominator != 0)
            {
                double u_a = ((p4_x - p3_x) * (p1_y - p3_y) - (p4_y - p3_y) * (p1_x - p3_x)) / denominator;
                double u_b = ((p2_x - p1_x) * (p1_y - p3_y) - (p2_y - p1_y) * (p1_x - p3_x)) / denominator;

                //Is intersecting if u_a and u_b are between 0 and 1
                if (u_a >= 0 && u_a <= 1 && u_b >= 0 && u_b <= 1)
                {
                    intersection = new GeoPoint(p1_y + u_a * (p2_y - p1_y), p1_x + u_a * (p2_x - p1_x));
                    isIntersecting = true;
                }
            }

            return isIntersecting;
        }

        public static ElevationMetrics ComputeMetrics(ref List<GeoPoint> points)
        {
			ElevationMetrics metrics = new ElevationMetrics();
            double total = 0;
			double minElevation = double.MaxValue;
			double maxElevation = double.MinValue;
			double totalClimb = 0;
			double totalDescent = 0;
            if (points.Count > 1)
            {
				double lastElevation = points[0].Elevation.GetValueOrDefault(0);
                for (int i = 1; i < points.Count; i++)
                {
					GeoPoint curPoint = points[i];
                    double v_dist = GetDistanceBetweenPoints(curPoint, points[i - 1]);
                    total += v_dist;
					curPoint.DistanceFromOriginMeters = total;

					minElevation = Math.Min(minElevation, curPoint.Elevation.GetValueOrDefault(double.MaxValue));
					maxElevation = Math.Max(maxElevation, curPoint.Elevation.GetValueOrDefault(double.MinValue));

					double currentElevation = curPoint.Elevation.GetValueOrDefault(lastElevation);
					double diff = currentElevation - lastElevation;
					if (diff>0)
					{
						totalClimb += diff;
					}
					else
					{
						totalDescent += diff;
					}
					lastElevation = currentElevation;

				}
			}
			metrics.Climb = totalClimb;
			metrics.Descent = totalDescent;
			metrics.NumPoints = points.Count;
			metrics.Distance = total;
			metrics.MinElevation = minElevation;
			metrics.MaxElevation = maxElevation;

			return metrics;
        }

        public static double GetLength(string lineWKT)
        {
            return GeometryService.ParseWKTAsGeography(lineWKT).STLength().Value;
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
