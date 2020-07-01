using DEM.Net.Core;
using DEM.Net.xUnit.Test;
using System;
using System.IO;
using System.Linq;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace DEM.Net.Test
{
    public class DatasetTests : IClassFixture<DemNetFixture>
    {
        readonly RasterService _rasterService;
        readonly ElevationService _elevationService;
        readonly RasterIndexServiceResolver _indexServiceResolver;


        public DatasetTests(DemNetFixture fixture)
        {
            _rasterService = fixture.ServiceProvider.GetService<RasterService>();
            _elevationService = fixture.ServiceProvider.GetService<ElevationService>();
            _indexServiceResolver = fixture.ServiceProvider.GetService<RasterIndexServiceResolver>();
        }


        [Theory(), TestPriority(1)]
        [InlineData(nameof(DEMDataSet.SRTM_GL1))]
        [InlineData(nameof(DEMDataSet.SRTM_GL3))]
        [InlineData(nameof(DEMDataSet.AW3D30))]
        [InlineData(nameof(DEMDataSet.ASTER_GDEMV3))]
        public void DatasetTest(string datasetName)
        {
            var datasets = DEMDataSet.RegisteredDatasets;

            Assert.True(datasets.Any(), "No datasets found");


            DEMDataSet dataset = datasets.FirstOrDefault(d => d.Name == datasetName);
            Assert.NotNull(dataset);

            var indexService = this._indexServiceResolver(dataset.DataSource.DataSourceType);
            Assert.NotNull(indexService);
            indexService.Setup(dataset, _rasterService.GetLocalDEMPath(dataset));

            Assert.True(indexService.GetFileSources(dataset).Any());
        }

        [Fact]
        public void DownloadTile_Location()
        {

            double lat = 43.537854;
            double lon = 5.429993;

            DEMDataSet dataset = DEMDataSet.SRTM_GL3;
            _elevationService.DownloadMissingFiles(dataset, lat, lon);

            var report = _rasterService.GenerateReportForLocation(dataset, lat, lon);

            Assert.NotNull(report);
            Assert.NotEmpty(report);
            Assert.True(report.Count == 1);
            Assert.True(report.First().IsExistingLocally);
        }

        [Theory()]
        [InlineData(nameof(DEMDataSet.SRTM_GL1))]
        [InlineData(nameof(DEMDataSet.SRTM_GL3))]
        [InlineData(nameof(DEMDataSet.AW3D30))]
        [InlineData(nameof(DEMDataSet.ASTER_GDEMV3))]
        public void DownloadTile_BBox(string datasetName)
        {
            var datasets = DEMDataSet.RegisteredDatasets;
            Assert.True(datasets.Any(), "No datasets found");

            DEMDataSet dataset = datasets.FirstOrDefault(d => d.Name == datasetName);
            Assert.NotNull(dataset);

            const string WKT_BBOX_AIX_PUYRICARD = "POLYGON ((5.429993 43.537854, 5.459132 43.537854, 5.459132 43.58151, 5.429993 43.58151, 5.429993 43.537854))";

            BoundingBox bbox = GeometryService.GetBoundingBox(WKT_BBOX_AIX_PUYRICARD);

            _elevationService.DownloadMissingFiles(dataset, bbox);
            var report = _rasterService.GenerateReport(dataset, bbox);

            Assert.NotNull(report);
            Assert.True(report.Count > 0);
            Assert.True(report.First().IsExistingLocally);

        }

        [Fact]
        public void GDALVrtPerDataset_Test()
        {
            double lat = 46.537854;
            double lon = 10.429993;

            DEMDataSet dataset = DEMDataSet.SRTM_GL3;
            var report_SRTM_GL3 = _rasterService.GenerateReportForLocation(dataset, lat, lon);

            // now change the dataset
            dataset = DEMDataSet.AW3D30;
            var report_AW3D30 = _rasterService.GenerateReportForLocation(dataset, lat, lon);

            Assert.NotNull(report_SRTM_GL3);
            Assert.True(report_SRTM_GL3.Count == 1);
            Assert.NotNull(report_AW3D30);
            Assert.True(report_AW3D30.Count == 1);
            Assert.NotEqual(report_AW3D30.First().LocalName
                                , report_SRTM_GL3.First().LocalName);
        }





    }
}
