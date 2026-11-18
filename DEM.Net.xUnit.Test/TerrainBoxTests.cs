using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DEM.Net.Core;
using DEM.Net.glTF.SharpglTF;
using Microsoft.Extensions.DependencyInjection;
using SharpGLTF.Schema2;
using Xunit;

namespace DEM.Net.Test;

public class TerrainBoxTests(DemNetFixture fixture) : IClassFixture<DemNetFixture>
{
    readonly SharpGltfService _sharpGltfService = fixture.ServiceProvider.GetService<SharpGltfService>();

    const int W = 60, H = 40;
    const float Cell = 25f; // meters

    /// <summary>Grid in projected coordinates (x east, y north), rows north to south, with hills.</summary>
    internal static HeightMap CreateHeightMap()
    {
        var hMap = new HeightMap(W, H);
        var coords = new GeoPoint[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                double e = 500 + 120 * Math.Sin(x * 0.21) * Math.Cos(y * 0.17) + 3 * x;
                coords[x + y * W] = new GeoPoint(latitude: (H - 1 - y) * Cell, longitude: x * Cell, altitude: e);
            }
        hMap.Coordinates = coords;
        hMap.Minimum = (float)coords.Min(c => c.Elevation.Value);
        hMap.Maximum = (float)coords.Max(c => c.Elevation.Value);
        return hMap;
    }

    static (List<Vector3> Positions, List<int> Indices) ReadPrimitive(ModelRoot model, string nodeName)
    {
        var primitive = model.LogicalScenes[0].FindNode(n => n.Name == nodeName).Mesh.Primitives[0];
        return ([.. primitive.GetVertices("POSITION").AsVector3Array()], [.. primitive.GetIndices().Select(i => (int)i)]);
    }

    /// <summary>
    /// Signed volume of terrain + box, terrain triangles oriented upwards. The box faces must point outwards
    /// and close the terrain exactly: a closed mesh's volume doesn't depend on the origin.
    /// </summary>
    static double SignedVolume(List<Vector3> terrainPos, List<int> terrainIdx, List<Vector3> boxPos, List<int> boxIdx, Vector3 origin)
    {
        double volume = 0;
        void Add(Vector3 a, Vector3 b, Vector3 c)
        {
            a -= origin; b -= origin; c -= origin;
            volume += Vector3.Dot(a, Vector3.Cross(b, c)) / 6.0;
        }
        // The terrain winding is consistent (mesh reduction can leave a few folded triangles, which must keep it):
        // one orientation for the whole mesh, from its total upward area
        double upArea = 0;
        for (int t = 0; t < terrainIdx.Count; t += 3)
        {
            Vector3 a = terrainPos[terrainIdx[t]], b = terrainPos[terrainIdx[t + 1]], c = terrainPos[terrainIdx[t + 2]];
            upArea += Vector3.Cross(b - a, c - a).Y;
        }
        for (int t = 0; t < terrainIdx.Count; t += 3)
        {
            Vector3 a = terrainPos[terrainIdx[t]], b = terrainPos[terrainIdx[t + 1]], c = terrainPos[terrainIdx[t + 2]];
            if (upArea >= 0) Add(a, b, c); else Add(a, c, b);
        }
        for (int t = 0; t < boxIdx.Count; t += 3)
            Add(boxPos[boxIdx[t]], boxPos[boxIdx[t + 1]], boxPos[boxIdx[t + 2]]);
        return volume;
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(0.25f)]
    [InlineData(0.1f)]
    [InlineData(0.05f)] // reduction collapses corners, leaving folded triangles there
    public void Box_closes_the_terrain_with_outward_flat_faces(float reduceFactor)
    {
        var model = _sharpGltfService.CreateTerrainMesh(CreateHeightMap(), null, reduceFactor, new TerrainBoxOptions());

        // Valid glTF, round trip
        model = ModelRoot.ParseGLB(model.WriteGLB());

        var (terrainPos, terrainIdx) = ReadPrimitive(model, "TerrainNode");
        var boxPrimitive = model.LogicalScenes[0].FindNode(n => n.Name == SharpGltfService.TERRAIN_BOX_NODE_NAME).Mesh.Primitives[0];
        var boxPos = boxPrimitive.GetVertices("POSITION").AsVector3Array().ToList();
        var boxNormals = boxPrimitive.GetVertices("NORMAL").AsVector3Array().ToList();
        var boxIdx = boxPrimitive.GetIndices().Select(i => (int)i).ToList();

        // Own material, solid color, no texture
        Assert.NotEqual(model.LogicalScenes[0].FindNode(n => n.Name == "TerrainNode").Mesh.Primitives[0].Material, boxPrimitive.Material);
        Assert.Null(boxPrimitive.GetVertexAccessor("TEXCOORD_0"));
        Assert.Null(boxPrimitive.Material.FindChannel("BaseColor")?.Texture);

        // Closed and consistently wound: every directed edge of terrain (facing up) + box, matched by position,
        // has its reverse
        var directed = new HashSet<(Vector3, Vector3)>();
        void AddTriangles(List<Vector3> pos, List<int> idx, bool flip)
        {
            for (int t = 0; t < idx.Count; t += 3)
            {
                Vector3 a = pos[idx[t]], b = pos[idx[t + (flip ? 2 : 1)]], c = pos[idx[t + (flip ? 1 : 2)]];
                Assert.True(directed.Add((a, b)) & directed.Add((b, c)) & directed.Add((c, a)), "edge used twice in the same direction");
            }
        }
        double upArea = 0;
        for (int t = 0; t < terrainIdx.Count; t += 3)
            upArea += Vector3.Cross(terrainPos[terrainIdx[t + 1]] - terrainPos[terrainIdx[t]], terrainPos[terrainIdx[t + 2]] - terrainPos[terrainIdx[t]]).Y;
        AddTriangles(terrainPos, terrainIdx, flip: upArea < 0);
        AddTriangles(boxPos, boxIdx, flip: false);
        Assert.All(directed, e => Assert.Contains((e.Item2, e.Item1), directed));

        // Positive volume (outward faces), the same from another origin
        double v0 = SignedVolume(terrainPos, terrainIdx, boxPos, boxIdx, Vector3.Zero);
        double v1 = SignedVolume(terrainPos, terrainIdx, boxPos, boxIdx, new Vector3(-3000, 800, 5000));
        Assert.True(v0 > 0, $"volume {v0}");
        Assert.Equal(1.0, v1 / v0, 4);

        // Flat shading: each triangle's face normal matches its vertex normals
        for (int t = 0; t < boxIdx.Count; t += 3)
        {
            Vector3 a = boxPos[boxIdx[t]], b = boxPos[boxIdx[t + 1]], c = boxPos[boxIdx[t + 2]];
            Vector3 cross = Vector3.Cross(b - a, c - a);
            if (cross.Length() < 1e-3f) continue; // degenerate fan triangle along a straight side
            Vector3 face = Vector3.Normalize(cross);
            for (int k = 0; k < 3; k++)
                Assert.True(Vector3.Dot(face, boxNormals[boxIdx[t + k]]) > 0.9f, $"triangle {t / 3}");
        }

        // Base: lowest point minus max(10 % of the relief, 1 % of the larger side)
        float minY = terrainPos.Min(p => p.Y), maxY = terrainPos.Max(p => p.Y);
        float expectedBase = minY - Math.Max(0.1f * (maxY - minY), 0.01f * (W - 1) * Cell);
        Assert.Equal(expectedBase, boxPos.Min(p => p.Y), 2);

        if (reduceFactor == 1f)
        {
            // Expected volume of the full grid: each terrain triangle's prism down to the base
            double expected = 0;
            for (int t = 0; t < terrainIdx.Count; t += 3)
            {
                Vector3 a = terrainPos[terrainIdx[t]], b = terrainPos[terrainIdx[t + 1]], c = terrainPos[terrainIdx[t + 2]];
                double area = Math.Abs(Vector3.Cross(b - a, c - a).Y) / 2.0;
                expected += area * ((a.Y + b.Y + c.Y) / 3.0 - expectedBase);
            }
            Assert.Equal(1.0, v0 / expected, 4);

            // Vertex budget: 2 per boundary vertex for the walls (+2 extra at each corner), 1 for the bottom, + center
            int perimeter = 2 * (W + H) - 4;
            Assert.Equal(3 * perimeter + 8 + 1, boxPos.Count);
        }
    }

    [Fact]
    public void Base_margin_is_configurable()
    {
        var model = _sharpGltfService.CreateTerrainMesh(CreateHeightMap(), null, 1f, new TerrainBoxOptions { BaseMargin = 7f });
        var (terrainPos, _) = ReadPrimitive(model, "TerrainNode");
        var (boxPos, _) = ReadPrimitive(model, SharpGltfService.TERRAIN_BOX_NODE_NAME);
        Assert.Equal(terrainPos.Min(p => p.Y) - 7f, boxPos.Min(p => p.Y), 3);
        Assert.Equal(boxPos.Min(p => p.Y), _sharpGltfService.GetTerrainBoxBaseElevation(model));
    }

    [Theory]
    [InlineData(0.5f)]
    [InlineData(0.1f)]
    public void Reduced_terrain_keeps_grid_points_and_the_rectangle(float reduceFactor)
    {
        var hMap = CreateHeightMap();
        var model = _sharpGltfService.CreateTerrainMesh(hMap, null, reduceFactor);
        var (terrainPos, terrainIdx) = ReadPrimitive(model, "TerrainNode");

        // Delatin: the vertices are grid points (not moved), the four corners are kept, the triangle count is the target
        var grid = hMap.Coordinates.Select(p => p.ToVector3GlTFSpace()).ToHashSet();
        Assert.All(terrainPos, p => Assert.Contains(p, grid));
        foreach (int i in new[] { 0, W - 1, (H - 1) * W, W * H - 1 })
            Assert.Contains(hMap.Coordinates[i].ToVector3GlTFSpace(), terrainPos);
        int target = (int)(reduceFactor * 2 * (W - 1) * (H - 1));
        Assert.InRange(terrainIdx.Count / 3, target, target + 1);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(0.3f)]
    [InlineData(0.05f)]
    public void Stl_box_is_closed_in_a_single_primitive(float reduceFactor)
    {
        // As the STL endpoints: one primitive (terrain + walls + bottom), Z up, base 5 under the lowest point
        var model = _sharpGltfService.CreateTerrainMesh(CreateHeightMap(), GenOptions.BoxedBaseElevationMin,
            Matrix4x4.CreateRotationX(MathF.PI / 2f), doubleSided: true, meshReduceFactor: reduceFactor);
        Assert.Single(model.LogicalMeshes);
        var primitive = Assert.Single(model.LogicalMeshes[0].Primitives);
        var pos = primitive.GetVertices("POSITION").AsVector3Array().ToList();
        var idx = primitive.GetIndices().Select(i => (int)i).ToList();

        // Closed: every directed edge (matched by position) has its reverse; the terrain is wound like the box
        var directed = new HashSet<(Vector3, Vector3)>();
        for (int t = 0; t < idx.Count; t += 3)
        {
            Vector3 a = pos[idx[t]], b = pos[idx[t + 1]], c = pos[idx[t + 2]];
            if (a == b || b == c || c == a) continue;
            Assert.True(directed.Add((a, b)) & directed.Add((b, c)) & directed.Add((c, a)), "edge used twice in the same direction");
        }
        Assert.All(directed, e => Assert.Contains((e.Item2, e.Item1), directed));

        // Z up after the rotation: the base is 5 under the lowest terrain point
        var hMap = CreateHeightMap();
        Assert.Equal(hMap.Minimum - 5f, pos.Min(p => p.Z), 2);
    }

    [Fact]
    public void No_box_by_default()
    {
        var model = _sharpGltfService.CreateTerrainMesh(CreateHeightMap(), null, 1f);
        Assert.Null(model.LogicalScenes[0].FindNode(n => n.Name == SharpGltfService.TERRAIN_BOX_NODE_NAME));
        Assert.Null(_sharpGltfService.GetTerrainBoxBaseElevation(model));
    }
}
