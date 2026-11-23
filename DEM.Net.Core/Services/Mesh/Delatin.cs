// Port of Delatin (https://github.com/mapbox/delatin), ISC License, Copyright (c) 2020, Mapbox.
//
// Permission to use, copy, modify, and/or distribute this software for any purpose with or without fee is hereby
// granted, provided that the above copyright notice and this permission notice appear in all copies.
//
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH REGARD TO THIS SOFTWARE INCLUDING ALL
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
// INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN
// AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
// PERFORMANCE OF THIS SOFTWARE.

using System;

namespace DEM.Net.Core;

/// <summary>
/// Greedy Delaunay refinement of a height grid (Delatin): starts from the two triangles of the grid rectangle and
/// inserts, one at a time, the grid point with the largest vertical error, keeping the triangulation Delaunay.
/// </summary>
/// <remarks>
/// Memory is proportional to the output mesh (plus the input grid); no full resolution mesh is built.
/// Coordinates are grid indices (x: column, y: row). The triangles have the same winding as the regular grid
/// triangulation of <see cref="MeshService.TriangulateHeightMap"/>. The outline is the grid rectangle: its sides
/// stay straight, split only at inserted points.
/// </remarks>
public sealed class Delatin
{
    private readonly float[] _data;
    private readonly int _width;
    private readonly int _height;

    private int[] _coords; // x, y per vertex
    private int _vertexCount;

    private int[] _triangles; // 3 vertex indices per triangle
    private int[] _halfedges; // opposite halfedge, -1 on the outline
    private int _triangleCount;

    // per triangle
    private int[] _candidates; // x, y of the point with the largest error
    private int[] _queueIndices; // position in the queue, -1 when not queued
    private double[] _rms; // sum of squared errors

    // max-heap of triangles by error
    private int[] _queue;
    private double[] _errors;
    private int _queueCount;

    // triangles added or changed by the last step, rasterized in Flush
    private int[] _pending;
    private int _pendingCount;

    private double _rmsSum;

    // Level (e.g. the sea at 0 m): a grid point the mesh puts on the wrong side of it is inserted first
    private readonly bool _hasLevel;
    private readonly double _level;
    private readonly double _levelPenalty; // added to the error of such points: more than any height error

    /// <summary>Credit for the algorithm (ISC license), for the attributions of generated models</summary>
    public static Attribution Attribution => new("Mesh reduction", "Delatin (Mapbox, Vladimir Agafonkin), ISC License",
        "https://github.com/mapbox/delatin", "Delatin (Mapbox), ported to C# in DEM.Net");

    /// <param name="data">Heights, row by row (<paramref name="width"/> values per row)</param>
    /// <param name="triangleCapacity">Expected triangle count, to size the buffers once (they grow otherwise)</param>
    /// <param name="level">
    /// Optional level, in height units (e.g. the sea at 0 m): grid points that the mesh puts on the wrong side of it
    /// (above it while the data is below, or the reverse) are inserted before any other, so that the mesh crosses the
    /// level where the data does (a coastline under a water surface at that level). See <see cref="HasWrongSide"/>.
    /// The coast is refined within the same triangle budget: from about 10 % of the grid's triangles it costs little
    /// accuracy elsewhere, but with far fewer triangles a long coast takes most of them.
    /// </param>
    public Delatin(float[] data, int width, int height, int triangleCapacity = 0, double? level = null)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (width < 2 || height < 2)
            throw new ArgumentOutOfRangeException(nameof(width), "The grid needs at least 2 x 2 points.");
        if (data.Length < width * height)
            throw new ArgumentException("Fewer heights than width x height.", nameof(data));

        _data = data;
        _width = width;
        _height = height;

        if (level is double l)
        {
            float min = float.PositiveInfinity, max = float.NegativeInfinity;
            for (int i = 0; i < width * height; i++)
            {
                min = Math.Min(min, data[i]);
                max = Math.Max(max, data[i]);
            }
            _hasLevel = true;
            _level = l;
            _levelPenalty = (double)max - min + 1;
        }

        // + 2: an insertion can add 2 triangles past the target
        int initialTriangles = Math.Max(64, triangleCapacity + 2);
        // V = (T + B + 2) / 2 for B outline vertices (at most the grid perimeter)
        _coords = new int[2 * Math.Max(64, Math.Min((initialTriangles + 2 * (width + height)) / 2 + 2, width * height))];
        _triangles = new int[initialTriangles * 3];
        _halfedges = new int[initialTriangles * 3];
        _candidates = new int[initialTriangles * 2];
        _queueIndices = new int[initialTriangles];
        _rms = new double[initialTriangles];
        _queue = new int[initialTriangles];
        _errors = new double[initialTriangles];
        _pending = new int[16];

        int x1 = width - 1, y1 = height - 1;
        int p0 = AddPoint(0, 0);
        int p1 = AddPoint(x1, 0);
        int p2 = AddPoint(0, y1);
        int p3 = AddPoint(x1, y1);

        int t0 = AddTriangle(p3, p0, p2, -1, -1, -1);
        AddTriangle(p0, p3, p1, t0, -1, -1);
        Flush();
    }

    public int Width => _width;
    public int Height => _height;
    public int VertexCount => _vertexCount;
    public int TriangleCount => _triangleCount;

    /// <summary>Grid coordinates (x, y) of the vertices</summary>
    public ReadOnlySpan<int> Coordinates => _coords.AsSpan(0, _vertexCount * 2);

    /// <summary>Vertex indices, three per triangle</summary>
    public ReadOnlySpan<int> Triangles => _triangles.AsSpan(0, _triangleCount * 3);

    /// <summary>
    /// Largest vertical error of the current mesh, in height units. With a level, the error of the next point to
    /// insert: a point on the wrong side of the level comes first, even when its error is smaller.
    /// </summary>
    public double MaxError => _queueCount == 0 ? 0 : HasWrongSide ? _errors[0] - _levelPenalty : _errors[0];

    /// <summary>With a level, whether the mesh still puts a grid point on the wrong side of it</summary>
    public bool HasWrongSide => _hasLevel && _queueCount > 0 && _errors[0] >= _levelPenalty;

    /// <summary>Root mean square vertical error of the current mesh over all the grid points</summary>
    public double Rmsd => _rmsSum > 0 ? Math.Sqrt(_rmsSum / ((double)_width * _height)) : 0;

    /// <summary>
    /// Refines until the largest error is at most <paramref name="maxError"/> (with a level, and no grid point on
    /// the wrong side of it)
    /// </summary>
    public void Run(double maxError)
    {
        while (MaxError > maxError || HasWrongSide)
            Refine();
    }

    /// <summary>With a level, refines until no grid point is on the wrong side of it</summary>
    public void RunToLevel()
    {
        while (HasWrongSide)
            Refine();
    }

    /// <summary>
    /// Refines until the mesh has at least <paramref name="triangleCount"/> triangles, or is exact
    /// (an insertion adds 1 or 2 triangles, so the count can exceed the target by 1)
    /// </summary>
    public void RunToTriangleCount(int triangleCount)
    {
        while (_triangleCount < triangleCount && MaxError > 0)
            Refine();
    }

    /// <summary>Inserts the point with the largest error</summary>
    public void Refine()
    {
        Step();
        Flush();
    }

    private float HeightAt(int x, int y) => _data[_width * y + x];

    private void Flush()
    {
        for (int i = 0; i < _pendingCount; i++)
        {
            int t = _pending[i];
            int a = 2 * _triangles[t * 3 + 0];
            int b = 2 * _triangles[t * 3 + 1];
            int c = 2 * _triangles[t * 3 + 2];
            FindCandidate(_coords[a], _coords[a + 1], _coords[b], _coords[b + 1], _coords[c], _coords[c + 1], t);
        }
        _pendingCount = 0;
    }

    /// <summary>Rasterizes the triangle to find its largest error, and queues it</summary>
    private void FindCandidate(int p0x, int p0y, int p1x, int p1y, int p2x, int p2y, int t)
    {
        int minX = Math.Min(p0x, Math.Min(p1x, p2x));
        int minY = Math.Min(p0y, Math.Min(p1y, p2y));
        int maxX = Math.Max(p0x, Math.Max(p1x, p2x));
        int maxY = Math.Max(p0y, Math.Max(p1y, p2y));

        // forward differencing of the edge functions (long: the products of grid sizes can exceed int)
        long w00 = Orient(p1x, p1y, p2x, p2y, minX, minY);
        long w01 = Orient(p2x, p2y, p0x, p0y, minX, minY);
        long w02 = Orient(p0x, p0y, p1x, p1y, minX, minY);
        long a01 = p1y - p0y, b01 = p0x - p1x;
        long a12 = p2y - p1y, b12 = p1x - p2x;
        long a20 = p0y - p2y, b20 = p2x - p0x;

        // heights at the vertices, pre-divided by twice the area
        double area = Orient(p0x, p0y, p1x, p1y, p2x, p2y);
        double z0 = HeightAt(p0x, p0y) / area;
        double z1 = HeightAt(p1x, p1y) / area;
        double z2 = HeightAt(p2x, p2y) / area;

        double maxError = 0, rms = 0;
        int mx = 0, my = 0;
        for (int y = minY; y <= maxY; y++)
        {
            // first column inside the triangle
            long dx = 0;
            if (w00 < 0 && a12 != 0) dx = Math.Max(dx, FloorDiv(-w00, a12));
            if (w01 < 0 && a20 != 0) dx = Math.Max(dx, FloorDiv(-w01, a20));
            if (w02 < 0 && a01 != 0) dx = Math.Max(dx, FloorDiv(-w02, a01));

            long w0 = w00 + a12 * dx;
            long w1 = w01 + a20 * dx;
            long w2 = w02 + a01 * dx;

            bool wasInside = false;
            int row = _width * y;
            for (long x = minX + dx; x <= maxX; x++)
            {
                if (w0 >= 0 && w1 >= 0 && w2 >= 0)
                {
                    wasInside = true;
                    double z = z0 * w0 + z1 * w1 + z2 * w2;
                    double h = _data[row + (int)x];
                    double dz = Math.Abs(z - h);
                    rms += dz * dz;
                    double priority = _hasLevel && (z > _level) != (h > _level) ? dz + _levelPenalty : dz;
                    if (priority > maxError)
                    {
                        maxError = priority;
                        mx = (int)x;
                        my = y;
                    }
                }
                else if (wasInside)
                {
                    break;
                }

                w0 += a12;
                w1 += a20;
                w2 += a01;
            }

            w00 += b12;
            w01 += b20;
            w02 += b01;
        }

        if ((mx == p0x && my == p0y) || (mx == p1x && my == p1y) || (mx == p2x && my == p2y))
            maxError = 0;

        _candidates[2 * t] = mx;
        _candidates[2 * t + 1] = my;
        _rms[t] = rms;

        QueuePush(t, maxError, rms);
    }

    /// <summary>Splits the triangle with the largest error at its candidate point</summary>
    private void Step()
    {
        int t = QueuePop();

        int e0 = t * 3 + 0, e1 = t * 3 + 1, e2 = t * 3 + 2;
        int p0 = _triangles[e0], p1 = _triangles[e1], p2 = _triangles[e2];

        int ax = _coords[2 * p0], ay = _coords[2 * p0 + 1];
        int bx = _coords[2 * p1], by = _coords[2 * p1 + 1];
        int cx = _coords[2 * p2], cy = _coords[2 * p2 + 1];
        int px = _candidates[2 * t], py = _candidates[2 * t + 1];

        int pn = AddPoint(px, py);

        if (Orient(ax, ay, bx, by, px, py) == 0)
        {
            HandleCollinear(pn, e0);
        }
        else if (Orient(bx, by, cx, cy, px, py) == 0)
        {
            HandleCollinear(pn, e1);
        }
        else if (Orient(cx, cy, ax, ay, px, py) == 0)
        {
            HandleCollinear(pn, e2);
        }
        else
        {
            int h0 = _halfedges[e0], h1 = _halfedges[e1], h2 = _halfedges[e2];

            int t0 = AddTriangle(p0, p1, pn, h0, -1, -1, e0);
            int t1 = AddTriangle(p1, p2, pn, h1, -1, t0 + 1);
            int t2 = AddTriangle(p2, p0, pn, h2, t0 + 2, t1 + 1);

            Legalize(t0);
            Legalize(t1);
            Legalize(t2);
        }
    }

    private int AddPoint(int x, int y)
    {
        int i = _vertexCount;
        if (2 * i + 2 > _coords.Length)
            Array.Resize(ref _coords, _coords.Length * 2);
        _coords[2 * i] = x;
        _coords[2 * i + 1] = y;
        _vertexCount++;
        return i;
    }

    /// <summary>Adds a triangle, or replaces the one starting at halfedge <paramref name="e"/></summary>
    /// <returns>Its first halfedge</returns>
    private int AddTriangle(int a, int b, int c, int ab, int bc, int ca, int e = -1)
    {
        if (e < 0)
        {
            e = _triangleCount * 3;
            _triangleCount++;
            EnsureTriangleCapacity(_triangleCount);
        }
        int t = e / 3;

        _triangles[e + 0] = a;
        _triangles[e + 1] = b;
        _triangles[e + 2] = c;

        _halfedges[e + 0] = ab;
        _halfedges[e + 1] = bc;
        _halfedges[e + 2] = ca;

        if (ab >= 0) _halfedges[ab] = e + 0;
        if (bc >= 0) _halfedges[bc] = e + 1;
        if (ca >= 0) _halfedges[ca] = e + 2;

        _candidates[2 * t + 0] = 0;
        _candidates[2 * t + 1] = 0;
        _queueIndices[t] = -1;
        _rms[t] = 0;

        if (_pendingCount == _pending.Length)
            Array.Resize(ref _pending, _pending.Length * 2);
        _pending[_pendingCount++] = t;

        return e;
    }

    private void EnsureTriangleCapacity(int count)
    {
        if (count <= _queueIndices.Length)
            return;
        int n = Math.Max(count, _queueIndices.Length * 2);
        Array.Resize(ref _triangles, n * 3);
        Array.Resize(ref _halfedges, n * 3);
        Array.Resize(ref _candidates, n * 2);
        Array.Resize(ref _queueIndices, n);
        Array.Resize(ref _rms, n);
        Array.Resize(ref _queue, n);
        Array.Resize(ref _errors, n);
    }

    /// <summary>
    /// Flips the edge <paramref name="a"/> if its pair of triangles isn't Delaunay, then checks the new pair
    /// </summary>
    /// <remarks>
    /// <code>
    ///           pl                    pl
    ///          /||\                  /  \
    ///       al/ || \bl            al/    \a
    ///        /  ||  \              /      \
    ///       /  a||b  \    flip    /___ar___\
    ///     p0\   ||   /p1   =>   p0\---bl---/p1
    ///        \  ||  /              \      /
    ///       ar\ || /br             b\    /br
    ///          \||/                  \  /
    ///           pr                    pr
    /// </code>
    /// </remarks>
    private void Legalize(int a)
    {
        int b = _halfedges[a];
        if (b < 0)
            return;

        int a0 = a - a % 3;
        int b0 = b - b % 3;
        int al = a0 + (a + 1) % 3;
        int ar = a0 + (a + 2) % 3;
        int bl = b0 + (b + 2) % 3;
        int br = b0 + (b + 1) % 3;
        int p0 = _triangles[ar];
        int pr = _triangles[a];
        int pl = _triangles[al];
        int p1 = _triangles[bl];

        if (!InCircle(
            _coords[2 * p0], _coords[2 * p0 + 1],
            _coords[2 * pr], _coords[2 * pr + 1],
            _coords[2 * pl], _coords[2 * pl + 1],
            _coords[2 * p1], _coords[2 * p1 + 1]))
        {
            return;
        }

        int hal = _halfedges[al];
        int har = _halfedges[ar];
        int hbl = _halfedges[bl];
        int hbr = _halfedges[br];

        QueueRemove(a0 / 3);
        QueueRemove(b0 / 3);

        int t0 = AddTriangle(p0, p1, pl, -1, hbl, hal, a0);
        int t1 = AddTriangle(p1, p0, pr, t0, har, hbr, b0);

        Legalize(t0 + 1);
        Legalize(t1 + 2);
    }

    /// <summary>The new vertex is on the edge <paramref name="a"/></summary>
    private void HandleCollinear(int pn, int a)
    {
        int a0 = a - a % 3;
        int al = a0 + (a + 1) % 3;
        int ar = a0 + (a + 2) % 3;
        int p0 = _triangles[ar];
        int pr = _triangles[a];
        int pl = _triangles[al];
        int hal = _halfedges[al];
        int har = _halfedges[ar];

        int b = _halfedges[a];

        if (b < 0)
        {
            // on the outline: split the triangle in two
            int u0 = AddTriangle(pn, p0, pr, -1, har, -1, a0);
            int u1 = AddTriangle(p0, pn, pl, u0, -1, hal);
            Legalize(u0 + 1);
            Legalize(u1 + 2);
            return;
        }

        int b0 = b - b % 3;
        int bl = b0 + (b + 2) % 3;
        int br = b0 + (b + 1) % 3;
        int p1 = _triangles[bl];
        int hbl = _halfedges[bl];
        int hbr = _halfedges[br];

        QueueRemove(b0 / 3);

        int t0 = AddTriangle(p0, pr, pn, har, -1, -1, a0);
        int t1 = AddTriangle(pr, p1, pn, hbr, -1, t0 + 1, b0);
        int t2 = AddTriangle(p1, pl, pn, hbl, -1, t1 + 1);
        int t3 = AddTriangle(pl, p0, pn, hal, t0 + 2, t2 + 1);

        Legalize(t0);
        Legalize(t1);
        Legalize(t2);
        Legalize(t3);
    }

    // priority queue (max-heap on _errors)

    private void QueuePush(int t, double error, double rms)
    {
        int i = _queueCount++;
        _queueIndices[t] = i;
        _queue[i] = t;
        _errors[i] = error;
        _rmsSum += rms;
        QueueUp(i);
    }

    private int QueuePop()
    {
        int n = _queueCount - 1;
        QueueSwap(0, n);
        QueueDown(0, n);
        return QueuePopBack();
    }

    private int QueuePopBack()
    {
        int t = _queue[--_queueCount];
        _rmsSum -= _rms[t];
        _queueIndices[t] = -1;
        return t;
    }

    private void QueueRemove(int t)
    {
        int i = _queueIndices[t];
        if (i < 0)
        {
            // not rasterized yet
            int it = Array.IndexOf(_pending, t, 0, _pendingCount);
            if (it < 0)
                throw new InvalidOperationException("Broken triangulation.");
            _pending[it] = _pending[--_pendingCount];
            return;
        }
        int n = _queueCount - 1;
        if (n != i)
        {
            QueueSwap(i, n);
            if (!QueueDown(i, n))
                QueueUp(i);
        }
        QueuePopBack();
    }

    private bool QueueLess(int i, int j) => _errors[i] > _errors[j];

    private void QueueSwap(int i, int j)
    {
        int pi = _queue[i], pj = _queue[j];
        _queue[i] = pj;
        _queue[j] = pi;
        _queueIndices[pi] = j;
        _queueIndices[pj] = i;
        (_errors[i], _errors[j]) = (_errors[j], _errors[i]);
    }

    private void QueueUp(int j)
    {
        while (j > 0)
        {
            int i = (j - 1) >> 1;
            if (!QueueLess(j, i))
                break;
            QueueSwap(i, j);
            j = i;
        }
    }

    private bool QueueDown(int i0, int n)
    {
        int i = i0;
        while (true)
        {
            int j1 = 2 * i + 1;
            if (j1 >= n || j1 < 0)
                break;
            int j2 = j1 + 1;
            int j = j1;
            if (j2 < n && QueueLess(j2, j1))
                j = j2;
            if (!QueueLess(j, i))
                break;
            QueueSwap(i, j);
            i = j;
        }
        return i > i0;
    }

    private static long Orient(long ax, long ay, long bx, long by, long cx, long cy)
        => (bx - cx) * (ay - cy) - (by - cy) * (ax - cx);

    private static bool InCircle(double ax, double ay, double bx, double by, double cx, double cy, double px, double py)
    {
        double dx = ax - px, dy = ay - py;
        double ex = bx - px, ey = by - py;
        double fx = cx - px, fy = cy - py;

        double ap = dx * dx + dy * dy;
        double bp = ex * ex + ey * ey;
        double cp = fx * fx + fy * fy;

        return dx * (ey * cp - bp * fy) - dy * (ex * cp - bp * fx) + ap * (ex * fy - ey * fx) < 0;
    }

    // Math.floor of a quotient of a positive numerator (JS semantics for the start column)
    private static long FloorDiv(long a, long b)
    {
        long q = a / b;
        if ((a % b != 0) && ((a < 0) != (b < 0)))
            q--;
        return q;
    }
}
