using System;
using System.IO;
using System.IO.Compression;
using DEM.Net.Core;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using DEM.Net.glTF;
using AssetGenerator.Runtime;
using AssetGenerator;

namespace DEM.Net.Test
{
    public class glTFTests : IClassFixture<DemNetFixture>
    {
        IRasterService _rasterService;
        IElevationService _elevationService;
        IglTFService _gltfService;
        IMeshService _meshService;

        public glTFTests(DemNetFixture fixture)
        {
            _rasterService = fixture.ServiceProvider.GetService<IRasterService>();
            _elevationService = fixture.ServiceProvider.GetService<IElevationService>();
            _gltfService= fixture.ServiceProvider.GetService<IglTFService>();
            _meshService = fixture.ServiceProvider.GetService<IMeshService>();
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

                string str2 = "zsq4";
                HeightMap heightMap = _elevationService.GetHeightMap(metaData)
                                            .ReprojectGeodeticToCartesian()
                                            .ZScale(2.5f);

                MeshPrimitive meshPrimitive = _gltfService.GenerateTriangleMesh(heightMap);
                Model model = _gltfService.GenerateModel(meshPrimitive, str2);
                _gltfService.Export(model, ".", str2, false, true);
            }

            
            
        }

        [Theory()]
        [InlineData(true)]
        [InlineData(false)]
        public void Test_GLB_Export_Bbox(bool exportAsBinary)
        {

            DEMDataSet dataset = DEMDataSet.SRTM_GL3;
            var modelName = $"SteVictoireLatLon";

            // You can get your boox from https://geojson.net/ (save as WKT)
            string bboxWKT = "POLYGON((5.54888 43.519525, 5.61209 43.519525, 5.61209 43.565225, 5.54888 43.565225, 5.54888 43.519525))";
            var bbox = GeometryService.GetBoundingBox(bboxWKT);
            var heightMap = _elevationService.GetHeightMap(ref bbox, dataset);
                    

            // Triangulate height map
            var mesh = _gltfService.GenerateTriangleMesh(heightMap);
            var model = _gltfService.GenerateModel(mesh, modelName);

            // Export Binary model file
            _gltfService.Export(model, Directory.GetCurrentDirectory(), modelName, exportglTF: !exportAsBinary, exportGLB: exportAsBinary);



        }


    }
}
