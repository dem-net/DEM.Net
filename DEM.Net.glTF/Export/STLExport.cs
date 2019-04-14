//
// STLExport.cs
//
// Author:
//       Xavier Fischer
//
// Copyright (c) 2019 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

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
