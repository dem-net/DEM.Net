using AssetGenerator;
using AssetGenerator.Runtime;
using DEM.Net.glTF;
using DEM.Net.Lib;
using DEM.Net.Lib.Imagery;
using DEM.Net.Lib.Services.Lab;
using DEM.Net.Lib.Services.Mesh;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SampleApp
{
    class TextureSamples
    {
        private readonly IElevationService _elevationService;
        private readonly string _bboxWkt;
        private DEMDataSet _normalsDataSet;
        private DEMDataSet _meshDataSet;
        private readonly string _outputDirectory;
        private readonly string _localdatadir;

        public TextureSamples(IElevationService elevationService, string localDataDir, string outputDirectory)
        {
            _elevationService = elevationService;
            // sugiton
            //_bboxWkt = "POLYGON ((5.42201042175293 43.20023317388979, 5.459775924682617 43.20023317388979, 5.459775924682617 43.22594305473314, 5.42201042175293 43.22594305473314, 5.42201042175293 43.20023317388979))";
            // ste victoire
            //_bboxWkt = "POLYGON((5.424004809009261 43.68472756348281, 5.884057299243636 43.68472756348281, 5.884057299243636 43.40402056297321, 5.424004809009261 43.40402056297321, 5.424004809009261 43.68472756348281))";
            // ventoux
            // _bboxWkt = "POLYGON ((5.192413330078125 44.12209907358672, 5.3015899658203125 44.12209907358672, 5.3015899658203125 44.201897151875094, 5.192413330078125 44.201897151875094, 5.192413330078125 44.12209907358672))";
            //ventoux avigon
            //_bboxWkt = "POLYGON ((4.73236083984375 43.902839992663196, 5.401153564453124 43.902839992663196, 5.401153564453124 44.268804788566165, 4.73236083984375 44.268804788566165, 4.73236083984375 43.902839992663196))";
            // duranne
            //_bboxWkt = "POLYGON ((5.303306579589844 43.45478810195138, 5.379180908203125 43.45478810195138, 5.379180908203125 43.51394981739109, 5.303306579589844 43.51394981739109, 5.303306579589844 43.45478810195138))";
            // ventoux debug
            //_bboxWkt = "POLYGON ((5.1340484619140625 44.17580225275465, 5.2700042724609375 44.17580225275465, 5.2700042724609375 44.21986144948162, 5.1340484619140625 44.21986144948162, 5.1340484619140625 44.17580225275465))";
            // zoom ste victoire
            //_bboxWkt = "POLYGON ((5.533332824707031 43.51668853502906, 5.582771301269531 43.51668853502906, 5.582771301269531 43.550289946081115, 5.533332824707031 43.550289946081115, 5.533332824707031 43.51668853502906))";
            // santiago
            //_bboxWkt = "POLYGON ((-70.8673095703125 -33.612331963363914, -70.04745483398438 -33.612331963363914, -70.04745483398438 -33.05586750447235, -70.8673095703125 -33.05586750447235, -70.8673095703125 -33.612331963363914))";
            //chile - richards deep
            //_bboxWkt = "POLYGON ((-75.47607421875 -25.74052909277321, -67.08251953125 -25.74052909277321, -67.08251953125 -21.53484700204879, -75.47607421875 -21.53484700204879, -75.47607421875 -25.74052909277321))";
            // chile full
            //_bboxWkt = "POLYGON ((-77.080078125 -56.6562264935022, -66.533203125 -56.6562264935022, -66.533203125 -15.792253570362446, -77.080078125 -15.792253570362446, -77.080078125 -56.6562264935022))";
            // aconcagua
            //_bboxWkt = "POLYGON ((-70.15800476074219 -32.861132322810946, -69.79820251464844 -32.861132322810946, -69.79820251464844 -32.558967346292164, -70.15800476074219 -32.558967346292164, -70.15800476074219 -32.861132322810946))";
            // fogo
            //_bboxWkt = "POLYGON ((-24.5654296875 14.78019397569999, -24.23858642578125 14.78019397569999, -24.23858642578125 15.077427674847987, -24.5654296875 15.077427674847987, -24.5654296875 14.78019397569999))";
            // valgo
            //_bboxWkt = "POLYGON ((6.373444 44.913277, 5.971403 44.913277, 5.971403 44.73893, 6.373444 44.73893, 6.373444 44.913277))";
            // robion
            //_bboxWkt = "POLYGON ((5.01800537109375 43.69369383336777, 5.350341796875 43.69369383336777, 5.350341796875 43.89294437871145, 5.01800537109375 43.89294437871145, 5.01800537109375 43.69369383336777))";
            // france
            //_bboxWkt = "POLYGON ((-6.1962890625 41.1290213474951, 10.04150390625 41.1290213474951, 10.04150390625 51.11041991029264, -6.1962890625 51.11041991029264, -6.1962890625 41.1290213474951))";
            // alps
            _bboxWkt = "POLYGON ((3.4716796874999996 42.71473218539458, 17.0947265625 42.71473218539458, 17.0947265625 48.67645370777654, 3.4716796874999996 48.67645370777654, 3.4716796874999996 42.71473218539458))";
            _normalsDataSet = DEMDataSet.SRTM_GL3;
            _meshDataSet = DEMDataSet.AW3D30;
            _outputDirectory = outputDirectory;
            _localdatadir = localDataDir;
        }

        internal void Run()
        {
            bool useTIN = true;
            glTFService glTF = new glTFService();
            string outputDir = Path.GetFullPath(Path.Combine(_outputDirectory, "glTF"));

            // Get GPX points
            var bbox = GeometryService.GetBoundingBox(_bboxWkt);

            //=======================
            // Textures
            //
            TextureInfo texInfo = null;


            ImageryService imageryService = new ImageryService();

            Console.WriteLine("Download image tiles...");
            TileRange tiles = imageryService.DownloadTiles(bbox, ImageryProvider.StamenWaterColor, 4);

            Console.WriteLine("Construct texture...");
            string fileName = Path.Combine(outputDir, "Texture.jpg");
            texInfo = imageryService.ConstructTexture(tiles, bbox, fileName, TextureImageFormat.image_jpeg);

            //
            //=======================

            //=======================
            // Normal map
            Console.WriteLine("Height map...");
            float Z_FACTOR = 10f;
            //HeightMap hMapNormal = _elevationService.GetHeightMap(bbox, _normalsDataSet);
            HeightMap hMapNormal = _elevationService.GetHeightMap(bbox, Path.Combine(_localdatadir, "ETOPO1", "ETOPO1_Bed_g_geotiff.tif"), DEMFileFormat.GEOTIFF);

            // hMapNormal = hMapNormal.ReprojectTo(4326, v_outSrid);
            hMapNormal = hMapNormal.ReprojectGeodeticToCartesian();

            Console.WriteLine("Generate normal map...");
            TextureInfo normal = imageryService.GenerateNormalMap(hMapNormal, outputDir);
            //
            //=======================

            //=======================
            // Get height map
            //HeightMap hMap = _elevationService.GetHeightMap(bbox, _meshDataSet);
            HeightMap hMap = _elevationService.GetHeightMap(bbox, Path.Combine(_localdatadir, "ETOPO1","ETOPO1_Bed_g_geotiff.tif"), DEMFileFormat.GEOTIFF);

            //=======================
            // UV mapping (before projection)
            PBRTexture pBRTexture = PBRTexture.Create(texInfo, normal, imageryService.ComputeUVMap(hMap, texInfo));

            hMap = hMap.ReprojectGeodeticToCartesian();
            hMap = hMap.CenterOnOrigin().ZScale(Z_FACTOR);

           
            //=======================


            //=======================
            // MESH 3D terrain

            List<MeshPrimitive> meshes = new List<MeshPrimitive>();
            // generate mesh with texture
            MeshPrimitive triangleMesh;
            if (useTIN)
            {
                Console.WriteLine("Create TIN...");
                //triangleMesh = GenerateTIN(hMapTIN, glTF, pBRTexture);
                triangleMesh = TINGeneration.GenerateTIN(hMap, glTF, pBRTexture);
            }
            else
            {
                Console.WriteLine("GenerateTriangleMesh...");
                triangleMesh = glTF.GenerateTriangleMesh(hMap, null, pBRTexture);

            }
            meshes.Add(triangleMesh);

            // model export
            Console.WriteLine("GenerateModel...");
            Model model = glTF.GenerateModel(meshes, this.GetType().Name);
            glTF.Export(model, outputDir, $"{GetType().Name} NONormal", false, true);
        }



        internal void RunImagery(bool withTexture)
        {
            glTFService glTF = new glTFService();
            List<MeshPrimitive> meshes = new List<MeshPrimitive>();
            string outputDir = Path.GetFullPath(Path.Combine(_outputDirectory, "glTF"));

            // Get GPX points
            var bbox = GeometryService.GetBoundingBox(_bboxWkt);

            //=======================
            // Textures
            //
            TextureInfo texInfo = null;
            if (withTexture)
            {

                ImageryService imageryService = new ImageryService();

                Console.WriteLine("Download image tiles...");
                TileRange tiles = imageryService.DownloadTiles(bbox, ImageryProvider.Osm, 4);
                string fileName = Path.Combine(outputDir, "Texture.png");

                Console.WriteLine("Construct texture...");
                texInfo = imageryService.ConstructTexture(tiles, bbox, fileName, TextureImageFormat.image_jpeg);
            }
            //
            //=======================

            //=======================
            // MESH 3D terrain

            Console.WriteLine("Height map...");
            HeightMap hMap = _elevationService.GetHeightMap(bbox, _meshDataSet);

            //hMap = hMap.ReprojectTo(4326, 2154);
            hMap = hMap.CenterOnOrigin().ZScale(0.00002f);

            Console.WriteLine("GenerateTriangleMesh...");
            // generate mesh with texture
            MeshPrimitive triangleMesh = glTF.GenerateTriangleMesh(hMap, null, PBRTexture.Create(texInfo));
            meshes.Add(triangleMesh);

            // model export
            Console.WriteLine("GenerateModel...");
            Model model = glTF.GenerateModel(meshes, this.GetType().Name);
            glTF.Export(model, outputDir, $"{GetType().Name} Packed", false, true);
        }



    }
}
