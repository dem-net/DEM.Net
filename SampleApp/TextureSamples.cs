using AssetGenerator;
using AssetGenerator.Runtime;
using DEM.Net.glTF;
using DEM.Net.Lib;
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
        private readonly string _gpxFile;
        private DEMDataSet _dataSet;
        private readonly string _outputDirectory;

        public TextureSamples(IElevationService elevationService, string gpxFile, string outputDirectory)
        {
            _elevationService = elevationService;
            _gpxFile = gpxFile;
            _dataSet = DEMDataSet.AW3D30;
            _outputDirectory = outputDirectory;
        }

        internal void Run()
        {
            glTFService glTF = new glTFService();
            List<MeshPrimitive> meshes = new List<MeshPrimitive>();

            // Get GPX points
            var segments = GpxImport.ReadGPX_Segments(_gpxFile);
            var points = segments.SelectMany(seg => seg);
            var bbox = points.GetBoundingBox().Scale(1.1);

            //=======================
            // MESH 3D terrain

            Console.Write("Height map...");
            HeightMap hMap = _elevationService.GetHeightMap(bbox, _dataSet);

            //hMap = hMap.ReprojectTo(4326, 2154);
            hMap = hMap.CenterOnOrigin(0.00002f);

            Console.Write("GenerateTriangleMesh...");
            MeshPrimitive triangleMesh = glTF.GenerateTriangleMesh(hMap);
            meshes.Add(triangleMesh);


            //=======================
            /// Line strip from GPX

            var pointsElevated = _elevationService.GetPointsElevation(points, _dataSet);
            pointsElevated = pointsElevated.Select(pt => { pt.Elevation += 8; return pt; });
            pointsElevated = pointsElevated.CenterOnOrigin(hMap.BoundingBox, 0.00002f);

            // take 1 point evert nth
            // int nSkip = 1;
            //pointsElevated = pointsElevated.Where((x, i) => (i + 1) % nSkip == 0);

            MeshPrimitive gpxLine = glTF.GenerateLine(pointsElevated, new System.Numerics.Vector3(1, 0, 0), 0.0001f);
            meshes.Add(gpxLine);

            // model export
            Console.Write("GenerateModel...");
            Model model = glTF.GenerateModel(meshes, this.GetType().Name);
            glTF.Export(model, Path.Combine(_outputDirectory, "glTF"), $"{GetType().Name} combined", false, true);
        }

    }
}
