using System;
using System.Buffers;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DEM.Net.glTF.Export;

/// <summary>
/// Text writer for the exporters: lines are formatted (invariant culture) into a pooled buffer, written asynchronously
/// to the stream when it is full. No string per line, one await per buffer.
/// </summary>
/// <remarks>
/// Call <see cref="ReserveAsync"/> before formatting with <see cref="AppendLine"/>: it makes room for
/// <see cref="MaxFormattedLength"/> characters. Text of any length goes through <see cref="WriteLineAsync"/>.
/// </remarks>
internal sealed class TextChunkWriter : IAsyncDisposable
{
    /// <summary>Room made by <see cref="ReserveAsync"/>: the formatted lines between two calls must fit</summary>
    public const int MaxFormattedLength = 1024;

    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);
    private readonly StreamWriter _writer;
    private readonly char[] _buffer;
    private int _length;

    public TextChunkWriter(Stream stream)
    {
        _writer = new StreamWriter(stream, Utf8NoBom, bufferSize: 1 << 16, leaveOpen: true);
        _buffer = ArrayPool<char>.Shared.Rent(1 << 15);
    }

    public ValueTask ReserveAsync(CancellationToken cancellationToken)
        => _buffer.Length - _length >= MaxFormattedLength ? ValueTask.CompletedTask : FlushBufferAsync(cancellationToken);

    /// <summary>Formats a line (invariant culture) in the room made by <see cref="ReserveAsync"/></summary>
    public void AppendLine([InterpolatedStringHandlerArgument("")] ref LineHandler line)
    {
        if (!MemoryExtensions.TryWrite(_buffer.AsSpan(_length), CultureInfo.InvariantCulture, ref line.Inner, out int written)
            || !Environment.NewLine.AsSpan().TryCopyTo(_buffer.AsSpan(_length + written)))
        {
            throw new InvalidOperationException($"Line longer than the reserved {MaxFormattedLength} characters");
        }
        _length += written + Environment.NewLine.Length;
    }

    public async ValueTask WriteLineAsync(string text, CancellationToken cancellationToken)
    {
        await FlushBufferAsync(cancellationToken).ConfigureAwait(false);
        await _writer.WriteAsync(text.AsMemory(), cancellationToken).ConfigureAwait(false);
        await _writer.WriteAsync(Environment.NewLine.AsMemory(), cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask FlushAsync(CancellationToken cancellationToken)
    {
        await FlushBufferAsync(cancellationToken).ConfigureAwait(false);
        await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask FlushBufferAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_length > 0)
        {
            await _writer.WriteAsync(_buffer.AsMemory(0, _length), cancellationToken).ConfigureAwait(false);
            _length = 0;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _writer.DisposeAsync().ConfigureAwait(false); // flushes what the stream writer holds, not the pending lines
        ArrayPool<char>.Shared.Return(_buffer);
    }

    /// <summary>Formats into the free part of the writer's buffer</summary>
    [InterpolatedStringHandler]
    public ref struct LineHandler
    {
        internal MemoryExtensions.TryWriteInterpolatedStringHandler Inner;

        public LineHandler(int literalLength, int formattedCount, TextChunkWriter writer, out bool shouldAppend)
            => Inner = new MemoryExtensions.TryWriteInterpolatedStringHandler(literalLength, formattedCount,
                writer._buffer.AsSpan(writer._length), CultureInfo.InvariantCulture, out shouldAppend);

        public bool AppendLiteral(string value) => Inner.AppendLiteral(value);
        public bool AppendFormatted<T>(T value) => Inner.AppendFormatted(value);
        public bool AppendFormatted<T>(T value, string? format) => Inner.AppendFormatted(value, format);
    }
}
