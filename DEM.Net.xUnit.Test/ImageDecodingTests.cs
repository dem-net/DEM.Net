using DEM.Net.Core;
using DEM.Net.Core.Configuration;
using DEM.Net.Core.Imagery;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using Xunit;

namespace DEM.Net.xUnit.Test
{
    /// <summary>
    /// Checks the temporary ImageSharp 3.1.x mitigation (SafeImageDecoding):
    /// PNG and JPEG tiles are decoded, TIFF tiles are rejected (rendered transparent).
    /// </summary>
    public class ImageDecodingTests
    {
        private const int Zoom = 10, StartX = 527, StartY = 374, TileSize = 256;

        [Fact]
        public void TiffTilesAreNotDecoded()
        {
            var tiles = new TileRange(new MapTileInfo(StartX, StartY, Zoom, TileSize), new MapTileInfo(StartX + 2, StartY, Zoom, TileSize), TileSize);
            tiles.Add(Tile(0)); // PNG
            tiles.Add(Tile(1)); // JPEG
            tiles.Add(Tile(2)); // TIFF

            // bbox covering the middle of the 3 tiles
            var topLeft = TileUtils.GlobalPixelToPosition(new Point<double>((StartX + 0.25) * TileSize, (StartY + 0.25) * TileSize), Zoom, TileSize);
            var bottomRight = TileUtils.GlobalPixelToPosition(new Point<double>((StartX + 2.75) * TileSize, (StartY + 0.75) * TileSize), Zoom, TileSize);
            var bbox = new BoundingBox(topLeft.Long, bottomRight.Long, bottomRight.Lat, topLeft.Lat);

            var service = new ImageryService(null, Options.Create(new AppSecrets()), Options.Create(new DEMNetOptions()), null, NullLogger<ImageryService>.Instance);
            string file = Path.Combine(Path.GetTempPath(), "demnet-decoding-" + Guid.NewGuid().ToString("N") + ".png");
            try
            {
                service.ConstructTexture(tiles, bbox, file, TextureImageFormat.image_png);
                using var image = Image.Load<Rgba32>(file);

                int y = image.Height / 2;
                int Center(int col) => (int)((col + 0.5 - 0.25) * TileSize);
                Assert.Equal(255, image[Center(0), y].A); // PNG decoded
                Assert.Equal(255, image[Center(1), y].A); // JPEG decoded
                Assert.Equal(0, image[Center(2), y].A);   // TIFF rejected
            }
            finally
            {
                File.Delete(file);
            }
        }

        private static MapTile Tile(int col)
        {
            using var img = new Image<Rgba32>(TileSize, TileSize, new Rgba32(200, 100, 50, 255));
            using var ms = new MemoryStream();
            switch (col)
            {
                case 0: img.SaveAsPng(ms); break;
                case 1: img.SaveAsJpeg(ms); break;
                default: img.SaveAsTiff(ms); break;
            }
            return new MapTile(ms.ToArray(), TileSize, new Uri($"http://test/{col}"), new MapTileInfo(StartX + col, StartY, Zoom, TileSize));
        }
    }
}
