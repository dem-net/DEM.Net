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
            using (IRasterFile raster = _rasterService.OpenFile(fileName, DEMFileFormat.SRTM_HGT))
            {
                FileMetadata metaData = raster.ParseMetaData();
                Assert.NotNull(metaData);

                string str2 = "zsq4";
                HeightMap heightMap = _elevationService.GetHeightMap(metaData)
                                            .ReprojectGeodeticToCartesian()
                                            .ZScale(2.5f);

                MeshPrimitive meshPrimitive = _gltfService.GenerateTriangleMesh(heightMap);
                Model model = _gltfService.GenerateModel(meshPrimitive, str2);
                _gltfService.Export(model, @"c:\geotiff_gltf\", str2, false, true);
            }

            
            
        }


    }
}
