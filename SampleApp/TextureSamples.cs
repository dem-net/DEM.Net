using AssetGenerator;
using AssetGenerator.Runtime;
using DEM.Net.glTF;
using DEM.Net.Lib;
using DEM.Net.Lib.Imagery;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleApp
{
    class TextureSamples
    {
        private readonly IElevationService _elevationService;
        private readonly string _bboxWkt;
        private DEMDataSet _dataSet;
        private readonly string _outputDirectory;

        public TextureSamples(IElevationService elevationService, string outputDirectory)
        {
            _elevationService = elevationService;
            _bboxWkt = "POLYGON((5.424004809009261 43.68472756348281, 5.884057299243636 43.68472756348281, 5.884057299243636 43.40402056297321, 5.424004809009261 43.40402056297321, 5.424004809009261 43.68472756348281))";
            _dataSet = DEMDataSet.AW3D30;
            _outputDirectory = outputDirectory;
        }

        internal void Run()
        {
            glTFService glTF = new glTFService();
            List<MeshPrimitive> meshes = new List<MeshPrimitive>();

            // Get GPX points
            var bbox = GeometryService.GetBoundingBox(_bboxWkt);

            //=======================
            // Textures
            //
            ImageryService imageryService = new ImageryService();
            imageryService.DownloadTiles(bbox, ImageryProvider.Osm);
            //
            //=======================


            //=======================
            // MESH 3D terrain

            Console.Write("Height map...");
            HeightMap hMap = _elevationService.GetHeightMap(bbox, _dataSet);

            //hMap = hMap.ReprojectTo(4326, 2154);
            hMap = hMap.CenterOnOrigin(0.00002f);

            Console.Write("GenerateTriangleMesh...");
            MeshPrimitive triangleMesh = glTF.GenerateTriangleMesh(hMap);
            meshes.Add(triangleMesh);

            // model export
            Console.Write("GenerateModel...");
            Model model = glTF.GenerateModel(meshes, this.GetType().Name);
            glTF.Export(model, Path.Combine(_outputDirectory, "glTF"), $"{GetType().Name} textured", false, true);
        }

    }
}
