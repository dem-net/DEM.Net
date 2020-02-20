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

    public partial class SharpGltfService
    {
        private readonly ILogger<SharpGltfService> _logger;
        private IMeshService _meshService;
        private const string TERRAIN_NODE_NAME = "TerrainNode";
        private const string TERRAIN_SCENE_NAME = "TerrainScene";
        private const string TERRAIN_MESH_NAME = "TerrainMesh";

        public SharpGltfService(IMeshService meshService, ILogger<SharpGltfService> logger = null)
        {
            _logger = logger;
            _meshService = meshService;
        }

        public ModelRoot CreateNewModel()
        {
            // create a basic scene
            var model = ModelRoot.CreateModel();
            model.Asset.Copyright = "DEM Net Elevation API https://elevationapi.com";
            model.Asset.Generator = "DEM Net Elevation API https://elevationapi.com with SharpGLTF";

            var scene = model.UseScene(TERRAIN_SCENE_NAME);
            scene.CreateNode(TERRAIN_NODE_NAME);

            return model;
        }
        public ModelRoot CreateTerrainMesh(HeightMap heightMap, GenOptions options = GenOptions.None, Matrix4x4 vectorTransform = default, bool doubleSided = true)
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
            var model = CreateNewModel();
            var rnode = model.LogicalScenes.First()?.FindNode(n => n.Name == TERRAIN_NODE_NAME);
            var rmesh = rnode.Mesh = model.CreateMesh(TERRAIN_MESH_NAME);

            var material = model.CreateMaterial("Default")
              .WithPBRMetallicRoughness()
              .WithDoubleSide(doubleSided);

            var indexedTriangulation = new IndexedTriangulation(triangulation, vectorTransform);

            // create mesh primitive
            var primitive = rmesh.CreatePrimitive()
                .WithVertexAccessor("POSITION", indexedTriangulation.Positions);

            if (options.HasFlag(GenOptions.Normals))
            {
                var normals = _meshService.ComputeMeshNormals(indexedTriangulation.Positions, indexedTriangulation.Indices);
                primitive = primitive.WithVertexAccessor("NORMAL", normals.ToList());
            }

            primitive = primitive.WithIndicesAccessor(PrimitiveType.TRIANGLES, indexedTriangulation.Indices)
                        .WithMaterial(material);
            return model;
        }

        public ModelRoot CreateTerrainMesh(HeightMap heightMap, PBRTexture textures)
        { return AddTerrainMesh(CreateNewModel(), heightMap, textures); }
        public ModelRoot AddTerrainMesh(ModelRoot model, HeightMap heightMap, PBRTexture textures)
        {
            Triangulation triangulation = _meshService.TriangulateHeightMap(heightMap);

            return AddTerrainMesh(model, triangulation, textures);
        }
        public ModelRoot CreateTerrainMesh(Triangulation triangulation, PBRTexture textures, bool doubleSided = true)
        { return AddTerrainMesh(CreateNewModel(), triangulation, textures, doubleSided); }
        public ModelRoot AddTerrainMesh(ModelRoot model, Triangulation triangulation, PBRTexture textures, bool doubleSided = true)
        {
            var indexedTriangulation = new IndexedTriangulation(triangulation);
            var normals = _meshService.ComputeMeshNormals(indexedTriangulation.Positions, indexedTriangulation.Indices);
            return AddMesh(model, indexedTriangulation, normals, textures, doubleSided);
        }
        public ModelRoot AddMesh(ModelRoot model, IndexedTriangulation indexedTriangulation, IEnumerable<Vector3> normals, PBRTexture textures, bool doubleSided = true)
        {
            // create a basic scene
            model = model ?? CreateNewModel();
            var rnode = model.LogicalScenes.First()?.FindNode(n => n.Name == TERRAIN_NODE_NAME);
            var rmesh = rnode.Mesh = FindOrCreateMesh(model, TERRAIN_MESH_NAME);


            var material = model.CreateMaterial("Default")
              .WithPBRMetallicRoughness(Vector4.One, textures?.BaseColorTexture?.FilePath, null, 0, 1)
              .WithDoubleSide(doubleSided);
            if (textures != null && textures.NormalTexture != null)
            {
                material.WithChannelTexture("NORMAL", 0, textures.NormalTexture.FilePath);
            }

            // create mesh primitive
            MeshPrimitive primitive = rmesh.CreatePrimitive();
            

            if (indexedTriangulation.Colors != null && indexedTriangulation.Colors.Any())
            {
                primitive = primitive.WithVertexAccessor("POSITION", indexedTriangulation.Positions);
                primitive = primitive.WithVertexAccessor("COLOR_0", indexedTriangulation.Colors);
            }else
            {
                primitive = primitive.WithVertexAccessor("POSITION", indexedTriangulation.Positions);
            }

            if (normals != null)
            {
                primitive = primitive.WithVertexAccessor("NORMAL", normals.ToList());
            }
            primitive = primitive.WithIndicesAccessor(PrimitiveType.TRIANGLES, indexedTriangulation.Indices);

            if (textures != null && textures.TextureCoordSets == null)
            {
                (Vector3 Min, Vector3 Max) coordBounds = CalculateBounds(indexedTriangulation.Positions);

                textures.TextureCoordSets = indexedTriangulation.Positions.Select(pos => new Vector2(
                    MathHelper.Map(coordBounds.Min.X, coordBounds.Max.X, 0, 1, pos.X, true)
                    , MathHelper.Map(coordBounds.Min.Z, coordBounds.Max.Z, 0, 1, pos.Z, true)
                    ));

                primitive = primitive
                    .WithVertexAccessor("TEXCOORD_0", textures.TextureCoordSets.ToList());
            }

            primitive = primitive.WithMaterial(material);
            return model;
        }

        public ModelRoot GenerateTriangleMesh(List<GeoPoint> points, List<int> indices, PBRTexture textures)
        {
            Triangulation triangulation = new Triangulation(points, indices);
            return CreateTerrainMesh(triangulation, textures);
        }

        private (Vector3 min, Vector3 max) CalculateBounds(List<Vector3> positions)
        {
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            foreach (var pos in positions)
            {
                // for UV coords
                min.X = Math.Min(pos.X, min.X);
                min.Y = Math.Min(pos.Y, min.Y);
                min.Z = Math.Min(pos.Z, min.Z);

                max.X = Math.Max(pos.X, max.X);
                max.Y = Math.Max(pos.Y, max.Y);
                max.Z = Math.Max(pos.Z, max.Z);
            }

            return (min, max);
        }

        public Mesh FindOrCreateMesh(ModelRoot model, string meshName)
        {
            var mesh = model.LogicalMeshes.FirstOrDefault(m => m.Name == meshName) ?? model.CreateMesh(meshName);
            return mesh;
        }
        public ModelRoot AddLine(ModelRoot model, IEnumerable<GeoPoint> gpxPointsElevated, Vector4 color, float trailWidthMeters)
        {
            var scene = model.UseScene(TERRAIN_SCENE_NAME);
            var rnode = scene.FindNode(n => n.Name == TERRAIN_NODE_NAME);
            var rmesh = rnode.Mesh = FindOrCreateMesh(model, TERRAIN_MESH_NAME);


            var material = model.CreateMaterial("Line")
               .WithPBRMetallicRoughness(color, null, null, 0, 0.9f)
              .WithDoubleSide(true);
            material.Alpha = SharpGLTF.Schema2.AlphaMode.BLEND;


            var triangulation = _meshService.GenerateTriangleMesh_Line(gpxPointsElevated, trailWidthMeters);
            var indexedTriangulation = new IndexedTriangulation(triangulation.positions, triangulation.indexes);
            var normals = _meshService.ComputeMeshNormals(indexedTriangulation.Positions, indexedTriangulation.Indices);


            // create mesh primitive
            var primitive = rmesh.CreatePrimitive()
                .WithVertexAccessor("POSITION", indexedTriangulation.Positions)
                .WithVertexAccessor("NORMAL", normals.ToList())
                .WithIndicesAccessor(PrimitiveType.TRIANGLES, indexedTriangulation.Indices);

            primitive = primitive.WithMaterial(material);
            return model;
        }
    }
}
