using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Hashing; // Requires 'System.IO.Hashing' NuGet package
using System.Text;

namespace DEM.Net.Core.Imagery;
public class IdatChunkStream : Stream
{
    private readonly Stream _baseStream;
    private readonly Crc32 _crc32 = new Crc32();
    private readonly byte[] _chunkType = Encoding.ASCII.GetBytes("IDAT");

    public IdatChunkStream(Stream baseStream)
    {
        _baseStream = baseStream;
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length == 0) return;

        // 1. Write Chunk Length (4 bytes, Big-Endian)
        Span<byte> lengthBytes = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(lengthBytes, buffer.Length);
        _baseStream.Write(lengthBytes);

        // 2. Write Chunk Type ("IDAT")
        _baseStream.Write(_chunkType);

        // 3. Write Compressed Data
        _baseStream.Write(buffer);

        // 4. Calculate and Write CRC32 (Calculated over Type + Data)
        _crc32.Reset();
        _crc32.Append(_chunkType);
        _crc32.Append(buffer);

        Span<byte> crcBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBytes, _crc32.GetCurrentHashAsUInt32());
        _baseStream.Write(crcBytes);
    }

    // Standard Stream overrides required by base class
    public override void Write(byte[] buffer, int offset, int count) => Write(new ReadOnlySpan<byte>(buffer, offset, count));
    public override void Flush() => _baseStream.Flush();
    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
}