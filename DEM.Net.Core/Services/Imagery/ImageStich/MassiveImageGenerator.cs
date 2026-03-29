using DEM.Net.Core.Imagery;
using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;

namespace DEM.Net.Core.Imagery;

public class MassiveMapGenerator
{
    public void Generate(string outputPath, string imageryZoomRootPath, TileRange tileRange)
    {
        int finalWidth = tileRange.Width;
        int finalHeight = tileRange.Height;

        // 1. Open the raw file stream on disk
        using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 65536);

        // 2. Write the PNG Signature and IHDR
        PngWriter.WriteHeader(fileStream, finalWidth, finalHeight);

        // 3. Wrap the file stream in our IDAT chunker
        using (var idatStream = new IdatChunkStream(fileStream))
        {
            // 4. Wrap the IDAT chunker in a ZLib compression stream.
            // Note: Use CompressionLevel.Fastest or NoCompression for a 100k x 100k image
            // unless you want to wait hours/days for optimal Deflate compression.
            using (var zlibStream = new ZLibStream(idatStream, CompressionLevel.Fastest, leaveOpen: true))
            {
                var decoder = new TileDecoder();         // From previous example (ImageSharp)
                var stitcher = new PngScanlineStitcher(); // From previous example (Span<byte>)

                // Build a fast lookup dictionary for tiles to avoid repeated linear scans
                var tileDictionary = new Dictionary<(int X, int Y), MapTile>(tileRange.Count);
                foreach (var t in tileRange.Tiles)
                {
                    if (t?.TileInfo != null)
                    {
                        tileDictionary[(t.TileInfo.X, t.TileInfo.Y)] = t;
                    }
                }

                for (int y = 0; y < tileRange.NumRows; y++)
                {
                    // Build an in-memory row of tile bytes from the TileRange (no disk I/O)
                    byte[][] rowBytes = new byte[tileRange.NumCols][];
                    for (int i = 0; i < tileRange.NumCols; i++)
                    {
                        int x = tileRange.Start.X + i;
                        int tileY = tileRange.Start.Y + y;
                        tileDictionary.TryGetValue((x, tileY), out var found);
                        rowBytes[i] = found?.Image;
                    }

                    // Decode tiles into rented memory arrays directly from bytes
                    byte[][] decodedTiles = decoder.DecodeTileRowFromBytes(rowBytes);

                    // Stitch scanlines and push them through ZLib -> IDAT -> File
                    stitcher.ProcessTileRow(decodedTiles, tileRange.NumCols, zlibStream);

                    // Clean up rented memory before the next row
                    decoder.ReturnBuffers(decodedTiles);

                    Console.WriteLine($"Processed Row {y + 1}/{tileRange.NumRows}");
                }
            } // ZLibStream closed here: Flushes remaining compressed data
        } // IdatChunkStream closed here

        // 5. Write the final end-of-file marker
        PngWriter.WriteEndChunk(fileStream);
    }
}