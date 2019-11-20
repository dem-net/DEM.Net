using System;
using System.IO;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;

namespace DEM.Net.Core.Imagery
{
    public class TileDebugProvider : ImageryProvider, ITileGenerator
    {
        public TileDebugProvider(int maxDegreeOfParallelism = -1, int maxZoom = 6)
        {
            base.MaxDegreeOfParallelism = maxDegreeOfParallelism;
            base.MaxZoom = maxZoom;
        }
        public byte[] GenerateTile(int x, int y, int zoom)
        {
            byte[] tileBytes = null;
            var font = SixLabors.Fonts.SystemFonts.CreateFont("Arial", 12);

            var px = TileUtils.TileXYToPixelXY(x, y);
            var latLong = TileUtils.PixelXYToLatLong(px.X, px.Y, zoom);
            var latLongOffset = TileUtils.PixelXYToLatLong(px.X + TileSize, px.Y + TileSize, zoom);

            var testX = Math.Pow(10, -Math.Log10(latLongOffset.Long - latLong.Long)); 
            var testY = Math.Pow(10, -Math.Log10(latLong.Lat - latLongOffset.Lat));



            using (Image<Rgba32> outputImage = new Image<Rgba32>(this.TileSize, this.TileSize))
            {
                string tileText = $"{x}/{y}/{zoom}{Environment.NewLine}";
                outputImage.Mutate(o => o
                    .Fill(Rgba32.White)
                    .DrawText(tileText, font, Rgba32.Black, new PointF(10, 10))
                );

                using (MemoryStream ms = new MemoryStream())
                {
                    outputImage.SaveAsPng(ms);
                    tileBytes = ms.ToArray();
                }
            }

            return tileBytes;
        }

    }
}