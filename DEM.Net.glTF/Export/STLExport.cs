using AssetGenerator.Runtime;
using IxMilia.Stl;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.glTF.Export
{
    public class STLExportService
    {
        public void STLExport(MeshPrimitive mesh, string fileName, bool ascii = true)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
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
            string folder = System.IO.Path.GetDirectoryName(fileName);
            if (!System.IO.Directory.Exists(folder))
            {
                System.IO.Directory.CreateDirectory(folder);
            }
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                stlFile.Save(fs, ascii);
            }
        }

        private StlTriangle CreateStlTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            //Compute the triangle's normal
            Vector3 dir = Vector3.Normalize(Vector3.Cross(v2 - v1, v3 - v1));
            return new StlTriangle(CreateStlNormal(dir), CreateStlVertex(v1), CreateStlVertex(v2), CreateStlVertex(v3));
        }
        private StlNormal CreateStlNormal(Vector3 normal)
        {
            return new StlNormal(normal.X, normal.Y, normal.Z);
        }
        private StlVertex CreateStlVertex(Vector3 vertex)
        {
            return new StlVertex(vertex.X, vertex.Y, vertex.Z);
        }
    }
}
