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
        readonly IRasterService _rasterService;
        readonly IElevationService _elevationService;
        readonly IDEMDataSetIndex _gdalService;


        public DatasetTests(DemNetFixture fixture)
        {
            _rasterService = fixture.ServiceProvider.GetService<IRasterService>();
            _elevationService = fixture.ServiceProvider.GetService<IElevationService>();
            _gdalService =  fixture.ServiceProvider.GetService<IDEMDataSetIndex>();
        }


        [Fact, TestPriority(1)]
        public void DatasetTest_SRTM_GL1()
        {

            DEMDataSet dataset = DEMDataSet.SRTM_GL1;
            _gdalService.Setup(dataset, _rasterService.GetLocalDEMPath(dataset));

            Assert.True(_gdalService.GetFileSources(dataset).Any());
        }

        [Fact, TestPriority(1)]
        public void DatasetTest_SRTM_GL3()
        {
            DEMDataSet dataset = DEMDataSet.SRTM_GL3;
            _gdalService.Setup(dataset, _rasterService.GetLocalDEMPath(dataset));

            Assert.True(_gdalService.GetFileSources(dataset).Any());

        }

        [Fact, TestPriority(1)]
        public void DatasetTest_AW3D()
        {
            DEMDataSet dataset = DEMDataSet.AW3D30;
            _gdalService.Setup(dataset, _rasterService.GetLocalDEMPath(dataset));

            Assert.True(_gdalService.GetFileSources(dataset).Any());

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
            Assert.True(report.IsExistingLocally);
        }

        [Fact]
        public void DownloadTile_BBox()
        {

            const string WKT_BBOX_AIX_PUYRICARD = "POLYGON ((5.429993 43.537854, 5.459132 43.537854, 5.459132 43.58151, 5.429993 43.58151, 5.429993 43.537854))";

            DEMDataSet dataset = DEMDataSet.SRTM_GL3;
            BoundingBox bbox = GeometryService.GetBoundingBox(WKT_BBOX_AIX_PUYRICARD);

            _elevationService.DownloadMissingFiles(dataset, bbox);
            var report = _rasterService.GenerateReport(dataset, bbox);

            Assert.NotNull(report);
            Assert.True(report.Count > 0);
            Assert.True(report.First().IsExistingLocally);
            Assert.Equal("N43E005.hgt", Path.GetFileName(report.First().LocalName));

        }

        [Fact]
        public void GDALVrtPerDataset_Test()
        {
            double lat = 43.537854;
            double lon = 5.429993;

            DEMDataSet dataset = DEMDataSet.SRTM_GL3;
            var report_SRTM_GL3 = _rasterService.GenerateReportForLocation(dataset, lat, lon);

            // now change the dataset
            dataset = DEMDataSet.AW3D30;
            var report_AW3D30 = _rasterService.GenerateReportForLocation(dataset, lat, lon);

            Assert.NotNull(report_SRTM_GL3);
            Assert.NotNull(report_AW3D30);
            Assert.NotEqual(report_SRTM_GL3.LocalName
                                , report_AW3D30.LocalName);
        }





    }
}
