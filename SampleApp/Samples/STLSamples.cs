using DEM.Net.glTF;
using DEM.Net.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleApp
{
    class STLSamples
    {
        public static void Run()
        {
            string bboxWKT = "POLYGON ((5.586891 43.519835, 5.586891 43.561486, 5.528271 43.561486, 5.528271 43.519835, 5.586891 43.519835))";
            RasterService rasterService = new RasterService();
            ElevationService elevationService = new ElevationService(rasterService);

            var bbox = GeometryService.GetBoundingBox(bboxWKT);
            var heightMap = elevationService.GetHeightMap(bbox, DEMDataSet.AW3D30);
            heightMap = heightMap.ReprojectGeodeticToCartesian()
                                    .ZScale(2.5f);
            glTFService glTFService = new glTFService();
            var mesh = glTFService.GenerateTriangleMesh(heightMap);
            var model = glTFService.GenerateModel(mesh, "STL Test");
            glTFService.Export(model, @"..\..\..\Data", "STL Test", false, true);

        }
    }
}
