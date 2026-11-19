using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using DEM.Net.Core;
using DEM.Net.Core.Imagery;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Schema2;

namespace DEM.Net.glTF.SharpglTF;


public partial class SharpGltfService(MeshService meshService, MeshReducer meshReducer, ILogger<SharpGltfService>? logger = null)
{
    private readonly ILogger<SharpGltfService> _logger = logger ?? NullLogger<SharpGltfService>.Instance;
    private MeshService _meshService = meshService;
    private MeshReducer _meshReducer = meshReducer;
    private const string TERRAIN_NODE_NAME = "TerrainNode";
    private const string TERRAIN_SCENE_NAME = "TerrainScene";
    private const string TERRAIN_MESH_NAME = "TerrainMesh";
    public const string TERRAIN_BOX_NODE_NAME = "TerrainBox";
    public const string TERRAIN_BOX_MONUMENTS_NODE_NAME = "TerrainBoxMonuments";

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

    /// <summary>
    /// Terrain mesh of the height map in a single primitive (the STL export reads it), reduced to
    /// <paramref name="meshReduceFactor"/> of the triangles.
    /// </summary>
    /// <remarks>
    /// The whole grid is reduced with Delatin (<see cref="MeshService.TriangulateHeightMapReduced"/>), and the
    /// boxed options add walls and a bottom along the reduced outline (<see cref="TerrainBoxBuilder"/>), closed with
    /// the terrain. <see cref="GenOptions.CropToNonEmpty"/> isn't a whole grid: it is reduced with
    /// <see cref="MeshReducer"/> (QEM), without box.
    /// </remarks>
    public ModelRoot CreateTerrainMesh(HeightMap heightMap, GenOptions options = GenOptions.None, Matrix4x4 vectorTransform = default, bool doubleSided = true, float meshReduceFactor = 0.5f)
    {
        if (options.HasFlag(GenOptions.CropToNonEmpty))
            return CreateTerrainMesh(_meshService.TriangulateHeightMapFiltered(heightMap), options, vectorTransform, doubleSided, meshReduceFactor);

        Triangulation triangulation = _meshService.TriangulateHeightMapReduced(heightMap, meshReduceFactor);
        List<Vector3> positions = [.. triangulation.Positions.ToVector3GlTFSpace()];
        List<int> indices = [.. triangulation.Indices];

        // Box base: elevation 0, or 5 units under the lowest grid point (reduction may drop it from the mesh)
        float? baseElevation = options.HasFlag(GenOptions.BoxedBaseElevation0) ? 0f
            : options.HasFlag(GenOptions.BoxedBaseElevationMin) ? (float)heightMap.Coordinates.Min(p => p.Elevation ?? 0) - 5f
            : null;
        if (baseElevation is float baseY)
        {
            var box = TerrainBoxBuilder.Build(positions, indices, baseMargin: MathF.Max(positions.Min(p => p.Y) - baseY, 0f));
            if (box != null)
            {
                int offset = positions.Count;
                positions.AddRange(box.Positions);
                indices.AddRange(box.Indices.Select(i => i + offset));
            }
        }

        return CreateTerrainModel(new IndexedTriangulation(positions, indices, vectorTransform), options, doubleSided);
    }

    /// <summary>Terrain mesh of any triangulation, reduced with <see cref="MeshReducer"/> (QEM)</summary>
    public ModelRoot CreateTerrainMesh(Triangulation triangulation, GenOptions options = GenOptions.None, Matrix4x4 vectorTransform = default, bool doubleSided = true, float meshReduceFactor = 0.5f)
    {
        triangulation = _meshReducer.Decimate(triangulation, meshReduceFactor);
        return CreateTerrainModel(new IndexedTriangulation(triangulation, vectorTransform), options, doubleSided);
    }

    private ModelRoot CreateTerrainModel(IndexedTriangulation indexedTriangulation, GenOptions options, bool doubleSided)
    {
        // create a basic scene
        var model = CreateNewModel();
        var rnode = model.LogicalScenes.First()?.FindNode(n => n.Name == TERRAIN_NODE_NAME);
        var rmesh = rnode!.Mesh = model.CreateMesh(TERRAIN_MESH_NAME);

        var material = model.CreateMaterial(string.Concat(TERRAIN_MESH_NAME, "Material"))
          .WithPBRMetallicRoughness()
          .WithDoubleSide(doubleSided);

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

    /// <param name="box">If set, adds walls and a bottom under the terrain (<see cref="AddTerrainBox"/>)</param>
    public ModelRoot CreateTerrainMesh(HeightMap heightMap, PBRTexture? textures, float reduceFactor, TerrainBoxOptions? box = null)
    { return AddTerrainMesh(CreateNewModel(), heightMap, textures, reduceFactor, box); }

    /// <summary>Terrain without its missing or zero elevation regions: not a whole grid, reduced with <see cref="MeshReducer"/> (QEM)</summary>
    public ModelRoot CreateTerrainMesh_NonRectangular(HeightMap heightMap, PBRTexture? textures, float reduceFactor)
    {
        var model = CreateNewModel();
        Triangulation triangulation = _meshService.TriangulateHeightMapFiltered(heightMap);

        triangulation = _meshReducer.Decimate(triangulation, reduceFactor);

        model = AddTerrainMesh(model, triangulation, textures);
        return model;
    }

    /// <summary>Terrain mesh of the whole height map grid, reduced with Delatin (<see cref="MeshService.TriangulateHeightMapReduced"/>)</summary>
    /// <param name="box">If set, adds walls and a bottom under the terrain (<see cref="AddTerrainBox"/>)</param>
    public ModelRoot AddTerrainMesh(ModelRoot? model, HeightMap heightMap, PBRTexture? textures, float reduceFactor, TerrainBoxOptions? box = null)
    {
        Triangulation triangulation = _meshService.TriangulateHeightMapReduced(heightMap, reduceFactor);

        model = AddTerrainMesh(model, triangulation, textures, box: box);
        return model;

    }
    public ModelRoot CreateTerrainMesh(Triangulation triangulation, PBRTexture? textures, bool doubleSided = true)
    { return AddTerrainMesh(CreateNewModel(), triangulation, textures, doubleSided); }
    /// <param name="box">If set, adds walls and a bottom under the terrain (<see cref="AddTerrainBox"/>)</param>
    public ModelRoot AddTerrainMesh(ModelRoot? model, Triangulation triangulation, PBRTexture? textures, bool doubleSided = true, Matrix4x4 matrix = default, TerrainBoxOptions? box = null)
    {
        var indexedTriangulation = new IndexedTriangulation(triangulation, matrix);
        var normals = _meshService.ComputeMeshNormals(indexedTriangulation.Positions, indexedTriangulation.Indices);
        model = AddMesh(model, TERRAIN_NODE_NAME, indexedTriangulation, normals, textures, doubleSided);
        if (box != null)
            model = AddTerrainBox(model, indexedTriangulation.Positions, indexedTriangulation.Indices, box);

        indexedTriangulation.Clear();
        indexedTriangulation = null;
        return model;
    }
    public ModelRoot AddMesh(ModelRoot? model, string nodeName, IndexedTriangulation indexedTriangulation, IEnumerable<Vector3>? normals, PBRTexture? textures, bool doubleSided = true)
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
        // Specular F0 = 0 (KHR_materials_specular): terrain is matte; the default 4 % dielectric
        // reflection washes out the imagery at grazing angles. Read by three.js and Sketchfab.
        material.InitializePBRMetallicRoughness("Specular");
        material.FindChannel("SpecularFactor")!.Value.SetFactor("SpecularFactor", 0f);
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

    /// <summary>
    /// Adds walls and a bottom under a terrain mesh, as their own node (<see cref="TERRAIN_BOX_NODE_NAME"/>)
    /// with one solid-color material and flat normals. The walls follow the mesh's boundary edges, so this
    /// works after mesh reduction.
    /// </summary>
    /// <param name="positions">Terrain positions, glTF space (as written in the model)</param>
    /// <param name="indices">Terrain triangles</param>
    public ModelRoot AddTerrainBox(ModelRoot model, IReadOnlyList<Vector3> positions, IReadOnlyList<int> indices, TerrainBoxOptions? box = null)
    {
        box ??= new TerrainBoxOptions();
        var boxMesh = TerrainBoxBuilder.Build(positions, indices, box.BaseMargin);
        if (boxMesh == null)
        {
            _logger.LogWarning("Terrain mesh has no boundary: no box added");
            return model;
        }

        model ??= CreateNewModel();
        var scene = model.UseScene(TERRAIN_SCENE_NAME);
        var rnode = scene.FindNode(n => n.Name == TERRAIN_BOX_NODE_NAME) ?? scene.CreateNode(TERRAIN_BOX_NODE_NAME);
        var rmesh = rnode.Mesh = FindOrCreateMesh(model, string.Concat(TERRAIN_BOX_NODE_NAME, "Mesh"));

        var material = model.CreateMaterial(string.Concat(TERRAIN_BOX_NODE_NAME, "Material"))
            .WithPBRMetallicRoughness(box.Color, null, null, 0f, 0.9f)
            .WithDoubleSide(false); // closed and wound outwards
        material.Alpha = box.Color.W < 1f ? SharpGLTF.Schema2.AlphaMode.BLEND : SharpGLTF.Schema2.AlphaMode.OPAQUE;

        rmesh.CreatePrimitive()
            .WithVertexAccessor("POSITION", boxMesh.Positions)
            .WithVertexAccessor("NORMAL", boxMesh.Normals)
            .WithIndicesAccessor(PrimitiveType.TRIANGLES, boxMesh.Indices)
            .WithMaterial(material);

        if (box.Monuments != null)
            AddTerrainBoxMonuments(model, MonumentPlacer.Place(positions, boxMesh.BaseElevation, box.Monuments), box.Monuments.Color);

        return model;
    }

    /// <summary>
    /// Monument decals as their own node (<see cref="TERRAIN_BOX_MONUMENTS_NODE_NAME"/>): one quad each, textured with
    /// the shared silhouette atlas (<see cref="MonumentAtlas"/>), alpha masked.
    /// </summary>
    private void AddTerrainBoxMonuments(ModelRoot model, List<MonumentDecal> decals, Vector4 color)
    {
        if (decals.Count == 0)
            return;
        var atlas = MonumentAtlas.Instance;
        var positions = new List<Vector3>(decals.Count * 4);
        var normals = new List<Vector3>(decals.Count * 4);
        var uvs = new List<Vector2>(decals.Count * 4);
        var indices = new List<int>(decals.Count * 6);
        foreach (var d in decals)
        {
            var e = atlas.Entries[d.Monument];
            int i = positions.Count;
            Vector3 right = d.Right * d.Width, up = Vector3.UnitY * d.Height;
            positions.AddRange([d.BottomLeft, d.BottomLeft + right, d.BottomLeft + right + up, d.BottomLeft + up]);
            normals.AddRange([d.Normal, d.Normal, d.Normal, d.Normal]);
            uvs.AddRange([new(e.U0, e.V1), new(e.U1, e.V1), new(e.U1, e.V0), new(e.U0, e.V0)]);
            // Counter-clockwise seen from outside: Cross(Right, Up) = Normal
            indices.AddRange([i, i + 1, i + 2, i, i + 2, i + 3]);
        }

        var scene = model.UseScene(TERRAIN_SCENE_NAME);
        var rnode = scene.FindNode(n => n.Name == TERRAIN_BOX_MONUMENTS_NODE_NAME) ?? scene.CreateNode(TERRAIN_BOX_MONUMENTS_NODE_NAME);
        var rmesh = rnode.Mesh = FindOrCreateMesh(model, string.Concat(TERRAIN_BOX_MONUMENTS_NODE_NAME, "Mesh"));

        var materialBuilder = new MaterialBuilder(string.Concat(TERRAIN_BOX_MONUMENTS_NODE_NAME, "Material"))
            .WithMetallicRoughnessShader()
            .WithAlpha(SharpGLTF.Materials.AlphaMode.MASK, 0.5f)
            .WithDoubleSide(false)
            .WithBaseColor(new SharpGLTF.Memory.MemoryImage(atlas.Png), color)
            .WithMetallicRoughness(0f, 0.9f);
        var material = model.CreateMaterial(materialBuilder);

        rmesh.CreatePrimitive()
            .WithVertexAccessor("POSITION", positions)
            .WithVertexAccessor("NORMAL", normals)
            .WithVertexAccessor("TEXCOORD_0", uvs)
            .WithIndicesAccessor(PrimitiveType.TRIANGLES, indices)
            .WithMaterial(material);
    }

    /// <summary>
    /// Upper bound of the bytes the monuments add to a GLB: the atlas and every decal on each of the 4 sides
    /// (4 vertices with position, normal and texture coordinates, 6 indices).
    /// </summary>
    public static int TerrainBoxMonumentsMaxBytes =>
        MonumentAtlas.Instance.Png.Length + 4 * Monuments.All.Count * (4 * (12 + 12 + 8) + 6 * sizeof(int));

    /// <summary>
    /// Elevation of the bottom of the terrain box (glTF Y, i.e. model Z), null when the model has no box.
    /// Lets other meshes (adornments) sit at the box base.
    /// </summary>
    public float? GetTerrainBoxBaseElevation(ModelRoot model)
    {
        var mesh = model?.LogicalScenes.FirstOrDefault()?.FindNode(n => n.Name == TERRAIN_BOX_NODE_NAME)?.Mesh;
        var positions = mesh?.Primitives.FirstOrDefault()?.GetVertexAccessor("POSITION");
        return positions == null ? null : positions.AsVector3Array().Min(p => p.Y);
    }

    public ModelRoot GenerateTriangleMesh(List<GeoPoint> points, List<int> indices, PBRTexture? textures)
    {
        Triangulation triangulation = new Triangulation(points, indices);
        return CreateTerrainMesh(triangulation, textures);
    }
    private (Vector3 min, Vector3 max) CalculateBounds(IReadOnlyList<Vector3> positions)
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
    public ModelRoot AddLine(ModelRoot? model, string nodeName, IEnumerable<GeoPoint> gpxPointsElevated, Vector4 color, float trailWidthMeters, Matrix4x4 transform = default)
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
        var decimatedTriangulation = _meshReducer.Decimate(triangulation, 0.1f);

        var normals = _meshService.ComputeMeshNormals(decimatedTriangulation.Positions, decimatedTriangulation.Indices);


        // create mesh primitive
        var primitive = rmesh.CreatePrimitive()
            .WithVertexAccessor("POSITION", decimatedTriangulation.Positions)
            .WithVertexAccessor("NORMAL", normals.ToList())
            .WithIndicesAccessor(PrimitiveType.TRIANGLES, decimatedTriangulation.Indices);

        primitive = primitive.WithMaterial(material);
        return model;
    }
    public ModelRoot AddLines(ModelRoot model, string nodeName, IEnumerable<(IEnumerable<GeoPoint> points, float trailWidthMeters)> lines, Vector4 color, Matrix4x4 transform = default)
    {
        List<Vector3> positions = [];
        List<int> indices = [];

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
