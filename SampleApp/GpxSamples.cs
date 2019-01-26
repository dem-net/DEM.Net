using AssetGenerator;
using AssetGenerator.Runtime;
using DEM.Net.glTF;
using DEM.Net.Lib;
using DEM.Net.Lib.Imagery;
using DEM.Net.Lib.Services.Lab;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SampleApp
{
    class GpxSamples
    {
        private readonly IElevationService _elevationService;
        private readonly string _gpxFile;
        private DEMDataSet _dataSet;
        private readonly string _outputDirectory;

        public GpxSamples(IElevationService elevationService, string outputDirectory, string gpxFile)
        {
            _elevationService = elevationService;
            _dataSet = DEMDataSet.AW3D30;
            _outputDirectory = outputDirectory;
            _gpxFile = gpxFile;
        }

        internal void Run()
        {
            bool withTexture = true;
            bool generateTIN = true;
            glTFService glTF = new glTFService();
            ImageryService imageryService = new ImageryService();
            List<MeshPrimitive> meshes = new List<MeshPrimitive>();
            string outputDir = Path.GetFullPath(Path.Combine(_outputDirectory, "glTF"));


            //=======================
            /// Line strip from GPX
            ///
            // Get GPX points
            var segments = GpxImport.ReadGPX_Segments(_gpxFile);
            var points = segments.SelectMany(seg => seg);
            var bbox = points.GetBoundingBox().Scale(1.1);

            var gpxPointsElevated = _elevationService.GetPointsElevation(points, _dataSet);

            //
            //=======================

            //=======================
            // Textures
            //
            TextureInfo texInfo = null;
            if (withTexture)
            {


                Console.WriteLine("Download image tiles...");
                TileRange tiles = imageryService.DownloadTiles(bbox, ImageryProvider.MapBoxSatelliteStreet, 8);
                string fileName = Path.Combine(outputDir, "Texture.jpg");

                Console.WriteLine("Construct texture...");
                texInfo = imageryService.ConstructTexture(tiles, bbox, fileName, TextureImageFormat.image_jpeg);
            }
            //
            //=======================

            //=======================
            // Normal map
            Console.WriteLine("Height map...");
            float Z_FACTOR = 0.00002f;
            HeightMap hMap = _elevationService.GetHeightMap(bbox, _dataSet);
            hMap = hMap.CenterOnOrigin().ZScale(Z_FACTOR);
            var normalMap = imageryService.GenerateNormalMap(hMap, outputDir);

            //hMap = hMap.CenterOnOrigin(Z_FACTOR);
            //
            //=======================

            //=======================
            // MESH 3D terrain
            Console.WriteLine("Height map...");
            
            Console.WriteLine("GenerateTriangleMesh...");
            MeshPrimitive triangleMesh = null;
            if (generateTIN)
            {
                hMap.ReprojectTo(4326, 2154);
                triangleMesh = TINGeneration.GenerateTIN(hMap, glTF, PBRTexture.Create(texInfo, normalMap));
            }
            else
            {
                hMap = hMap.CenterOnOrigin().ZScale(Z_FACTOR);
                // generate mesh with texture
                triangleMesh = glTF.GenerateTriangleMesh(hMap, null, PBRTexture.Create(texInfo, normalMap));
            }
            meshes.Add(triangleMesh);

            // take 1 point evert nth
            int nSkip = 2;
            gpxPointsElevated = gpxPointsElevated.Where((x, i) => (i + 1) % nSkip == 0);
            gpxPointsElevated = gpxPointsElevated.Select(pt => { pt.Elevation += 5; return pt; });
            gpxPointsElevated = gpxPointsElevated.CenterOnOrigin(hMap.BoundingBox).ZScale(0.00002f);
            MeshPrimitive gpxLine = glTF.GenerateLine(gpxPointsElevated, new Vector4(1, 0, 0, 0.5f), 0.00015f);
            meshes.Add(gpxLine);

            // model export
            Console.WriteLine("GenerateModel...");
            Model model = glTF.GenerateModel(meshes, this.GetType().Name);
            glTF.Export(model, outputDir, $"{GetType().Name} Packed", false, true);
        }

        
    }
}
