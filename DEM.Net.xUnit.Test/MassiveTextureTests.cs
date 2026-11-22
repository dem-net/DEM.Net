using System;
using System.IO;
using DEM.Net.Core;
using DEM.Net.Core.Configuration;
using DEM.Net.Core.Imagery;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace DEM.Net.xUnit.Test;

/// <summary>
/// Checks that the streamed texture generator (MassiveImageGenerator option) produces
/// exactly the same pixels as the in-memory ImageSharp path.
/// </summary>
public class MassiveTextureTests
{
    private const int Zoom = 10;
    private const int StartX = 527, StartY = 374, Cols = 3, Rows = 2;

    [Theory]
    // tileSize, crop in tile units (left, top, right, bottom), index of a missing tile (-1 = none)
    [InlineData(256, 0.37, 0.21, 2.60, 1.80, -1)]
    [InlineData(512, 0.37, 0.21, 2.60, 1.80, -1)]
    [InlineData(256, 0.05, 0.95, 2.97, 1.99, 4)]
    [InlineData(512, 1.10, 0.40, 1.90, 0.90, -1)] // crop inside a single tile
    public void StreamedTextureMatchesInMemoryTexture(int tileSize, double left, double top, double right, double bottom, int missingTile)
    {
        var tiles = CreateTiles(tileSize, missingTile);
        var bbox = CreateBbox(tileSize, left, top, right, bottom);

        string dir = Path.Combine(Path.GetTempPath(), "demnet-massive-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            string inMemoryFile = Path.Combine(dir, "inmemory.png");
            string streamedFile = Path.Combine(dir, "streamed.png");

            var inMemoryInfo = CreateService(massive: false).ConstructTexture(tiles, bbox, inMemoryFile, TextureImageFormat.image_png);
            var streamedInfo = CreateService(massive: true).ConstructTexture(tiles, bbox, streamedFile, TextureImageFormat.image_png);

            Assert.Equal(inMemoryInfo.Width, streamedInfo.Width);
            Assert.Equal(inMemoryInfo.Height, streamedInfo.Height);

            using var expected = Image.Load<Rgba32>(inMemoryFile);
            using var actual = Image.Load<Rgba32>(streamedFile);
            Assert.Equal(expected.Width, actual.Width);
            Assert.Equal(expected.Height, actual.Height);

            var expectedPixels = new Rgba32[expected.Width * expected.Height];
            var actualPixels = new Rgba32[actual.Width * actual.Height];
            expected.CopyPixelDataTo(expectedPixels);
            actual.CopyPixelDataTo(actualPixels);
            Assert.True(expectedPixels.AsSpan().SequenceEqual(actualPixels), "Streamed texture pixels differ from in-memory texture.");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Theory]
    // Tile range declares 512px tiles but the provider returned other sizes
    // (1024px: Mapbox styles "512 @2x" tiles, which used to be drawn over their neighbours)
    [InlineData(true, 256)]
    [InlineData(false, 256)]
    [InlineData(true, 1024)]
    [InlineData(false, 1024)]
    public void TilesWithUnexpectedSizeAreResized(bool massive, int imageSize)
    {
        // Tile 0 is red, tile 1 is blue: each must stay in its own half of the texture.
        // Red is added (so drawn) last: unresized, it would cover the blue tile.
        var tiles = new TileRange(new MapTileInfo(StartX, StartY, Zoom, 512), new MapTileInfo(StartX + 1, StartY, Zoom, 512), 512);
        tiles.Add(new MapTile(CreateSolidTileImage(imageSize, new Rgba32(0, 0, 255, 255)), 512, null, new MapTileInfo(StartX + 1, StartY, Zoom, 512)));
        tiles.Add(new MapTile(CreateSolidTileImage(imageSize, new Rgba32(255, 0, 0, 255)), 512, null, new MapTileInfo(StartX, StartY, Zoom, 512)));
        var bbox = CreateBbox(512, 0.0, 0.1, 2.0, 0.9);

        string file = Path.Combine(Path.GetTempPath(), "demnet-massive-" + Guid.NewGuid().ToString("N") + ".png");
        try
        {
            var info = CreateService(massive).ConstructTexture(tiles, bbox, file, TextureImageFormat.image_png);
            using var image = Image.Load<Rgba32>(file);
            Assert.Equal(info.Width, image.Width);
            Assert.Equal(info.Height, image.Height);
            int y = image.Height / 2;
            Assert.Equal(new Rgba32(255, 0, 0, 255), image[image.Width / 4, y]);
            Assert.Equal(new Rgba32(0, 0, 255, 255), image[image.Width * 3 / 4, y]);
        }
        finally
        {
            File.Delete(file);
        }
    }

    private static byte[] CreateSolidTileImage(int size, Rgba32 color)
    {
        using var img = new Image<Rgba32>(size, size, color);
        using var ms = new MemoryStream();
        img.SaveAsPng(ms);
        return ms.ToArray();
    }

    private static ImageryService CreateService(bool massive)
    {
        var options = Options.Create(new DEMNetOptions { MassiveImageGenerator = massive, PowerOfTwoImages = false });
        return new ImageryService(null!, Options.Create(new AppSecrets()), options, null!, NullLogger<ImageryService>.Instance);
    }

    private static TileRange CreateTiles(int tileSize, int missingTile)
    {
        var range = new TileRange(new MapTileInfo(StartX, StartY, Zoom, tileSize),
            new MapTileInfo(StartX + Cols - 1, StartY + Rows - 1, Zoom, tileSize), tileSize);
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
            {
                int index = r * Cols + c;
                byte[] bytes = index == missingTile ? [] : CreateTileImage(tileSize, c, r);
                range.Add(new MapTile(bytes, tileSize, new Uri($"http://test/{c}/{r}"), new MapTileInfo(StartX + c, StartY + r, Zoom, tileSize)));
            }
        return range;
    }

    private static byte[] CreateTileImage(int size, int col, int row)
    {
        using var img = new Image<Rgba32>(size, size);
        img.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var span = accessor.GetRowSpan(y);
                for (int x = 0; x < span.Length; x++)
                    span[x] = new Rgba32((byte)(x * 7 + col * 50), (byte)(y * 3 + row * 90), (byte)(x ^ y), 255);
            }
        });
        using var ms = new MemoryStream();
        img.SaveAsPng(ms);
        return ms.ToArray();
    }

    /// <summary>Builds a geographic bbox from a pixel rectangle expressed in tile units relative to the range start.</summary>
    private static BoundingBox CreateBbox(int tileSize, double left, double top, double right, double bottom)
    {
        var topLeft = TileUtils.GlobalPixelToPosition(new Point<double>((StartX + left) * tileSize, (StartY + top) * tileSize), Zoom, tileSize);
        var bottomRight = TileUtils.GlobalPixelToPosition(new Point<double>((StartX + right) * tileSize, (StartY + bottom) * tileSize), Zoom, tileSize);
        return new BoundingBox(topLeft.Long, bottomRight.Long, bottomRight.Lat, topLeft.Lat);
    }
}
