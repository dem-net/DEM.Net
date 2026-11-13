using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Hashing;

namespace DEM.Net.Core.Imagery;

/// <summary>
/// Write-only stream that wraps every buffer written to it in a PNG IDAT chunk
/// (length + "IDAT" + data + CRC32). Intended to sit below a <see cref="System.IO.Compression.ZLibStream"/>.
/// Does not dispose the underlying stream.
/// </summary>
internal sealed class IdatChunkStream : Stream
{
    private static readonly byte[] ChunkType = "IDAT"u8.ToArray();

    private readonly Stream _baseStream;
    private readonly Crc32 _crc32 = new Crc32();

    public IdatChunkStream(Stream baseStream)
    {
        _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length == 0) return;

        Span<byte> lengthBytes = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(lengthBytes, buffer.Length);
        _baseStream.Write(lengthBytes);
        _baseStream.Write(ChunkType);
        _baseStream.Write(buffer);

        // CRC is computed over chunk type + data
        _crc32.Reset();
        _crc32.Append(ChunkType);
        _crc32.Append(buffer);

        Span<byte> crcBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBytes, _crc32.GetCurrentHashAsUInt32());
        _baseStream.Write(crcBytes);
    }

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
