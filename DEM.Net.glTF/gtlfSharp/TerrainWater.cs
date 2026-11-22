using System;
using System.Collections.Generic;
using System.Numerics;

namespace DEM.Net.glTF.SharpglTF;

/// <summary>
/// Options of the water added over a terrain mesh: a flat, translucent surface at <see cref="Level"/>.
/// </summary>
public sealed record TerrainWaterOptions
{
    /// <summary>Translucent sea blue.</summary>
    public static readonly Vector4 DefaultColor = new(0.05f, 0.27f, 0.42f, 0.5f);

    /// <summary>Water surface elevation, in model units (glTF Y, Z factor included).</summary>
    public required float Level { get; init; }

    /// <summary>Color (linear RGBA) of the water. Its alpha is the opacity.</summary>
    public Vector4 Color { get; init; } = DefaultColor;

    /// <summary>
    /// Also adds water walls on the terrain sides, from the terrain edge up to the surface, where the terrain is
    /// below it (a boxed model: the box walls go down from the same edge).
    /// </summary>
    public bool Walls { get; init; }
}

/// <summary>Water surface and walls of a terrain mesh, with flat normals.</summary>
public sealed record TerrainWaterMesh(List<Vector3> Positions, List<Vector3> Normals, List<int> Indices);

/// <summary>
/// Builds the water of a terrain mesh in glTF space (Y up). The surface covers the whole terrain outline: the
/// terrain above the water hides it. Works on any (reduced) terrain mesh, like <see cref="TerrainBoxBuilder"/>.
/// </summary>
public static class TerrainWaterBuilder
{
    /// <param name="positions">Terrain positions, glTF space (Y up)</param>
    /// <param name="indices">Terrain triangles</param>
    /// <returns>Null when the mesh has no boundary</returns>
    public static TerrainWaterMesh? Build(IReadOnlyList<Vector3> positions, IReadOnlyList<int> indices, TerrainWaterOptions options)
    {
        ArgumentNullException.ThrowIfNull(positions);
        ArgumentNullException.ThrowIfNull(indices);
        ArgumentNullException.ThrowIfNull(options);
        if (positions.Count == 0 || indices.Count < 3)
            return null;

        var edges = TerrainBoxBuilder.FindBoundaryEdges(positions, indices);
        if (edges.Count == 0)
            return null;

        float level = options.Level;
        var outPositions = new List<Vector3>();
        var outNormals = new List<Vector3>();
        var outIndices = new List<int>();

        // Surface: a fan from the center of the outline at the water level, facing up
        var surfaceVertices = new Dictionary<int, int>();
        Vector3 center = Vector3.Zero;
        void AddOutlineVertex(int v)
        {
            if (surfaceVertices.TryAdd(v, -1))
                center += positions[v];
        }
        foreach (var edge in edges)
        {
            AddOutlineVertex(edge.A);
            AddOutlineVertex(edge.B);
        }
        center /= surfaceVertices.Count;
        center.Y = level;

        int centerIndex = AddVertex(center, Vector3.UnitY);
        int SurfaceVertex(int v)
        {
            int index = surfaceVertices[v];
            if (index < 0)
                surfaceVertices[v] = index = AddVertex(positions[v] with { Y = level }, Vector3.UnitY);
            return index;
        }
        foreach (var edge in edges)
        {
            int a = SurfaceVertex(edge.A), b = SurfaceVertex(edge.B);
            // Facing up (+Y): counter-clockwise seen from above
            float crossY = Vector3.Cross(outPositions[a] - center, outPositions[b] - center).Y;
            if (crossY >= 0)
                outIndices.AddRange([centerIndex, a, b]);
            else
                outIndices.AddRange([centerIndex, b, a]);
        }

        // Walls: on each boundary edge, the part below the water, from the terrain up to the surface
        if (options.Walls)
        {
            foreach (var edge in edges)
            {
                Vector3 pa = positions[edge.A], pb = positions[edge.B];
                bool aBelow = pa.Y < level, bBelow = pb.Y < level;
                if (!aBelow && !bBelow)
                    continue;
                if (aBelow && bBelow)
                {
                    AddWall(edge.Normal, pa, pb, pb with { Y = level }, pa with { Y = level });
                    continue;
                }
                // The terrain crosses the water level along the edge: a triangle up to the crossing point
                float t = (level - pa.Y) / (pb.Y - pa.Y);
                Vector3 crossing = Vector3.Lerp(pa, pb, t) with { Y = level };
                if (aBelow)
                    AddWall(edge.Normal, pa, crossing, pa with { Y = level });
                else
                    AddWall(edge.Normal, crossing, pb, pb with { Y = level });
            }
        }

        return new TerrainWaterMesh(outPositions, outNormals, outIndices);

        int AddVertex(Vector3 p, Vector3 normal)
        {
            outPositions.Add(p);
            outNormals.Add(normal);
            return outPositions.Count - 1;
        }

        // A triangle or a quad (as a fan), wound so that its front face points outwards
        void AddWall(Vector3 normal, params ReadOnlySpan<Vector3> corners)
        {
            int first = outPositions.Count;
            foreach (var p in corners)
                AddVertex(p, normal);
            Vector3 cross = Vector3.Cross(corners[1] - corners[0], corners[^1] - corners[0]);
            bool outwards = Vector3.Dot(cross, normal) >= 0;
            for (int k = 1; k + 1 < corners.Length; k++)
            {
                if (outwards)
                    outIndices.AddRange([first, first + k, first + k + 1]);
                else
                    outIndices.AddRange([first, first + k + 1, first + k]);
            }
        }
    }
}
