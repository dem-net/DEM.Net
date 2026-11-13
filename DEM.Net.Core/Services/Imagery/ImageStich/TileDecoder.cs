using System;
using System.Buffers;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace DEM.Net.Core.Imagery;

/// <summary>
/// Decodes encoded tile images (PNG/JPEG/...) into raw RGBA32 buffers rented from <see cref="ArrayPool{T}"/>.
/// Missing or undecodable tiles are returned as fully transparent buffers.
/// Tiles whose size differs from the expected tile size are resized.
/// </summary>
internal sealed class TileDecoder
{
    internal const int BytesPerPixel = 4; // RGBA32

    private readonly int _tileSize;
    private readonly int _tileByteSize;
    private readonly ILogger? _logger;

    public TileDecoder(int tileSize, ILogger? logger = null)
    {
        if (tileSize <= 0) throw new ArgumentOutOfRangeException(nameof(tileSize));
        _tileSize = tileSize;
        _tileByteSize = tileSize * tileSize * BytesPerPixel;
        _logger = logger;
    }

    /// <summary>
    /// Decodes a single tile into a rented buffer of tileSize * tileSize * 4 bytes.
    /// Caller must give it back with <see cref="Return(byte[])"/>.
    /// </summary>
    public byte[] Decode(MapTile? tile)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(_tileByteSize);
        Span<byte> pixels = buffer.AsSpan(0, _tileByteSize);

        if (tile?.Image == null || tile.Image.Length == 0)
        {
            if (tile != null) _logger?.LogWarning("Tile {Uri} is empty. Rendering as transparent.", tile.Uri);
            pixels.Clear();
            return buffer;
        }

        try
        {
            using var tileImage = Image.Load<Rgba32>(SafeImageDecoding.Options, tile.Image);
            if (tileImage.Width != _tileSize || tileImage.Height != _tileSize)
            {
                tileImage.Mutate(o => o.Resize(_tileSize, _tileSize));
            }
            tileImage.CopyPixelDataTo(pixels);
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error while decoding tile {Uri}: {Message}. Rendering as transparent.", tile.Uri, ex.Message);
            pixels.Clear();
        }
        return buffer;
    }

    public void Return(byte[]? buffer)
    {
        if (buffer != null) ArrayPool<byte>.Shared.Return(buffer);
    }
}
