using System;
using System.Collections.Generic;
using System.Numerics;

namespace DEM.Net.glTF.SharpglTF;

/// <summary>
/// Options of the box (walls and bottom) added under a terrain mesh.
/// </summary>
public sealed record TerrainBoxOptions
{
    /// <summary>Neutral warm grey.</summary>
    public static readonly Vector4 DefaultColor = new(0.45f, 0.42f, 0.39f, 1f);

    /// <summary>
    /// Distance between the terrain's lowest point and the bottom, in model units (Z factor included).
    /// Null: 10 % of the relief, and at least 1 % of the larger horizontal side.
    /// </summary>
    public float? BaseMargin { get; init; }

    /// <summary>Solid color (linear RGBA) of the walls and the bottom.</summary>
    public Vector4 Color { get; init; } = DefaultColor;

    /// <summary>Monument silhouettes on the walls, as a scale reference. Null: plain walls.</summary>
    public TerrainBoxMonumentOptions? Monuments { get; init; }
}

/// <summary>
/// Walls and bottom of a terrain mesh, with flat normals.
/// </summary>
public sealed record TerrainBoxMesh(List<Vector3> Positions, List<Vector3> Normals, List<int> Indices, float BaseElevation);

/// <summary>
/// Builds a box under a terrain mesh in glTF space (Y up): one wall quad per boundary edge, down to a flat bottom.
/// Works on any (reduced) terrain mesh: the walls follow its boundary edges, the edges used by a single triangle.
/// </summary>
public static class TerrainBoxBuilder
{
    // Adjacent boundary edges closer than this angle share their wall vertices (one straight side);
    // above it (a corner), each wall has its own vertices so the normals stay flat.
    private static readonly float SharedNormalMinDot = MathF.Cos(20f * MathF.PI / 180f);

    /// <param name="positions">Terrain positions, glTF space (Y up)</param>
    /// <param name="indices">Terrain triangles</param>
    /// <param name="baseMargin">See <see cref="TerrainBoxOptions.BaseMargin"/></param>
    /// <returns>Null when the mesh has no boundary</returns>
    public static TerrainBoxMesh? Build(IReadOnlyList<Vector3> positions, IReadOnlyList<int> indices, float? baseMargin = null)
    {
        ArgumentNullException.ThrowIfNull(positions);
        ArgumentNullException.ThrowIfNull(indices);
        if (positions.Count == 0 || indices.Count < 3)
            return null;

        List<BoundaryEdge> edges = FindBoundaryEdges(positions, indices);
        if (edges.Count == 0)
            return null;

        float baseY = GetBaseElevation(positions, baseMargin);

        // Boundary vertex => its boundary edges (2 on a manifold boundary)
        var vertexEdges = new Dictionary<int, (int First, int Second)>(edges.Count);
        for (int e = 0; e < edges.Count; e++)
        {
            AddVertexEdge(vertexEdges, edges[e].A, e);
            AddVertexEdge(vertexEdges, edges[e].B, e);
        }

        int capacity = edges.Count * 3 + 1;
        var outPositions = new List<Vector3>(capacity);
        var outNormals = new List<Vector3>(capacity);
        var outIndices = new List<int>(edges.Count * 9);

        // Walls. Key: (vertex, -1) for a vertex shared by both its walls, (vertex, edge) otherwise.
        // Value: index of the top vertex, the bottom one follows.
        var wallVertices = new Dictionary<(int Vertex, int Edge), int>(edges.Count * 2);
        int WallVertex(int v, int e)
        {
            var (first, second) = vertexEdges[v];
            int other = first == e ? second : first;
            bool shared = other >= 0 && Vector3.Dot(edges[e].Normal, edges[other].Normal) >= SharedNormalMinDot;
            var key = shared ? (v, -1) : (v, e);
            if (!wallVertices.TryGetValue(key, out int top))
            {
                Vector3 normal = shared ? Vector3.Normalize(edges[e].Normal + edges[other].Normal) : edges[e].Normal;
                top = outPositions.Count;
                Vector3 p = positions[v];
                outPositions.Add(p);
                outPositions.Add(p with { Y = baseY });
                outNormals.Add(normal);
                outNormals.Add(normal);
                wallVertices.Add(key, top);
            }
            return top;
        }

        for (int e = 0; e < edges.Count; e++)
        {
            var edge = edges[e];
            int ta = WallVertex(edge.A, e), ba = ta + 1;
            int tb = WallVertex(edge.B, e), bb = tb + 1;
            // Quad ta tb bb ba, wound so that its front face points outwards
            Vector3 cross = Vector3.Cross(outPositions[tb] - outPositions[ta], outPositions[bb] - outPositions[ta]);
            if (Vector3.Dot(cross, edge.Normal) >= 0)
                outIndices.AddRange([ta, tb, bb, ta, bb, ba]);
            else
                outIndices.AddRange([ta, bb, tb, ta, ba, bb]);
        }

        // Bottom: a fan from the center of the outline (star-shaped from it for a rectangular terrain).
        // It shares the walls' bottom outline, so the box is closed even when mesh reduction moved boundary vertices.
        Vector3 center = Vector3.Zero;
        foreach (int v in vertexEdges.Keys)
            center += positions[v];
        center /= vertexEdges.Count;
        center.Y = baseY;

        int centerIndex = outPositions.Count;
        outPositions.Add(center);
        outNormals.Add(-Vector3.UnitY);
        var bottomVertices = new Dictionary<int, int>(vertexEdges.Count);
        int BottomVertex(int v)
        {
            if (!bottomVertices.TryGetValue(v, out int index))
            {
                index = outPositions.Count;
                outPositions.Add(positions[v] with { Y = baseY });
                outNormals.Add(-Vector3.UnitY);
                bottomVertices.Add(v, index);
            }
            return index;
        }
        foreach (var edge in edges)
        {
            int a = BottomVertex(edge.A), b = BottomVertex(edge.B);
            // Facing down (-Y): counter-clockwise seen from below
            float crossY = Vector3.Cross(outPositions[a] - center, outPositions[b] - center).Y;
            if (crossY <= 0)
                outIndices.AddRange([centerIndex, a, b]);
            else
                outIndices.AddRange([centerIndex, b, a]);
        }

        return new TerrainBoxMesh(outPositions, outNormals, outIndices, baseY);
    }

    internal static float GetBaseElevation(IReadOnlyList<Vector3> positions, float? baseMargin)
    {
        Vector3 min = new(float.MaxValue), max = new(float.MinValue);
        foreach (var p in positions)
        {
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }
        float margin = baseMargin ?? MathF.Max(0.1f * (max.Y - min.Y), 0.01f * MathF.Max(max.X - min.X, max.Z - min.Z));
        return min.Y - MathF.Max(margin, 0f);
    }

    private static void AddVertexEdge(Dictionary<int, (int First, int Second)> vertexEdges, int v, int e)
    {
        if (vertexEdges.TryGetValue(v, out var existing))
        {
            // More than 2 boundary edges (non-manifold vertex): the extra ones don't share wall vertices
            if (existing.Second < 0)
                vertexEdges[v] = (existing.First, e);
        }
        else
        {
            vertexEdges.Add(v, (e, -1));
        }
    }

    /// <summary>Directed edge A→B of a triangle, its horizontal outward normal.</summary>
    private readonly record struct BoundaryEdge(int A, int B, Vector3 Normal);

    /// <summary>
    /// Edges used by a single triangle. Sorting the edge keys keeps memory to 12 bytes per triangle corner,
    /// with no per-edge object.
    /// </summary>
    private static List<BoundaryEdge> FindBoundaryEdges(IReadOnlyList<Vector3> positions, IReadOnlyList<int> indices)
    {
        int numCorners = indices.Count / 3 * 3;
        var keys = new long[numCorners];
        var corners = new int[numCorners]; // triangle corner the edge starts from
        double upArea = 0; // > 0 when the triangles face up (counter-clockwise seen from +Y)
        for (int t = 0; t < numCorners; t += 3)
        {
            Vector3 p0 = positions[indices[t]];
            upArea += Vector3.Cross(positions[indices[t + 1]] - p0, positions[indices[t + 2]] - p0).Y;
            for (int k = 0; k < 3; k++)
            {
                int a = indices[t + k], b = indices[t + (k + 1) % 3];
                keys[t + k] = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                corners[t + k] = t + k;
            }
        }
        Array.Sort(keys, corners);

        var edges = new List<BoundaryEdge>();
        for (int i = 0; i < numCorners;)
        {
            int j = i + 1;
            while (j < numCorners && keys[j] == keys[i])
                j++;
            if (j - i == 1)
            {
                int corner = corners[i];
                int t = corner - corner % 3, k = corner % 3;
                int a = indices[t + k], b = indices[t + (k + 1) % 3];
                if (a != b && TryGetOutwardNormal(positions[a], positions[b], upArea >= 0, out Vector3 normal))
                    edges.Add(new BoundaryEdge(a, b, normal));
            }
            i = j;
        }
        return edges;
    }

    /// <summary>
    /// Horizontal outward normal of the boundary edge a→b, from the mesh's winding: with triangles facing up, the
    /// inside is on the left of a→b seen from above. Unlike the side of the triangle's third vertex, this stays
    /// right for the folded triangles mesh reduction can leave at a collapsed corner.
    /// </summary>
    private static bool TryGetOutwardNormal(Vector3 a, Vector3 b, bool facingUp, out Vector3 normal)
    {
        Vector2 d = new(b.X - a.X, b.Z - a.Z);
        float length = d.Length();
        if (length <= 1e-9f)
        {
            normal = default;
            return false; // vertical edge: no wall
        }
        // (dz, -dx) points to the third vertex of an up-facing triangle: Cross(b - a, c - a).Y = dz·(cx - ax) - dx·(cz - az)
        normal = new Vector3(d.Y / length, 0, -d.X / length);
        if (facingUp)
            normal = -normal;
        return true;
    }
}
