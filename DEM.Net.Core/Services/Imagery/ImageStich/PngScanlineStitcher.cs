using System;
using System.IO;

namespace DEM.Net.Core.Imagery;

/// <summary>
/// Assembles PNG scanlines for a crop rectangle from a row of decoded tiles.
/// </summary>
internal sealed class PngScanlineStitcher
{
    private readonly int _tileSize;
    private readonly int _tileRowBytes;

    public PngScanlineStitcher(int tileSize)
    {
        if (tileSize <= 0) throw new ArgumentOutOfRangeException(nameof(tileSize));
        _tileSize = tileSize;
        _tileRowBytes = tileSize * TileDecoder.BytesPerPixel;
    }

    /// <summary>
    /// Builds one PNG scanline (filter byte + cropWidth RGBA pixels) and writes it to <paramref name="output"/>.
    /// </summary>
    /// <param name="decodedTiles">Decoded tiles of the current tile row. Index 0 is tile column <paramref name="firstTileColumn"/>.
    /// Entries may be null (rendered transparent).</param>
    /// <param name="firstTileColumn">Tile column (relative to the tile range start) of decodedTiles[0].</param>
    /// <param name="cropX">Crop start in pixels, relative to the tile range left edge. May be negative.</param>
    /// <param name="cropWidth">Crop width in pixels.</param>
    /// <param name="yInTile">Scanline index inside the tile (0..tileSize-1).</param>
    /// <param name="scanline">Work buffer of at least cropWidth * 4 + 1 bytes.</param>
    public void WriteCroppedScanline(byte[]?[] decodedTiles, int firstTileColumn, int cropX, int cropWidth,
        int yInTile, Span<byte> scanline, Stream output)
    {
        int scanlineLength = cropWidth * TileDecoder.BytesPerPixel + 1;
        scanline = scanline.Slice(0, scanlineLength);
        scanline.Clear(); // filter byte 0 (None), and transparent pixels outside the tile area

        int cropEnd = cropX + cropWidth; // exclusive
        for (int i = 0; i < decodedTiles.Length; i++)
        {
            byte[]? tile = decodedTiles[i];
            if (tile == null) continue;

            int tileStart = (firstTileColumn + i) * _tileSize;
            int from = Math.Max(tileStart, cropX);
            int to = Math.Min(tileStart + _tileSize, cropEnd);
            if (to <= from) continue;

            int srcOffset = yInTile * _tileRowBytes + (from - tileStart) * TileDecoder.BytesPerPixel;
            int destOffset = 1 + (from - cropX) * TileDecoder.BytesPerPixel;
            int copyBytes = (to - from) * TileDecoder.BytesPerPixel;

            tile.AsSpan(srcOffset, copyBytes).CopyTo(scanline.Slice(destOffset, copyBytes));
        }

        output.Write(scanline);
    }
}
