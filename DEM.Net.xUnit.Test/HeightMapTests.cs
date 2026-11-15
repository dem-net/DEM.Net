using System.Threading.Tasks;
using DEM.Net.Core;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace DEM.Net.Test
{
    // Downloads datasets / tiles: excluded from CI (dotnet test --filter Category!=Network)
    [Trait("Category", "Network")]
    public class HeightMapTests : IClassFixture<DemNetFixture>
    {
        ElevationService _elevationService;

        public HeightMapTests(DemNetFixture fixture)
        {
            _elevationService = fixture.ServiceProvider.GetService<ElevationService>();
        }

        [Theory()]
        [InlineData(nameof(DEMDataSet.SRTM_GL3))]
        public async Task BoudingBoxConservationTest(string datasetName)
        {

            var dataset = DEMDataSet.RegisteredDatasets.First(d => d.Name == datasetName);

            string bboxWKT = "POLYGON((5.54888 43.519525, 5.61209 43.519525, 5.61209 43.565225, 5.54888 43.565225, 5.54888 43.519525))";

           
            BoundingBox bbox = GeometryService.GetBoundingBox(bboxWKT);

            Assert.NotNull(bbox);
            Assert.Equal(bboxWKT, bbox.WKT);

            HeightMap heightMap = await _elevationService.GetHeightMapAsync(bbox, dataset);

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
        public async Task BoudingBoxEqualityTest()
        {

            string bboxWKT = "POLYGON((5.54888 43.519525, 5.61209 43.519525, 5.61209 43.565225, 5.54888 43.565225, 5.54888 43.519525))";

           BoundingBox bbox = GeometryService.GetBoundingBox(bboxWKT);

            Assert.NotNull(bbox);
            Assert.Equal(bboxWKT, bbox.WKT);

            HeightMap heightMap = await _elevationService.GetHeightMapAsync(bbox, DEMDataSet.SRTM_GL3);

            Assert.False(object.ReferenceEquals(bbox, null));
            Assert.NotNull(heightMap.Coordinates.GetBoundingBox());
            Assert.True(heightMap.BoundingBox == heightMap.Coordinates.GetBoundingBox());
            Assert.False(heightMap.BoundingBox != heightMap.Coordinates.GetBoundingBox());

        }

    }
}
