using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DEM.Net.Lib;
using System.IO;
using System.Linq;

namespace DEM.Net.Test
{
    [TestClass]
    public class DatasetTests
    {
        IRasterService _rasterService;
        IElevationService _elevationService;

        [TestInitialize()]
        public void Initialize()
        {
            _rasterService = new RasterService(".");
            _elevationService = new ElevationService(_rasterService);

           
        }

        [TestMethod]
        [TestCategory("Dataset")]
        [Priority(1)]
        public void DatasetTest_SRTM_GL1()
        {
           
            DEMDataSet dataset = DEMDataSet.SRTM_GL1;
            GDALVRTFileService vrtService = new GDALVRTFileService(_rasterService.GetLocalDEMPath(dataset), dataset);
            vrtService.Setup(false);

            Assert.IsTrue(vrtService.Sources().Any());
        }
        [TestMethod]
        [TestCategory("Dataset")]
        [Priority(1)]
        public void DatasetTest_SRTM_GL3()
        {
            DEMDataSet dataset = DEMDataSet.SRTM_GL3;
            GDALVRTFileService vrtService = new GDALVRTFileService(_rasterService.GetLocalDEMPath(dataset), dataset);
            vrtService.Setup(false);

            Assert.IsTrue(vrtService.Sources().Any());

        }
        [TestMethod]
        [TestCategory("Dataset")]
        [Priority(1)]
        public void DatasetTest_AW3D()
        {
            DEMDataSet dataset = DEMDataSet.AW3D30;
            GDALVRTFileService vrtService = new GDALVRTFileService(_rasterService.GetLocalDEMPath(dataset), dataset);
            vrtService.Setup(false);

            Assert.IsTrue(vrtService.Sources().Any());

        }



        [TestMethod]
        [TestCategory("Dataset")]
        public void DownloadTile_Location()
        {

            double lat = 43.537854;
            double lon = 5.429993;

            DEMDataSet dataset = DEMDataSet.SRTM_GL3;
            _elevationService.DownloadMissingFiles(dataset, lat, lon);

            var report = _rasterService.GenerateReportForLocation(dataset, lat, lon);

            Assert.IsNotNull(report);
            Assert.IsTrue(report.Count > 0);
            Assert.IsTrue(report.Values.First().IsExistingLocally);
        }

        [TestMethod]
        [TestCategory("Dataset")]
        public void DownloadTile_BBox()
        {

            const string WKT_BBOX_AIX_PUYRICARD = "POLYGON ((5.429993 43.537854, 5.459132 43.537854, 5.459132 43.58151, 5.429993 43.58151, 5.429993 43.537854))";

            DEMDataSet dataset = DEMDataSet.SRTM_GL3;
            BoundingBox bbox = GeometryService.GetBoundingBox(WKT_BBOX_AIX_PUYRICARD);

            _elevationService.DownloadMissingFiles(dataset, bbox);
            var report = _rasterService.GenerateReport(dataset, bbox);

            Assert.IsNotNull(report);
            Assert.IsTrue(report.Count > 0);
            Assert.IsTrue(report.Values.First().IsExistingLocally);
            Assert.AreEqual(Path.GetFileName(report.Values.First().LocalName), "N43E005.hgt", "Bad bbox query result");
        }

        [TestMethod]
        [TestCategory("Dataset")]
        public void GDALVrtPerDataset_Test()
        {
            double lat = 43.537854;
            double lon = 5.429993;

            DEMDataSet dataset = DEMDataSet.SRTM_GL3;
            var report_SRTM_GL3 = _rasterService.GenerateReportForLocation(dataset, lat, lon);

            // now change the dataset
            dataset = DEMDataSet.AW3D30;
            var report_AW3D30 = _rasterService.GenerateReportForLocation(dataset, lat, lon);

            Assert.IsNotNull(report_SRTM_GL3);
            Assert.IsNotNull(report_AW3D30);
            Assert.IsTrue(report_SRTM_GL3.Count == 1);
            Assert.IsTrue(report_AW3D30.Count == 1);
            Assert.AreNotEqual(report_SRTM_GL3.Values.First().LocalName
                                , report_AW3D30.Values.First().LocalName
                                , "Raster service is using the same instance of GDALVrtService !");
        }





    }
}
