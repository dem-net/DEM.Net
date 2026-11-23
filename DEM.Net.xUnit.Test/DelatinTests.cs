using System;
using System.Collections.Generic;
using System.Linq;
using DEM.Net.Core;
using Xunit;

namespace DEM.Net.Test;

public class DelatinTests
{
    /// <summary>Hills, a ridge and integer noise: many local maxima, like real terrain</summary>
    static float[] Terrain(int w, int h, int seed = 1)
    {
        var random = new Random(seed);
        var data = new float[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                data[y * w + x] = (float)Math.Round(500 + 120 * Math.Sin(x * 0.21) * Math.Cos(y * 0.17) + 3 * x
                    + 40 * Math.Exp(-Math.Pow((x - y * 0.7) / 4.0, 2)) + random.Next(0, 4));
        return data;
    }

    static long Orient(int ax, int ay, int bx, int by, int cx, int cy) => (long)(bx - cx) * (ay - cy) - (long)(by - cy) * (ax - cx);

    /// <summary>
    /// Locates every grid point in the mesh, and returns the largest difference between its height and the
    /// height interpolated in its triangle (independent of the rasterization in <see cref="Delatin"/>)
    /// </summary>
    static double MaxErrorAtGridPoints(Delatin delatin, float[] data)
    {
        int w = delatin.Width, h = delatin.Height;
        var c = delatin.Coordinates;
        var t = delatin.Triangles;
        var errors = new double[w * h];
        Array.Fill(errors, double.NaN);
        for (int i = 0; i < t.Length; i += 3)
        {
            int ax = c[2 * t[i]], ay = c[2 * t[i] + 1];
            int bx = c[2 * t[i + 1]], by = c[2 * t[i + 1] + 1];
            int cx = c[2 * t[i + 2]], cy = c[2 * t[i + 2] + 1];
            double area = Orient(ax, ay, bx, by, cx, cy);
            for (int y = Math.Min(ay, Math.Min(by, cy)); y <= Math.Max(ay, Math.Max(by, cy)); y++)
                for (int x = Math.Min(ax, Math.Min(bx, cx)); x <= Math.Max(ax, Math.Max(bx, cx)); x++)
                {
                    long w0 = Orient(bx, by, cx, cy, x, y), w1 = Orient(cx, cy, ax, ay, x, y), w2 = Orient(ax, ay, bx, by, x, y);
                    if (w0 < 0 || w1 < 0 || w2 < 0)
                        continue;
                    double z = (w0 * data[ay * w + ax] + w1 * data[by * w + bx] + w2 * data[cy * w + cx]) / area;
                    double e = Math.Abs(z - data[y * w + x]);
                    errors[y * w + x] = double.IsNaN(errors[y * w + x]) ? e : Math.Min(errors[y * w + x], e);
                }
        }
        double max = 0;
        foreach (double e in errors)
        {
            Assert.False(double.IsNaN(e), "A grid point is outside the mesh");
            max = Math.Max(max, e);
        }
        return max;
    }

    /// <summary>
    /// Counter-clockwise (positive) triangles that tile the grid rectangle: total area, every inner edge shared by
    /// two triangles in opposite directions, the other edges on the rectangle sides, and the Delaunay condition
    /// </summary>
    static void AssertValidTriangulation(Delatin delatin)
    {
        int w = delatin.Width, h = delatin.Height;
        var c = delatin.Coordinates;
        var t = delatin.Triangles;

        long area = 0;
        var edges = new Dictionary<(int, int), int>();
        for (int i = 0; i < t.Length; i += 3)
        {
            int a = t[i], b = t[i + 1], d = t[i + 2];
            long ta = Orient(c[2 * a], c[2 * a + 1], c[2 * b], c[2 * b + 1], c[2 * d], c[2 * d + 1]);
            Assert.True(ta > 0, $"Triangle {i / 3} is degenerate or reversed");
            area += ta;
            foreach (var (p, q, r) in new[] { (a, b, d), (b, d, a), (d, a, b) })
                Assert.True(edges.TryAdd((p, q), r), "Edge used twice in the same direction");
        }
        Assert.Equal(2L * (w - 1) * (h - 1), area);

        int boundaryVertices = 0;
        var onBoundary = new HashSet<int>();
        foreach (var ((p, q), r) in edges)
        {
            if (edges.TryGetValue((q, p), out int s))
            {
                // Delaunay: s isn't strictly inside the circumcircle of (p, q, r) (exact: integer coordinates).
                // The triangles have a positive Orient, so "inside" is a negative determinant.
                double dx = c[2 * p] - c[2 * s], dy = c[2 * p + 1] - c[2 * s + 1];
                double ex = c[2 * q] - c[2 * s], ey = c[2 * q + 1] - c[2 * s + 1];
                double fx = c[2 * r] - c[2 * s], fy = c[2 * r + 1] - c[2 * s + 1];
                double ap = dx * dx + dy * dy, bp = ex * ex + ey * ey, cp = fx * fx + fy * fy;
                double inCircle = dx * (ey * cp - bp * fy) - dy * (ex * cp - bp * fx) + ap * (ex * fy - ey * fx);
                Assert.True(inCircle >= 0, "An edge isn't Delaunay");
                continue;
            }
            int px = c[2 * p], py = c[2 * p + 1], qx = c[2 * q], qy = c[2 * q + 1];
            bool side = (px == qx && (px == 0 || px == w - 1)) || (py == qy && (py == 0 || py == h - 1));
            Assert.True(side, $"Outline edge ({px},{py})-({qx},{qy}) isn't on the grid rectangle");
            onBoundary.Add(p);
            boundaryVertices++;
        }
        Assert.Equal(onBoundary.Count, boundaryVertices); // a simple outline
        // Euler for a disk: T = 2V - B - 2
        Assert.Equal(2 * delatin.VertexCount - boundaryVertices - 2, delatin.TriangleCount);
    }

    [Fact]
    public void PlaneIsTwoTriangles()
    {
        int w = 33, h = 20;
        var data = new float[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                data[y * w + x] = 100 + 2 * x - 3 * y;

        var delatin = new Delatin(data, w, h);
        delatin.Run(1e-6); // the interpolation rounds: 0 would refine on ~1e-13 errors

        Assert.Equal(4, delatin.VertexCount);
        Assert.Equal(2, delatin.TriangleCount);
        Assert.True(delatin.MaxError <= 1e-6);
        Assert.Equal(0, MaxErrorAtGridPoints(delatin, data), 4);
    }

    [Theory]
    [InlineData(65, 65, 20.0)]
    [InlineData(97, 41, 5.0)]
    [InlineData(40, 73, 1.0)]
    [InlineData(2, 50, 1.0)]
    public void RunReachesMaxError(int w, int h, double maxError)
    {
        var data = Terrain(w, h);
        var delatin = new Delatin(data, w, h);
        delatin.Run(maxError);

        Assert.True(delatin.MaxError <= maxError);
        Assert.True(MaxErrorAtGridPoints(delatin, data) <= maxError + 1e-3);
        AssertValidTriangulation(delatin);
    }

    [Fact]
    public void ExactMeshHasNoError()
    {
        int w = 31, h = 23;
        var data = Terrain(w, h, seed: 7);
        var delatin = new Delatin(data, w, h);
        delatin.Run(0);

        Assert.Equal(0, MaxErrorAtGridPoints(delatin, data), 3);
        Assert.Equal(0, delatin.Rmsd, 3);
        AssertValidTriangulation(delatin);
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(0.1)]
    [InlineData(0.02)]
    public void RunToTriangleCountStopsAtTheTarget(double factor)
    {
        int w = 121, h = 87;
        var data = Terrain(w, h);
        int target = (int)(factor * 2 * (w - 1) * (h - 1));
        var delatin = new Delatin(data, w, h);
        delatin.RunToTriangleCount(target);

        Assert.InRange(delatin.TriangleCount, target, target + 1);
        AssertValidTriangulation(delatin);
    }

    [Fact]
    public void ErrorDecreasesWithMoreTriangles()
    {
        int w = 101, h = 101;
        var data = Terrain(w, h);
        var delatin = new Delatin(data, w, h);
        double previousMax = double.MaxValue, previousRms = double.MaxValue;
        foreach (int count in new[] { 100, 1_000, 5_000, 15_000 })
        {
            delatin.RunToTriangleCount(count);
            Assert.True(delatin.MaxError <= previousMax);
            Assert.True(delatin.Rmsd < previousRms);
            previousMax = delatin.MaxError;
            previousRms = delatin.Rmsd;
        }
    }

    [Fact]
    public void RejectsInvalidGrids()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Delatin(new float[10], 1, 10));
        Assert.Throws<ArgumentException>(() => new Delatin(new float[10], 4, 4));
    }

    /// <summary>
    /// Grid points that the mesh puts on the other side of <paramref name="level"/> than the data (independent of the
    /// rasterization in <see cref="Delatin"/>). <paramref name="coords"/>: grid x, y per vertex.
    /// </summary>
    internal static int WrongSideCount(int w, int h, ReadOnlySpan<int> coords, ReadOnlySpan<int> triangles, float[] data, double level)
    {
        var wrong = new bool?[w * h];
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int ax = coords[2 * triangles[i]], ay = coords[2 * triangles[i] + 1];
            int bx = coords[2 * triangles[i + 1]], by = coords[2 * triangles[i + 1] + 1];
            int cx = coords[2 * triangles[i + 2]], cy = coords[2 * triangles[i + 2] + 1];
            double area = Orient(ax, ay, bx, by, cx, cy);
            if (area == 0)
                continue;
            for (int y = Math.Min(ay, Math.Min(by, cy)); y <= Math.Max(ay, Math.Max(by, cy)); y++)
                for (int x = Math.Min(ax, Math.Min(bx, cx)); x <= Math.Max(ax, Math.Max(bx, cx)); x++)
                {
                    long w0 = Orient(bx, by, cx, cy, x, y), w1 = Orient(cx, cy, ax, ay, x, y), w2 = Orient(ax, ay, bx, by, x, y);
                    if (area > 0 ? (w0 < 0 || w1 < 0 || w2 < 0) : (w0 > 0 || w1 > 0 || w2 > 0))
                        continue;
                    double z = (w0 * data[ay * w + ax] + w1 * data[by * w + bx] + w2 * data[cy * w + cx]) / area;
                    // on a shared edge, the point is on the right side if one of its triangles has it there
                    bool isWrong = (z > level) != (data[y * w + x] > level);
                    wrong[y * w + x] = wrong[y * w + x] is bool b ? b && isWrong : isWrong;
                }
        }
        Assert.DoesNotContain(null, wrong); // every grid point is in the mesh
        return wrong.Count(b => b == true);
    }

    /// <summary>A coast: the level crosses the hills, so that a reduced mesh puts points on the wrong side</summary>
    const float CoastLevel = 520f;

    /// <remarks>
    /// From 10 % of the triangles (the Elevation API's minimum): the coast costs little elsewhere. With far fewer
    /// triangles, the coast takes most of them (2 %: RMS error 10 m → 28 m on this terrain).
    /// </remarks>
    [Theory]
    [InlineData(0.1f)]
    [InlineData(0.25f)]
    [InlineData(0.5f)]
    public void Level_puts_no_grid_point_on_the_wrong_side_within_the_same_budget(float reduceFactor)
    {
        const int w = 120, h = 90;
        var data = Terrain(w, h);
        int target = (int)(reduceFactor * 2 * (w - 1) * (h - 1));

        var plain = new Delatin(data, w, h, target);
        plain.RunToTriangleCount(target);
        Assert.True(WrongSideCount(w, h, plain.Coordinates, plain.Triangles, data, CoastLevel) > 0,
            "Without the level, the reduced mesh moves the coast (else this test proves nothing)");

        var coast = new Delatin(data, w, h, target, CoastLevel);
        coast.RunToTriangleCount(target);
        Assert.False(coast.HasWrongSide);
        Assert.Equal(0, WrongSideCount(w, h, coast.Coordinates, coast.Triangles, data, CoastLevel));
        Assert.InRange(coast.TriangleCount, target, target + 1);
        Assert.InRange(coast.Rmsd, 0, plain.Rmsd * 1.1);
    }

    [Fact]
    public void Level_outside_the_data_changes_nothing()
    {
        const int w = 80, h = 60;
        var data = Terrain(w, h);
        int target = 2 * (w - 1) * (h - 1) / 10;

        var plain = new Delatin(data, w, h, target);
        plain.RunToTriangleCount(target);
        var under = new Delatin(data, w, h, target, level: -1000);
        under.RunToTriangleCount(target);

        Assert.False(under.HasWrongSide);
        Assert.Equal(plain.Coordinates.ToArray(), under.Coordinates.ToArray());
        Assert.Equal(plain.Triangles.ToArray(), under.Triangles.ToArray());
        Assert.Equal(plain.MaxError, under.MaxError);
    }

    [Fact]
    public void Run_to_an_error_also_fixes_the_level()
    {
        const int w = 100, h = 70;
        var data = Terrain(w, h);
        var delatin = new Delatin(data, w, h, level: CoastLevel);
        delatin.Run(30);

        Assert.False(delatin.HasWrongSide);
        Assert.True(delatin.MaxError <= 30);
        Assert.Equal(0, WrongSideCount(w, h, delatin.Coordinates, delatin.Triangles, data, CoastLevel));
        Assert.True(MaxErrorAtGridPoints(delatin, data) <= 30 + 1e-6);
    }
}
