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
    }
}
