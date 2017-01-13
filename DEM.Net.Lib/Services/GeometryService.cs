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

		//Calculate the intersection point of two lines. Returns true if lines intersect, otherwise false.
		//Note that in 3d, two lines do not intersect most of the time. So if the two lines are not in the 
		//same plane, use ClosestPointsOnTwoLines() instead.
		public static bool LineLineIntersection(out Vector2 intersection, Vector2 linePoint1, Vector2 lineVec1, Vector2 linePoint2, Vector2 lineVec2)
		{

			Vector2 lineVec3 = linePoint2 - linePoint1;
			Vector2 crossVec1and2 = lineVec1 * lineVec2;
			Vector2 crossVec3and2 = lineVec3 * lineVec2;

			float planarFactor = Vector2.Dot(lineVec3, crossVec1and2);

			//is coplanar, and not parrallel
			if (Math.Abs(planarFactor) < 0.0001f && crossVec1and2.LengthSquared() > 0.0001f)
			{
				float s = Vector2.Dot(crossVec3and2, crossVec1and2) / crossVec1and2.LengthSquared();
				intersection = linePoint1 + (lineVec1 * s);
				return true;
			}
			else
			{
				intersection = Vector2.Zero;
				return false;
			}
		}

	}
}
