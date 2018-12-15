using System;
using DEM.Net.Lib;
using Microsoft.SqlServer.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DEM.Net.Test
{
    [TestClass]
    public class MiscTests
    {
        [TestInitialize()]
        public void Initialize()
        {
          
            SqlServerTypes.Utilities.LoadNativeAssemblies(AppDomain.CurrentDomain.BaseDirectory);
        }

        [TestMethod]
        [TestCategory("Misc / Geometry")]
        public void LineLineIntersectionTest()
        {
            string wkt1 = "LINESTRING(-5.888671875 47.90161354142077,3.4716796875 44.11914151643737)";
            string wkt2 = "LINESTRING(-2.8564453125 44.30812668488613,5.625 48.166085419012525)";
            SqlGeometry geom1 = GeometryService.ParseWKTAsGeometry(wkt1);
            SqlGeometry geom2 = GeometryService.ParseWKTAsGeometry(wkt2);
            SqlGeometry intersection = geom1.STIntersection(geom2);

            GeoSegment seg1 = new GeoSegment(new GeoPoint(geom1.STStartPoint().STY.Value, geom1.STStartPoint().STX.Value), new GeoPoint(geom1.STEndPoint().STY.Value, geom1.STEndPoint().STX.Value));
            GeoSegment seg2 = new GeoSegment(new GeoPoint(geom2.STStartPoint().STY.Value, geom2.STStartPoint().STX.Value), new GeoPoint(geom2.STEndPoint().STY.Value, geom2.STEndPoint().STX.Value));
            GeoPoint intersectionResult = GeoPoint.Zero;

            bool intersects = GeometryService.LineLineIntersection(out intersectionResult, seg1, seg2);


            SqlGeography geog1 = null;
            intersection.TryToGeography(out geog1);
            SqlGeography geog2 = SqlGeography.Point(intersectionResult.Latitude, intersectionResult.Longitude, 4326);
            double dist = geog1.STDistance(geog2).Value;

            Assert.IsTrue(dist < 0.05d, "Problem in intersection calculation.");
        }
    }
}
