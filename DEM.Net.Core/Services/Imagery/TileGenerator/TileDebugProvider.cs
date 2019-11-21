using System;
using System.IO;
using System.Linq;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;

namespace DEM.Net.Core.Imagery
{
    public class TileDebugProvider : ImageryProvider, ITileGenerator
    {
        private readonly Graticules graticuleService;
        public TileDebugProvider(int maxDegreeOfParallelism = -1, int maxZoom = 23)
        {
            base.MaxDegreeOfParallelism = maxDegreeOfParallelism;
            base.MaxZoom = maxZoom;
            graticuleService = new Graticules();
        }
        public byte[] GenerateTile(int x, int y, int zoom)
        {
            byte[] tileBytes = null;
            var font = SixLabors.Fonts.SystemFonts.CreateFont("Arial", 12);

            var corner = TileUtils.TileXYToPixelXY(x, y);
            var latLong = TileUtils.PixelXYToLatLong(corner.X, corner.Y, zoom);
            var latLongOffset = TileUtils.PixelXYToLatLong(corner.X + TileSize, corner.Y + TileSize, zoom);

            var graticules = graticuleService.DrawCore(latLong, latLongOffset);
            
            
            using (Image<Rgba32> outputImage = new Image<Rgba32>(this.TileSize, this.TileSize))
            {
                string tileText = $"{x}/{y}/{zoom}{Environment.NewLine}";
                outputImage.Mutate(o => o
                    .Fill(Rgba32.White)
                    .DrawText(tileText, font, Rgba32.Black, new PointF(10, 10))
                );
                outputImage.Mutate( o => DrawGraticules(o, graticules, corner, zoom));

                

                using (MemoryStream ms = new MemoryStream())
                {
                    outputImage.SaveAsPng(ms);
                    tileBytes = ms.ToArray();
                }
            }

            return tileBytes;
        }

        private void DrawGraticules(IImageProcessingContext<Rgba32> img, GraticuleLabels graticules, PointInt corner, int zoom)
        {
            foreach (var meridian in graticules.VerticalLabels)
            {
                var loc = meridian.worldLocation;
                var pt = TileUtils.LatLongToPixelXY(loc.Lat, loc.Long, zoom);
                var xpos = pt.X - corner.X;
                var start = new PointF(xpos,0);
                var end = new PointF(xpos,TileSize);
                img.DrawLines(Rgba32.Gray, 1f, new PointF[] {start, end});
            }
            foreach (var parallel in graticules.HorizontalLabels)
            {
                var loc = parallel.worldLocation;
                var pt = TileUtils.LatLongToPixelXY(loc.Lat, loc.Long, zoom);
                var ypos = pt.Y - corner.Y;
                var start = new PointF(0,ypos);
                var end = new PointF(TileSize,ypos);
                img.DrawLines(Rgba32.Gray, 1f, new PointF[] {start, end});
            }
        }
    }
}