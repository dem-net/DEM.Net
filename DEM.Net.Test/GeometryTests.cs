using System;
using System.Linq;
using DEM.Net.Lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DEM.Net.Test
{
    [TestClass]
    public class GeometryTests
    {
        GeoPoint[] _linePoints;

        [TestInitialize()]
        public void Init()
        {
            _linePoints = new GeoPoint[] {
                                        new GeoPoint(43.429923, 0.517782),
                                          new GeoPoint(43.093901, 0.451817),
                                          new GeoPoint(42.610654, 0.27591),
                                          new GeoPoint(42.237605, 0.165969),
                                          new GeoPoint(41.796848, -0.053915)
                                        };

        }
        [TestMethod]
        public void SegmentsTest()
        {


            var lineGeom = GeometryService.ParseGeoPointAsGeometryLine(_linePoints);

            Assert.IsNotNull(lineGeom, "Line construction from points failed");
            Assert.IsTrue(lineGeom.Coordinates.Length == _linePoints.Length, "Coordinate count mismatch");

            var segments = GeometryService.Segments(lineGeom);
            Assert.IsTrue(segments.Count() == lineGeom.Coordinates.Length - 1, "Segment enumeration failed");
        }

    }
}
