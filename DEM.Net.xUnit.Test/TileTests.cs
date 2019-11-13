using System;
using System.Linq;
using DEM.Net.Core;
using DEM.Net.Test;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace DEM.Net.xUnit.Test
{
    public class TileTests : IClassFixture<DemNetFixture>
    {
        readonly IElevationService _elevationService;
        readonly IRasterService _rasterService;
        readonly RasterIndexServiceResolver _rasterResolver;

        public TileTests(DemNetFixture fixture)
        {
            _elevationService = fixture.ServiceProvider.GetService<IElevationService>();
            _rasterService = fixture.ServiceProvider.GetService<IRasterService>();
            _rasterResolver = fixture.ServiceProvider.GetService<RasterIndexServiceResolver>();
        }

        [Theory()]
        [InlineData(nameof(DEMDataSet.SRTM_GL3), 46.5, 10.5, "N46E010.hgt")]
        [InlineData(nameof(DEMDataSet.SRTM_GL3), 46.00000000000004, 10.000000000000007, "N45E010.hgt","N46E010.hgt","N45E009.hgt","N46E009.hgt")]
        [InlineData(nameof(DEMDataSet.SRTM_GL3), 46.5, 10.000000000000007, "N46E010.hgt", "N46E009.hgt")]
        public void SourceBboxCheck(string dataSetName, double lat, double lon,params string[] expectedFileNames)
        {
            DEMDataSet dataSet = DEMDataSet.RegisteredDatasets.FirstOrDefault(d => d.Name == dataSetName);
            Assert.NotNull(dataSet);

            var indexService = _rasterResolver(dataSet.DataSource.DataSourceType);
            Assert.NotNull(dataSet);

            indexService.Setup(dataSet, _rasterService.LocalDirectory);
            var intersectingTiles = indexService.GetFileSources(dataSet).Where(tile => tile.BBox.Intersects(lat, lon)).ToList();
            
            var report = _rasterService.GenerateReportForLocation(dataSet, lat, lon)
                         .ToList();
            Assert.Equal(intersectingTiles.Count, report.Count);
            Assert.NotNull(report);
            Assert.Equal(expectedFileNames.Length, report.Count);

            var commonItems = expectedFileNames.Select(n => n.ToUpper())
                                    .Intersect(report.Select(n => Path.GetFileName(n.LocalName).ToUpper()))
                                    .ToList();

            Assert.Equal(expectedFileNames.Length, commonItems.Count);

        }

        //[Theory()]
        //[InlineData(nameof(DEMDataSet.SRTM_GL3), 46.5, 10.5, "N045E010.hgt")]
        //[InlineData(nameof(DEMDataSet.SRTM_GL3), 46.00000000000004, 10.000000000000007, "N045E010.hgt")]
        //public void TileDetection(string dataSetName, double lat, double lon, string expectedFileName)
        //{
        //    DEMDataSet dataSet = DEMDataSet.RegisteredDatasets.FirstOrDefault(d => d.Name == dataSetName);
        //    Assert.NotNull(dataSet);

        //    var report = _rasterService.GenerateReportForLocation(dataSet, lat, lon);
        //    Assert.NotNull(report);
        //    //var fileName = Path.GetFileName(report.LocalName);
        //    //Assert.Equal(expectedFileName, fileName, ignoreCase: true);
        //}

        [Theory()]
        [InlineData(nameof(DEMDataSet.SRTM_GL3))]
        [InlineData(nameof(DEMDataSet.ASTER_GDEMV3))]
        public void BoudingBoxConservationTest(string datasetName)
        {

            var dataset = DEMDataSet.RegisteredDatasets.First(d => d.Name == datasetName);

            string bboxWKT = "POLYGON((5.54888 43.519525, 5.61209 43.519525, 5.61209 43.565225, 5.54888 43.565225, 5.54888 43.519525))";


            BoundingBox bbox = GeometryService.GetBoundingBox(bboxWKT);

            Assert.NotNull(bbox);
            Assert.Equal(bboxWKT, bbox.WKT);

            HeightMap heightMap = _elevationService.GetHeightMap(bbox, dataset);

            heightMap = heightMap.ReprojectGeodeticToCartesian().BakeCoordinates();
            Assert.True(heightMap.BoundingBox == heightMap.Coordinates.GetBoundingBox());

            heightMap = heightMap.ZScale(2.5f).BakeCoordinates();
            Assert.True(heightMap.BoundingBox == heightMap.Coordinates.GetBoundingBox());

            heightMap = heightMap.CenterOnOrigin().BakeCoordinates();
            Assert.True(heightMap.BoundingBox == heightMap.Coordinates.GetBoundingBox());

            heightMap = heightMap.FitInto(30f).BakeCoordinates();
            Assert.True(heightMap.BoundingBox == heightMap.Coordinates.GetBoundingBox());

        }
    }
}
