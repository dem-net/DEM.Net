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
        private MeshService _meshService;
        private MeshReducer _meshReducer;
        private const string TERRAIN_NODE_NAME = "TerrainNode";
        private const string TERRAIN_SCENE_NAME = "TerrainScene";
        private const string TERRAIN_MESH_NAME = "TerrainMesh";

        public SharpGltfService(MeshService meshService, MeshReducer meshReducer, ILogger<SharpGltfService> logger = null)
        {
            _logger = logger;
            _meshService = meshService;
            _meshReducer = meshReducer;
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
        public ModelRoot CreateTerrainMesh(HeightMap heightMap, GenOptions options = GenOptions.None, Matrix4x4 vectorTransform = default, bool doubleSided = true, float meshReduceFactor = 0.5f)
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

            triangulation = _meshReducer.Decimate(triangulation, meshReduceFactor);

            // create a basic scene
            var model = CreateNewModel();
            var rnode = model.LogicalScenes.First()?.FindNode(n => n.Name == TERRAIN_NODE_NAME);
            var rmesh = rnode.Mesh = model.CreateMesh(TERRAIN_MESH_NAME);

            var material = model.CreateMaterial(string.Concat(TERRAIN_MESH_NAME, "Material"))
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

        public ModelRoot CloneWithMesh(ModelRoot inputModel, IndexedTriangulation indexedTriangulation)
        {
            var model = CreateNewModel();
            var rnode = model.LogicalScenes.First().CreateNode(TERRAIN_NODE_NAME);
            var rmesh = rnode.Mesh = FindOrCreateMesh(model, string.Concat(rnode.Name, "Mesh"));

            MaterialBuilder b = new MaterialBuilder();
            inputModel.LogicalMaterials.First().CopyTo(b);
            var material = Toolkit.CreateMaterial(model, b);

            // create mesh primitive
            MeshPrimitive primitive = rmesh.CreatePrimitive();

            if (indexedTriangulation.Colors != null && indexedTriangulation.Colors.Any())
            {
                primitive = primitive.WithVertexAccessor("POSITION", indexedTriangulation.Positions);
                primitive = primitive.WithVertexAccessor("COLOR_0", indexedTriangulation.Colors);
            }
            else
            {
                primitive = primitive.WithVertexAccessor("POSITION", indexedTriangulation.Positions);
            }
            var normals = _meshService.ComputeMeshNormals(indexedTriangulation.Positions, indexedTriangulation.Indices);
            primitive = primitive.WithVertexAccessor("NORMAL", normals.ToList());
            primitive = primitive.WithIndicesAccessor(PrimitiveType.TRIANGLES, indexedTriangulation.Indices);



            (Vector3 Min, Vector3 Max) coordBounds = CalculateBounds(indexedTriangulation.Positions);

            var coordSets = indexedTriangulation.Positions.Select(pos => new Vector2(
                MathHelper.Map(coordBounds.Min.X, coordBounds.Max.X, 0, 1, pos.X, true)
                , MathHelper.Map(coordBounds.Min.Z, coordBounds.Max.Z, 0, 1, pos.Z, true)
                ));

            primitive = primitive
                .WithVertexAccessor("TEXCOORD_0", coordSets.ToList());


            primitive = primitive.WithMaterial(material);
            return model;
        }
        public ModelRoot CloneGlbWithMesh(SharpGLTF.Schema2.ModelRoot inputModel, Triangulation outTriangulation)
        {
            ModelRoot outputModel = CloneWithMesh(inputModel, new IndexedTriangulation(outTriangulation));
            return outputModel;
        }

        public ModelRoot CreateTerrainMesh(HeightMap heightMap, PBRTexture textures, float reduceFactor)
        { return AddTerrainMesh(CreateNewModel(), heightMap, textures, reduceFactor); }
        public ModelRoot AddTerrainMesh(ModelRoot model, HeightMap heightMap, PBRTexture textures, float reduceFactor)
        {
            Triangulation triangulation = _meshService.TriangulateHeightMap(heightMap);

            triangulation = reduceFactor < 1f ? _meshReducer.Decimate(triangulation, reduceFactor) : triangulation;

            model = AddTerrainMesh(model, triangulation, textures);

            triangulation = null;

            return model;

        }
        public ModelRoot CreateTerrainMesh(Triangulation triangulation, PBRTexture textures, bool doubleSided = true)
        { return AddTerrainMesh(CreateNewModel(), triangulation, textures, doubleSided); }
        public ModelRoot AddTerrainMesh(ModelRoot model, Triangulation triangulation, PBRTexture textures, bool doubleSided = true)
        {
            var indexedTriangulation = new IndexedTriangulation(triangulation);
            var normals = _meshService.ComputeMeshNormals(indexedTriangulation.Positions, indexedTriangulation.Indices);
            model = AddMesh(model, TERRAIN_NODE_NAME, indexedTriangulation, normals, textures, doubleSided);

            indexedTriangulation.Clear();
            indexedTriangulation = null;
            return model;
        }
        public ModelRoot AddMesh(ModelRoot model, string nodeName, IndexedTriangulation indexedTriangulation, IEnumerable<Vector3> normals, PBRTexture textures, bool doubleSided = true)
        {
            // create a basic scene
            model = model ?? CreateNewModel();
            var rnode = model.LogicalScenes.First()?.FindNode(n => n.Name == nodeName);
            if (rnode == null)
            {
                rnode = model.LogicalScenes.First().CreateNode(nodeName);
            }

            var rmesh = rnode.Mesh = FindOrCreateMesh(model, string.Concat(rnode.Name, "Mesh"));


            var material = model.CreateMaterial(string.Concat(nodeName, "Material"))
              .WithPBRMetallicRoughness(new Vector4(1, 1, 1, 0.8f), textures?.BaseColorTexture?.FilePath, null, 0.15f, 0.85f)
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
            }
            else
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

        public ModelRoot AddMesh(ModelRoot model, string nodeName, TriangulationList<Vector3> triangulation, Vector4 color = default, bool doubleSided = true)
        {
            if (color == default || triangulation.HasColors) color = Vector4.One;

            var scene = model.UseScene(TERRAIN_SCENE_NAME);
            var rnode = scene.FindNode(n => n.Name == nodeName);
            if (rnode == null)
                rnode = scene.CreateNode(nodeName);
            var rmesh = rnode.Mesh = FindOrCreateMesh(model, string.Concat(nodeName, "Mesh"));


            var material = model.CreateMaterial(string.Concat(nodeName, "Material"))
                .WithPBRMetallicRoughness(color, null, null, 0, 1f)
                .WithDoubleSide(doubleSided);
            material.Alpha = (color.W < 1.0 || (triangulation.HasColors && triangulation.Colors.Any(c => c.W < 1.0))) ?
                                SharpGLTF.Schema2.AlphaMode.BLEND
                                : SharpGLTF.Schema2.AlphaMode.OPAQUE;

            // Rotate for glTF compliance
            triangulation.Positions.ToGlTFSpace();

            var normals = _meshService.ComputeMeshNormals(triangulation.Positions, triangulation.Indices);


            // create mesh primitive
            var primitive = rmesh.CreatePrimitive()
                .WithVertexAccessor("POSITION", triangulation.Positions)
                .WithVertexAccessor("NORMAL", normals.ToList())
                .WithIndicesAccessor(PrimitiveType.TRIANGLES, triangulation.Indices);

            if (triangulation.HasColors)
            {
                primitive = primitive.WithVertexAccessor("COLOR_0", triangulation.Colors);
            }

            primitive = primitive.WithMaterial(material);
            return model;
        }
        public ModelRoot AddLine(ModelRoot model, string nodeName, IEnumerable<GeoPoint> gpxPointsElevated, Vector4 color, float trailWidthMeters, Matrix4x4 transform = default)
        {
            model = model ?? CreateNewModel();
            var scene = model.UseScene(TERRAIN_SCENE_NAME);
            var rnode = scene.FindNode(n => n.Name == nodeName);
            if (rnode == null)
                rnode = scene.CreateNode(nodeName);
            var rmesh = rnode.Mesh = FindOrCreateMesh(model, string.Concat(nodeName, "Mesh"));


            var material = model.CreateMaterial(string.Concat(nodeName, "Material"))
               .WithPBRMetallicRoughness(color, null, null, 0, 0.9f)
              .WithDoubleSide(true);
            material.Alpha = SharpGLTF.Schema2.AlphaMode.BLEND;


            var triangulation = _meshService.GenerateTriangleMesh_Line(gpxPointsElevated, trailWidthMeters, transform);
            var normals = _meshService.ComputeMeshNormals(triangulation.Positions, triangulation.Indices);


            // create mesh primitive
            var primitive = rmesh.CreatePrimitive()
                .WithVertexAccessor("POSITION", triangulation.Positions)
                .WithVertexAccessor("NORMAL", normals.ToList())
                .WithIndicesAccessor(PrimitiveType.TRIANGLES, triangulation.Indices);

            primitive = primitive.WithMaterial(material);
            return model;
        }
        public ModelRoot AddLines(ModelRoot model, string nodeName, IEnumerable<(IEnumerable<GeoPoint> points, float trailWidthMeters)> lines, Vector4 color, Matrix4x4 transform = default)
        {
            List<Vector3> positions = new List<Vector3>();
            List<int> indices = new List<int>();

            foreach (var line in lines)
            {
                TriangulationList<Vector3> triangulation = _meshService.GenerateTriangleMesh_Line(line.points, line.trailWidthMeters, transform);

                indices.AddRange(triangulation.Indices.Select(i => i + positions.Count)); // offset indices, adding last positions count
                positions.AddRange(triangulation.Positions);
            }
            if (positions.Count == 0)
            {
                _logger.LogWarning($"Lines triangulation has 0 positions. No data written to model");
                return model;
            }

            var scene = model.UseScene(TERRAIN_SCENE_NAME);
            var rnode = scene.FindNode(n => n.Name == nodeName);
            if (rnode == null)
                rnode = scene.CreateNode(nodeName);
            var rmesh = rnode.Mesh = FindOrCreateMesh(model, string.Concat(nodeName, "Mesh"));


            var material = model.CreateMaterial(string.Concat(nodeName, "Material"))
               .WithPBRMetallicRoughness(color, null, null, 0, 0.9f)
              .WithDoubleSide(true);
            material.Alpha = SharpGLTF.Schema2.AlphaMode.BLEND;


            var normals = _meshService.ComputeMeshNormals(positions, indices);


            // create mesh primitive
            var primitive = rmesh.CreatePrimitive()
                .WithVertexAccessor("POSITION", positions)
                .WithVertexAccessor("NORMAL", normals.ToList())
                .WithIndicesAccessor(PrimitiveType.TRIANGLES, indices)
                .WithMaterial(material);

            return model;
        }
    }
}
