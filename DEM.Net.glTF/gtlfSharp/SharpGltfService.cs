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

      
        public IMeshBuilder<MaterialBuilder> CreateTerrainMesh_NoTexture(HeightMap heightMap)
        {  Triangulation triangulation = _meshService.TriangulateHeightMap(heightMap);
            return CreateTerrainMesh_NoTexture(triangulation);
        }
        public IMeshBuilder<MaterialBuilder> CreateTerrainMesh_NoTextureColor(HeightMap heightMap, Func<float, Vector4> colorFunc = null)
        {
            Triangulation triangulation = _meshService.TriangulateHeightMap(heightMap);
            return CreateTerrainMesh_NoTexture(triangulation, colorFunc);
        }

        public IMeshBuilder<MaterialBuilder> CreateTerrainMesh_NoTexture(Triangulation triangulation)
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
                    var a = indexedTriangulation[i].ToVector3();
                    var b = indexedTriangulation[i+1].ToVector3();
                    var c = indexedTriangulation[i+2].ToVector3();

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

        public IMeshBuilder<MaterialBuilder> CreateTerrainMesh_NoTexture(Triangulation triangulation, Func<float, Vector4> colorFunc = null)
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
                var a = indexedTriangulation[i].ToVector3();
                var b = indexedTriangulation[i + 1].ToVector3();
                var c = indexedTriangulation[i + 2].ToVector3();

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

        public ModelRoot CreateModel(HeightMap heightMap, Func<float, Vector4> colorFunc = null)
        {
            Triangulation triangulation = _meshService.TriangulateHeightMap(heightMap);

            // create a basic scene
            var model = ModelRoot.CreateModel();
            var scene = model.UseScene("Default");
            var rnode = scene.CreateNode("Terrain");
            var rmesh = rnode.Mesh = model.CreateMesh("Terrain");

            var material = model.CreateMaterial("Default")
              .WithPBRMetallicRoughness()
              .WithDoubleSide(true);

            var indexedTriangulation = new IndexedTriangulation(triangulation);

            var positions = triangulation.Positions.Select(p => p.ToVector3()).ToList();
            var indices = triangulation.Indices.ToList();
            // create mesh primitive
            var primitive = rmesh.CreatePrimitive()
                .WithVertexAccessor("POSITION", positions)
                .WithIndicesAccessor(PrimitiveType.TRIANGLES, indices)
                .WithMaterial(material);

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
            private readonly GeoPoint[] positions;

            public int[] indices { get; }

            public IndexedTriangulation(Triangulation triangulation)
            {
                this.Triangulation = triangulation;
                positions = triangulation.Positions.ToArray();
                indices = triangulation.Indices.ToArray();
            }

            public GeoPoint this[int index]
            {
                get
                {

                    return positions[indices[index]];
                }


            }
        }

    }
}
