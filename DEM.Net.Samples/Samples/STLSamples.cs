using AssetGenerator;
using AssetGenerator.Runtime;
using DEM.Net.glTF;
using DEM.Net.glTF.Export;
using DEM.Net.Lib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Samples
{
    class STLSamples
    {
        public static void Run(string outputDirectory, DEMDataSet dataset)
        {
            //string modelName= "Montagne Sainte Victoire";
            //string bboxWKT = "POLYGON((5.54888 43.519525, 5.61209 43.519525, 5.61209 43.565225, 5.54888 43.565225, 5.54888 43.519525))";
            string modelName= "Sainte Baume";
            string bboxWKT = "POLYGON ((5.778465 43.358636, 5.589294 43.358636, 5.589294 43.259706, 5.778465 43.259706, 5.778465 43.358636))";
            //string modelName = "Canigou";
            //string bboxWKT = "POLYGON ((2.523766 42.557131, 2.377167 42.557131, 2.377167 42.495137, 2.523766 42.495137, 2.523766 42.557131))";
            //string modelName = "Mont Blanc";
            //string bboxWKT = "POLYGON ((6.944733 45.904438, 6.778999 45.904438, 6.778999 45.776144, 6.944733 45.776144, 6.944733 45.904438))";

            Logger.Info($"Processing model {modelName}...");
            Logger.RestartPerf("STL");
            // small test
            //string bboxWKT = "POLYGON ((5.558267 43.538602, 5.557902 43.538602, 5.557902 43.538353, 5.558267 43.538353, 5.558267 43.538602))";// zoom ste
            RasterService rasterService = new RasterService();
            ElevationService elevationService = new ElevationService(rasterService);

            var bbox = GeometryService.GetBoundingBox(bboxWKT);
            //bbox = bbox.Scale(1.3); // test
            var heightMap = elevationService.GetHeightMap(bbox, dataset);

            heightMap = heightMap
                                    .ReprojectGeodeticToCartesian()
                                    .ZScale(2f)
                                    .CenterOnOrigin()
                                    .FitInto(250f)
                                    .BakeCoordinates();

            glTFService glTFService = new glTFService();
            var mesh = glTFService.GenerateTriangleMesh_Boxed(heightMap);

            // STL axis differ from glTF 
            mesh.RotateX((float)Math.PI / 2f);

            var stlFileName = Path.Combine(outputDirectory, $"{modelName}.stl");
            STLExportService stlService = new STLExportService();
            stlService.STLExport(mesh, stlFileName, false);

            Model model = glTFService.GenerateModel(mesh, modelName);
            glTFService.Export(model, outputDirectory, $"{modelName}", false, true);
            Logger.StopPerf("STL");
        }


    }
}
