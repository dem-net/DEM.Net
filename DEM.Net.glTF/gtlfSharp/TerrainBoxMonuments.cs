using System;
using System.Collections.Generic;
using System.Numerics;

namespace DEM.Net.glTF.SharpglTF;

/// <summary>
/// Monument silhouettes on the box walls (<see cref="Monuments"/>), drawn at the model's scale as a height reference.
/// </summary>
public sealed record TerrainBoxMonumentOptions
{
    /// <summary>Darker than <see cref="TerrainBoxOptions.DefaultColor"/>.</summary>
    public static readonly Vector4 DefaultColor = new(0.22f, 0.2f, 0.18f, 1f);

    /// <summary>Model units per meter on the ground (the projection's scale at the model, e.g. 1/cos(latitude) in Web Mercator).</summary>
    public required float HorizontalScale { get; init; }

    /// <summary>Model units per meter of height: the Z factor.</summary>
    public required float VerticalScale { get; init; }

    /// <summary>Solid color (linear RGBA) of the silhouettes.</summary>
    public Vector4 Color { get; init; } = DefaultColor;

    /// <summary>
    /// Monuments lower than this fraction of the model's larger side are not drawn: they would be unreadable.
    /// </summary>
    public float MinHeightRatio { get; init; } = 0.02f;

    /// <summary>Free space between a monument's top and the top of the wall, as a fraction of the monument height.</summary>
    public float TopMarginRatio { get; init; } = 0.05f;
}

/// <summary>
/// A monument decal: a quad standing on the box base, just outside a wall, facing <see cref="Normal"/>.
/// </summary>
/// <param name="BottomLeft">Bottom left corner, seen from outside</param>
/// <param name="Right">Unit vector along the wall, to the right seen from outside</param>
/// <param name="Normal">Outward unit normal of the wall</param>
/// <param name="Width">Model units</param>
/// <param name="Height">Model units</param>
public sealed record MonumentDecal(Monument Monument, Vector3 BottomLeft, Vector3 Right, Vector3 Normal, float Width, float Height);

/// <summary>
/// Places monuments on the straight sides of a rectangular terrain's box without cutting them with the terrain edge.
/// </summary>
/// <remarks>
/// Each side's wall top is the terrain outline through the boundary vertices, so the wall height is a 1D profile.
/// It is sampled on a grid of cells, each cell keeping the lowest wall top over it, and a monument of width w fits
/// where the minimum over a w window (sliding window minimum, O(cells)) is above the monument. Greedy: tallest monuments
/// first, the south side first (the default view), the middle of the longest free run.
/// </remarks>
public static class MonumentPlacer
{
    private const int CellsPerSide = 1024;

    /// <param name="positions">Terrain positions, glTF space (Y up, south = +Z)</param>
    /// <param name="baseY">Box base elevation</param>
    public static List<MonumentDecal> Place(IReadOnlyList<Vector3> positions, float baseY, TerrainBoxMonumentOptions options, IReadOnlyList<Monument>? monuments = null)
    {
        ArgumentNullException.ThrowIfNull(positions);
        ArgumentNullException.ThrowIfNull(options);
        monuments ??= Monuments.All;
        var decals = new List<MonumentDecal>();
        if (positions.Count < 3 || options.HorizontalScale <= 0 || options.VerticalScale <= 0)
            return decals;

        Vector3 min = new(float.MaxValue), max = new(float.MinValue);
        foreach (var p in positions)
        {
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }
        float size = MathF.Max(max.X - min.X, max.Z - min.Z);
        if (size <= 0)
            return decals;
        float offset = 0.001f * size; // a hair outside the wall: no z-fighting
        float minHeight = options.MinHeightRatio * size;

        // South, east, north, west. Right = Y x Normal: a viewer facing the wall sees it go left to right.
        Side[] sides =
        [
            new(Vector3.UnitZ, new Vector3(min.X, baseY, max.Z), max.X - min.X),
            new(Vector3.UnitX, new Vector3(max.X, baseY, max.Z), max.Z - min.Z),
            new(-Vector3.UnitZ, new Vector3(max.X, baseY, min.Z), max.X - min.X),
            new(-Vector3.UnitX, new Vector3(min.X, baseY, min.Z), max.Z - min.Z),
        ];
        float tolerance = 1e-5f * size;
        foreach (var side in sides)
            side.BuildProfile(positions, tolerance);

        var atlas = MonumentAtlas.Instance;
        var ordered = new List<Monument>(monuments);
        ordered.Sort((a, b) => b.Height.CompareTo(a.Height));
        foreach (var monument in ordered)
        {
            var entry = atlas.Entries[monument];
            float height = entry.Height * options.VerticalScale;
            float width = entry.Width * options.HorizontalScale;
            if (height < minHeight)
                continue;
            float clearance = height * (1f + options.TopMarginRatio);
            foreach (var side in sides)
            {
                if (side.TryPlace(width, clearance, out float left))
                {
                    decals.Add(new MonumentDecal(monument, side.Origin + side.Normal * offset + side.Right * left, side.Right, side.Normal, width, height));
                    break;
                }
            }
        }
        return decals;
    }

    private sealed class Side(Vector3 normal, Vector3 origin, float length)
    {
        public Vector3 Normal { get; } = normal;
        public Vector3 Right { get; } = Vector3.Cross(Vector3.UnitY, normal);
        /// <summary>Left end of the side seen from outside, at the base</summary>
        public Vector3 Origin { get; } = origin;

        private float _step;
        private float[] _wall = []; // per cell: lowest wall height (top - base) over the cell
        private bool[] _used = [];

        public void BuildProfile(IReadOnlyList<Vector3> positions, float tolerance)
        {
            if (length <= 0)
                return;
            float plane = Vector3.Dot(Origin, Normal);
            var profile = new List<(float T, float H)>();
            foreach (var p in positions)
            {
                if (MathF.Abs(Vector3.Dot(p, Normal) - plane) <= tolerance)
                    profile.Add((Math.Clamp(Vector3.Dot(p - Origin, Right), 0f, length), p.Y - Origin.Y));
            }
            if (profile.Count < 2)
                return;
            profile.Sort((a, b) => a.T.CompareTo(b.T));

            int n = CellsPerSide;
            _step = length / n;
            _wall = new float[n];
            _used = new bool[n];
            // Wall top at each cell boundary (linear between boundary vertices), then the vertices inside each cell
            int k = 0;
            float previous = 0;
            for (int i = 0; i <= n; i++)
            {
                float t = i * _step;
                while (k < profile.Count - 1 && profile[k + 1].T < t)
                    k++;
                float h = Interpolate(profile, k, t);
                if (i > 0)
                    _wall[i - 1] = MathF.Min(previous, h);
                previous = h;
            }
            foreach (var (t, h) in profile)
            {
                int cell = Math.Min((int)(t / _step), n - 1);
                _wall[cell] = MathF.Min(_wall[cell], h);
            }
            // Keep the corners free
            int corner = (int)MathF.Ceiling(0.01f * n);
            for (int i = 0; i < corner; i++)
                _used[i] = _used[n - 1 - i] = true;
        }

        private static float Interpolate(List<(float T, float H)> profile, int k, float t)
        {
            if (t <= profile[0].T)
                return profile[0].H;
            if (k >= profile.Count - 1)
                return profile[^1].H;
            var (t0, h0) = profile[k];
            var (t1, h1) = profile[k + 1];
            return t1 - t0 <= 0 ? MathF.Min(h0, h1) : h0 + (h1 - h0) * (t - t0) / (t1 - t0);
        }

        /// <summary>Finds a free window of the side where the wall is at least <paramref name="clearance"/> high, and marks it used.</summary>
        /// <param name="left">Distance from the origin to the monument's left edge</param>
        public bool TryPlace(float width, float clearance, out float left)
        {
            left = 0;
            int n = _wall.Length;
            if (n == 0 || width > length)
                return false;
            int window = Math.Max(1, (int)MathF.Ceiling(width / _step));
            if (window > n)
                return false;

            // Sliding window minimum (monotonic deque of cell indices, increasing wall heights) and used cell count
            var fits = new bool[n - window + 1];
            var deque = new int[n];
            int head = 0, tail = 0, used = 0;
            for (int i = 0; i < n; i++)
            {
                while (tail > head && _wall[deque[tail - 1]] >= _wall[i])
                    tail--;
                deque[tail++] = i;
                if (_used[i]) used++;
                int start = i - window + 1;
                if (start < 0)
                    continue;
                if (deque[head] < start)
                    head++;
                if (start > 0 && _used[start - 1]) used--;
                fits[start] = used == 0 && _wall[deque[head]] >= clearance;
            }

            // Longest run of fitting start cells, the monument in its middle
            int bestStart = -1, bestLength = 0;
            for (int i = 0; i < fits.Length;)
            {
                if (!fits[i]) { i++; continue; }
                int j = i;
                while (j < fits.Length && fits[j]) j++;
                if (j - i > bestLength)
                {
                    bestLength = j - i;
                    bestStart = i;
                }
                i = j;
            }
            if (bestStart < 0)
                return false;

            int cell = bestStart + (bestLength - 1) / 2;
            // Center the monument in its window of cells (the window is up to one cell wider than the monument)
            left = cell * _step + (window * _step - width) / 2;

            // Spacing: a quarter of the monument's width on each side, and at least 1 % of the side
            int gap = (int)MathF.Ceiling(MathF.Max(0.25f * width, 0.01f * length) / _step);
            for (int i = Math.Max(0, cell - gap); i < Math.Min(n, cell + window + gap); i++)
                _used[i] = true;
            return true;
        }
    }
}
