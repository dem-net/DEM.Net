
using System;
using System.Buffers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace DEM.Net.Core.Imagery;

public class TileDecoder
{
    private const int TileSize = 256;
    private const int BytesPerPixel = 4; // RGBA32
    private const int TileByteSize = TileSize * TileSize * BytesPerPixel; // 262,144 bytes

    /// <summary>
    /// Loads a horizontal sequence of tiles from disk and extracts their raw RGBA pixels.
    /// </summary>
    public byte[][] DecodeTileRow(string[] tileFilePaths)
    {
        int tilesAcross = tileFilePaths.Length;
        byte[][] decodedTiles = new byte[tilesAcross][];

        for (int i = 0; i < tilesAcross; i++)
        {
            // 1. Rent a buffer for this specific tile
            byte[] rawPixelBuffer = ArrayPool<byte>.Shared.Rent(TileByteSize);

            // 2. Load the tile using ImageSharp
            // The 'using' statement ensures the ImageSharp memory is freed immediately
            using (var tileImage = Image.Load<Rgba32>(tileFilePaths[i]))
            {
                // Ensure the tile is exactly 256x256 (optional validation)
                if (tileImage.Width != TileSize || tileImage.Height != TileSize)
                    throw new InvalidOperationException($"Tile {tileFilePaths[i]} is not 256x256.");

                // 3. Fast-copy the decoded pixels into our rented buffer
                // This copies the entire 256x256 image as a contiguous block of memory
                tileImage.CopyPixelDataTo(rawPixelBuffer.AsSpan(0, TileByteSize));
            }

            // 4. Store the buffer in our array to pass to the stitcher
            decodedTiles[i] = rawPixelBuffer;
        }

        return decodedTiles;
    }

    /// <summary>
    /// Loads a horizontal sequence of tiles from in-memory byte arrays and extracts their raw RGBA pixels.
    /// Supports null or empty entries (treated as transparent tiles).
    /// </summary>
    public byte[][] DecodeTileRowFromBytes(byte[][] tileFileBytes)
    {
        int tilesAcross = tileFileBytes.Length;
        byte[][] decodedTiles = new byte[tilesAcross][];

        for (int i = 0; i < tilesAcross; i++)
        {
            // 1. Rent a buffer for this specific tile
            byte[] rawPixelBuffer = ArrayPool<byte>.Shared.Rent(TileByteSize);

            // If tile bytes are missing, clear buffer to transparent
            if (tileFileBytes[i] == null || tileFileBytes[i].Length == 0)
            {
                Array.Clear(rawPixelBuffer, 0, TileByteSize);
            }
            else
            {
                // 2. Load the tile using ImageSharp from bytes
                using (var tileImage = Image.Load<Rgba32>(tileFileBytes[i]))
                {
                    // Ensure the tile is exactly 256x256 (optional validation)
                    if (tileImage.Width != TileSize || tileImage.Height != TileSize)
                        throw new InvalidOperationException($"Tile image in memory is not 256x256.");

                    // 3. Fast-copy the decoded pixels into our rented buffer
                    tileImage.CopyPixelDataTo(rawPixelBuffer.AsSpan(0, TileByteSize));
                }
            }

            // 4. Store the buffer in our array to pass to the stitcher
            decodedTiles[i] = rawPixelBuffer;
        }

        return decodedTiles;
    }

    /// <summary>
    /// Always remember to return the buffers when the stitcher is done with the row!
    /// </summary>
    public void ReturnBuffers(byte[][] decodedTiles)
    {
        foreach (var buffer in decodedTiles)
        {
            if (buffer != null)
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
