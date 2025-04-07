using DEM.Net.Core;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace DEM.Net.glTF.SharpglTF
{

    public record Triangle3D(Vector4 A, Vector4 B, Vector4 C, int Index);


    public class IndexedTriangulation
    {
        private Triangulation Triangulation;

        public List<Vector3> Positions { get; set; }
        public List<Vector4> Colors { get; private set; }
        public List<int> Indices { get; private set; }

        public IndexedTriangulation(Triangulation triangulation, Matrix4x4 vectorTransform = default, bool transformToGltfCoords = true)
        {
            if (vectorTransform == default)
                vectorTransform = Matrix4x4.Identity;
            this.Triangulation = triangulation;
            Positions = new List<Vector3>(triangulation.NumPositions);
            if (transformToGltfCoords)
            {
                Positions.AddRange(triangulation.Positions.Select(p => Vector3.Transform(p.ToVector3GlTFSpace(), vectorTransform)));
            }
            else
            {
                Positions.AddRange(triangulation.Positions.Select(p => Vector3.Transform(p.AsVector3(), vectorTransform)));
            }

            Indices = new(triangulation.NumIndices);
            Indices.AddRange(triangulation.Indices);
        }
        public IndexedTriangulation(Triangulation triangulation, Func<GeoPoint, Vector3> transform)
        {
            this.Triangulation = triangulation;
            Positions = new List<Vector3>(triangulation.NumPositions);
            Positions.AddRange(triangulation.Positions.Select(p => transform(p)));
            Indices = new(triangulation.NumIndices);
            Indices.AddRange(triangulation.Indices);
        }
        public IndexedTriangulation(IEnumerable<Vector3> positions, IEnumerable<int> indices, Matrix4x4 vectorTransform = default)
        {
            if (vectorTransform == default)
                vectorTransform = Matrix4x4.Identity;
            Positions = vectorTransform == default ? positions.ToList() : positions.Select(p => Vector3.Transform(p, vectorTransform)).ToList();
            Indices = indices.ToList();
        }

        public IndexedTriangulation(TriangulationNormals triangulation)
        {
            Positions = triangulation.Positions.ToList();
            Indices = triangulation.Indices.ToList();
            Colors = triangulation.Colors?.ToList();
        }


        public Vector3 this[int index]
        {
            get
            {

                return Positions[Indices[index]];
            }


        }

        internal void Clear()
        {
            Positions?.Clear();
            Indices?.Clear();
            Colors?.Clear();

            Positions = null;
            Indices = null;
            Colors = null;
        }
    }

    public class Triangulation3DModel : IndexedTriangulation
    {
        public Triangulation3DModel(Triangulation triangulation, Matrix4x4 vectorTransform = default, bool transformToGltfCoords = true)
            : base(triangulation, vectorTransform, transformToGltfCoords) { }
        public Triangulation3DModel(IEnumerable<Vector3> positions, IEnumerable<int> indices, Matrix4x4 vectorTransform = default)
            : base(positions, indices, vectorTransform) { }
        public Triangulation3DModel(Triangulation triangulation, Func<GeoPoint, Vector3> transform)
            : base(triangulation, transform) { }

        private List<Triangle3D> _triangles = null;
        public List<Triangle3D> Triangles
        {
            get
            {
                if (_triangles == null)
                {
                    _triangles = new List<Triangle3D>(Indices.Count / 3);

                    int triangleIndex = 0;
                    for (int i = 0; i < Indices.Count; i += 3)
                    {
                        _triangles.Add(new(
                            new Vector4(this[i], 1),
                            new Vector4(this[i + 1], 1),
                            new Vector4(this[i + 2], 1),
                            triangleIndex++
                            ));
                    }
                }
                return _triangles;
            }
        }

        public List<Triangle3D> ProjectedTriangles { get; set; }
    }
}
