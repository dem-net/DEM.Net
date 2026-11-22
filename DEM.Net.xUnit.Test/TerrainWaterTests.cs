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

public class TerrainWaterTests(DemNetFixture fixture) : IClassFixture<DemNetFixture>
{
    readonly SharpGltfService _sharpGltfService = fixture.ServiceProvider.GetRequiredService<SharpGltfService>();

    const int W = 60, H = 40; // TerrainBoxTests.CreateHeightMap
    const float Cell = 25f;
    const float SizeX = (W - 1) * Cell, SizeZ = (H - 1) * Cell;

    static (List<Vector3> Positions, List<Vector3> Normals, List<int> Indices) ReadWater(ModelRoot model)
    {
        var primitive = model.LogicalScenes[0].FindNode(n => n.Name == SharpGltfService.WATER_NODE_NAME).Mesh.Primitives[0];
        return ([.. primitive.GetVertices("POSITION").AsVector3Array()],
                [.. primitive.GetVertices("NORMAL").AsVector3Array()],
                [.. primitive.GetIndices().Select(i => (int)i)]);
    }

    static IEnumerable<(Vector3 A, Vector3 B, Vector3 C)> Triangles(List<Vector3> pos, List<int> idx)
    {
        for (int t = 0; t < idx.Count; t += 3)
            yield return (pos[idx[t]], pos[idx[t + 1]], pos[idx[t + 2]]);
    }

    /// <summary>
    /// Area under <paramref name="level"/> of the full grid's sides: along each side, the terrain is linear between
    /// grid points, so each segment's part below the level is a trapezoid or a triangle.
    /// </summary>
    static double ExpectedWallArea(HeightMap hMap, float level)
    {
        var p = hMap.Coordinates.Select(c => c.ToVector3GlTFSpace()).ToArray();
        IEnumerable<(Vector3, Vector3)> Side(IEnumerable<int> cells) => cells.Zip(cells.Skip(1), (i, j) => (p[i], p[j]));
        var segments = Side(Enumerable.Range(0, W))                              // north row
            .Concat(Side(Enumerable.Range(0, W).Select(x => x + (H - 1) * W)))     // south row
            .Concat(Side(Enumerable.Range(0, H).Select(y => y * W)))               // west column
            .Concat(Side(Enumerable.Range(0, H).Select(y => W - 1 + y * W)));      // east column
        double area = 0;
        foreach (var (a, b) in segments)
        {
            double length = Vector2.Distance(new(a.X, a.Z), new(b.X, b.Z));
            double da = level - a.Y, db = level - b.Y; // depth at each end
            if (da <= 0 && db <= 0) continue;
            if (da >= 0 && db >= 0) area += length * (da + db) / 2;
            else
            {
                double wet = Math.Max(da, db), dry = -Math.Min(da, db);
                area += length * wet / (wet + dry) * wet / 2; // triangle up to the crossing
            }
        }
        return area;
    }

    [Theory]
    [InlineData(550f, true)]
    [InlineData(550f, false)]
    [InlineData(1000f, true)] // above the whole terrain: walls all around
    [InlineData(300f, true)]  // under the whole terrain: no wall
    public void Water_covers_the_outline_and_fills_the_sides_below_the_level(float level, bool walls)
    {
        var hMap = TerrainBoxTests.CreateHeightMap();
        var options = new TerrainWaterOptions { Level = level, Walls = walls };
        var model = _sharpGltfService.CreateTerrainMesh(hMap, null, 1f, new TerrainBoxOptions(), options);

        // Valid glTF, round trip
        model = ModelRoot.ParseGLB(model.WriteGLB());
        var (pos, normals, idx) = ReadWater(model);

        // Own translucent material, double-sided, no texture
        var material = model.LogicalScenes[0].FindNode(n => n.Name == SharpGltfService.WATER_NODE_NAME).Mesh.Primitives[0].Material;
        Assert.Equal(AlphaMode.BLEND, material.Alpha);
        Assert.True(material.DoubleSided);
        Assert.Null(material.FindChannel("BaseColor")?.Texture);

        var surface = new List<int>();
        var wall = new List<int>();
        for (int t = 0; t < idx.Count; t += 3)
            (normals[idx[t]] == Vector3.UnitY ? surface : wall).AddRange(idx.Skip(t).Take(3));

        // Surface: flat at the level, facing up, over the whole rectangle
        Assert.All(surface, i => Assert.Equal(level, pos[i].Y));
        double surfaceArea = 0;
        foreach (var (a, b, c) in Triangles(pos, surface))
        {
            float upY = Vector3.Cross(b - a, c - a).Y;
            Assert.True(upY >= 0, "surface triangle facing down");
            surfaceArea += upY / 2.0;
        }
        Assert.Equal(1.0, surfaceArea / (SizeX * SizeZ), 4);

        if (!walls)
        {
            Assert.Empty(wall);
            return;
        }

        // Walls: on the sides, under the level, facing outwards (flat normals), with the expected area
        Vector3 center = new(SizeX / 2, 0, -SizeZ / 2); // glTF Z = -north
        double wallArea = 0;
        foreach (var (a, b, c) in Triangles(pos, wall))
        {
            foreach (var p in new[] { a, b, c })
            {
                Assert.True(p.Y <= level + 1e-3f);
                bool onSide = MathF.Abs(p.X) < 1e-3f || MathF.Abs(p.X - SizeX) < 1e-3f || MathF.Abs(p.Z) < 1e-3f || MathF.Abs(p.Z + SizeZ) < 1e-3f;
                Assert.True(onSide, $"wall vertex {p} off the sides");
            }
            Vector3 cross = Vector3.Cross(b - a, c - a);
            if (cross.Length() < 1e-4f) continue;
            Vector3 outward = (a + b + c) / 3 - center;
            outward.Y = 0;
            Assert.True(Vector3.Dot(cross, outward) > 0, "wall facing inwards");
            wallArea += cross.Length() / 2.0;
        }
        foreach (int i in wall)
            Assert.Equal(0f, normals[i].Y, 4);

        double expected = ExpectedWallArea(hMap, level);
        if (expected == 0)
            Assert.Empty(wall);
        else
            Assert.Equal(1.0, wallArea / expected, 3);
    }

    [Theory]
    [InlineData(0.25f)]
    [InlineData(0.05f)]
    public void Reduced_terrain_gets_water_too(float reduceFactor)
    {
        var model = _sharpGltfService.CreateTerrainMesh(TerrainBoxTests.CreateHeightMap(), null, reduceFactor,
            new TerrainBoxOptions(), new TerrainWaterOptions { Level = 550f, Walls = true });
        var (pos, normals, idx) = ReadWater(model);
        Assert.NotEmpty(idx);
        Assert.Equal(550f, pos.Max(p => p.Y), 3);
        double up = 0;
        for (int t = 0; t < idx.Count; t += 3)
            if (normals[idx[t]] == Vector3.UnitY)
                up += Vector3.Cross(pos[idx[t + 1]] - pos[idx[t]], pos[idx[t + 2]] - pos[idx[t]]).Y / 2.0;
        Assert.Equal(1.0, up / (SizeX * SizeZ), 4);
    }

    [Fact]
    public void No_water_by_default()
    {
        var model = _sharpGltfService.CreateTerrainMesh(TerrainBoxTests.CreateHeightMap(), null, 1f);
        Assert.Null(model.LogicalScenes[0].FindNode(n => n.Name == SharpGltfService.WATER_NODE_NAME));
    }
}
