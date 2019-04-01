using AssetGenerator.Runtime;
using DEM.Net.glTF;
using DEM.Net.Lib;
using IxMilia.Stl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SampleApp
{
    class STLSamples
    {
        public static void Run(string outputDirectory)
        {
            string bboxWKT = "POLYGON((5.54888 43.519525, 5.61209 43.519525, 5.61209 43.565225, 5.54888 43.565225, 5.54888 43.519525))";
            RasterService rasterService = new RasterService();
            ElevationService elevationService = new ElevationService(rasterService);

            var bbox = GeometryService.GetBoundingBox(bboxWKT);
            var heightMap = elevationService.GetHeightMap(bbox, DEMDataSet.SRTM_GL3);
            heightMap = heightMap.ReprojectGeodeticToCartesian()
                                    .ZScale(2.5f)
                                    .CenterOnOrigin()
                                    .BakeCoordinates();
            glTFService glTFService = new glTFService();
            var mesh = glTFService.GenerateTriangleMesh(heightMap);

            var stlFileName = Path.Combine(outputDirectory, "STL Test.stl");

            STLExport(mesh, stlFileName);


        }

        public static void STLExport(MeshPrimitive mesh, string fileName)
        {

            // ...

            StlFile stlFile = new StlFile();
            stlFile.SolidName = "dem-net";

            //The number of the vertices
            var positions = mesh.Positions.ToList();
            var indices = mesh.Indices.ToList();

            stlFile.Triangles.Capacity = indices.Count / 3;
            int numTriangle = 0;
            for (int i = 0; i < indices.Count; i += 3)
            {

                stlFile.Triangles.Add(CreateStlTriangle(positions[indices[i]]
                                            , positions[indices[i + 1]]
                                            , positions[indices[i + 2]]));
                numTriangle++;
            }

            // ...

            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                stlFile.Save(fs);
            }
        }

        private static StlTriangle CreateStlTriangle( Vector3 v1, Vector3 v2, Vector3 v3)
        {
            //Compute the triangle's normal
            Vector3 dir = Vector3.Normalize(Vector3.Cross(v2 - v1, v3 - v1));
            return new StlTriangle(CreateStlNormal(dir), CreateStlVertex(v1), CreateStlVertex(v2), CreateStlVertex(v3));
        }
        private static StlNormal CreateStlNormal(Vector3 normal)
        {
            return new StlNormal(normal.X, normal.Y, normal.Z);
        }
        private static StlVertex CreateStlVertex(Vector3 vertex)
        {
            return new StlVertex(vertex.X, vertex.Y, vertex.Z);
        }
    }
}
