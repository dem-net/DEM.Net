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
using NetTopologySuite.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
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
        public List<Vector4> Colors { get; set; }
        public TriangulationNormals(IEnumerable<Vector3> positions, IEnumerable<int> indices, IEnumerable<Vector3> normals, List<Vector4> colors)
            : base(positions, indices)
        {
            Normals = normals;
            Colors = colors;
        }
    }

    public class TriangulationList<T>
    {
        public List<T> Positions { get; set; } 
        public List<Vector4> Colors { get; set; }
        public bool HasColors => Colors?.Count > 0;
        public List<int> Indices { get; internal set; }

        public TriangulationList()
        {
            Positions = new List<T>();
            Indices = new List<int>();
            Colors = new List<Vector4>();
        }
        public TriangulationList(List<T> positions, List<int> indices)
        {
            Positions = positions;
            Indices = indices;
        }
        public TriangulationList(List<T> positions, List<Vector4> colors, List<int> indices)
        {
            Positions = positions;
            Indices = indices;
            Colors = colors;
        }

        public int NumPositions => Positions.Count;
        public int NumTriangles => NumIndices / 3;
        public int NumIndices => Indices.Count;

        public static TriangulationList<T> operator +(TriangulationList<T> a, TriangulationList<T> b)
        {
            if (Object.ReferenceEquals(a, null))
                return b;
            if (Object.ReferenceEquals(b, null))
                return a;

            return new TriangulationList<T>(
                positions: a.Positions.Concat(b.Positions).ToList(),
                colors: a.Colors.Concat(b.Colors).ToList(),
                indices: a.Indices.Concat(b.Indices.Select(i => i + a.NumPositions)).ToList());
        }

        public TriangulationList<T> Clone()
        {
            return new TriangulationList<T>(new List<T>(Positions), new List<Vector4>(Colors), new List<int>(Indices));
        }
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

        public static Triangulation<T> operator +(Triangulation<T> a, Triangulation<T> b)
        {
            if (Object.ReferenceEquals(a, null))
                return b;
            if (Object.ReferenceEquals(b, null))
                return a;

            return new Triangulation<T>(
                positions: a.Positions.Concat(b.Positions),
                indices: a.Indices.Concat(b.Indices.Select(i => i + a.NumPositions)));
        }
    }
}
