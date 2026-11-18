using System;
using System.Diagnostics;
using System.Linq;
using DEM.Net.Core;
using DEM.Net.glTF;
using Microsoft.Extensions.DependencyInjection;

namespace DEM.Net.Benchmarks;

/// <summary>
/// Vertical error of the reduced meshes against every grid point (not a BenchmarkDotNet benchmark):
/// <c>dotnet run -c Release --project DEM.Net.Benchmarks -- quality</c>
/// </summary>
/// <remarks>
/// QEM moves vertices off the grid, so each grid point is located in the reduced mesh (in the XY plane) and compared
/// with the height interpolated there. Errors are in meters (the Z factor is divided out).
/// </remarks>
public static class ReductionQuality
{
    private const float ZFactor = 2f;

    public static void Run()
    {
        using var data = new LocalHgtDataSet();
        var elevation = data.Services.GetRequiredService<ElevationService>();
        var meshService = data.Services.GetRequiredService<MeshService>();
        var reducer = data.Services.GetRequiredService<MeshReducer>();
        elevation.DownloadMissingFilesAsync(data.DataSet, new BoundingBox(LocalHgtDataSet.West, LocalHgtDataSet.West + 1, LocalHgtDataSet.South, LocalHgtDataSet.South + 1))
            .GetAwaiter().GetResult();

        Console.WriteLine("| Grid | Factor | Method | Triangles | Max error (m) | RMS (m) | Uncovered points | Time |");
        Console.WriteLine("|---|---|---|--:|--:|--:|--:|--:|");
        foreach (double degrees in new[] { 0.2, 0.5, 1.0 })
        {
            double half = degrees / 2, lon = LocalHgtDataSet.West + 0.5, lat = LocalHgtDataSet.South + 0.5;
            var heightMap = elevation.GetHeightMap(new BoundingBox(lon - half, lon + half, lat - half, lat + half), data.DataSet)
                .ReprojectGeodeticToCartesian().ZScale(ZFactor).CenterOnOrigin();
            string grid = $"{heightMap.Width}²";

            foreach (float factor in new[] { 0.5f, 0.1f })
            {
                var sw = Stopwatch.StartNew();
                var qem = reducer.Decimate(meshService.TriangulateHeightMap(heightMap), factor);
                var qemPositions = qem.Positions.ToArray();
                var qemIndices = qem.Indices.ToArray();
                sw.Stop();
                var q = Measure(heightMap, qemPositions, qemIndices);
                Print(grid, factor, "QEM", qemIndices.Length / 3, q, sw.Elapsed);

                sw.Restart();
                var delatin = DelatinMesh.Reduce(heightMap, factor);
                sw.Stop();
                var d = Measure(heightMap, delatin.Positions, delatin.Indices);
                Print(grid, factor, "Delatin, same triangle count", delatin.Indices.Length / 3, d, sw.Elapsed);

                sw.Restart();
                var equal = DelatinMesh.ReduceToMaxError(heightMap, q.Max * ZFactor);
                sw.Stop();
                var e = Measure(heightMap, equal.Positions, equal.Indices);
                Print(grid, factor, "Delatin, QEM's max error", equal.Indices.Length / 3, e, sw.Elapsed);
            }
        }
    }

    private static void Print(string grid, float factor, string method, int triangles, (double Max, double Rms, int Uncovered) q, TimeSpan time)
        => Console.WriteLine($"| {grid} | {factor} | {method} | {triangles:N0} | {q.Max:F2} | {q.Rms:F3} | {q.Uncovered:N0} | {time.TotalMilliseconds:N0} ms |");

    /// <summary>Max and RMS vertical error at the grid points, and the grid points outside the mesh</summary>
    public static (double Max, double Rms, int Uncovered) Measure(HeightMap heightMap, GeoPoint[] positions, int[] indices)
    {
        int w = heightMap.Width, h = heightMap.Height;
        var grid = heightMap.Coordinates;
        // rectilinear grid: X per column (increasing), Y per row (decreasing, north to south)
        var xs = new double[w];
        var ys = new double[h];
        for (int x = 0; x < w; x++) xs[x] = grid[x].Longitude;
        for (int y = 0; y < h; y++) ys[y] = -grid[y * w].Latitude; // negated: increasing

        var errors = new double[w * h];
        Array.Fill(errors, double.NaN);

        for (int t = 0; t < indices.Length; t += 3)
        {
            var a = positions[indices[t]];
            var b = positions[indices[t + 1]];
            var c = positions[indices[t + 2]];
            double ax = a.Longitude, ay = a.Latitude, bx = b.Longitude, by = b.Latitude, cx = c.Longitude, cy = c.Latitude;
            double det = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy);
            if (Math.Abs(det) < 1e-12)
                continue;

            int x0 = LowerBound(xs, Math.Min(ax, Math.Min(bx, cx)) - 1e-6);
            int x1 = UpperBound(xs, Math.Max(ax, Math.Max(bx, cx)) + 1e-6);
            int y0 = LowerBound(ys, -Math.Max(ay, Math.Max(by, cy)) - 1e-6);
            int y1 = UpperBound(ys, -Math.Min(ay, Math.Min(by, cy)) + 1e-6);
            const double eps = -1e-9;
            for (int y = y0; y < y1; y++)
            {
                for (int x = x0; x < x1; x++)
                {
                    int i = y * w + x;
                    if (!double.IsNaN(errors[i]))
                        continue;
                    double px = xs[x], py = -ys[y];
                    double l1 = ((by - cy) * (px - cx) + (cx - bx) * (py - cy)) / det;
                    double l2 = ((cy - ay) * (px - cx) + (ax - cx) * (py - cy)) / det;
                    double l3 = 1 - l1 - l2;
                    if (l1 < eps || l2 < eps || l3 < eps)
                        continue;
                    double z = l1 * a.Elevation!.Value + l2 * b.Elevation!.Value + l3 * c.Elevation!.Value;
                    errors[i] = Math.Abs(z - grid[i].Elevation!.Value) / ZFactor;
                }
            }
        }

        double max = 0, sum = 0;
        int uncovered = 0;
        foreach (double e in errors)
        {
            if (double.IsNaN(e)) { uncovered++; continue; }
            max = Math.Max(max, e);
            sum += e * e;
        }
        return (max, Math.Sqrt(sum / (errors.Length - uncovered)), uncovered);
    }

    // first index with values[i] >= v
    private static int LowerBound(double[] values, double v)
    {
        int lo = 0, hi = values.Length;
        while (lo < hi) { int m = (lo + hi) >> 1; if (values[m] < v) lo = m + 1; else hi = m; }
        return lo;
    }

    // first index with values[i] > v
    private static int UpperBound(double[] values, double v)
    {
        int lo = 0, hi = values.Length;
        while (lo < hi) { int m = (lo + hi) >> 1; if (values[m] <= v) lo = m + 1; else hi = m; }
        return lo;
    }
}

/// <summary>Reduced mesh of a height map with <see cref="Delatin"/> (vertices are grid points)</summary>
public static class DelatinMesh
{
    public static (GeoPoint[] Positions, int[] Indices) Reduce(HeightMap heightMap, float factor)
    {
        var delatin = Create(heightMap);
        delatin.RunToTriangleCount((int)(factor * 2L * (heightMap.Width - 1) * (heightMap.Height - 1)));
        return ToMesh(heightMap, delatin);
    }

    public static (GeoPoint[] Positions, int[] Indices) ReduceToMaxError(HeightMap heightMap, double maxError)
    {
        var delatin = Create(heightMap);
        delatin.Run(maxError);
        return ToMesh(heightMap, delatin);
    }

    private static Delatin Create(HeightMap heightMap)
    {
        var coords = heightMap.Coordinates;
        var data = new float[coords.Length];
        for (int i = 0; i < coords.Length; i++)
            data[i] = (float)(coords[i].Elevation ?? 0);
        return new Delatin(data, heightMap.Width, heightMap.Height);
    }

    private static (GeoPoint[] Positions, int[] Indices) ToMesh(HeightMap heightMap, Delatin delatin)
    {
        var grid = heightMap.Coordinates;
        var c = delatin.Coordinates;
        var positions = new GeoPoint[delatin.VertexCount];
        for (int i = 0; i < positions.Length; i++)
            positions[i] = grid[c[2 * i + 1] * heightMap.Width + c[2 * i]];
        return (positions, delatin.Triangles.ToArray());
    }
}
