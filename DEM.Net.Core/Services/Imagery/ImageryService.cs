﻿// ImageryService.cs
//
// Author:
//       Xavier Fischer 
//
// Copyright (c) 2019 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

#if NETFULL
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
#elif NETSTANDARD
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
#endif

namespace DEM.Net.Core.Imagery
{
    public class ImageryService : IImageryService
    {
        #region Tiled imagery

        private int _serverCycle = 0;
        private readonly ILogger<ImageryService> _logger;
        private readonly IMeshService _meshService;

#if NETSTANDARD
        private readonly IConfigurationRoot _config;

        public ImageryService(IMeshService meshService, ILogger<ImageryService> logger = null)
        {
            _logger = logger;
            _meshService = meshService;
            _config = new ConfigurationBuilder()
                 .SetBasePath(Directory.GetCurrentDirectory())
                 .AddJsonFile(Path.Combine("Config", "tokens.json"), optional: true, reloadOnChange: true)
                 .Build();
        }
#elif NETFULL
        public ImageryService(ILogger<ImageryService> logger)
        {
            _logger = logger;
        }
#endif

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
            Stopwatch swDownload = Stopwatch.StartNew();
            _logger?.LogTrace("Starting images download");


            // 2 max download threads
            var options = new ParallelOptions() { MaxDegreeOfParallelism = provider.MaxDegreeOfParallelism };
            var range = tiles.EnumerateRange().ToList();
            Console.WriteLine($"Downloading {range.Count} tiles...");
            Parallel.ForEach(range, options, tileInfo =>
                {
                    using (HttpClient client = new HttpClient())
                    {
                        Uri tileUri = BuildUri(provider, tileInfo.X, tileInfo.Y, tileInfo.Zoom);
                        _logger?.LogInformation($"Downloading {tileUri}");

                        var contentbytes = client.GetByteArrayAsync(tileUri).Result;
                        tiles.Add(new MapTile(contentbytes, provider.TileSize, tileUri, tileInfo));


                    }
                }
                );

            swDownload.Stop();
            _logger?.LogTrace($"DownloadImages done in : {swDownload.Elapsed:g}");


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

            // where is the bbox in the final image ?

            // get pixel in full map
            int zoomLevel = tiles.First().TileInfo.Zoom;
            var projectedBbox = ConvertWorldToMap(bbox, zoomLevel);
            var tilesBbox = GetTilesBoundingBox(tiles);

            //DrawDebugBmpBbox(tiles, localBbox, tilesBbox, fileName, mimeType);
            int tileSize = tiles.Provider.TileSize;

#if NETFULL
            ImageFormat format = mimeType == TextureImageFormat.image_jpeg ?
                 ImageFormat.Jpeg
                 : ImageFormat.Png;

            using (Bitmap bmp = new Bitmap((int)projectedBbox.Width, (int)projectedBbox.Height))
            {
                int xOffset = (int)(tilesBbox.xMin - projectedBbox.xMin);
                int yOffset = (int)(tilesBbox.yMin - projectedBbox.yMin);
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
                bmp.Save(fileName, format);
            }
#else
            using (Image<Rgba32> outputImage = new Image<Rgba32>((int)projectedBbox.Width, (int)projectedBbox.Height))
            {
                int xOffset = (int)(tilesBbox.xMin - projectedBbox.xMin);
                int yOffset = (int)(tilesBbox.yMin - projectedBbox.yMin);

                foreach (var tile in tiles)
                {
                    using (Image<Rgba32> tileImg = Image.Load(tile.Image))
                    {
                        int x = (tile.TileInfo.X - tiles.Start.X) * tileSize + xOffset;
                        int y = (tile.TileInfo.Y - tiles.Start.Y) * tileSize + yOffset;

                        outputImage.Mutate(o => o
                            .DrawImage(tileImg, new Point(x, y), 1f)
                            );
                    }
                }

                // with encoder
                //IImageEncoder encoder = ConvertFormat(mimeType);
                //outputImage.Save(fileName, encoder);

                outputImage.Save(fileName);
            }
#endif
            return new TextureInfo(fileName, mimeType, (int)projectedBbox.Width, (int)projectedBbox.Height, zoomLevel, projectedBbox);
            //return new TextureInfo(fileName, format, (int)tilesBbox.Width, (int)tilesBbox.Height);

        }

        public TextureInfo ConstructTextureWithGpxTrack(TileRange tiles, BoundingBox bbox, string fileName, TextureImageFormat mimeType, IEnumerable<GeoPoint> gpxPoints)
        {

            // where is the bbox in the final image ?

            // get pixel in full map
            int zoomLevel = tiles.First().TileInfo.Zoom;
            var projectedBbox = ConvertWorldToMap(bbox, zoomLevel);
            var tilesBbox = GetTilesBoundingBox(tiles);
            int xOffset = (int)(tilesBbox.xMin - projectedBbox.xMin);
            int yOffset = (int)(tilesBbox.yMin - projectedBbox.yMin);


            //DrawDebugBmpBbox(tiles, localBbox, tilesBbox, fileName, mimeType);
            int tileSize = tiles.Provider.TileSize;

            var pointsOnTexture = gpxPoints.Select(pt => TileUtils.LatLongToPixelXY(pt.Latitude, pt.Longitude, zoomLevel))
                     .Select(pt => new PointF(pt.X - (int)projectedBbox.xMin, pt.Y - (int)projectedBbox.yMin));

#if NETFULL
            ImageFormat format = mimeType == TextureImageFormat.image_jpeg ?
                 ImageFormat.Jpeg
                 : ImageFormat.Png;

            using (Bitmap bmp = new Bitmap((int)projectedBbox.Width, (int)projectedBbox.Height))
            {
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
                bmp.Save(fileName, format);
            }
            throw new NotImplementedException("GPX drawing not implemented yet in .Net Full");
#else
            using (Image<Rgba32> outputImage = new Image<Rgba32>((int)projectedBbox.Width, (int)projectedBbox.Height))
            {
                
                foreach (var tile in tiles)
                {
                    using (Image<Rgba32> tileImg = Image.Load(tile.Image))
                    {
                        int x = (tile.TileInfo.X - tiles.Start.X) * tileSize + xOffset;
                        int y = (tile.TileInfo.Y - tiles.Start.Y) * tileSize + yOffset;

                        outputImage.Mutate(o => o
                            .DrawImage(tileImg, new Point(x, y), 1f)
                            );
                    }
                }

                outputImage.Mutate(o => o
                             .DrawLines(new Rgba32(1,0,0,1f), 5f, pointsOnTexture.ToArray())
                             );
                // with encoder
                //IImageEncoder encoder = ConvertFormat(mimeType);
                //outputImage.Save(fileName, encoder);

                outputImage.Save(fileName);
            }
#endif
            return new TextureInfo(fileName, mimeType, (int)projectedBbox.Width, (int)projectedBbox.Height, zoomLevel, projectedBbox);
            //return new TextureInfo(fileName, format, (int)tilesBbox.Width, (int)tilesBbox.Height);

        }


#if NETSTANDARD
        private IImageEncoder ConvertFormat(TextureImageFormat format)
        {
            ImageFormatManager imageFormatManager = new ImageFormatManager();
            IImageFormat imageFormat = null;
            if (format == TextureImageFormat.image_jpeg)
                imageFormat = imageFormatManager.FindFormatByFileExtension(".jpg");
            else
                imageFormat = imageFormatManager.FindFormatByFileExtension(".png");

            return imageFormatManager.FindEncoder(imageFormat);
        }
#endif

        public Uri BuildUri(ImageryProvider provider, int x, int y, int zoom)
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
            if (url.Contains("{t}"))
            {
#if NETFULL
                var token = ConfigurationManager.AppSettings[provider.TokenAppSettingsKey];
                if (String.IsNullOrWhiteSpace(token))
                {
                    _logger?.LogError($"There is no token found for {provider.Name} provider. Make sure an App.SECRETS.config file is present in running directory with a {provider.TokenAppSettingsKey} key / value.");
                }
#else

                IConfigurationSection configurationSection = _config.GetSection("Tokens").GetSection(provider.TokenAppSettingsKey);
                var token = configurationSection.Value;
                if (String.IsNullOrWhiteSpace(token))
                {
                    _logger?.LogError($"There is no token found for {provider.Name} provider. Make sure a config/tokens.json file is present in running directory with a {provider.TokenAppSettingsKey} key / value.");
                }
#endif
                url = url.Replace("{t}", token);
            }

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
            List<Vector3> normals = _meshService.ComputeNormals(heightMap).ToList();

#if NETSTANDARD
            using (Image<Bgra32> outputImage = new Image<Bgra32>(heightMap.Width, heightMap.Height))
            {

                for (int j = 0; j < heightMap.Height; j++)
                    for (int i = 0; i < heightMap.Width; i++)
                    {
                        int index = i + (j * heightMap.Width);
                        Vector3 norm = normals[index];
                        Bgra32 color = FromVec3NormalToColor(norm);

                        outputImage[i, j] = color;
                    }

                outputImage.Save(Path.Combine(outputDirectory, "normalmap.jpg"), new JpegEncoder());
            }
#elif NETFULL
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
#endif


            TextureInfo normal = new TextureInfo(Path.Combine(outputDirectory, "normalmap.jpg"), TextureImageFormat.image_jpeg, heightMap.Width, heightMap.Height);
            return normal;
        }

#if NETSTANDARD
        private Bgra32 FromVec3ToHeightColor(Vector3 vector3, float maxHeight)
        {
            byte height = (byte)Math.Round(MathHelper.Map(0, maxHeight, 0, 255, vector3.Z, true), 0);
            return new Bgra32(height, height, height);
        }

        private Bgra32 FromVec3NormalToColor(Vector3 normal)
        {
            return new Bgra32((byte)Math.Round(MathHelper.Map(-1, 1, 0, 255, normal.X, true), 0),
                (byte)Math.Round(MathHelper.Map(-1, 1, 0, 255, normal.Y, true), 0),
                (byte)Math.Round(MathHelper.Map(0, -1, 128, 255, -normal.Z, true), 0));
        }
#elif NETFULL
        private Color FromVec3ToHeightColor(Vector3 vector3, float maxHeight)
        {
            int height = (int)Math.Round(MathHelper.Map(0, maxHeight, 0, 255, vector3.Z, true), 0);
            return Color.FromArgb(height, height, height);
        }

        private Color FromVec3NormalToColor(Vector3 normal)
        {
            return Color.FromArgb((int)Math.Round(MathHelper.Map(-1, 1, 0, 255, normal.X, true), 0),
                (int)Math.Round(MathHelper.Map(-1, 1, 0, 255, normal.Y, true), 0),
                (int)Math.Round(MathHelper.Map(0, -1, 128, 255, -normal.Z, true), 0));
        }
#endif

        #endregion

        #region UV mapping

        public List<Vector2> ComputeUVMap(HeightMap heightMap, TextureInfo textureInfo)
        {
            /**********************************
            * We need to map texture pixels to heightmap points
            * Linear mapping does not work because or Mercator projection distortion
            * Pseudo code : 
            * for each point
            *   project to texture coordinates (pixelXY at same zoom than texture)
            *   get pixel offset from origin
            *   map this offset to (0 -> texWidth, 0 -> textHeight) => (0->1, 0->1)
            */
            List<Vector2> uvs = new List<Vector2>(heightMap.Count);
            var bbox = textureInfo.ProjectedBounds;
            foreach (GeoPoint geoPoint in heightMap.Coordinates)
            {
                PointInt projPoint = TileUtils.LatLongToPixelXY(geoPoint.Latitude, geoPoint.Longitude, textureInfo.ProjectedZoom);

                float xOffset = projPoint.X - (float)bbox.xMin;
                float uvX = MathHelper.Map(1, textureInfo.Width, 0, 1, xOffset, true);

                float yOffset = projPoint.Y - (float)bbox.yMin;
                float uvY = MathHelper.Map(1, textureInfo.Height, 0, 1, yOffset, true);

                uvs.Add(new Vector2(uvX, uvY));
            }

            return uvs;
        }

        #endregion


    }
}
