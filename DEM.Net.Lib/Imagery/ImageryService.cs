using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Imagery
{
    public class ImageryService
    {
        private int _serverCycle = 0;

        public TileRange DownloadTiles(BoundingBox bbox, ImageryProvider provider)
        {
            TileRange tiles = new TileRange(provider);
            BoundingBox mapBbox = null;
            PointInt topLeft = new PointInt();
            PointInt bottomRight = new PointInt();
            int TILESPERIMAGE = 4;

            // optimal zoom calculation (maybe there's a direct way)
            // calculate the size of the full bbox at increasing zoom levels
            // until the full image would be greater than a tile
            int zoom = 0;
            do
            {
                zoom++;

                // coords are pixels in global map image (see TileUtils.MapSize(zoom))
                topLeft = TileUtils.LatLongToPixelXY(bbox.yMax, bbox.xMin, zoom);
                bottomRight = TileUtils.LatLongToPixelXY(bbox.yMin, bbox.xMax, zoom);
                mapBbox = new BoundingBox(topLeft.X, bottomRight.X, topLeft.Y, bottomRight.Y);
            }
            while (zoom < provider.MaxZoom
                    && (mapBbox.Width < provider.TileSize * TILESPERIMAGE || mapBbox.Height < provider.TileSize * TILESPERIMAGE));

            // now we have the minimum zoom without image
            // we can know which tiles are needed
            tiles.Start = new MapTileInfo(TileUtils.PixelXYToTileXY(topLeft.X, topLeft.Y), zoom);
            tiles.End = new MapTileInfo(TileUtils.PixelXYToTileXY(bottomRight.X, bottomRight.Y), zoom);
            tiles.AreaOfInterest = mapBbox;

            // downdload tiles
            Logger.StartPerf("downloadImages");
            Parallel.ForEach(tiles.EnumerateRange(), tileInfo =>
                {
                    using (WebClient webClient = new WebClient())
                    {
                        Uri tileUri = BuildUri(provider, tileInfo.X, tileInfo.Y, tileInfo.Zoom);
                        var imgBytes = webClient.DownloadData(tileUri);

                        System.Diagnostics.Debug.WriteLine($"Downloading {tileUri}");
                        tiles.Add(new MapTile(imgBytes, provider.TileSize, tileUri, tileInfo));
                        System.Diagnostics.Debug.WriteLine($"Downloading {tileUri} Finished");
                    }
                }
                );
            Logger.StopPerf("downloadImages");



            return tiles;
        }

        BoundingBox ConvertWorldToMap(BoundingBox bbox, int zoomLevel)
        {
            var bboxTopLeft = TileUtils.LatLongToPixelXY(bbox.yMax, bbox.xMin, zoomLevel);
            var bboxBottomRight = TileUtils.LatLongToPixelXY(bbox.yMin, bbox.xMax, zoomLevel);
            return new BoundingBox(bboxTopLeft.X, bboxBottomRight.X, bboxTopLeft.Y, bboxBottomRight.Y);
        }
        BoundingBox GetTilesBoundingBox(TileRange tiles)
        {
            var bboxTopLeft = TileUtils.TileXYToPixelXY(tiles.Min(tile => tile.TileInfo.X), tiles.Min(tile => tile.TileInfo.Y));
            var bboxBottomRight = TileUtils.TileXYToPixelXY(tiles.Max(tile => tile.TileInfo.X) + 1, tiles.Max(tile => tile.TileInfo.Y) + 1);

            return new BoundingBox(bboxTopLeft.X, bboxBottomRight.X, bboxTopLeft.Y, bboxBottomRight.Y);
        }

        public TextureInfo ConstructTexture(TileRange tiles, BoundingBox bbox, string fileName)
        {
            // where is the bbox in the final image ?

            // get pixel in full map
            var localBbox = ConvertWorldToMap(bbox, tiles.First().TileInfo.Zoom);
            var tilesBbox = GetTilesBoundingBox(tiles);

            int tileSize = tiles.Provider.TileSize;
            using (Bitmap bmp = new Bitmap((int)localBbox.Width, (int)localBbox.Height))
            {
                int xOffset = (int)(tilesBbox.xMin - localBbox.xMin);
                int yOffset = (int)(tilesBbox.yMin - localBbox.yMin);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    foreach (var tile in tiles)
                    {
                        using (MemoryStream stream = new MemoryStream(tile.Image))
                        {
                            using (Image tileImg = Image.FromStream(stream))
                            {
                                int x = (tile.TileInfo.X - tiles.Start.X) * tileSize + xOffset;
                                int y = (tile.TileInfo.Y - tiles.Start.Y) * tileSize + yOffset;
                                g.DrawImage(tileImg, x, y);
                            }
                        }
                    }
                }
                //bmp.Save("debug_" + fileName, ImageFormat.Png);

                // power of two texture
                int maxSize = Math.Max((int)tilesBbox.Width, (int)tilesBbox.Height);
                using (Bitmap bmpOut = new Bitmap(maxSize, maxSize))
                {
                    using (Graphics g = Graphics.FromImage(bmpOut))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.DrawImage(bmp, 0, 0, bmpOut.Width, bmpOut.Height);
                    }
                    bmpOut.Save(fileName, ImageFormat.Png);
                }
            }
            //return new TextureInfo(fileName, ImageFormat.Png, (int)localBbox.Width, (int)localBbox.Height);
            return new TextureInfo(fileName, ImageFormat.Png, (int)tilesBbox.Width, (int)tilesBbox.Height);


        }


        private Uri BuildUri(ImageryProvider provider, int x, int y, int zoom)
        {
            string[] serverNodes = provider.UrlModel.Servers;
            string server = string.Empty;
            if (serverNodes != null && serverNodes.Length > 0)
            {
                _serverCycle = (_serverCycle + 1) % serverNodes.Length;
                server = serverNodes[_serverCycle];
            }

            string url = provider.UrlModel.UrlFormat;
            url = url.Replace("{s}", server);
            url = url.Replace("{x}", x.ToString());
            url = url.Replace("{y}", y.ToString());
            url = url.Replace("{z}", zoom.ToString());

            return new Uri(url, UriKind.Absolute);
        }
    }
}
