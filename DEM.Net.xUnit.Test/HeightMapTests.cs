using DEM.Net.Lib;
using Xunit;

namespace DEM.Net.Test
{
    public class HeightMapTests
    {

        [Fact]
        public void BoudingBoxConservationTest()
        {

            string bboxWKT = "POLYGON((5.54888 43.519525, 5.61209 43.519525, 5.61209 43.565225, 5.54888 43.565225, 5.54888 43.519525))";

            RasterService rasterService = new RasterService();
            ElevationService elevationService = new ElevationService(rasterService);
            BoundingBox bbox = GeometryService.GetBoundingBox(bboxWKT);

            Assert.NotNull(bbox);
            Assert.Equal(bboxWKT, bbox.WKT);

            HeightMap heightMap = elevationService.GetHeightMap(bbox, DEMDataSet.SRTM_GL1);

            heightMap = heightMap.ReprojectGeodeticToCartesian().BakeCoordinates();
            Assert.True(heightMap.BoundingBox == heightMap.Coordinates.GetBoundingBox());

            heightMap = heightMap.ZScale(2.5f).BakeCoordinates();
            Assert.True(heightMap.BoundingBox == heightMap.Coordinates.GetBoundingBox());

            heightMap = heightMap.CenterOnOrigin().BakeCoordinates();
            Assert.True(heightMap.BoundingBox == heightMap.Coordinates.GetBoundingBox());

            heightMap = heightMap.FitInto(30f).BakeCoordinates();
            Assert.True(heightMap.BoundingBox == heightMap.Coordinates.GetBoundingBox());

        }

        [Fact]
        public void BoudingBoxEqualityTest()
        {

            string bboxWKT = "POLYGON((5.54888 43.519525, 5.61209 43.519525, 5.61209 43.565225, 5.54888 43.565225, 5.54888 43.519525))";

            RasterService rasterService = new RasterService();
            ElevationService elevationService = new ElevationService(rasterService);
            BoundingBox bbox = GeometryService.GetBoundingBox(bboxWKT);

            Assert.NotNull(bbox);
            Assert.Equal(bboxWKT, bbox.WKT);

            HeightMap heightMap = elevationService.GetHeightMap(bbox, DEMDataSet.SRTM_GL1);

            Assert.False(object.ReferenceEquals(bbox, null));
            Assert.NotNull(heightMap.Coordinates.GetBoundingBox());
            Assert.True(heightMap.BoundingBox == heightMap.Coordinates.GetBoundingBox());
            Assert.False(heightMap.BoundingBox != heightMap.Coordinates.GetBoundingBox());

        }

    }
}
