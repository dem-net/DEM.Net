using DEM.Net.Core;
using DEM.Net.Core.Imagery;
using Microsoft.Extensions.Logging;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Schema2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace DEM.Net.glTF.SharpglTF
{

    public class SharpGltfService
    {
        private readonly ILogger<glTFService> _logger;
        private IMeshService _meshService;

        public SharpGltfService(IMeshService meshService, ILogger<glTFService> logger = null)
        {
            _logger = logger;
            _meshService = meshService;
        }


        public ModelRoot CreateModel(IMeshBuilder<MaterialBuilder> terrain)
        {
            // create a new gltf model
            var model = ModelRoot.CreateModel();

            // add all meshes (just one in this case) to the model
            model.CreateMeshes(terrain);

            // create a scene, a node, and assign the first mesh (the terrain)
            model.UseScene("Default")
                .CreateNode().WithMesh(model.LogicalMeshes[0]);

            return model;
        }


        public IMeshBuilder<MaterialBuilder> CreateTerrainMesh(HeightMap heightMap)
        {
            Triangulation triangulation = _meshService.TriangulateHeightMap(heightMap);
            return CreateTerrainMesh(triangulation);
        }
        public IMeshBuilder<MaterialBuilder> CreateTerrainMesh(HeightMap heightMap, Func<float, Vector4> colorFunc = null)
        {
            Triangulation triangulation = _meshService.TriangulateHeightMap(heightMap);
            return CreateTerrainMesh(triangulation, colorFunc);
        }

        public IMeshBuilder<MaterialBuilder> CreateTerrainMesh(Triangulation triangulation)
        {
            var material = new MaterialBuilder("TerrainMaterial")
                .WithMetallicRoughnessShader()
                .WithDoubleSide(true);

            var indexedTriangulation = new IndexedTriangulation(triangulation);

            // we create a MeshBuilder
            var terrainMesh = new MeshBuilder<VertexPosition>("terrain");

            // fill the MeshBuilder with quads using the heightFunction.
            for (int i = 0; i < triangulation.NumIndices; i += 3)
            {
                var a = indexedTriangulation[i];
                var b = indexedTriangulation[i + 1];
                var c = indexedTriangulation[i + 2];

                terrainMesh
                    .UsePrimitive(material)
                    .AddTriangle(
                    new VertexPosition(a)
                    , new VertexPosition(b)
                    , new VertexPosition(c));

            }

            terrainMesh.Validate();

            return terrainMesh;
        }

        public IMeshBuilder<MaterialBuilder> CreateTerrainMesh(Triangulation triangulation, Func<float, Vector4> colorFunc = null)
        {
            var material = new MaterialBuilder("TerrainMaterial")
                .WithMetallicRoughnessShader()
                .WithDoubleSide(true);

            var indexedTriangulation = new IndexedTriangulation(triangulation);

            // we create a MeshBuilder
            var terrainMesh = new MeshBuilder<VertexPosition, VertexColor1>("terrain");

            // fill the MeshBuilder with quads using the heightFunction.
            for (int i = 0; i < triangulation.NumIndices; i += 3)
            {
                var a = indexedTriangulation[i];
                var b = indexedTriangulation[i + 1];
                var c = indexedTriangulation[i + 2]; ;

                var ac = new Vector4(1, 0, 0, 1);
                var bc = new Vector4(0, 1, 0, 1);
                var cc = new Vector4(0, 0, 1, 1);

                terrainMesh
                    .UsePrimitive(material)
                    .AddTriangle((a, ac), (b, bc), (c, cc));

            }

            terrainMesh.Validate();

            return terrainMesh;
        }

        public ModelRoot CreateModel(HeightMap heightMap, GenOptions options = GenOptions.None, Matrix4x4 vectorTransform = default)
        {
            Triangulation triangulation = default;
            if (options.HasFlag(GenOptions.BoxedBaseElevation0))
            {
                triangulation = _meshService.GenerateTriangleMesh_Boxed(heightMap, BoxBaseThickness.FixedElevation, 0);
            }
            else if (options.HasFlag(GenOptions.BoxedBaseElevationMin))
            {
                triangulation = _meshService.GenerateTriangleMesh_Boxed(heightMap, BoxBaseThickness.FromMinimumPoint, 5);
            }
            else
            {
                triangulation = _meshService.TriangulateHeightMap(heightMap);
            }


            // create a basic scene
            var model = ModelRoot.CreateModel();
            var scene = model.UseScene("Default");
            var rnode = scene.CreateNode("Terrain");
            var rmesh = rnode.Mesh = model.CreateMesh("Terrain");

            var material = model.CreateMaterial("Default")
              .WithPBRMetallicRoughness()
              .WithDoubleSide(true);

            var indexedTriangulation = new IndexedTriangulation(triangulation, vectorTransform);

            // create mesh primitive
            var primitive = rmesh.CreatePrimitive()
                .WithVertexAccessor("POSITION", indexedTriangulation.Positions);

            if (options.HasFlag(GenOptions.Normals))
            {
                var normals = _meshService.ComputeNormals(indexedTriangulation.Positions, indexedTriangulation.Indices);
                primitive = primitive.WithVertexAccessor("NORMAL", normals.ToList());
            }

            primitive = primitive.WithIndicesAccessor(PrimitiveType.TRIANGLES, indexedTriangulation.Indices)
                        .WithMaterial(material);
            return model;
        }
        public ModelRoot CreateSTLModel(HeightMap heightMap, GenOptions options = GenOptions.None, Func<float, Vector4> colorFunc = null)
        {
            var model = CreateModel(heightMap, GenOptions.BoxedBaseElevationMin);

            // create mesh primitive
            var primitive = model.LogicalMeshes.First().Primitives.First();
            return model;
        }

        private class IndexedHeightMap
        {
            private readonly HeightMap _heightMap;
            private readonly GeoPoint[] positions;
            public IndexedHeightMap(HeightMap heightMap)
            {
                this._heightMap = heightMap;
                positions = heightMap.Coordinates.ToArray();
            }

            public GeoPoint this[int x, int y]
            {
                get
                {

                    return positions[x + y * _heightMap.Width];
                }


            }
        }
        private class IndexedTriangulation
        {
            public Triangulation Triangulation { get; private set; }
            public List<Vector3> Positions { get; private set; }
            public List<int> Indices { get; private set; }

            public IndexedTriangulation(Triangulation triangulation, Matrix4x4 vectorTransform = default)
            {
                this.Triangulation = triangulation;
                Positions = triangulation.Positions.Select(p => Vector3.Transform(p.ToVector3(), vectorTransform)).ToList();
                Indices = triangulation.Indices.ToList();
            }

            public Vector3 this[int index]
            {
                get
                {

                    return Positions[Indices[index]];
                }


            }
        }

    }
}
