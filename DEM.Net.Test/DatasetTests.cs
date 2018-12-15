using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.SqlServer.Types;
using DEM.Net.Lib;
using System.Windows.Media.Imaging;
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
            _rasterService = new RasterService("Temp");
            _elevationService = new ElevationService(_rasterService);

            SqlServerTypes.Utilities.LoadNativeAssemblies(AppDomain.CurrentDomain.BaseDirectory);
        }

        [TestMethod]
        [TestCategory("Dataset")]
        public void DatasetTest_SRTM_GL1()
        {
            DEMDataSet dataset = DEMDataSet.SRTM_GL1;
            GDALVRTFileService vrtService = new GDALVRTFileService(_rasterService.GetLocalDEMPath(dataset), dataset);
            vrtService.Setup(false);

            Assert.IsTrue(vrtService.Sources().Any());
        }
        [TestMethod]
        [TestCategory("Dataset")]
        public void DatasetTest_SRTM_GL3()
        {
            DEMDataSet dataset = DEMDataSet.SRTM_GL3;
            GDALVRTFileService vrtService = new GDALVRTFileService(_rasterService.GetLocalDEMPath(dataset), dataset);
            vrtService.Setup(false);

            Assert.IsTrue(vrtService.Sources().Any());

        }
        [TestMethod]
        [TestCategory("Dataset")]
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
        }


        


    }
}
