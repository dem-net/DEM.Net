using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Hashing;
using System.Text;

namespace DEM.Net.Core.Imagery;

public static class PngWriter
{
    private static readonly byte[] PngSignature = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };

    public static void WriteHeader(Stream stream, int width, int height)
    {
        // Write the PNG Magic Number
        stream.Write(PngSignature);

        // Write the IHDR Chunk
        // Length of IHDR data is always 13 bytes
        Span<byte> ihdrChunk = stackalloc byte[4 + 4 + 13 + 4]; // Length + Type + Data + CRC

        // Length (13)
        BinaryPrimitives.WriteInt32BigEndian(ihdrChunk.Slice(0, 4), 13);

        // Type ("IHDR")
        Encoding.ASCII.GetBytes("IHDR", ihdrChunk.Slice(4, 4));

        // Data (Width, Height, BitDepth, ColorType, Compression, Filter, Interlace)
        BinaryPrimitives.WriteInt32BigEndian(ihdrChunk.Slice(8, 4), width);
        BinaryPrimitives.WriteInt32BigEndian(ihdrChunk.Slice(12, 4), height);
        ihdrChunk[16] = 8; // Bit depth: 8 bits per channel
        ihdrChunk[17] = 6; // Color type: 6 = Truecolor with Alpha (RGBA)
        ihdrChunk[18] = 0; // Compression method: 0 = DEFLATE
        ihdrChunk[19] = 0; // Filter method: 0 = Standard
        ihdrChunk[20] = 0; // Interlace method: 0 = No interlace

        // CRC32 (Over Type + Data)
        uint crc = Crc32.HashToUInt32(ihdrChunk.Slice(4, 17));
        BinaryPrimitives.WriteUInt32BigEndian(ihdrChunk.Slice(21, 4), crc);

        stream.Write(ihdrChunk);
    }

    public static void WriteEndChunk(Stream stream)
    {
        // IEND chunk: Length (0), Type ("IEND"), CRC32 (Calculated over "IEND")
        Span<byte> iendChunk = stackalloc byte[12];

        BinaryPrimitives.WriteInt32BigEndian(iendChunk.Slice(0, 4), 0); // Length 0
        Encoding.ASCII.GetBytes("IEND", iendChunk.Slice(4, 4));

        uint crc = Crc32.HashToUInt32(iendChunk.Slice(4, 4));
        BinaryPrimitives.WriteUInt32BigEndian(iendChunk.Slice(8, 4), crc);

        stream.Write(iendChunk);
    }
}