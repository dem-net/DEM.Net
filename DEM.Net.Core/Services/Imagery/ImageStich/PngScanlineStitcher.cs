using System;
using System.Buffers;
using System.IO;

namespace DEM.Net.Core.Imagery;


public class PngScanlineStitcher
{
    private const int TileSize = 256;
    private const int BytesPerPixel = 4; // 32-bit RGBA
    private const int TileRowBytes = TileSize * BytesPerPixel; // 1024 bytes per row in a tile

    /// <summary>
    /// Stitches a horizontal row of tiles together scanline-by-scanline and writes to a compression stream.
    /// </summary>
    /// <param name="decodedTiles">Array of raw RGBA byte arrays for the current row of tiles.</param>
    /// <param name="tilesAcross">Total number of tiles in this horizontal row.</param>
    /// <param name="zlibStream">The Deflate/Zlib stream that will compress the pixel data.</param>
    public void ProcessTileRow(byte[][] decodedTiles, int tilesAcross, Stream zlibStream)
    {
        // Total scanline width: (tilesAcross * 256 pixels * 4 bytes) + 1 byte for PNG filter
        int scanlineLength = (tilesAcross * TileRowBytes) + 1;

        // Rent a buffer from the shared pool. 
        // Note: Rented arrays are often larger than requested, so we MUST slice it.
        byte[] scanlineBuffer = ArrayPool<byte>.Shared.Rent(scanlineLength);

        try
        {
            // Slice the rented array to the exact length we need
            Span<byte> scanlineSpan = scanlineBuffer.AsSpan(0, scanlineLength);

            // Iterate through each horizontal line (y = 0 to 255) inside this row of tiles
            for (int y = 0; y < TileSize; y++)
            {
                // 1. The first byte of EVERY PNG scanline is the filter type. 
                // We use 0 (None) because calculating predictive filters across a 100k image 
                // destroys CPU performance for minimal compression gain.
                scanlineSpan[0] = 0;

                // 2. Extract this specific scanline (y) from every tile and copy it in
                for (int tileX = 0; tileX < tilesAcross; tileX++)
                {
                    // Calculate destination offset: +1 skips the filter byte, then jump by tile index
                    int destOffset = 1 + (tileX * TileRowBytes);
                    Span<byte> destSlice = scanlineSpan.Slice(destOffset, TileRowBytes);

                    // Calculate source offset inside the specific tile's raw byte array
                    int srcOffset = y * TileRowBytes;
                    Span<byte> srcSlice = decodedTiles[tileX].AsSpan(srcOffset, TileRowBytes);

                    // 3. Fast memory copy. 
                    // Under the hood, this uses highly optimized SIMD instructions (memmove).
                    srcSlice.CopyTo(destSlice);
                }

                // 4. Write the fully assembled, 1-pixel-high scanline to the compression stream
                zlibStream.Write(scanlineSpan);
            }
        }
        finally
        {
            // CRITICAL: Always return rented arrays to the pool, even if the stream throws an exception
            ArrayPool<byte>.Shared.Return(scanlineBuffer);
        }
    }
}