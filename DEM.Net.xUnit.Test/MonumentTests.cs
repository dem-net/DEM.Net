using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DEM.Net.glTF.SharpglTF;
using Microsoft.Extensions.DependencyInjection;
using SharpGLTF.Schema2;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace DEM.Net.Test;

public class MonumentTests(DemNetFixture fixture) : IClassFixture<DemNetFixture>
{
    readonly SharpGltfService _sharpGltfService = fixture.ServiceProvider.GetService<SharpGltfService>();

    static TerrainBoxMonumentOptions Scale(float horizontal, float vertical) => new() { HorizontalScale = horizontal, VerticalScale = vertical };

    static (List<Vector3> Positions, List<int> Indices) ReadPrimitive(ModelRoot model, string nodeName)
    {
        var primitive = model.LogicalScenes[0].FindNode(n => n.Name == nodeName).Mesh.Primitives[0];
        return ([.. primitive.GetVertices("POSITION").AsVector3Array()], [.. primitive.GetIndices().Select(i => (int)i)]);
    }

    [Fact]
    public void Silhouettes_have_the_cited_heights_and_stand_on_the_ground()
    {
        var atlas = MonumentAtlas.Instance;
        Assert.InRange(Monuments.All.Count, 6, 10);
        foreach (var m in Monuments.All)
        {
            var e = atlas.Entries[m];
            Assert.Equal(m.Height, e.Height, 0.01f);
            Assert.False(string.IsNullOrWhiteSpace(m.Source));
        }
    }

    [Fact]
    public void Atlas_is_a_png_with_each_silhouette_in_its_cell()
    {
        var atlas = MonumentAtlas.Instance;
        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(atlas.Png);
        Assert.True(atlas.Png.Length < 200_000, $"{atlas.Png.Length} bytes");
        foreach (var m in Monuments.All)
        {
            var e = atlas.Entries[m];
            int x0 = (int)(e.U0 * image.Width), x1 = (int)(e.U1 * image.Width);
            int y0 = (int)(e.V0 * image.Height), y1 = (int)(e.V1 * image.Height);
            Assert.Equal(MonumentAtlas.CellHeight, y1 - y0);
            // Mostly opaque inside its cell, clear in the padding
            int opaque = 0;
            for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                    if (image[x, y].A > 127) opaque++;
            Assert.True(opaque > 0.05 * (x1 - x0) * (y1 - y0), m.Name);
            Assert.Equal(0, image[Math.Max(0, x0 - 2), (y0 + y1) / 2].A);
            // The base row is filled somewhere (on the ground) and the top row is reached (the height)
            Assert.Contains(Enumerable.Range(x0, x1 - x0), x => image[x, y1 - 1].A > 0);
            Assert.Contains(Enumerable.Range(x0, x1 - x0), x => image[x, y0].A > 0 || image[x, y0 + 1].A > 0);
        }
    }

    [Theory]
    [InlineData(1f, 1f)]
    [InlineData(1.4f, 2f)]
    [InlineData(1f, 0.4f)]
    public void Monuments_are_never_cut_by_the_terrain_edge(float horizontal, float vertical)
    {
        var model = _sharpGltfService.CreateTerrainMesh(TerrainBoxTests.CreateHeightMap(), null, 0.3f,
            new TerrainBoxOptions { Monuments = Scale(horizontal, vertical) });
        model = ModelRoot.ParseGLB(model.WriteGLB());

        var (terrainPos, _) = ReadPrimitive(model, "TerrainNode");
        float baseY = _sharpGltfService.GetTerrainBoxBaseElevation(model).Value;
        float size = Math.Max(terrainPos.Max(p => p.X) - terrainPos.Min(p => p.X), terrainPos.Max(p => p.Z) - terrainPos.Min(p => p.Z));
        var decals = MonumentPlacer.Place(terrainPos, baseY, Scale(horizontal, vertical));
        Assert.NotEmpty(decals);
        Assert.Contains(decals, d => d.Normal == Vector3.UnitZ); // the south side is used first

        foreach (var d in decals)
        {
            // True scale, standing on the base
            var e = MonumentAtlas.Instance.Entries[d.Monument];
            Assert.Equal(e.Height * vertical, d.Height, 0.01f);
            Assert.Equal(e.Width * horizontal, d.Width, 0.01f);
            Assert.Equal(baseY, d.BottomLeft.Y, 0.001f);
            Assert.True(d.Height >= 0.02f * size, d.Monument.Name);

            // The wall top under the decal: the terrain outline on that side, through its vertices and at the decal's edges
            var side = terrainPos
                .Where(p => Math.Abs(Vector3.Dot(p - d.BottomLeft, d.Normal) + 0.001f * size) < 1e-3f * size)
                .Select(p => (T: Vector3.Dot(p - d.BottomLeft, d.Right), Y: p.Y))
                .OrderBy(p => p.T).ToList();
            Assert.True(side.Count >= 2);
            float top = baseY + d.Height;
            foreach (var (t, y) in side.Where(p => p.T >= 0 && p.T <= d.Width))
                Assert.True(y >= top, $"{d.Monument.Name} cut at {t}: wall top {y} < {top}");
            foreach (float t in new[] { 0f, d.Width })
            {
                int k = side.FindLastIndex(p => p.T <= t);
                var (t0, y0) = side[k];
                var (t1, y1) = side[Math.Min(k + 1, side.Count - 1)];
                float y = t1 > t0 ? y0 + (y1 - y0) * (t - t0) / (t1 - t0) : y0;
                Assert.True(y >= top, $"{d.Monument.Name} cut at its edge {t}");
            }
        }

        // No overlap on a side, each monument once
        Assert.Equal(decals.Count, decals.Select(d => d.Monument).Distinct().Count());
        foreach (var group in decals.GroupBy(d => d.Normal))
        {
            var spans = group.Select(d => (Start: Vector3.Dot(d.BottomLeft, d.Right), End: Vector3.Dot(d.BottomLeft, d.Right) + d.Width)).OrderBy(s => s.Start).ToList();
            for (int i = 1; i < spans.Count; i++)
                Assert.True(spans[i].Start > spans[i - 1].End);
        }

        // The model: one quad per decal, textured and alpha masked; the same decals as the placer
        var node = model.LogicalScenes[0].FindNode(n => n.Name == SharpGltfService.TERRAIN_BOX_MONUMENTS_NODE_NAME);
        var primitive = Assert.Single(node.Mesh.Primitives);
        Assert.Equal(4 * decals.Count, primitive.GetVertices("POSITION").AsVector3Array().Count);
        Assert.Equal(6 * decals.Count, primitive.GetIndices().Count);
        Assert.NotNull(primitive.GetVertexAccessor("TEXCOORD_0"));
        Assert.Equal(AlphaMode.MASK, primitive.Material.Alpha);
        Assert.NotNull(primitive.Material.FindChannel("BaseColor")?.Texture);
        Assert.Equal(TerrainBoxMonumentOptions.DefaultColor, primitive.Material.FindChannel("BaseColor").Value.Color);

        // Front faces outward
        var pos = primitive.GetVertices("POSITION").AsVector3Array();
        var normals = primitive.GetVertices("NORMAL").AsVector3Array();
        var idx = primitive.GetIndices();
        for (int t = 0; t < idx.Count; t += 3)
        {
            Vector3 face = Vector3.Normalize(Vector3.Cross(pos[(int)idx[t + 1]] - pos[(int)idx[t]], pos[(int)idx[t + 2]] - pos[(int)idx[t]]));
            Assert.True(Vector3.Dot(face, normals[(int)idx[t]]) > 0.99f);
        }
    }

    [Fact]
    public void Box_is_the_same_with_or_without_monuments()
    {
        var hMap = TerrainBoxTests.CreateHeightMap();
        var solid = _sharpGltfService.CreateTerrainMesh(hMap, null, 0.3f, new TerrainBoxOptions());
        var withMonuments = _sharpGltfService.CreateTerrainMesh(TerrainBoxTests.CreateHeightMap(), null, 0.3f,
            new TerrainBoxOptions { Monuments = Scale(1f, 1f) });

        Assert.Equal(ReadPrimitive(solid, SharpGltfService.TERRAIN_BOX_NODE_NAME), ReadPrimitive(withMonuments, SharpGltfService.TERRAIN_BOX_NODE_NAME), new BoxComparer());
        Assert.Equal(_sharpGltfService.GetTerrainBoxBaseElevation(solid), _sharpGltfService.GetTerrainBoxBaseElevation(withMonuments));
        Assert.Null(solid.LogicalScenes[0].FindNode(n => n.Name == SharpGltfService.TERRAIN_BOX_MONUMENTS_NODE_NAME));

        // Bounded size: the atlas and a few vertices
        int extra = withMonuments.WriteGLB().Count - solid.WriteGLB().Count;
        Assert.InRange(extra, MonumentAtlas.Instance.Png.Length, SharpGltfService.TerrainBoxMonumentsMaxBytes + 2048);
    }

    sealed class BoxComparer : IEqualityComparer<(List<Vector3> Positions, List<int> Indices)>
    {
        public bool Equals((List<Vector3> Positions, List<int> Indices) a, (List<Vector3> Positions, List<int> Indices) b)
            => a.Positions.SequenceEqual(b.Positions) && a.Indices.SequenceEqual(b.Indices);
        public int GetHashCode((List<Vector3> Positions, List<int> Indices) obj) => obj.Positions.Count;
    }

    [Fact]
    public void Monuments_too_small_to_read_are_left_out()
    {
        // 1475 m model at Z factor 0.01: the tallest monument would be 8 m high, under 2 % of the model
        var decals = MonumentPlacer.Place(ReadPrimitive(_sharpGltfService.CreateTerrainMesh(TerrainBoxTests.CreateHeightMap(), null, 0.3f), "TerrainNode").Positions,
            0f, Scale(1f, 0.01f));
        Assert.Empty(decals);
    }

    [Fact]
    public void Monuments_taller_than_every_wall_are_left_out()
    {
        // Flat terrain 10 m above the base: nothing over 10 m fits
        var positions = new List<Vector3>();
        for (int i = 0; i <= 10; i++)
            for (int j = 0; j <= 10; j++)
                positions.Add(new Vector3(i * 100, 10, j * 100));
        var decals = MonumentPlacer.Place(positions, 0f, Scale(1f, 1f) with { MinHeightRatio = 0 });
        Assert.Empty(decals);

        // 200 m walls: the 6 monuments under 190 m (5 % top margin), each placed once
        positions = [.. positions.Select(p => p with { Y = 200 })];
        decals = MonumentPlacer.Place(positions, 0f, Scale(1f, 1f) with { MinHeightRatio = 0 });
        Assert.Equal(Monuments.All.Where(m => m.Height * 1.05f <= 200).Select(m => m.Name).OrderBy(n => n), decals.Select(d => d.Monument.Name).OrderBy(n => n));
    }
}
