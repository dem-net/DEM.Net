using System;
using System.Linq;
using DEM.Net.Core;
using NetTopologySuite.Geometries;
using Xunit;

namespace DEM.Net.Test
{
    public class GeometryTests
    {
        GeoPoint[] _linePoints;

        public GeometryTests()
        {
            _linePoints = new GeoPoint[] {
                                        new GeoPoint(43.429923, 0.517782),
                                          new GeoPoint(43.093901, 0.451817),
                                          new GeoPoint(42.610654, 0.27591),
                                          new GeoPoint(42.237605, 0.165969),
                                          new GeoPoint(41.796848, -0.053915)
                                        };

        }

        [Fact]
        public void SegmentsTest()
        {


            var lineGeom = GeometryService.ParseGeoPointAsGeometryLine(_linePoints);

            Assert.NotNull(lineGeom);
            Assert.True(lineGeom.Coordinates.Length == _linePoints.Length, "Coordinate count mismatch");

            var segments = GeometryService.Segments(lineGeom);
            Assert.True(segments.Count() == lineGeom.Coordinates.Length - 1, "Segment enumeration failed");
        }

        [Fact]
        public void LineLineIntersectionTest()
        {
            string wkt1 = "LINESTRING(-5.888671875 47.90161354142077,3.4716796875 44.11914151643737)";
            string wkt2 = "LINESTRING(-2.8564453125 44.30812668488613,5.625 48.166085419012525)";
            Geometry geom1 = GeometryService.ParseWKTAsGeometry(wkt1);
            Geometry geom2 = GeometryService.ParseWKTAsGeometry(wkt2);


            Geometry intersection = geom1.Intersection(geom2);

            GeoSegment seg1 = geom1.Segments().First();
            GeoSegment seg2 = geom2.Segments().First();
            GeoPoint intersectionResult = GeoPoint.Zero;

            bool intersects = GeometryService.LineLineIntersection(out intersectionResult, seg1, seg2);


            double dist = intersection.Coordinate.ToGeoPoint().DistanceTo(intersectionResult);


            Assert.True(dist < 0.05d, "Problem in intersection calculation.");
        }

        [Theory(DisplayName = "BoundingBox validity")]
        [InlineData(0, 10, 0, 10, true)]
        // out of bounds coords
        [InlineData(-182, 0, 0, 10, false)]
        [InlineData(0, 182, 0, 10, false)]
        [InlineData(0, 10, -95, 10, false)]
        [InlineData(0, 10, 0, 95, false)]
        // inverted order
        [InlineData(10, 0, 0, 10, false)]
        [InlineData(0, 10, 10, 0, false)]
        [InlineData(10, 0, 10, 0, false)]
        // both
        [InlineData(10, 0, -95, 0, false)]
        public void BoundingBoxValidTest(double xmin, double xmax, double ymin, double ymax, bool isExpectedValid)
        {
            BoundingBox bbox = new BoundingBox(xmin, xmax, ymin, ymax);
            Assert.Equal(bbox.IsValid(), isExpectedValid);
        }


        [Theory(DisplayName = "BoundingBox reorder checks")]
        [InlineData(0, 10, 0, 10, true)]
        // out of bounds coords
        [InlineData(-182, 0, 0, 10, false)]
        [InlineData(0, 182, 0, 10, false)]
        [InlineData(0, 10, -95, 10, false)]
        [InlineData(0, 10, 0, 95, false)]
        // inverted order
        [InlineData(10, 0, 0, 10, true)]
        [InlineData(0, 10, 10, 0, true)]
        [InlineData(10, 0, 10, 0, true)]
        // both
        [InlineData(10, 0, -95, 0, false)]
        public void BoundingBoxReorderTest(double xmin, double xmax, double ymin, double ymax, bool isExpectedValid)
        {
            BoundingBox bbox = new BoundingBox(xmin, xmax, ymin, ymax);
            Assert.Equal(bbox.ReorderMinMax().IsValid(), isExpectedValid);
        }
    }
}
