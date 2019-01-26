using DEM.Net.Lib.Services.Mesh;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Imagery
{
    public class ImageryService
    {
        #region Tiled imagery

        private int _serverCycle = 0;

        public TileRange DownloadTiles(BoundingBox bbox, ImageryProvider provider, int minTilesPerImage = 4)
        {
            TileRange tiles = new TileRange(provider);
            BoundingBox mapBbox = null;
            PointInt topLeft = new PointInt();
            PointInt bottomRight = new PointInt();

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
                    && (mapBbox.Width < provider.TileSize * minTilesPerImage && mapBbox.Height < provider.TileSize * minTilesPerImage));

            // now we have the minimum zoom without image
            // we can know which tiles are needed
            tiles.Start = new MapTileInfo(TileUtils.PixelXYToTileXY(topLeft.X, topLeft.Y), zoom);
            tiles.End = new MapTileInfo(TileUtils.PixelXYToTileXY(bottomRight.X, bottomRight.Y), zoom);
            tiles.AreaOfInterest = mapBbox;

            // downdload tiles
            Logger.StartPerf("downloadImages");

            // 2 max download threads
            var options = new ParallelOptions() { MaxDegreeOfParallelism = provider.MaxDegreeOfParallelism };
            var range = tiles.EnumerateRange().ToList();
            Console.WriteLine($"Downloading {range.Count} tiles...");
            Parallel.ForEach(range, options, tileInfo =>
                {
                    using (WebClient webClient = new WebClient())
                    {
                        Uri tileUri = BuildUri(provider, tileInfo.X, tileInfo.Y, tileInfo.Zoom);
                        var imgBytes = webClient.DownloadData(tileUri);

                        Console.WriteLine($"Downloading {tileUri}");
                        tiles.Add(new MapTile(imgBytes, provider.TileSize, tileUri, tileInfo));
                        //System.Diagnostics.Debug.WriteLine($"Downloading {tileUri} Finished");
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

        public TextureInfo ConstructTexture(TileRange tiles, BoundingBox bbox, string fileName, TextureImageFormat mimeType)
        {
            ImageFormat format = ConvertFormat(mimeType);

            // where is the bbox in the final image ?

            // get pixel in full map
            var localBbox = ConvertWorldToMap(bbox, tiles.First().TileInfo.Zoom);
            var tilesBbox = GetTilesBoundingBox(tiles);

            //DrawDebugBmpBbox(tiles, localBbox, tilesBbox, fileName, mimeType);
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
                //bmp.Save(Path.ChangeExtension( fileName,".debug.jpg"), format);
                bmp.Save(fileName, format);
                //// power of two texture
                //int maxSize = Math.Max((int)tilesBbox.Width, (int)tilesBbox.Height);
                //using (Bitmap bmpOut = new Bitmap(maxSize, maxSize))
                //{
                //    using (Graphics g = Graphics.FromImage(bmpOut))
                //    {
                //        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                //        g.DrawImage(bmp, 0, 0, maxSize, maxSize);
                //    }
                //    bmpOut.Save(fileName, format);
                //}
            }
            return new TextureInfo(fileName, mimeType, (int)localBbox.Width, (int)localBbox.Height);
            //return new TextureInfo(fileName, format, (int)tilesBbox.Width, (int)tilesBbox.Height);


        }

        private void DrawDebugBmpBbox(TileRange tiles, BoundingBox localBbox, BoundingBox tilesBbox, string fileName, TextureImageFormat mimeType)
        {
            int tileSize = tiles.Provider.TileSize;
            using (Bitmap bmp = new Bitmap((int)tilesBbox.Width, (int)tilesBbox.Height))
            {
                int xOffset =  (int)(localBbox.xMin - tilesBbox.xMin);
                int yOffset =  (int)(localBbox.yMin - tilesBbox.yMin);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    foreach (var tile in tiles)
                    {
                        using (MemoryStream stream = new MemoryStream(tile.Image))
                        {
                            using (Image tileImg = Image.FromStream(stream))
                            {
                                int x = (tile.TileInfo.X - tiles.Start.X) * tileSize;
                                int y = (tile.TileInfo.Y - tiles.Start.Y) * tileSize;
                                g.DrawImage(tileImg, x, y);
                            }
                        }
                    }

                    g.DrawRectangle(Pens.Red, xOffset, yOffset, (int)localBbox.Width, (int)localBbox.Height);
                }
                //bmp.Save(Path.ChangeExtension( fileName,".debug.jpg"), format);
                var ext = Path.GetExtension(fileName);
                bmp.Save(Path.ChangeExtension(fileName,".debug"+ ext), ConvertFormat(mimeType));
            }
           
        }

        private ImageFormat ConvertFormat(TextureImageFormat format)
        {
            if (format == TextureImageFormat.image_jpeg)
                return ImageFormat.Jpeg;
            else
                return ImageFormat.Png;
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
            url = url.Replace("{t}", ConfigurationManager.AppSettings[provider.TokenAppSettingsKey]);

            return new Uri(url, UriKind.Absolute);
        }

        #endregion

        #region Normal map generation

        /// <summary>
        /// Generate normal texture from height map.
        /// Note : heightMap should be in projected coordinates (see ReprojectToCartesian())
        /// </summary>
        /// <param name="heightMap">heightMap in projected coordinates</param>
        /// <param name="outputDirectory"></param>
        /// <returns></returns>
        public TextureInfo GenerateNormalMap(HeightMap heightMap, string outputDirectory)
        {
            List<Vector3> normals = MeshService.ComputeNormals(heightMap).ToList();

            bool debugBMP = false;
            if (debugBMP)
            {
                using (var dbm = new DirectBitmap(heightMap.Width, heightMap.Height))
                using (var dbmHeight = new DirectBitmap(heightMap.Width, heightMap.Height))
                {
                    // for debug only
                    List<Vector3> coordinates = heightMap.Coordinates.Select(c => new Vector3((float)c.Longitude, (float)c.Latitude, (float)c.Elevation)).ToList();
                    float maxHeight = (float)heightMap.Coordinates.Max(c => c.Elevation.GetValueOrDefault(0));

                    for (int j = 0; j < heightMap.Height; j++)
                        for (int i = 0; i < heightMap.Width; i++)
                        {
                            int index = i + (j * heightMap.Width);
                            Vector3 norm = normals[index];
                            Color color = FromVec3NormalToColor(norm);
                            dbm.SetPixel(i, j, color);
                            dbmHeight.SetPixel(i, j, FromVec3ToHeightColor(coordinates[index], maxHeight));
                        }

                    dbm.Bitmap.Save(Path.Combine(outputDirectory, "normalmap.jpg"), ImageFormat.Jpeg);
                    dbmHeight.Bitmap.Save(Path.Combine(outputDirectory, "heightmap.jpg"), ImageFormat.Jpeg);
                }
            }
            else
            {
                using (var dbm = new DirectBitmap(heightMap.Width, heightMap.Height))
                {

                    for (int j = 0; j < heightMap.Height; j++)
                        for (int i = 0; i < heightMap.Width; i++)
                        {
                            int index = i + (j * heightMap.Width);
                            Vector3 norm = normals[index];
                            Color color = FromVec3NormalToColor(norm);
                            dbm.SetPixel(i, j, color);
                        }

                    dbm.Bitmap.Save(Path.Combine(outputDirectory, "normalmap.jpg"), ImageFormat.Jpeg);
                }
            }

            TextureInfo normal = new TextureInfo(Path.Combine(outputDirectory, "normalmap.jpg"), TextureImageFormat.image_jpeg, heightMap.Width, heightMap.Height);
            return normal;
        }


        private Color FromVec3ToHeightColor(Vector3 vector3, float maxHeight)
        {
            int height = (int)Math.Round(MathHelper.Map(0, maxHeight, 0, 255, vector3.Z, true), 0);
            return Color.FromArgb(height, height, height);
        }

        public Color FromVec3NormalToColor(Vector3 normal)
        {
            return Color.FromArgb((int)Math.Round(MathHelper.Map(-1, 1, 0, 255, normal.X, true), 0),
                (int)Math.Round(MathHelper.Map(-1, 1, 0, 255, normal.Y, true), 0),
                (int)Math.Round(MathHelper.Map(0, -1, 128, 255, -normal.Z, true), 0));
        }

        #endregion


    }
}
