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
    class ReprojectionSamples
    {
        private readonly int _destinationSRID;
        private readonly IElevationService _elevationService;
        private readonly string _gpxFile;
        private DEMDataSet _dataSet;
        private readonly string _outputDirectory;
        

        public ReprojectionSamples(string outputDirectory, string gpxFile, int destinationSRID = Reprojection.SRID_PROJECTED_MERCATOR)
        {
            _destinationSRID = destinationSRID;
            _elevationService = new ElevationService(new RasterService(outputDirectory));
            _dataSet = DEMDataSet.AW3D30;
            _outputDirectory = outputDirectory;
            _gpxFile = gpxFile;
        }

        internal void Run()
        {
            glTFService glTF = new glTFService();
            ImageryService imageryService = new ImageryService();

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

            Console.WriteLine("Download image tiles...");
            TileRange tiles = imageryService.DownloadTiles(bbox, ImageryProvider.MapBoxSatellite, 4);
            string fileName = Path.Combine(outputDir, "Texture.jpg");

            Console.WriteLine("Construct texture...");
            TextureInfo texInfo = imageryService.ConstructTexture(tiles, bbox, fileName, TextureImageFormat.image_jpeg);

            //
            //=======================

            //=======================
            // Normal map
            Console.WriteLine("Height map...");
            float Z_FACTOR = 2f;
            HeightMap hMap = _elevationService.GetHeightMap(bbox, _dataSet);
            hMap = hMap.ReprojectTo(Reprojection.SRID_GEODETIC, _destinationSRID)
                        .BakeCoordinates();
            var normalMap = imageryService.GenerateNormalMap(hMap, outputDir);
            hMap = hMap.CenterOnOrigin()
                       .ZScale(Z_FACTOR);

            //hMap = hMap.CenterOnOrigin(Z_FACTOR);
            //
            //=======================

            //=======================
            // MESH 3D terrain
            Console.WriteLine("Height map...");


            // TIN mesh
            Console.WriteLine("Generate TIN TriangleMesh...");
            MeshPrimitive TINtriangleMesh = TINGeneration.GenerateTIN(hMap, 10d, glTF, PBRTexture.Create(texInfo, normalMap), _destinationSRID);

            // raw Mesh
            Console.WriteLine("Generate raw TriangleMesh textured...");
            MeshPrimitive triangleMesh = glTF.GenerateTriangleMesh(hMap, null, PBRTexture.Create(texInfo, normalMap));
            // raw Mesh no textures

            Console.WriteLine("Generate raw TriangleMesh no textures...");
            MeshPrimitive triangleMeshNoTexture = glTF.GenerateTriangleMesh(hMap, null, null);



            // take 1 point evert nth
            Console.WriteLine("Generate GPX track line mesh...");
            int nSkip = 1;
            gpxPointsElevated = gpxPointsElevated.Where((x, i) => (i + 1) % nSkip == 0);
            gpxPointsElevated = gpxPointsElevated.ReprojectGeodeticToCartesian();
            gpxPointsElevated = gpxPointsElevated.ZScale(Z_FACTOR).ZTranslate(20);
            MeshPrimitive gpxLine = glTF.GenerateLine(gpxPointsElevated, new Vector4(1, 0, 0, 0.1f), 10f);

            //======================
            // glTF export

            Console.WriteLine("Export triangleMesh...");
            Model model = glTF.GenerateModel(new MeshPrimitive[] { triangleMesh, gpxLine }, this.GetType().Name);
            glTF.Export(model, outputDir, $"{GetType().Name} Raw", false, true);

            Console.WriteLine("Export TINtriangleMesh...");
            model = glTF.GenerateModel(new MeshPrimitive[] { TINtriangleMesh, gpxLine }, this.GetType().Name);
            glTF.Export(model, outputDir, $"{GetType().Name} TIN", false, true);

            Console.WriteLine("Export triangleMeshNoTexture...");
            model = glTF.GenerateModel(new MeshPrimitive[] { triangleMeshNoTexture, gpxLine }, this.GetType().Name);
            glTF.Export(model, outputDir, $"{GetType().Name} no texture", false, true);
        }


    }
}
