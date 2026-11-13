using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;

namespace DEM.Net.Core.Imagery;

/// <summary>
/// Streams a cropped texture from a tile range directly to a PNG file, one tile row at a time.
/// Memory usage is bounded by one row of decoded tiles (NumCols * tileSize² * 4 bytes),
/// instead of the full output image.
/// </summary>
internal sealed class MassiveMapGenerator
{
    private readonly ILogger? _logger;

    public MassiveMapGenerator(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generates the PNG.
    /// </summary>
    /// <param name="outputPath">Output PNG file path.</param>
    /// <param name="tileRange">Downloaded tiles.</param>
    /// <param name="cropRect">Output area in pixels, relative to the top-left corner of the tile range.
    /// Areas outside the tile range are transparent.</param>
    /// <param name="compressionLevel">Deflate compression level.</param>
    public void Generate(string outputPath, TileRange tileRange, Rectangle cropRect,
        CompressionLevel compressionLevel = CompressionLevel.Optimal)
    {
        if (cropRect.Width <= 0 || cropRect.Height <= 0)
            throw new ArgumentException("Crop rectangle must have positive dimensions.", nameof(cropRect));

        var sw = Stopwatch.StartNew();
        int tileSize = tileRange.TileSize;

        var tileLookup = new Dictionary<(int X, int Y), MapTile>(tileRange.Count);
        foreach (var t in tileRange.Tiles)
        {
            if (t?.TileInfo != null) tileLookup[(t.TileInfo.X, t.TileInfo.Y)] = t;
        }

        // Only the tile rows/columns intersecting the crop are decoded
        int firstCol = Math.Max(0, FloorDiv(cropRect.Left, tileSize));
        int lastCol = Math.Min(tileRange.NumCols - 1, FloorDiv(cropRect.Right - 1, tileSize));
        int firstRow = Math.Max(0, FloorDiv(cropRect.Top, tileSize));
        int lastRow = Math.Min(tileRange.NumRows - 1, FloorDiv(cropRect.Bottom - 1, tileSize));
        int colCount = Math.Max(0, lastCol - firstCol + 1);

        var decoder = new TileDecoder(tileSize, _logger);
        var stitcher = new PngScanlineStitcher(tileSize);
        byte[] scanlineBuffer = ArrayPool<byte>.Shared.Rent(cropRect.Width * TileDecoder.BytesPerPixel + 1);
        byte[]?[] rowTiles = new byte[]?[colCount];

        try
        {
            using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 1 << 16);
            PngWriter.WriteHeader(fileStream, cropRect.Width, cropRect.Height);

            // ZLib -> 64KB buffer (so IDAT chunks are reasonably sized) -> IDAT chunker -> file
            using (var idatStream = new IdatChunkStream(fileStream))
            using (var bufferedIdat = new BufferedStream(idatStream, 1 << 16))
            using (var zlibStream = new ZLibStream(bufferedIdat, compressionLevel, leaveOpen: true))
            {
                for (int y = cropRect.Top; y < cropRect.Bottom; y++)
                {
                    int tileRow = FloorDiv(y, tileSize);
                    int yInTile = y - tileRow * tileSize;
                    bool rowInRange = tileRow >= firstRow && tileRow <= lastRow;

                    // Entering a new tile row: decode its tiles
                    if (rowInRange && (y == cropRect.Top || yInTile == 0))
                    {
                        ReturnAll(decoder, rowTiles);
                        for (int i = 0; i < colCount; i++)
                        {
                            tileLookup.TryGetValue((tileRange.Start.X + firstCol + i, tileRange.Start.Y + tileRow), out var tile);
                            rowTiles[i] = decoder.Decode(tile);
                        }
                        _logger?.LogDebug("Massive texture: tile row {Row}/{Rows}", tileRow + 1, tileRange.NumRows);
                    }

                    stitcher.WriteCroppedScanline(rowInRange ? rowTiles : Array.Empty<byte[]?>(), firstCol,
                        cropRect.X, cropRect.Width, yInTile, scanlineBuffer, zlibStream);
                }
            } // flushes remaining compressed data into IDAT chunks

            PngWriter.WriteEndChunk(fileStream);
        }
        finally
        {
            ReturnAll(decoder, rowTiles);
            ArrayPool<byte>.Shared.Return(scanlineBuffer);
        }

        _logger?.LogInformation("Massive texture {Width}x{Height} generated in {Elapsed:g}", cropRect.Width, cropRect.Height, sw.Elapsed);
    }

    private static void ReturnAll(TileDecoder decoder, byte[]?[] tiles)
    {
        for (int i = 0; i < tiles.Length; i++)
        {
            decoder.Return(tiles[i]);
            tiles[i] = null;
        }
    }

    private static int FloorDiv(int a, int b) => (int)Math.Floor((double)a / b);
}
