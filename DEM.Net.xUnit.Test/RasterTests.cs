using System;
using System.IO;
using System.IO.Compression;
using DEM.Net.Lib;
using Xunit;

namespace DEM.Net.Test
{
    public class RasterTests
    {
        IRasterService _rasterService;
        IElevationService _elevationService;

        public RasterTests()
        {
            _rasterService = new RasterService(); // local dir
            _elevationService = new ElevationService(_rasterService);

        }

        [Fact]
        public void Test_FileFormat_GeoTIFF()
        {

            string fileName = "N043E005_AVE_DSM.tif";
            string sourceFile = Path.Combine("TestData",  fileName + ".zip");
            bool fileOk = File.Exists(sourceFile);

            Assert.True(fileOk, "TestData is missing");

            if (!File.Exists(fileName))
            {
                ZipFile.ExtractToDirectory(sourceFile, ".", true);
            }

            Assert.True(File.Exists(fileName), "Unzip failed.");


            using (IRasterFile raster = _rasterService.OpenFile(fileName, DEMFileFormat.GEOTIFF))
            {
                FileMetadata metaData = raster.ParseMetaData();
                Assert.NotNull(metaData);

                float elevation = raster.GetElevationAtPoint(metaData, 1000, 200);
                Assert.Equal(304f, elevation);

                elevation = raster.GetElevationAtPoint(metaData, 200, 1000);
                Assert.Equal(95f, elevation);

            }
        }

        [Fact]
        public void Test_FileFormat_HGT()
        {
            string fileName = "N43E005.hgt";
            string sourceFile = Path.Combine("TestData", fileName + ".zip");
            bool fileOk = File.Exists(sourceFile);

            Assert.True(fileOk, "TestData is missing");

            if (!File.Exists(fileName))
            {
                ZipFile.ExtractToDirectory(sourceFile, ".", true);
            }

            Assert.True(File.Exists(fileName), "Unzip failed.");


            using (IRasterFile raster = _rasterService.OpenFile(fileName, DEMFileFormat.SRTM_HGT))
            {
                FileMetadata metaData = raster.ParseMetaData();
                Assert.NotNull(metaData);

                float elevation = raster.GetElevationAtPoint(metaData, 300, 10);
                Assert.Equal(751f, elevation);

                elevation = raster.GetElevationAtPoint(metaData, 10, 300);
                Assert.Equal(297f, elevation);
            }
        }

        [Fact]
        public void Test_FileFormat_HGT_OutOfRange()
        {
            string fileName = "N43E005.hgt";
            string sourceFile = Path.Combine("TestData", fileName + ".zip");
            bool fileOk = File.Exists(sourceFile);

            Assert.True(fileOk, "TestData is missing");

            if (!File.Exists(fileName))
            {
                ZipFile.ExtractToDirectory(sourceFile, ".", true);
            }

            Assert.True(File.Exists(fileName), "Unzip failed.");


            using (IRasterFile raster = _rasterService.OpenFile(fileName, DEMFileFormat.SRTM_HGT))
            {
                FileMetadata metaData = raster.ParseMetaData();
                Assert.NotNull(metaData);

                Assert.Throws<IndexOutOfRangeException>(() =>
               {
                   float elevation = raster.GetElevationAtPoint(metaData, 10000, 10000);
               });
                // Expect exception
            }
        }
    }
}
