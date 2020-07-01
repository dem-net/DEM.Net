using System;
using System.IO;
using System.IO.Compression;
using DEM.Net.Core;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using DEM.Net.glTF.SharpglTF;

namespace DEM.Net.Test
{
    public class glTFTests : IClassFixture<DemNetFixture>
    {
        RasterService _rasterService;
        ElevationService _elevationService;
        SharpGltfService _sharpGltfService;
        MeshService _meshService;

        public glTFTests(DemNetFixture fixture)
        {
            _rasterService = fixture.ServiceProvider.GetService<RasterService>();
            _elevationService = fixture.ServiceProvider.GetService<ElevationService>();
            _sharpGltfService = fixture.ServiceProvider.GetService<SharpGltfService>();
            _meshService = fixture.ServiceProvider.GetService<MeshService>();
        }

        [Fact]
        public void Test_GLB_Export()
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

            // Pass the full file name
            fileName = Path.GetFullPath(fileName);
            using (IRasterFile raster = _rasterService.OpenFile(fileName, DEMFileType.SRTM_HGT))
            {
                FileMetadata metaData = raster.ParseMetaData(new DEMFileDefinition( DEMFileType.SRTM_HGT, DEMFileRegistrationMode.Grid));
                Assert.NotNull(metaData);

                HeightMap heightMap = _elevationService.GetHeightMap(metaData)
                                            .ReprojectGeodeticToCartesian()
                                            .ZScale(2.5f);

                var model = _sharpGltfService.CreateTerrainMesh(heightMap);
                model.SaveGLB(Path.Combine(Directory.GetCurrentDirectory(),"test.glb"));
            }

            
            
        }

        [Theory()]
        [InlineData(true)]
        [InlineData(false)]
        public void Test_GLB_Export_Bbox(bool exportAsBinary)
        {

            DEMDataSet dataset = DEMDataSet.SRTM_GL3;
            var modelName = $"SteVictoireLatLon.glb";

            // You can get your boox from https://geojson.net/ (save as WKT)
            string bboxWKT = "POLYGON((5.54888 43.519525, 5.61209 43.519525, 5.61209 43.565225, 5.54888 43.565225, 5.54888 43.519525))";
            var bbox = GeometryService.GetBoundingBox(bboxWKT);
            var heightMap = _elevationService.GetHeightMap(ref bbox, dataset);
                    

            // Triangulate height map
            var model = _sharpGltfService.CreateTerrainMesh(heightMap);

            // Export Binary model file
            model.SaveGLB(Path.Combine(Directory.GetCurrentDirectory(), modelName));

            Assert.True(File.Exists(Path.Combine(Directory.GetCurrentDirectory(), modelName)));

        }


    }
}
