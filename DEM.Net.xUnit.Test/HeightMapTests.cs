using DEM.Net.Core;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace DEM.Net.Test
{
    public class HeightMapTests : IClassFixture<DemNetFixture>
    {
        ElevationService _elevationService;

        public HeightMapTests(DemNetFixture fixture)
        {
            _elevationService = fixture.ServiceProvider.GetService<ElevationService>();
        }

        [Theory()]
        [InlineData(nameof(DEMDataSet.SRTM_GL1))]
        [InlineData(nameof(DEMDataSet.ASTER_GDEMV3))]
        public void BoudingBoxConservationTest(string datasetName)
        {

            var dataset = DEMDataSet.RegisteredDatasets.First(d => d.Name == datasetName);

            string bboxWKT = "POLYGON((5.54888 43.519525, 5.61209 43.519525, 5.61209 43.565225, 5.54888 43.565225, 5.54888 43.519525))";

           
            BoundingBox bbox = GeometryService.GetBoundingBox(bboxWKT);

            Assert.NotNull(bbox);
            Assert.Equal(bboxWKT, bbox.WKT);

            HeightMap heightMap = _elevationService.GetHeightMap(ref bbox, dataset);

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

           BoundingBox bbox = GeometryService.GetBoundingBox(bboxWKT);

            Assert.NotNull(bbox);
            Assert.Equal(bboxWKT, bbox.WKT);

            HeightMap heightMap = _elevationService.GetHeightMap(ref bbox, DEMDataSet.SRTM_GL1);

            Assert.False(object.ReferenceEquals(bbox, null));
            Assert.NotNull(heightMap.Coordinates.GetBoundingBox());
            Assert.True(heightMap.BoundingBox == heightMap.Coordinates.GetBoundingBox());
            Assert.False(heightMap.BoundingBox != heightMap.Coordinates.GetBoundingBox());

        }

    }
}
