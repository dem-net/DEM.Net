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
	}
}
