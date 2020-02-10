// Triangulation.cs
//
// Author:
//       Xavier Fischer
//
// Copyright (c) 2019 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the right
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

using DEM.Net.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core
{
    public class Triangulation : Triangulation<GeoPoint>
    {
        public Triangulation() : base() { }
        public Triangulation(IEnumerable<GeoPoint> positions, IEnumerable<int> indices)
             : base(positions, indices) { }
    }
    public class TriangulationNormals : Triangulation<Vector3>
    {
        public IEnumerable<Vector3> Normals { get; internal set; }
        public TriangulationNormals(IEnumerable<Vector3> positions, IEnumerable<int> indices, IEnumerable<Vector3> normals)
            : base(positions, indices)
        {
            Normals = normals;
        }
    }

    public class TriangulationList<T>
    {
        public List<T> Positions { get; internal set; }
        public List<int> Indices { get; internal set; }

        public TriangulationList()
        {
            Positions = new List<T>();
            Indices = new List<int>();
        }
        public TriangulationList(List<T> positions, List<int> indices)
        {
            Positions = positions;
            Indices = indices;
        }

        public int NumPositions => Positions.Count;
        public int NumTriangles => NumIndices / 3;
        public int NumIndices => Indices.Count;
    }

    public class Triangulation<T>
    {
        public IEnumerable<T> Positions { get; internal set; }
        public IEnumerable<int> Indices { get; internal set; }

        public Triangulation()
        {

        }
        public Triangulation(IEnumerable<T> positions, IEnumerable<int> indices)
        {
            Positions = positions;
            Indices = indices;
        }

        public int NumPositions { get; internal set; }
        public int NumTriangles => NumIndices / 3;
        public int NumIndices { get; internal set; }
    }
}
