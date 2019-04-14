using System;
using System.Linq;
using DEM.Net.Core;
using GeoAPI.Geometries;
using Xunit;

namespace DEM.Net.Test
{
    
    public class MiscTests
    {

        [Fact]
        public void LineLineIntersectionTest()
        {
            string wkt1 = "LINESTRING(-5.888671875 47.90161354142077,3.4716796875 44.11914151643737)";
            string wkt2 = "LINESTRING(-2.8564453125 44.30812668488613,5.625 48.166085419012525)";
            IGeometry geom1 = GeometryService.ParseWKTAsGeometry(wkt1);
            IGeometry geom2 = GeometryService.ParseWKTAsGeometry(wkt2);


            IGeometry intersection = geom1.Intersection(geom2);

            GeoSegment seg1 = geom1.Segments().First();
            GeoSegment seg2 = geom2.Segments().First();
            GeoPoint intersectionResult = GeoPoint.Zero;

            bool intersects = GeometryService.LineLineIntersection(out intersectionResult, seg1, seg2);


            double dist = intersection.Coordinate.ToGeoPoint().DistanceTo(intersectionResult);
            
        
            Assert.True(dist < 0.05d, "Problem in intersection calculation.");
        }
    }
}
