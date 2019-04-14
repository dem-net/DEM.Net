using DEM.Net.Lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DEM.Net.Test
{
    [TestClass]
    public class HeightMapTests
    {

        [TestMethod]
        public void BoudingBoxConservationTest()
        {

            string bboxWKT = "POLYGON((5.54888 43.519525, 5.61209 43.519525, 5.61209 43.565225, 5.54888 43.565225, 5.54888 43.519525))";

            RasterService rasterService = new RasterService();
            ElevationService elevationService = new ElevationService(rasterService);
            BoundingBox bbox = GeometryService.GetBoundingBox(bboxWKT);

            Assert.IsNotNull(bbox);
            Assert.AreEqual(bbox.WKT, bboxWKT);

            HeightMap heightMap = elevationService.GetHeightMap(bbox, DEMDataSet.SRTM_GL1);

            heightMap = heightMap.ReprojectGeodeticToCartesian().BakeCoordinates();
            Assert.IsTrue(heightMap.BoundingBox == heightMap.Coordinates.GetBoundingBox());

            heightMap = heightMap.ZScale(2.5f).BakeCoordinates();
            Assert.IsTrue(heightMap.BoundingBox == heightMap.Coordinates.GetBoundingBox());

            heightMap = heightMap.CenterOnOrigin().BakeCoordinates();
            Assert.IsTrue(heightMap.BoundingBox == heightMap.Coordinates.GetBoundingBox());

            heightMap = heightMap.FitInto(30f).BakeCoordinates();
            Assert.IsTrue(heightMap.BoundingBox == heightMap.Coordinates.GetBoundingBox());
                                    
        }

        [TestMethod]
        public void BoudingBoxEqualityTest()
        {

            string bboxWKT = "POLYGON((5.54888 43.519525, 5.61209 43.519525, 5.61209 43.565225, 5.54888 43.565225, 5.54888 43.519525))";

            RasterService rasterService = new RasterService();
            ElevationService elevationService = new ElevationService(rasterService);
            BoundingBox bbox = GeometryService.GetBoundingBox(bboxWKT);

            Assert.IsNotNull(bbox);
            Assert.AreEqual(bbox.WKT, bboxWKT);

            HeightMap heightMap = elevationService.GetHeightMap(bbox, DEMDataSet.SRTM_GL1);

            Assert.AreNotEqual(bbox, null);
            Assert.AreNotEqual(heightMap.Coordinates.GetBoundingBox(), null);
            Assert.IsTrue(heightMap.BoundingBox == heightMap.Coordinates.GetBoundingBox());
            Assert.IsFalse(heightMap.BoundingBox != heightMap.Coordinates.GetBoundingBox());

        }

    }
}
