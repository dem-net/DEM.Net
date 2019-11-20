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
        public byte[] GenerateTile(int x, int y, int zoom)
        {
            byte[] tileBytes = null;
            var font = SixLabors.Fonts.SystemFonts.CreateFont("Arial", 8);
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