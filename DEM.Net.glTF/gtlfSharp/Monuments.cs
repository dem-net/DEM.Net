using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Matrix3x2 = System.Numerics.Matrix3x2;

namespace DEM.Net.glTF.SharpglTF;

/// <summary>
/// A monument silhouette, drawn for DEM.Net as a flat SVG path.
/// </summary>
/// <param name="Name">Display name</param>
/// <param name="Height">Height above the ground, in meters</param>
/// <param name="Source">Where the height comes from</param>
/// <param name="SvgPath">
/// SVG path data in meters: x horizontal, y down (SVG convention), the ground on y = 0, so the top is at y = -<paramref name="Height"/>.
/// Filled with the even-odd rule (inner subpaths are holes).
/// </param>
public sealed record Monument(string Name, float Height, string Source, string SvgPath);

/// <summary>
/// The fixed set of monuments drawn on the walls of boxed models, as a scale reference.
/// </summary>
/// <remarks>
/// The outlines are simplified, drawn for this project from the published dimensions (no third-party image). The
/// height of each path's bounds is the monument's height: <c>MonumentTests</c> checks it.
/// </remarks>
public static class Monuments
{
    public static readonly IReadOnlyList<Monument> All =
    [
        new("Burj Khalifa", 828f, "Council on Tall Buildings and Urban Habitat (CTBUH), architectural height",
            "M-60 0 L-60 -140 L-50 -140 L-50 -260 L-41 -260 L-41 -380 L-32 -380 L-32 -480 L-24 -480 L-24 -570 L-16 -570 L-16 -640 L-9 -640 L-9 -700 L-3.5 -700 L-2 -760 L-0.3 -828 L0.3 -828 L2 -760 L3.5 -700 L9 -700 L9 -640 L16 -640 L16 -570 L24 -570 L24 -480 L32 -480 L32 -380 L41 -380 L41 -260 L50 -260 L50 -140 L60 -140 L60 0 Z"),
        new("Tokyo Skytree", 634f, "Tobu Tower Skytree Co.",
            "M-34 0 L-26 -60 L-19 -150 L-14 -250 L-12 -335 L-17 -340 L-17 -360 L-12 -365 L-10 -440 L-13 -445 L-13 -455 L-8 -460 L-6 -495 L-3 -500 L-3 -600 L-1.5 -610 L-0.4 -634 L0.4 -634 L1.5 -610 L3 -600 L3 -500 L6 -495 L8 -460 L13 -455 L13 -445 L10 -440 L12 -365 L17 -360 L17 -340 L12 -335 L14 -250 L19 -150 L26 -60 L34 0 Z"),
        new("Empire State Building", 443f, "CTBUH, height to the antenna tip (443.2 m)",
            "M-65 0 L-65 -25 L-42 -25 L-42 -90 L-37 -90 L-37 -110 L-31 -110 L-31 -320 L-23 -320 L-23 -330 L-15 -330 L-15 -350 L-8 -350 L-8 -373 L-4 -373 L-4 -381 L-1.2 -381 L-0.4 -443 L0.4 -443 L1.2 -381 L4 -381 L4 -373 L8 -373 L8 -350 L15 -350 L15 -330 L23 -330 L23 -320 L31 -320 L31 -110 L37 -110 L37 -90 L42 -90 L42 -25 L65 -25 L65 0 Z"),
        new("Eiffel Tower", 330f, "Société d'Exploitation de la Tour Eiffel, height since 2022",
            "M-62.5 0 L-52 -25 L-45 -50 L-36 -57.6 L-36 -64 L-30 -64 L-24 -90 L-19.5 -112 L-21 -112 L-21 -120 L-16.5 -120 L-12 -160 L-8.5 -200 L-6 -240 L-4.6 -272 L-6.2 -274 L-6.2 -281 L-4 -281 L-3 -295 L-1.8 -305 L-0.9 -315 L-0.4 -330 L0.4 -330 L0.9 -315 L1.8 -305 L3 -295 L4 -281 L6.2 -281 L6.2 -274 L4.6 -272 L6 -240 L8.5 -200 L12 -160 L16.5 -120 L21 -120 L21 -112 L19.5 -112 L24 -90 L30 -64 L36 -64 L36 -57.6 L45 -50 L52 -25 L62.5 0 L38 0 L37.27 -7.61 L35.11 -14.92 L31.6 -21.67 L26.87 -27.58 L21.11 -32.43 L14.54 -36.03 L7.41 -38.25 L0 -39 L-7.41 -38.25 L-14.54 -36.03 L-21.11 -32.43 L-26.87 -27.58 L-31.6 -21.67 L-35.11 -14.92 L-37.27 -7.61 L-38 0 Z M-15 -68 L-9 -90 L-0.01 -106 L0.01 -106 L9 -90 L15 -68 Z"),
        new("Great Pyramid of Giza", 138.5f, "Encyclopaedia Britannica, present height (base 230.3 m)",
            "M-115.15 0 L0 -138.5 L115.15 0 Z"),
        new("Elizabeth Tower (Big Ben)", 96f, "UK Parliament",
            "M-6 0 L-6 -55 L-7 -55 L-7 -70 L-6.5 -70 L-6.5 -78 L-5 -78 L-3 -85 L-1.5 -92 L-0.25 -96 L0.25 -96 L1.5 -92 L3 -85 L5 -78 L6.5 -78 L6.5 -70 L7 -70 L7 -55 L6 -55 L6 0 Z"),
        new("Statue of Liberty", 93f, "US National Park Service, ground to the tip of the torch",
            "M-24 0 L-24 -6 L-14 -6 L-14 -20 L-10 -20 L-10 -24 L-8.5 -24 L-8.5 -44 L-9.5 -44 L-9.5 -47 L-6 -47 L-5.5 -62 L-4.5 -76 L-3.8 -79.5 L-5.2 -87 L-6.2 -88.5 L-6.2 -90 L-5.4 -90.5 L-5.2 -93 L-4.6 -90.5 L-4 -90 L-4.2 -88.5 L-4.4 -87.2 L-2.8 -80.5 L-1.6 -81 L-1.7 -83.5 L-3.6 -85 L-1.4 -84.6 L-1.8 -86.5 L-0.6 -85 L0 -87 L0.6 -85 L1.8 -86.5 L1.4 -84.6 L3.6 -85 L1.7 -83.5 L1.6 -81 L3.8 -79.5 L4.5 -76 L6.8 -75 L7.2 -66 L5.6 -65 L5.5 -62 L6 -47 L9.5 -47 L9.5 -44 L8.5 -44 L8.5 -24 L10 -24 L10 -20 L14 -20 L14 -6 L24 -6 L24 0 Z"),
        new("Leaning Tower of Pisa", 56.67f, "Opera della Primaziale Pisana, height on the high side (leaning 3.97°)",
            "M-7.73 0 L-7.18 -8.95 L-7.53 -8.97 L-7.46 -9.96 L-7.11 -9.93 L-4.41 -48.39 L-4.76 -48.41 L-4.69 -49.4 L-2.59 -49.25 L-2.14 -55.66 L-2.44 -55.68 L-2.37 -56.67 L10.2 -55.81 L10.13 -54.82 L9.83 -54.84 L9.38 -48.43 L11.47 -48.29 L11.4 -47.3 L11.05 -47.33 L8.35 -8.87 L8.7 -8.85 L8.63 -7.86 L8.29 -7.89 L7.73 0 Z"),
        new("Arc de Triomphe", 50f, "Centre des monuments nationaux (45 m wide)",
            "M-22.5 0 L-22.5 -40 L-23.2 -40 L-23.2 -42 L-22.5 -42 L-22.5 -50 L22.5 -50 L22.5 -42 L23.2 -42 L23.2 -40 L22.5 -40 L22.5 0 L7.31 0 L7.31 -21.88 L7.06 -23.77 L6.33 -25.54 L5.17 -27.05 L3.66 -28.21 L1.89 -28.94 L0 -29.19 L-1.89 -28.94 L-3.65 -28.21 L-5.17 -27.05 L-6.33 -25.54 L-7.06 -23.77 L-7.31 -21.88 L-7.31 0 Z"),
        new("Christ the Redeemer", 38f, "Encyclopaedia Britannica, 30 m statue on an 8 m pedestal, arms 28 m",
            "M-4.5 0 L-4.5 -8 L-3.6 -8 L-3.2 -27 L-3 -31 L-13.5 -31.6 L-14 -32.2 L-14 -33.2 L-3 -33.6 L-1.4 -34.2 L-1.6 -36.5 L-1 -37.6 L-0.01 -38 L0.01 -38 L1 -37.6 L1.6 -36.5 L1.4 -34.2 L3 -33.6 L14 -33.2 L14 -32.2 L13.5 -31.6 L3 -31 L3.2 -27 L3.6 -8 L4.5 -8 L4.5 0 Z"),
    ];
}

/// <summary>
/// One PNG holding every monument silhouette (white, the shape in the alpha channel), rasterized once and cached.
/// The silhouettes are scaled by the geometry, so the atlas is the same for every model.
/// </summary>
public sealed class MonumentAtlas
{
    /// <summary>Atlas pixel height of every silhouette</summary>
    public const int CellHeight = 512;
    private const int Padding = 4;
    private const int MaxWidth = 2048;

    private static readonly Lazy<MonumentAtlas> _instance = new(() => new MonumentAtlas(Monuments.All));
    public static MonumentAtlas Instance => _instance.Value;

    /// <summary>PNG bytes (grayscale with alpha)</summary>
    public byte[] Png { get; }

    /// <summary>Per monument: its outline bounds in meters (x right, y up from the ground) and its texture coordinates (v down).</summary>
    public IReadOnlyDictionary<Monument, MonumentAtlasEntry> Entries { get; }

    private MonumentAtlas(IReadOnlyList<Monument> monuments)
    {
        var paths = new List<(Monument Monument, IPath Path, RectangleF Bounds, int Width)>();
        foreach (var m in monuments)
        {
            if (!Path.TryParseSvgPath(m.SvgPath, out IPath path))
                throw new InvalidOperationException($"Invalid SVG path for {m.Name}");
            RectangleF b = path.Bounds;
            int width = (int)MathF.Ceiling(b.Width / b.Height * CellHeight);
            paths.Add((m, path, b, width));
        }

        // Shelf packing: rows of CellHeight, left to right
        var cells = new List<(int X, int Y)>(paths.Count);
        int x = 0, y = 0, atlasWidth = 0;
        foreach (var p in paths)
        {
            int cellWidth = p.Width + 2 * Padding;
            if (x > 0 && x + cellWidth > MaxWidth)
            {
                x = 0;
                y += CellHeight + 2 * Padding;
            }
            cells.Add((x + Padding, y + Padding));
            x += cellWidth;
            atlasWidth = Math.Max(atlasWidth, x);
        }
        int atlasHeight = y + CellHeight + 2 * Padding;

        var entries = new Dictionary<Monument, MonumentAtlasEntry>(paths.Count);
        using var image = new Image<Rgba32>(atlasWidth, atlasHeight, new Rgba32(255, 255, 255, 0));
        var options = new DrawingOptions { ShapeOptions = new ShapeOptions { IntersectionRule = IntersectionRule.EvenOdd } };
        for (int i = 0; i < paths.Count; i++)
        {
            var (monument, path, b, width) = paths[i];
            var (cx, cy) = cells[i];
            float scaleX = width / b.Width, scaleY = (float)CellHeight / b.Height;
            var transform = Matrix3x2.CreateTranslation(-b.Left, -b.Top) * Matrix3x2.CreateScale(scaleX, scaleY) * Matrix3x2.CreateTranslation(cx, cy);
            image.Mutate(c => c.Fill(options, Color.White, path.Transform(transform)));
            entries.Add(monument, new MonumentAtlasEntry(
                Left: b.Left, Right: b.Right, Height: b.Height,
                U0: (float)cx / atlasWidth, V0: (float)cy / atlasHeight,
                U1: (float)(cx + width) / atlasWidth, V1: (float)(cy + CellHeight) / atlasHeight));
        }
        Entries = entries;

        using var stream = new System.IO.MemoryStream();
        image.SaveAsPng(stream, new PngEncoder { ColorType = PngColorType.GrayscaleWithAlpha, BitDepth = PngBitDepth.Bit8 });
        Png = stream.ToArray();
    }
}

/// <param name="Left">Outline left bound, meters from the path origin</param>
/// <param name="Right">Outline right bound, meters</param>
/// <param name="Height">Outline height, meters</param>
public readonly record struct MonumentAtlasEntry(float Left, float Right, float Height, float U0, float V0, float U1, float V1)
{
    public float Width => Right - Left;
}
