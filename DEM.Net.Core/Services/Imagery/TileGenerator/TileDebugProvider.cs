using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace DEM.Net.Core.Imagery
{
    public class TileDebugProvider : ImageryProvider, ITileGenerator
    {
        private readonly Graticules graticuleService;
        public TileDebugProvider(GeoPoint debugPoint = null, int maxDegreeOfParallelism = -1, int maxZoom = 23)
        {
            base.Name = nameof(TileDebugProvider);
            base.MaxDegreeOfParallelism = maxDegreeOfParallelism;
            base.MaxZoom = maxZoom;
            graticuleService = new Graticules();
            this.DebugPoint = debugPoint;
        }

        public GeoPoint DebugPoint { get; set; }

        public byte[] GenerateTile(int x, int y, int zoom)
        {
            byte[] tileBytes = null;
            var font = SixLabors.Fonts.SystemFonts.CreateFont("Arial", 12);

            var corner = TileUtils.TileXYToGlobalPixel(x, y, TileSize);
            var latLong = TileUtils.GlobalPixelToPosition(new Point<double>(corner.X, corner.Y), zoom, TileSize);
            var latLongOffset = TileUtils.GlobalPixelToPosition(new Point<double>(corner.X + TileSize, corner.Y + TileSize), zoom, TileSize);



            var graticules = graticuleService.DrawCore(latLong, latLongOffset);


            using (Image<Rgba32> outputImage = new Image<Rgba32>(this.TileSize, this.TileSize))
            {
                string tileText = $"{x}/{y}/{zoom}{Environment.NewLine}";
                outputImage.Mutate(o => o
                    .Fill(Color.White)
                    .DrawText(tileText, font, Color.Black, new PointF(10, 10))
                );
                outputImage.Mutate(o => DrawGraticules(o, graticules, corner, zoom));



                // Test
                if (DebugPoint != null)
                {
                    var testPixel = TileUtils.PositionToGlobalPixel(new LatLong(DebugPoint.Latitude, DebugPoint.Longitude), zoom, TileSize);
                    var testTile = TileUtils.GlobalPixelToTileXY(testPixel.X, testPixel.Y, TileSize);

                    // Draw test pixel
                    if (testTile.X == x && testTile.Y == y)
                    {
                        var basex = TileUtils.TileXYToGlobalPixel(x, y, TileSize);
                        var ptLoc = new PointF((float)(testPixel.X - basex.X), (float)(testPixel.Y - basex.Y));
                        outputImage.Mutate(o =>
                            o.DrawLine(Color.Blue, 1f,
                                new PointF[] { new PointF(ptLoc.X - 10, ptLoc.Y - 10), new PointF(ptLoc.X + 10, ptLoc.Y + 10) })
                                .DrawLine(Color.Blue, 1f,
                                    new PointF[] { new PointF(ptLoc.X - 10, ptLoc.Y + 10), new PointF(ptLoc.X + 10, ptLoc.Y - 10) }));

                    }
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    outputImage.SaveAsPng(ms);
                    tileBytes = ms.ToArray();
                }
            }

            return tileBytes;
        }

        private void DrawGraticules(IImageProcessingContext img, GraticuleLabels graticules, Point<double> corner, int zoom)
        {
            var font = SixLabors.Fonts.SystemFonts.CreateFont("Arial", 8);
            foreach (var meridian in graticules.VerticalLabels)
            {
                var loc = meridian.worldLocation;
                var pt = TileUtils.PositionToGlobalPixel(new LatLong(loc.Lat, loc.Long), zoom, TileSize);
                var xpos = pt.X - corner.X;
                var start = new PointF((float)xpos, 0);
                var end = new PointF((float)xpos, TileSize);
                img.DrawLine(Color.Gray, 1f, new PointF[] { start, end });
                try
                {
                    if (xpos < TileSize - 10)
                    {
                        img.DrawText(Math.Round(loc.Long, 2).ToString(), font, Color.Black, new PointF((float)xpos, 50));
                    }
                }
                catch (Exception)
                {

                }


            }
            foreach (var parallel in graticules.HorizontalLabels)
            {
                var loc = parallel.worldLocation;
                var pt = TileUtils.PositionToGlobalPixel(new LatLong(loc.Lat, loc.Long), zoom, TileSize);
                var ypos = pt.Y - corner.Y;
                var start = new PointF(0, (float)ypos);
                var end = new PointF(TileSize, (float)ypos);
                img.DrawLine(Color.Gray, 1f, new PointF[] { start, end });
                try
                {
                    img.DrawText(Math.Round(loc.Lat, 4).ToString(), font, Color.Black, new PointF(50, (float)ypos));
                }
                catch (Exception)
                {

                }

            }



        }
    }
}