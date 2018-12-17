using System;
using System.IO;
using System.IO.Compression;
using DEM.Net.Lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DEM.Net.Test
{
    [TestClass]
    public class RasterTests
    {
        IRasterService _rasterService;
        IElevationService _elevationService;

        [TestInitialize()]
        public void Initialize()
        {
            _rasterService = new RasterService("."); // local dir
            _elevationService = new ElevationService(_rasterService);

        }

        [TestMethod]
        [TestCategory("Raster")]
        [DeploymentItem(@"TestData\N043E005_AVE_DSM.tif.zip")]
        public void Test_GeoTIFF()
        {
            string fileName = "N043E005_AVE_DSM.tif";
            bool fileOk = File.Exists(fileName + ".zip");

            Assert.IsTrue(fileOk, "DeploymentItem is not enabled. Please use the .testsettings file provided in the project.");

            ZipFile.ExtractToDirectory(fileName + ".zip", ".");

            Assert.IsTrue(File.Exists(fileName), "Unzip failed.");


            using (IRasterFile raster = _rasterService.OpenFile(fileName, DEMFileFormat.GEOTIFF))
            {
                FileMetadata metaData = raster.ParseMetaData();
                Assert.IsNotNull(metaData);

                float elevation = raster.GetElevationAtPoint(metaData, 1000, 200);
                Assert.AreEqual<float>(elevation, 304);

                elevation = raster.GetElevationAtPoint(metaData, 200, 1000);
                Assert.AreEqual<float>(elevation, 95);

            }
        }
    }
}
