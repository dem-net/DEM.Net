// ImageryService.cs
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
using System.Reflection;
using System.Threading;
using DEM.Net.Core.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using Path = SixLabors.Shapes.Path;
using Microsoft.Extensions.Caching.Memory;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Shapes;
using SixLabors.Primitives;
using DEM.Net.Core.Services.Imagery;
using SixLabors.ImageSharp.Formats.Png;

namespace DEM.Net.Core.Imagery
{
    public class ImageryService
    {
        #region Tiled imagery

        private int _serverCycle = 0;



        private readonly ILogger<ImageryService> _logger;
        private readonly MeshService _meshService;
        private readonly AppSecrets appSecrets;
        private readonly DEMNetOptions options;
        private readonly ImageryCache cache;


        public ImageryService(MeshService meshService,
            IOptions<AppSecrets> appSecrets,
            IOptions<DEMNetOptions> options,
            ImageryCache cache,
            ILogger<ImageryService> logger = null)
        {
            _logger = logger;
            _meshService = meshService;
            this.appSecrets = appSecrets?.Value;
            this.options = options?.Value;
            this.cache = cache;
        }

        public TileRange ComputeBoundingBoxTileRange(BoundingBox bbox, ImageryProvider provider,
            int minTilesPerImage = 4)
        {
            // TODO good one, to test
            // texture quality would be expressed in tex size
            //TileUtils.BestMapView(new double[] { bbox.xMin, bbox.yMin, bbox.xMax, bbox.yMax }, 16384, 16384, 0, provider.TileSize, out double centerLat, out double centerLon, out double zoomBestView);

            TileRange tiles = new TileRange(provider);
            BoundingBox mapBbox;
            Point<double> topLeft;
            Point<double> bottomRight;

            // optimal zoom calculation (maybe there's a direct way) 
            // see : https://docs.microsoft.com/fr-fr/azure/azure-maps/zoom-levels-and-tile-grid?tabs=csharp#tile-math-source-code (prepare tissues for nose bleed, and don't go if you're allergic to trigo and/or magical constants)
            // calculate the size of the full bbox at increasing zoom levels
            // until the full image would be greater than a tile
            int zoom = 0;
            int maxSize = 256 * minTilesPerImage; /* fixed to 256px to limit number of tiles */
            do
            {
                zoom++;

                // coords are pixels in global map image (see TileUtils.MapSize(zoom))
                topLeft = TileUtils.PositionToGlobalPixel(new LatLong(bbox.yMax, bbox.xMin), zoom, provider.TileSize);
                bottomRight = TileUtils.PositionToGlobalPixel(new LatLong(bbox.yMin, bbox.xMax), zoom, provider.TileSize);
                mapBbox = new BoundingBox(topLeft.X, bottomRight.X, topLeft.Y, bottomRight.Y);
            } while (zoom < provider.MaxZoom
                     && Math.Min(mapBbox.Width, mapBbox.Height) < maxSize);

            // now we have the minimum zoom without image
            // we can know which tiles are needed
            tiles.Start = new MapTileInfo(TileUtils.GlobalPixelToTileXY(topLeft.X, topLeft.Y, provider.TileSize), zoom, provider.TileSize);
            tiles.End = new MapTileInfo(TileUtils.GlobalPixelToTileXY(bottomRight.X, bottomRight.Y, provider.TileSize), zoom, provider.TileSize);
            tiles.AreaOfInterest = mapBbox;

            return tiles;
        }

        public TileRange ComputeBoundingBoxTileRangeForTargetResolution(BoundingBox bbox, ImageryProvider imageryProvider, int width, int height)
        {
            TileRange tiles = new TileRange(imageryProvider);
            TileUtils.BestMapView(new double[] { bbox.xMin, bbox.yMin, bbox.xMax, bbox.yMax }, width, height, 0, imageryProvider.TileSize, out double _, out double _, out double zoom);
            zoom = Math.Round(zoom, 0);
            var topLeft = TileUtils.PositionToGlobalPixel(new LatLong(bbox.yMax, bbox.xMin), (int)zoom, imageryProvider.TileSize);
            var bottomRight = TileUtils.PositionToGlobalPixel(new LatLong(bbox.yMin, bbox.xMax), (int)zoom, imageryProvider.TileSize);
            var mapBbox = new BoundingBox(topLeft.X, bottomRight.X, topLeft.Y, bottomRight.Y);
            tiles.Start = new MapTileInfo(TileUtils.GlobalPixelToTileXY(topLeft.X, topLeft.Y, imageryProvider.TileSize), (int)zoom, imageryProvider.TileSize);
            tiles.End = new MapTileInfo(TileUtils.GlobalPixelToTileXY(bottomRight.X, bottomRight.Y, imageryProvider.TileSize), (int)zoom, imageryProvider.TileSize);
            tiles.AreaOfInterest = mapBbox;
            return tiles;
        }

        public TileRange ComputeBoundingBoxTileRangeForZoomLevel(BoundingBox bbox, ImageryProvider provider, int zoom)
        {
            TileRange tiles = new TileRange(provider);
            Point<double> topLeft = TileUtils.PositionToGlobalPixel(new LatLong(bbox.yMax, bbox.xMin), zoom, provider.TileSize);
            Point<double> bottomRight = TileUtils.PositionToGlobalPixel(new LatLong(bbox.yMin, bbox.xMax), zoom, provider.TileSize);
            BoundingBox mapBbox = new BoundingBox(topLeft.X, bottomRight.X, topLeft.Y, bottomRight.Y);

            tiles.Start = new MapTileInfo(TileUtils.GlobalPixelToTileXY(topLeft.X, topLeft.Y, provider.TileSize), zoom, provider.TileSize);
            tiles.End = new MapTileInfo(TileUtils.GlobalPixelToTileXY(bottomRight.X, bottomRight.Y, provider.TileSize), zoom, provider.TileSize);
            tiles.AreaOfInterest = mapBbox;

            return tiles;
        }

        public TileRange DownloadTiles(TileRange tiles, ImageryProvider provider)
        {
            Stopwatch sw = Stopwatch.StartNew();
            int intervalMs = 1000;
            if (provider is ITileGenerator generator)
            {
                // download tiles
                using (TimeSpanBlock timer = new TimeSpanBlock("Tile generation", _logger))
                {
                    // Max download threads defined in provider
                    var parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = provider.MaxDegreeOfParallelism };
                    var range = tiles.TilesInfo.ToList();
                    _logger?.LogInformation($"Generating {range.Count} tiles with {provider.Name} generator...");
                    Parallel.ForEach(range, parallelOptions, tile =>
                        {
                            var contentbytes = generator.GenerateTile(tile.X, tile.Y, tile.Zoom);
                            tiles.Add(new MapTile(contentbytes, provider.TileSize, null, tile));
                        }
                    );
                }
            }
            else
            {
                // download tiles
                Stopwatch swDownload = Stopwatch.StartNew();
                _logger?.LogTrace("Starting images download");


                // Max download threads defined in provider
                var parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = provider.MaxDegreeOfParallelism };
                var range = tiles.TilesInfo.ToList();
                int numTilesDownloaded = 0;
                _logger?.LogInformation($"Downloading {range.Count} tiles...");
                try
                {
                    Parallel.ForEach(range, parallelOptions, (tile, state) =>
                    {
                        Uri tileUri = BuildUri(provider, tile.X, tile.Y, tile.Zoom);

                        var contentBytes = cache.GetTile(tileUri, provider, tile);

                        tiles.Add(new MapTile(contentBytes, provider.TileSize, tileUri, tile));

                        Interlocked.Increment(ref numTilesDownloaded);

                        if (sw.ElapsedMilliseconds > intervalMs)
                        {
                            _logger.LogInformation($"{numTilesDownloaded:N0}/{range.Count:N0} tiles downloaded...");
                            sw.Restart();
                        }
                    }
                );
                }
                catch (AggregateException ex)
                {
                    throw ex.GetInnerMostException();
                }
                catch (Exception)
                {
                    throw;
                }


                swDownload.Stop();
                _logger?.LogInformation($"DownloadImages done in : {swDownload.Elapsed:g}");
            }


            return tiles;
        }

        public TileRange DownloadTiles(BoundingBox bbox, ImageryProvider provider, int minTilesPerImage = 4)
        {
            TileRange tiles = this.ComputeBoundingBoxTileRange(bbox, provider, minTilesPerImage);
            return this.DownloadTiles(tiles, provider);
        }

        public BoundingBox ConvertWorldToMap(BoundingBox bbox, int zoomLevel, int tileSize)
        {
            var bboxTopLeft = TileUtils.PositionToGlobalPixel(new LatLong(bbox.yMax, bbox.xMin), zoomLevel, tileSize);
            var bboxBottomRight = TileUtils.PositionToGlobalPixel(new LatLong(bbox.yMin, bbox.xMax), zoomLevel, tileSize);
            return new BoundingBox(bboxTopLeft.X, bboxBottomRight.X, bboxTopLeft.Y, bboxBottomRight.Y, bbox.zMin, bbox.zMax);
        }

        BoundingBox GetTilesBoundingBox(TileRange tiles)
        {
            var tileInfos = tiles.TilesInfo.ToList();
            var bboxTopLeft =
                TileUtils.TileXYToGlobalPixel(tileInfos.Min(t => t.X), tileInfos.Min(t => t.Y), tiles.TileSize);
            var bboxBottomRight = TileUtils.TileXYToGlobalPixel(tileInfos.Max(t => t.X) + 1,
                tileInfos.Max(t => t.Y) + 1, tiles.TileSize);

            return new BoundingBox(bboxTopLeft.X, bboxBottomRight.X, bboxTopLeft.Y, bboxBottomRight.Y);
        }

        public TextureInfo ConstructTexture(TileRange tiles, BoundingBox bbox, string fileName,
            TextureImageFormat mimeType)
        {
            // where is the bbox in the final image ?

            // get pixel in full map
            int zoomLevel = tiles.Tiles.First().TileInfo.Zoom;
            var projectedBbox = ConvertWorldToMap(bbox, zoomLevel, tiles.TileSize);
            var tilesBbox = GetTilesBoundingBox(tiles);

            //DrawDebugBmpBbox(tiles, localBbox, tilesBbox, fileName, mimeType);
            int tileSize = tiles.TileSize;

            using (Image<Rgba32> outputImage = new Image<Rgba32>((int)Math.Ceiling(projectedBbox.Width), (int)Math.Ceiling(projectedBbox.Height)))
            {
                int xOffset = (int)(tilesBbox.xMin - projectedBbox.xMin);
                int yOffset = (int)(tilesBbox.yMin - projectedBbox.yMin);

                foreach (var tile in tiles.Tiles)
                {
                    try
                    {
                        int x = (tile.TileInfo.X - tiles.Start.X) * tileSize + xOffset;
                        int y = (tile.TileInfo.Y - tiles.Start.Y) * tileSize + yOffset;

                        if (x >= projectedBbox.Width || y >= projectedBbox.Height)
                            continue;

                        using (Image<Rgba32> tileImg = Image.Load(tile.Image))
                        {
                            outputImage.Mutate(o => o
                                .DrawImage(tileImg, new Point(x, y), 1f)
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error while generating texture: {ex.Message}");
                    }

                }

                // with encoder
                //IImageEncoder encoder = ConvertFormat(mimeType);
                //outputImage.Save(fileName, encoder);

                SaveImage(outputImage, fileName);
            }

            return new TextureInfo(fileName, mimeType, (int)Math.Ceiling(projectedBbox.Width), (int)Math.Ceiling(projectedBbox.Height), zoomLevel,
                projectedBbox, tiles.Count);
        }

        private void SaveImage(Image outputImage, string fileName)
        {
            IImageEncoder imageEncoder = GetEncoder(fileName);

            outputImage.Save(fileName, imageEncoder);
        }

        private IImageEncoder GetEncoder(string fileName)
        {
            var fileExtension = System.IO.Path.GetExtension(fileName).ToLower();
            if (fileExtension.EndsWith("jpg"))
                return new JpegEncoder { Quality = 98, Subsample = JpegSubsample.Ratio444 };
            else if (fileExtension.EndsWith("png"))
                return new PngEncoder();
            else return null;
        }

        public TextureInfo ConstructTextureWithGpxTrack(TileRange tiles, BoundingBox bbox, string fileName,
            TextureImageFormat mimeType, IEnumerable<GeoPoint> gpxPoints, bool drawGpxVertices = false, Rgba32 color = default(Rgba32), float lineWidth = 5f)
        {
            // where is the bbox in the final image ?

            // get pixel in full map
            int zoomLevel = tiles.Tiles.First().TileInfo.Zoom;
            var projectedBbox = ConvertWorldToMap(bbox, zoomLevel, tiles.TileSize);
            var tilesBbox = GetTilesBoundingBox(tiles);
            int xOffset = (int)(tilesBbox.xMin - projectedBbox.xMin);
            int yOffset = (int)(tilesBbox.yMin - projectedBbox.yMin);


            //DrawDebugBmpBbox(tiles, localBbox, tilesBbox, fileName, mimeType);
            int tileSize = tiles.TileSize;

            var pointsOnTexture = gpxPoints
                .Select(pt => TileUtils.PositionToGlobalPixel(new LatLong(pt.Latitude, pt.Longitude), zoomLevel, tiles.TileSize))
                .Select(pt => new PointF((float)(pt.X - (int)projectedBbox.xMin), (float)(pt.Y - (int)projectedBbox.yMin)));


            using (Image<Rgba32> outputImage = new Image<Rgba32>((int)projectedBbox.Width, (int)projectedBbox.Height))
            {
                foreach (var tile in tiles.Tiles)
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
                    .DrawLines(color == default(Rgba32) ? new Rgba32(1, 0, 0, 1f) : color, lineWidth, pointsOnTexture.ToArray())
                );

                if (drawGpxVertices)
                {
                    PathCollection pc = new PathCollection(pointsOnTexture.Select(p => new EllipsePolygon(p, new SizeF(10f, 10f))));
                    outputImage.Mutate(o => o.Draw(GraphicsOptions.Default, Pens.Solid(Rgba32.Violet, 3), pc));
                }

                // with encoder
                //IImageEncoder encoder = ConvertFormat(mimeType);
                //outputImage.Save(fileName, encoder);

                outputImage.Save(fileName);
            }

            return new TextureInfo(fileName, mimeType, (int)projectedBbox.Width, (int)projectedBbox.Height, zoomLevel,
                projectedBbox);
        }

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
            url = url.Replace("{quadkey}", TileUtils.TileXYToQuadKey(x, y, zoom));
            if (url.Contains("{t}"))
            {
                var token = GetToken(provider);
                if (String.IsNullOrWhiteSpace(token))
                {
                    var message =
                        $"There is no token found for {provider.Name} provider. Make sure a user secrets are set with a {provider.TokenUserSecretsKey} value.";
                    _logger?.LogError(message);
                    throw new Exception(message);
                }

                url = url.Replace("{t}", token);
            }

            return new Uri(url, UriKind.Absolute);
        }


        ConcurrentDictionary<string, PropertyInfo> tokenGetters = new ConcurrentDictionary<string, PropertyInfo>();

        private string GetToken(ImageryProvider provider)
        {
            PropertyInfo tokenGetter = tokenGetters.GetOrAdd(provider.TokenUserSecretsKey,
                tokenKey => typeof(AppSecrets).GetProperty(provider.TokenUserSecretsKey));
            return tokenGetter?.GetValue(appSecrets)?.ToString();
        }

        public bool IsTokenConfigurationValid(ImageryProvider provider)
        {
            var token = GetToken(provider);
            if (String.IsNullOrWhiteSpace(token))
            {
                var message =
                    $"There is no token found for {provider.Name} provider. Make sure a user secrets are set with a {provider.TokenUserSecretsKey} value.";
                _logger?.LogWarning(message);

                return false;
            }

            return true;
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
        public TextureInfo GenerateNormalMap(HeightMap heightMap, string outputDirectory, string fileName = "normalmap.png")
        {
            List<Vector3> normals = _meshService.ComputeNormals(heightMap).ToList();

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

                outputImage.Save(System.IO.Path.Combine(outputDirectory, fileName));
            }

            TextureInfo normal = new TextureInfo(System.IO.Path.Combine(outputDirectory, fileName), TextureImageFormat.image_jpeg,
                heightMap.Width, heightMap.Height);
            return normal;
        }

        /// <summary>
        /// Generate height map texture from height map, as a 16 bit grayscale PNG image. Grayscale is mapped from local min (black) to local highest point (white)
        /// Note : heightMap should be in projected coordinates (see ReprojectToCartesian())
        /// </summary>
        /// <param name="heightMap">heightMap in projected coordinates</param>
        /// <param name="outputDirectory"></param>
        /// <returns></returns>
        public TextureInfo GenerateHeightMap(HeightMap heightMap, string outputDirectory,
            string fileName = "heightmap.png")
        {
            return GenerateHeightMap(heightMap, System.IO.Path.Combine(outputDirectory, fileName));
        }

        /// <summary>
        /// Generate height map texture from height map, as a 16 bit grayscale PNG image. Grayscale is mapped from local min (black) to local highest point (white)
        /// Note : heightMap should be in projected coordinates (see ReprojectToCartesian())
        /// </summary>
        /// <param name="heightMap">heightMap in projected coordinates</param>
        /// <param name="outputDirectory"></param>
        /// <returns></returns>
        public TextureInfo GenerateHeightMap(HeightMap heightMap, string outputFileName)
        {
            using (Image<Gray16> outputImage = new Image<Gray16>(heightMap.Width, heightMap.Height))
            {
                int hMapIndex = 0;
                foreach (var coord in heightMap.Coordinates)
                {
                    // index is i + (j * heightMap.Width);
                    var j = hMapIndex / heightMap.Width;
                    var i = hMapIndex - j * heightMap.Width;

                    float gray = MathHelper.Map(heightMap.Minimum, heightMap.Maximum, 0, ushort.MaxValue, (float)(coord.Elevation ?? 0f), true);

                    outputImage[i, j] = new Gray16((ushort)Math.Round(gray, 0));

                    hMapIndex++;
                }

                outputImage.Save(outputFileName, new PngEncoder() { BitDepth = PngBitDepth.Bit16 });
            }

            TextureInfo normal = new TextureInfo(outputFileName, TextureImageFormat.image_png,
                heightMap.Width, heightMap.Height);
            return normal;
        }
        public TextureInfo GenerateHeightMap(List<GeoPoint> heightMap, int width, int height, string outputFileName)
        {
            double min = double.MaxValue;
            double max = double.MinValue;
            foreach (var coord in heightMap)
            {
                min = Math.Min(min, coord.Elevation.GetValueOrDefault(0));
                max = Math.Max(max, coord.Elevation.GetValueOrDefault(0));
            }
            using (Image<Gray16> outputImage = new Image<Gray16>(width, height))
            {
                int hMapIndex = 0;
                foreach (var coord in heightMap)
                {
                    // index is i + (j * heightMap.Width);
                    var j = hMapIndex / width;
                    var i = hMapIndex - j * width;

                    float gray = MathHelper.Map((float)min, (float)max, 0, ushort.MaxValue, (float)(coord.Elevation ?? 0f), true);

                    outputImage[i, j] = new Gray16((ushort)Math.Round(gray, 0));

                    hMapIndex++;
                }

                outputImage.Save(outputFileName, new PngEncoder() { BitDepth = PngBitDepth.Bit16 });
            }

            TextureInfo normal = new TextureInfo(outputFileName, TextureImageFormat.image_png,
                width, height);
            return normal;
        }

        public TextureInfo GenerateHeightMap(float[] heightMap, int width, int height, float minHeight, float maxHeight, string outputDirectory,
            string fileName = "heightmap.png")
        {
            using (Image<Gray16> outputImage = new Image<Gray16>(width, height))
            {
                int hMapIndex = 0;
                foreach (var coord in heightMap)
                {
                    // index is i + (j * heightMap.Width);
                    var j = hMapIndex / width;
                    var i = hMapIndex - j * height;

                    float gray = MathHelper.Map(minHeight, maxHeight, 0, ushort.MaxValue, coord, true);

                    outputImage[i, j] = new Gray16((ushort)Math.Round(gray, 0));

                    hMapIndex++;
                }

                outputImage.Save(System.IO.Path.Combine(outputDirectory, fileName));
            }

            TextureInfo normal = new TextureInfo(System.IO.Path.Combine(outputDirectory, fileName), TextureImageFormat.image_png,
                width, height);
            return normal;

        }

        public unsafe void GenerateTerrainRGB(float[] heightMap, int width, int height, float minHeight, float maxHeight, string outputDirectory,
            string fileName = "terrainRGB.png")
        {
            byte[] bytes = new byte[4];

            using (Image<Rgb24> outputImage = new Image<Rgb24>(width, height))
            {
                int hMapIndex = 0;
                foreach (var coord in heightMap)
                {
                    // index is i + (j * heightMap.Width);
                    var j = hMapIndex / width;
                    var i = hMapIndex - j * height;

                    int data = (int)Math.Round((coord + 10000) * 10, 0);

                    fixed (byte* b = bytes)
                        *((int*)b) = data;

                    outputImage[i, j] = new Rgb24(bytes[2], bytes[1], bytes[0]);

                    hMapIndex++;
                }

                outputImage.Save(System.IO.Path.Combine(outputDirectory, fileName));
            }
        }

        /// <summary>
        /// Warp height map, supposing it's already projected to 3857, thus the warp is only resizing the image
        /// we'll use image resizing algorithms (bicubic interpolation)
        /// Margin is there for RTIN, we add 1px borders to the right and bottom edges (source: https://observablehq.com/@mourner/martin-real-time-rtin-terrain-mesh)
        /// </summary>
        /// <param name="heightMap"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="margin"></param>
        /// <returns></returns>
        public float[] ResizeHeightMap_RTIN(HeightMap heightMap, int width, int height, int margin)
        {
            return ResizeHeightMap_RTIN(heightMap.Coordinates.Select(c => (float)(c.Elevation ?? 0)), heightMap.Width, heightMap.Height, width, height, heightMap.Minimum, heightMap.Maximum, margin);
        }
        public float[] ResizeHeightMap_RTIN(GeoPoint[] heightMap, int sourceWidth, int sourceHeight, int destWidth, int destHeight, float minElevation, float maxElevation, int margin)
        {
            return ResizeHeightMap_RTIN(heightMap.Select(c => (float)(c.Elevation ?? 0)), sourceWidth, sourceHeight, destWidth, destHeight, minElevation, maxElevation, margin);
        }
        //{

        //    float[] outMap = new float[(width + margin) * (height + margin)];

        //    using (Image<Gray16> outputImage = new Image<Gray16>(heightMap.Width, heightMap.Height))
        //    {

        //        int hMapIndex = 0;
        //        foreach (var coord in heightMap.Coordinates)
        //        {
        //            var j = hMapIndex / heightMap.Width;
        //            var i = hMapIndex - j * heightMap.Width;

        //            float gray = MathHelper.Map(heightMap.Minimum, heightMap.Maximum, 0, (float)ushort.MaxValue, (float)(coord.Elevation ?? 0f), true);

        //            outputImage[i, j] = new Gray16((ushort)Math.Round(gray, 0));

        //            hMapIndex++;
        //        }

        //        // Resize
        //        outputImage.Mutate(i => i.Resize(width, height));

        //        // Read heights from pixels again
        //        for (int j = 0; j < height + margin; j++)
        //            for (int i = 0; i < width + margin; i++)
        //            {
        //                Gray16 color = outputImage[Math.Min(i, width - 1), Math.Min(j, height - 1)];
        //                outMap[i + (j * width)] = FromColorToHeight(color, heightMap.Minimum, heightMap.Maximum);
        //            }
        //    }

        //    return outMap;
        //}
        public float[] ResizeHeightMap_RTIN(IEnumerable<float> heightMap, int sourceWidth, int sourceHeight, int destWidth, int destHeight, float minElevation, float maxElevation, int margin)
        {

            float[] outMap = new float[(destWidth + margin) * (destHeight + margin)];

            using (Image<Gray16> outputImage = new Image<Gray16>(sourceWidth, sourceHeight))
            {

                int hMapIndex = 0;
                foreach (var coord in heightMap)
                {
                    var j = hMapIndex / sourceWidth;
                    var i = hMapIndex - j * sourceWidth;

                    float gray = MathHelper.Map(minElevation, maxElevation, 0, (float)ushort.MaxValue, coord, true);

                    outputImage[i, j] = new Gray16((ushort)Math.Round(gray, 0));

                    hMapIndex++;
                }

                // Resize
                outputImage.Mutate(i => i.Resize(destWidth, destHeight));

                // Read heights from pixels again
                for (int j = 0; j < destHeight + margin; j++)
                    for (int i = 0; i < destWidth + margin; i++)
                    {
                        Gray16 color = outputImage[Math.Min(i, destWidth - 1), Math.Min(j, destHeight - 1)];
                        outMap[i + (j * destWidth)] = FromColorToHeight(color, minElevation, maxElevation);
                    }
            }

            return outMap;
        }
        public unsafe float[] ResizeHeightMap_RGB_RTIN(IEnumerable<float> heightMap, int sourceWidth, int sourceHeight, int destWidth, int destHeight, float minElevation, float maxElevation, int margin)
        {

            float[] outMap = new float[(destWidth + margin) * (destHeight + margin)];
            byte[] bytes = new byte[4];

            using (Image<Rgb24> outputImage = new Image<Rgb24>(sourceWidth, sourceHeight))
            {

                int hMapIndex = 0;
                foreach (var coord in heightMap)
                {
                    var j = hMapIndex / sourceWidth;
                    var i = hMapIndex - j * sourceWidth;

                    int data = (int)Math.Round((coord + 10000) * 10, 0);

                    fixed (byte* b = bytes)
                        *((int*)b) = data;

                    outputImage[i, j] = new Rgb24(bytes[2], bytes[1], bytes[0]);

                    hMapIndex++;
                }

                // Resize
                outputImage.Mutate(i => i.Resize(destWidth, destHeight));

                // Read heights from pixels again
                for (int j = 0; j < destHeight + margin; j++)
                    for (int i = 0; i < destWidth + margin; i++)
                    {
                        Rgb24 color = outputImage[Math.Min(i, destWidth - 1), Math.Min(j, destHeight - 1)];
                        outMap[i + (j * destWidth)] = FromColorRGBToHeight(color);
                    }
            }

            return outMap;
        }



        private Gray16 FromGeoPointToHeightMapColor(GeoPoint point, float min, float max)
        {
            float gray = MathHelper.Map(min, max, 0, (float)ushort.MaxValue, (float)(point.Elevation ?? 0f), true);
            ushort height = (ushort)Math.Round(gray, 0);
            return new Gray16(height);
        }
        private Gray16 FromElevationToHeightMapColor(float elevation, float min, float max)
        {
            float gray = MathHelper.Map(min, max, 0, (float)ushort.MaxValue, elevation, true);
            ushort height = (ushort)Math.Round(gray, 0);
            return new Gray16(height);
        }
        private float FromColorToHeight(Gray16 color, float min, float max)
        {
            float height = MathHelper.Map(0, (float)ushort.MaxValue, min, max, color.PackedValue, true);
            return height;
        }
        private float FromColorRGBToHeight(Rgb24 color)
        {
            float height = height = -10000 + ((color.R * 256 * 256 + color.G * 256 + color.B) * 0.1f);
            return height;
        }


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

        #endregion


        public List<Vector2> ComputeUVMap(HeightMap heightMap, TextureInfo textureInfo, int tileSize)
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
                Point<double> projPoint =
                    TileUtils.PositionToGlobalPixel(new LatLong(geoPoint.Latitude, geoPoint.Longitude), textureInfo.ProjectedZoom, tileSize);

                float xOffset = (float)projPoint.X - (float)bbox.xMin;
                float uvX = MathHelper.Map(1, textureInfo.Width, 0, 1, xOffset, true);

                float yOffset = (float)projPoint.Y - (float)bbox.yMin;
                float uvY = MathHelper.Map(1, textureInfo.Height, 0, 1, yOffset, true);

                uvs.Add(new Vector2(uvX, uvY));
            }

            return uvs;
        }


        /// <summary>
        /// Get lists of all registered providers.
        /// </summary>
        /// <remarks>Uses reflection so use with care</remarks>
        /// <returns></returns>
        public List<ImageryProvider> GetRegisteredProviders()
        {
            List<ImageryProvider> providers = new List<ImageryProvider>();
            foreach (var f in predefinedStaticProviders.Value)
            {
                providers.Add(f.GetValue(this) as ImageryProvider);
            }

            if (options.ImageryProviders != null)
            {
                providers.AddRange(options.ImageryProviders);
            }

            return providers;
        }

        private Lazy<List<FieldInfo>> predefinedStaticProviders =
            new Lazy<List<FieldInfo>>(GetPredefinedProviders, false);

        private static List<FieldInfo> GetPredefinedProviders()
        {
            Type t = typeof(ImageryProvider);
            string providerTypeName = t.FullName;
            var fields = t.GetRuntimeFields().Where(f => f.FieldType.FullName == providerTypeName).ToList();

            return fields;
        }
    }
}