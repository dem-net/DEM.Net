using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Hashing;

namespace DEM.Net.Core.Imagery;

/// <summary>
/// Writes the non-pixel parts of a PNG file (signature, IHDR, IEND).
/// Pixel data is written separately through <see cref="IdatChunkStream"/>.
/// </summary>
internal static class PngWriter
{
    private static readonly byte[] PngSignature = { 137, 80, 78, 71, 13, 10, 26, 10 };

    /// <summary>
    /// Writes the PNG signature and an IHDR chunk for an 8-bit RGBA, non-interlaced image.
    /// </summary>
    public static void WriteHeader(Stream stream, int width, int height)
    {
        stream.Write(PngSignature);

        Span<byte> ihdrChunk = stackalloc byte[4 + 4 + 13 + 4]; // Length + Type + Data + CRC
        BinaryPrimitives.WriteInt32BigEndian(ihdrChunk.Slice(0, 4), 13);
        "IHDR"u8.CopyTo(ihdrChunk.Slice(4, 4));
        BinaryPrimitives.WriteInt32BigEndian(ihdrChunk.Slice(8, 4), width);
        BinaryPrimitives.WriteInt32BigEndian(ihdrChunk.Slice(12, 4), height);
        ihdrChunk[16] = 8; // Bit depth: 8 bits per channel
        ihdrChunk[17] = 6; // Color type: 6 = Truecolor with Alpha (RGBA)
        ihdrChunk[18] = 0; // Compression method: 0 = DEFLATE
        ihdrChunk[19] = 0; // Filter method: 0 = Standard
        ihdrChunk[20] = 0; // Interlace method: 0 = No interlace

        uint crc = Crc32.HashToUInt32(ihdrChunk.Slice(4, 17)); // over Type + Data
        BinaryPrimitives.WriteUInt32BigEndian(ihdrChunk.Slice(21, 4), crc);

        stream.Write(ihdrChunk);
    }

    public static void WriteEndChunk(Stream stream)
    {
        Span<byte> iendChunk = stackalloc byte[12];
        BinaryPrimitives.WriteInt32BigEndian(iendChunk.Slice(0, 4), 0);
        "IEND"u8.CopyTo(iendChunk.Slice(4, 4));
        uint crc = Crc32.HashToUInt32(iendChunk.Slice(4, 4));
        BinaryPrimitives.WriteUInt32BigEndian(iendChunk.Slice(8, 4), crc);

        stream.Write(iendChunk);
    }
}
