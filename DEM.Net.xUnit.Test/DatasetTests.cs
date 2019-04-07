using DEM.Net.Lib;
using DEM.Net.xUnit.Test;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace DEM.Net.Test
{
    public class DatasetTests
    {
        IRasterService _rasterService;
        IElevationService _elevationService;

        
        public DatasetTests()
        {
            _rasterService = new RasterService(".");
            _elevationService = new ElevationService(_rasterService);

        }


        [Fact, TestPriority(1)]
        public void DatasetTest_SRTM_GL1()
        {
           
            DEMDataSet dataset = DEMDataSet.SRTM_GL1;
            GDALVRTFileService vrtService = new GDALVRTFileService(_rasterService.GetLocalDEMPath(dataset), dataset);
            vrtService.Setup(false);

            Assert.True(vrtService.Sources().Any());
        }

        [Fact, TestPriority(1)]
        public void DatasetTest_SRTM_GL3()
        {
            DEMDataSet dataset = DEMDataSet.SRTM_GL3;
            GDALVRTFileService vrtService = new GDALVRTFileService(_rasterService.GetLocalDEMPath(dataset), dataset);
            vrtService.Setup(false);

            Assert.True(vrtService.Sources().Any());

        }

        [Fact, TestPriority(1)]
        public void DatasetTest_AW3D()
        {
            DEMDataSet dataset = DEMDataSet.AW3D30;
            GDALVRTFileService vrtService = new GDALVRTFileService(_rasterService.GetLocalDEMPath(dataset), dataset);
            vrtService.Setup(false);

            Assert.True(vrtService.Sources().Any());

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
            Assert.True(report.Count > 0);
            Assert.True(report.Values.First().IsExistingLocally);
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
            Assert.True(report.Values.First().IsExistingLocally);
            Assert.Equal("N43E005.hgt", Path.GetFileName(report.Values.First().LocalName));

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
            Assert.True(report_SRTM_GL3.Count == 1);
            Assert.True(report_AW3D30.Count == 1);
            Assert.NotEqual(report_SRTM_GL3.Values.First().LocalName
                                , report_AW3D30.Values.First().LocalName);
        }





    }
}
