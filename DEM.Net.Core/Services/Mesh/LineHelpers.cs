// LineHelpers.cs
//
// Author:
//       Xavier Fischer
//
// Copyright (c) 2026 Xavier Fischer
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the right
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Numerics;

namespace DEM.Net.Core;

/// <summary>
/// Polyline helpers for ribbon meshes, in glTF space (Y up, horizontal plane XZ).
/// </summary>
public static class LineHelpers
{
    /// <summary>
    /// Miters longer than this many half widths become bevels (a turn sharper than 120°).
    /// </summary>
    private const float MiterLimit = 2f;

    /// <summary>
    /// 3D Douglas–Peucker simplification. The first and last points are always kept.
    /// </summary>
    /// <remarks>3D rather than 2D, so the points that only carry an elevation change (a track over a ridge) are kept.</remarks>
    public static List<Vector3> SimplifyDouglasPeucker(IReadOnlyList<Vector3> line, float tolerance)
    {
        if (line.Count < 3 || tolerance <= 0)
            return [.. line];

        var keep = new bool[line.Count];
        keep[0] = keep[^1] = true;
        float tolSq = tolerance * tolerance;

        var stack = new Stack<(int first, int last)>();
        stack.Push((0, line.Count - 1));
        while (stack.Count > 0)
        {
            var (first, last) = stack.Pop();
            float maxDistSq = 0;
            int index = -1;
            for (int i = first + 1; i < last; i++)
            {
                float d = DistanceToSegmentSquared(line[i], line[first], line[last]);
                if (d > maxDistSq)
                {
                    maxDistSq = d;
                    index = i;
                }
            }
            if (index >= 0 && maxDistSq > tolSq)
            {
                keep[index] = true;
                stack.Push((first, index));
                stack.Push((index, last));
            }
        }

        var result = new List<Vector3>();
        for (int i = 0; i < line.Count; i++)
            if (keep[i]) result.Add(line[i]);
        return result;
    }

    /// <summary>
    /// Drops the points closer than <paramref name="minDistance"/> (horizontally) to the previous kept point.
    /// The last point is kept. Vertical segments are dropped, as they have no horizontal direction.
    /// </summary>
    public static List<Vector3> DropCloseHorizontalPoints(IReadOnlyList<Vector3> line, float minDistance)
    {
        var kept = new List<Vector3>(line.Count);
        if (line.Count == 0)
            return kept;

        kept.Add(line[0]);
        for (int i = 1; i < line.Count; i++)
        {
            if (HorizontalDistance(line[i], kept[^1]) >= minDistance)
                kept.Add(line[i]);
        }

        var lastPoint = line[^1];
        if (kept[^1] != lastPoint)
        {
            if (kept.Count == 1)
            {
                // the whole line is shorter than minDistance: draw it anyway when it has a horizontal length
                if (HorizontalDistance(lastPoint, kept[0]) > 1e-6f)
                    kept.Add(lastPoint);
            }
            else if (HorizontalDistance(lastPoint, kept[^2]) >= minDistance)
            {
                kept[^1] = lastPoint; // end the line on its real end
            }
        }
        return kept;
    }

    /// <summary>
    /// Builds a flat ribbon of half width <paramref name="halfWidth"/> along the line.
    /// Every triangle faces up (normal Y &gt; 0). Consecutive points must not be vertically aligned
    /// (see <see cref="DropCloseHorizontalPoints"/>).
    /// </summary>
    public static TriangulationList<Vector3> BuildRibbon(IReadOnlyList<Vector3> line, float halfWidth)
    {
        var triangulation = new TriangulationList<Vector3>();
        if (line.Count < 2)
            return triangulation;

        int segmentCount = line.Count - 1;
        var dirs = new Vector3[segmentCount];
        var lengths = new float[segmentCount];
        for (int i = 0; i < segmentCount; i++)
        {
            var d = line[i + 1] - line[i];
            d.Y = 0;
            lengths[i] = d.Length();
            dirs[i] = lengths[i] > 0 ? d / lengths[i] : (i > 0 ? dirs[i - 1] : Vector3.UnitX);
        }

        var positions = triangulation.Positions;
        var indices = triangulation.Indices;

        // index of the previous cross-section's first vertex (the second one is +1), -1 when none
        int previous = -1;

        int AddSection(Vector3 center, Vector3 offset)
        {
            int i0 = positions.Count;
            positions.Add(center - offset);
            positions.Add(center + offset);
            return i0;
        }
        void AddQuad(int a, int b)
        {
            AddTriangle(a, a + 1, b + 1);
            AddTriangle(a, b + 1, b);
        }
        void AddTriangle(int a, int b, int c)
        {
            var normal = Vector3.Cross(positions[b] - positions[a], positions[c] - positions[a]);
            if (normal.Y < 0)
                (b, c) = (c, b);
            indices.Add(a);
            indices.Add(b);
            indices.Add(c);
        }

        for (int i = 0; i < line.Count; i++)
        {
            var p = line[i];
            if (i == 0 || i == line.Count - 1)
            {
                var n = Side(dirs[i == 0 ? 0 : segmentCount - 1]);
                int s = AddSection(p, n * halfWidth);
                if (previous >= 0) AddQuad(previous, s);
                previous = s;
                continue;
            }

            Vector3 dIn = dirs[i - 1], dOut = dirs[i];
            Vector3 nIn = Side(dIn), nOut = Side(dOut);
            Vector3 m = nIn + nOut;
            float mLength = m.Length();
            bool miter = false;
            float miterLength = 0;
            if (mLength > 1e-6f)
            {
                m /= mLength;
                float cosHalf = Vector3.Dot(m, nIn); // cos of half the turn angle
                if (cosHalf >= 1f / MiterLimit)
                {
                    miterLength = halfWidth / cosHalf;
                    float sinHalf = MathF.Sqrt(MathF.Max(0, 1 - cosHalf * cosHalf));
                    // the inner miter vertex recedes this far along each segment: it must stay on them
                    float recede = halfWidth * sinHalf / cosHalf;
                    miter = recede <= MathF.Min(lengths[i - 1], lengths[i]);
                }
            }

            if (miter)
            {
                int s = AddSection(p, m * miterLength);
                AddQuad(previous, s);
                previous = s;
            }
            else
            {
                // bevel: end the incoming segment, fill the outer gap with a triangle, start the outgoing one
                int sIn = AddSection(p, nIn * halfWidth);
                AddQuad(previous, sIn);
                int sOut = AddSection(p, nOut * halfWidth);
                float turn = dIn.X * dOut.Z - dIn.Z * dOut.X;
                if (MathF.Abs(turn) > 1e-6f)
                {
                    int outer = turn > 0 ? 0 : 1; // the side opposite to the turn
                    int center = positions.Count;
                    positions.Add(p);
                    AddTriangle(center, sIn + outer, sOut + outer);
                }
                previous = sOut;
            }
        }

        return triangulation;
    }

    /// <summary>
    /// Horizontal side vector of a horizontal unit direction (the same side as Cross(dir, Y)).
    /// </summary>
    private static Vector3 Side(Vector3 dir) => new(-dir.Z, 0, dir.X);

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        float dx = a.X - b.X, dz = a.Z - b.Z;
        return MathF.Sqrt(dx * dx + dz * dz);
    }

    private static float DistanceToSegmentSquared(Vector3 p, Vector3 a, Vector3 b)
    {
        var ab = b - a;
        float lengthSq = ab.LengthSquared();
        if (lengthSq == 0)
            return Vector3.DistanceSquared(p, a);
        float t = Math.Clamp(Vector3.Dot(p - a, ab) / lengthSq, 0, 1);
        return Vector3.DistanceSquared(p, a + ab * t);
    }
}
