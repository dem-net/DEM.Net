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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Numerics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DEM.Net.Core.Configuration;
using DEM.Net.Core.Services.Imagery;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace DEM.Net.Core.Imagery;

public class ImageryService(MeshService meshService,
    IOptions<AppSecrets> appSecrets,
    IOptions<DEMNetOptions> options,
    ImageryCache cache,
    ILogger<ImageryService>? logger = null)
{
    private int _serverCycle = 0;

    private readonly ILogger<ImageryService> _logger = logger ?? NullLogger<ImageryService>.Instance;
    private readonly MeshService _meshService = meshService;
    private readonly AppSecrets? appSecrets = appSecrets?.Value;
    private readonly DEMNetOptions options = options?.Value!;
    private readonly ImageryCache cache = cache;

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
        TileUtils.BestMapView([bbox.xMin, bbox.yMin, bbox.xMax, bbox.yMax], width, height, 0, imageryProvider.TileSize, out double _, out double _, out double zoom);
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

    public async Task<TileRange> DownloadTilesAsync(TileRange tiles, ImageryProvider provider, CancellationToken cancellationToken = default)
    {
        var range = tiles.TilesInfo.ToList();
        // Max download threads defined in provider
        var parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = provider.MaxDegreeOfParallelism, CancellationToken = cancellationToken };

        if (provider is ITileGenerator generator)
        {
            // generated tiles: CPU bound
            using (TimeSpanBlock timer = new TimeSpanBlock("Tile generation", _logger))
            {
                _logger?.LogInformation($"Generating {range.Count} tiles with {provider.Name} generator...");
                Parallel.ForEach(range, parallelOptions, tile =>
                {
                    var contentbytes = generator.GenerateTile(tile.X, tile.Y, tile.Zoom);
                    tiles.Add(new MapTile(contentbytes, provider.TileSize, null, tile));
                });
            }
            return tiles;
        }

        Stopwatch swDownload = Stopwatch.StartNew();
        Stopwatch swProgress = Stopwatch.StartNew();
        const int progressIntervalMs = 1000;
        int numTilesDownloaded = 0;
        int numFallbackTiles = 0;
        _logger?.LogInformation($"Downloading {range.Count} tiles...");

        await Parallel.ForEachAsync(range, parallelOptions, async (tile, ct) =>
        {
            Uri tileUri = BuildUri(provider, tile.X, tile.Y, tile.Zoom);

            byte[] contentBytes;
            try
            {
                contentBytes = await cache.GetTileAsync(tileUri, provider, tile, ct).ConfigureAwait(false);
            }
            catch (HttpRequestException ex) when (IsOutOfCoverage(ex) && provider.FallbackProvider is { } fallback)
            {
                // outside the provider's coverage: same tile from the fallback
                tileUri = BuildUri(fallback, tile.X, tile.Y, tile.Zoom);
                contentBytes = await cache.GetTileAsync(tileUri, fallback, tile, ct).ConfigureAwait(false);
                tiles.FallbackProviderUsed = fallback;
                Interlocked.Increment(ref numFallbackTiles);
            }

            tiles.Add(new MapTile(contentBytes, provider.TileSize, tileUri, tile));

            int done = Interlocked.Increment(ref numTilesDownloaded);
            if (swProgress.ElapsedMilliseconds > progressIntervalMs)
            {
                _logger?.LogInformation($"{done:N0}/{range.Count:N0} tiles downloaded...");
                swProgress.Restart();
            }
        }).ConfigureAwait(false);

        if (numFallbackTiles > 0)
            _logger?.LogInformation("{Count:N0}/{Total:N0} tiles outside {Provider} coverage taken from {Fallback}",
                numFallbackTiles, range.Count, provider.Name, tiles.FallbackProviderUsed?.Name);
        _logger?.LogInformation($"DownloadImages done in : {swDownload.Elapsed:g}");
        return tiles;
    }

    /// <summary>
    /// A tile the provider doesn't have: 404 (IGN.fr), or 400 for a tile outside the server's grid
    /// (SwissImage: "Tile out of bounds").
    /// </summary>
    private static bool IsOutOfCoverage(HttpRequestException ex)
        => ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest;

    public Task<TileRange> DownloadTilesAsync(BoundingBox bbox, ImageryProvider provider, int minTilesPerImage = 4, CancellationToken cancellationToken = default)
    {
        TileRange tiles = this.ComputeBoundingBoxTileRange(bbox, provider, minTilesPerImage);
        return this.DownloadTilesAsync(tiles, provider, cancellationToken);
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
        TextureImageFormat mimeType, float quality = 0.98f)
    {
        // The streaming generator only writes PNG and cannot resize: JPEG and power-of-two
        // textures use the in-memory path below.
        if (options?.MassiveImageGenerator == true
            && options.PowerOfTwoImages == false
            && System.IO.Path.GetExtension(fileName).EndsWith("png", StringComparison.OrdinalIgnoreCase))
        {
            return ConstructTextureStreamed(tiles, bbox, fileName, mimeType);
        }

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

                    if (tile.Image.Length == 0)
                    {
                        _logger.LogWarning($"Tile {tile.Uri} is empty. Skipping.");
                        continue;
                    }
                    using Image<Rgba32> tileImg = LoadTile(tile, tileSize);
                    outputImage.Mutate(o => o
                        .DrawImage(tileImg, new Point(x, y), 1f)
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error while generating texture: {ex.Message}");
                }

            }

            // Make image power of two dimensions
            if (options!.PowerOfTwoImages)
            {
                int nearestPowerOf2 = ToNextNearestPowerOf2(Math.Max(outputImage.Width, outputImage.Height));
                outputImage.Mutate(o => o.Resize(nearestPowerOf2, nearestPowerOf2));
            }

            SaveImage(outputImage, fileName, quality);
        }

        return new TextureInfo(fileName, mimeType, (int)Math.Ceiling(projectedBbox.Width), (int)Math.Ceiling(projectedBbox.Height), zoomLevel,
            projectedBbox, tiles.Count);
    }

    /// <summary>
    /// Streams the texture to a PNG file one tile row at a time (see <see cref="MassiveMapGenerator"/>).
    /// Produces the same pixels as the in-memory path of <see cref="ConstructTexture"/>.
    /// </summary>
    private TextureInfo ConstructTextureStreamed(TileRange tiles, BoundingBox bbox, string fileName,
        TextureImageFormat mimeType)
    {
        int zoomLevel = tiles.Tiles.First().TileInfo.Zoom;
        var projectedBbox = ConvertWorldToMap(bbox, zoomLevel, tiles.TileSize);
        var tilesBbox = GetTilesBoundingBox(tiles);

        // Same offset truncation as the in-memory path, so both produce identical images
        int cropX = -(int)(tilesBbox.xMin - projectedBbox.xMin);
        int cropY = -(int)(tilesBbox.yMin - projectedBbox.yMin);
        int width = (int)Math.Ceiling(projectedBbox.Width);
        int height = (int)Math.Ceiling(projectedBbox.Height);

        new MassiveMapGenerator(_logger).Generate(fileName, tiles, new Rectangle(cropX, cropY, width, height));

        return new TextureInfo(fileName, mimeType, width, height, zoomLevel, projectedBbox, tiles.Count);
    }

    int ToNextNearestPowerOf2(int x)
    {
        if (x < 0) { return 0; }
        --x;
        x |= x >> 1;
        x |= x >> 2;
        x |= x >> 4;
        x |= x >> 8;
        x |= x >> 16;
        return x + 1;
    }

    /// <summary>
    /// Decodes a tile and resizes it to <paramref name="tileSize"/> when the provider sent another size
    /// (e.g. a 1024px "@2x" tile in a 512px grid), so it is never drawn over its neighbours.
    /// Same behaviour as <see cref="TileDecoder"/> in the streamed path.
    /// </summary>
    private Image<Rgba32> LoadTile(MapTile tile, int tileSize)
    {
        var tileImg = Image.Load<Rgba32>(SafeImageDecoding.Options, tile.Image);
        if (tileImg.Width != tileSize || tileImg.Height != tileSize)
        {
            _logger.LogWarning("Tile {Uri} is {Width}x{Height}px, expected {TileSize}px. Resizing.", tile.Uri, tileImg.Width, tileImg.Height, tileSize);
            tileImg.Mutate(o => o.Resize(tileSize, tileSize));
        }
        return tileImg;
    }

    private void SaveImage(Image outputImage, string fileName, float quality = 0.98f)
    {
        IImageEncoder imageEncoder = GetEncoder(fileName, quality)!;

        outputImage.Save(fileName, imageEncoder);
    }

    private IImageEncoder? GetEncoder(string fileName, float quality = 0.98f)
    {
        var fileExtension = System.IO.Path.GetExtension(fileName).ToLower();
        if (fileExtension.EndsWith("jpg"))
            return new JpegEncoder() { Quality = (int)(quality * 100F), ColorType = JpegEncodingColor.YCbCrRatio444 };
        else if (fileExtension.EndsWith("png"))
            return new PngEncoder();
        else return null;
    }

    public TextureInfo ConstructTextureWithGpxTrack(TileRange tiles, BoundingBox bbox, string fileName,
        TextureImageFormat mimeType, IEnumerable<GeoPoint> gpxPoints, bool drawGpxVertices = false, Rgba32 color = default(Rgba32), float lineWidth = 5f)
        => ConstructTextureWithGpxTrack(tiles, bbox, fileName, mimeType, [gpxPoints], drawGpxVertices, color, lineWidth);

    /// <summary>
    /// Texture with the GPX track drawn on it: one line per segment (track segment or route), so separate
    /// segments are not joined by a straight line.
    /// </summary>
    public TextureInfo ConstructTextureWithGpxTrack(TileRange tiles, BoundingBox bbox, string fileName,
        TextureImageFormat mimeType, IEnumerable<IEnumerable<GeoPoint>> gpxSegments, bool drawGpxVertices = false, Rgba32 color = default(Rgba32), float lineWidth = 5f)
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

        List<PointF[]> segmentsOnTexture = [.. gpxSegments.Select(segment => segment
            .Select(pt => TileUtils.PositionToGlobalPixel(new LatLong(pt.Latitude, pt.Longitude), zoomLevel, tiles.TileSize))
            .Select(pt => new PointF((float)(pt.X - (int)projectedBbox.xMin), (float)(pt.Y - (int)projectedBbox.yMin)))
            .ToArray())];


        using (Image<Rgba32> outputImage = new Image<Rgba32>((int)projectedBbox.Width, (int)projectedBbox.Height))
        {
            foreach (var tile in tiles.Tiles)
            {
                using Image<Rgba32> tileImg = LoadTile(tile, tileSize);
                int x = (tile.TileInfo.X - tiles.Start.X) * tileSize + xOffset;
                int y = (tile.TileInfo.Y - tiles.Start.Y) * tileSize + yOffset;

                outputImage.Mutate(o => o
                    .DrawImage(tileImg, new Point(x, y), 1f)
                );
            }

            var lineColor = color == default(Rgba32) ? new Rgba32(1, 0, 0, 1f) : color;
            foreach (var segment in segmentsOnTexture.Where(seg => seg.Length >= 2))
            {
                outputImage.Mutate(o => o.DrawLine(lineColor, lineWidth, segment));
            }

            if (drawGpxVertices)
            {
                PathCollection pc = new PathCollection(segmentsOnTexture.SelectMany(seg => seg).Select(p => new EllipsePolygon(p, new SizeF(10f, 10f))));
                outputImage.Mutate(o => o.Draw(Pens.Solid(Color.Violet, 3), pc));
            }

            // with encoder
            //IImageEncoder encoder = ConvertFormat(mimeType);
            //outputImage.Save(fileName, encoder);

            outputImage.Save(fileName);
        }

        return new TextureInfo(fileName, mimeType, (int)projectedBbox.Width, (int)projectedBbox.Height, zoomLevel,
            projectedBbox);
    }

    public TextureInfo ConstructTextureWithPolygonMask(TileRange tiles, BoundingBox bbox, string fileName,
        TextureImageFormat mimeType, IEnumerable<GeoPoint> outerRing)
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

        var pointsOnTexture = outerRing
            .Select(pt => TileUtils.PositionToGlobalPixel(new LatLong(pt.Latitude, pt.Longitude), zoomLevel, tiles.TileSize))
            .Select(pt => new PointF((float)(pt.X - (int)projectedBbox.xMin), (float)(pt.Y - (int)projectedBbox.yMin)));

        using (Image<Rgba32> outputImage = new Image<Rgba32>((int)projectedBbox.Width, (int)projectedBbox.Height))
        {
            foreach (var tile in tiles.Tiles)
            {
                using Image<Rgba32> tileImg = new Image<Rgba32>(tile.PixelSize, tile.PixelSize);
                int x = (tile.TileInfo.X - tiles.Start.X) * tileSize + xOffset;
                int y = (tile.TileInfo.Y - tiles.Start.Y) * tileSize + yOffset;

                outputImage.Mutate(o => o
                    .FillPolygon(Color.Black, new PointF(x, y), new PointF(x + tileSize, y), new PointF(x + tileSize, y + tileSize), new PointF(x, y + tileSize), new PointF(x, y))
                );
            }

            var poly = new Polygon(pointsOnTexture.ToArray());
            outputImage.Mutate(o => o
                .Fill(Color.White, poly)
            );

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
        IImageFormat? imageFormat = null;
        if (format == TextureImageFormat.image_jpeg)
            imageFormatManager.TryFindFormatByFileExtension(".jpg", out imageFormat);
        else
            imageFormatManager.TryFindFormatByFileExtension(".png", out imageFormat);

        return imageFormatManager.GetEncoder(imageFormat!);
    }


    public Uri BuildUri(ImageryProvider provider, int x, int y, int zoom)
    {
        string[]? serverNodes = provider.UrlModel.Servers;
        string server = string.Empty;
        if (serverNodes != null && serverNodes.Length > 0)
        {
            // round robin over the servers, shared by the parallel downloads
            server = serverNodes[(int)((uint)Interlocked.Increment(ref _serverCycle) % (uint)serverNodes.Length)];
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
                throw new InvalidOperationException(message);
            }

            url = url.Replace("{t}", token);
        }

        return new Uri(url, UriKind.Absolute);
    }


    ConcurrentDictionary<string, PropertyInfo?> tokenGetters = new();

    private string? GetToken(ImageryProvider provider)
    {
        PropertyInfo? tokenGetter = tokenGetters.GetOrAdd(provider.TokenUserSecretsKey!,
            tokenKey => typeof(AppSecrets).GetProperty(provider.TokenUserSecretsKey!));
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

    /// <summary>
    /// Generate normal texture from height map.
    /// Note : heightMap should be in projected coordinates (see ReprojectToCartesian())
    /// </summary>
    /// <param name="heightMap">heightMap in projected coordinates</param>
    /// <param name="outputDirectory"></param>
    /// <returns></returns>
    public TextureInfo GenerateNormalMap(HeightMap heightMap, string outputDirectory, string fileName = "normalmap.png")
    {
        List<Vector3> normals = [.. _meshService.ComputeNormals(heightMap)];

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
        using (Image<L16> outputImage = new Image<L16>(heightMap.Width, heightMap.Height))
        {
            int hMapIndex = 0;
            foreach (var coord in heightMap.Coordinates)
            {
                // index is i + (j * heightMap.Width);
                var j = hMapIndex / heightMap.Width;
                var i = hMapIndex - j * heightMap.Width;

                float gray = MathHelper.Map(heightMap.Minimum, heightMap.Maximum, 0, ushort.MaxValue, (float)(coord.Elevation ?? 0f), true);

                outputImage[i, j] = new L16((ushort)Math.Round(gray, 0));

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
        using (Image<L16> outputImage = new Image<L16>(width, height))
        {
            int hMapIndex = 0;
            foreach (var coord in heightMap)
            {
                // index is i + (j * heightMap.Width);
                var j = hMapIndex / width;
                var i = hMapIndex - j * width;

                float gray = MathHelper.Map((float)min, (float)max, 0, ushort.MaxValue, (float)(coord.Elevation ?? 0f), true);

                outputImage[i, j] = new L16((ushort)Math.Round(gray, 0));

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
        using (Image<L16> outputImage = new Image<L16>(width, height))
        {
            int hMapIndex = 0;
            foreach (var coord in heightMap)
            {
                // index is i + (j * heightMap.Width);
                var j = hMapIndex / width;
                var i = hMapIndex - j * height;

                float gray = MathHelper.Map(minHeight, maxHeight, 0, ushort.MaxValue, coord, true);

                outputImage[i, j] = new L16((ushort)Math.Round(gray, 0));

                hMapIndex++;
            }

            outputImage.Save(System.IO.Path.Combine(outputDirectory, fileName));
        }

        TextureInfo normal = new TextureInfo(System.IO.Path.Combine(outputDirectory, fileName), TextureImageFormat.image_png,
            width, height);
        return normal;

    }

    public void GenerateTerrainRGB(IEnumerable<float> heightMap, int width, int height, string outputDirectory,
        string fileName = "terrainRGB.png")
    {
        using Image<Rgb24> outputImage = GenerateTerrainRGB(heightMap, width, height);
        Directory.CreateDirectory(outputDirectory);
        outputImage.Save(System.IO.Path.Combine(outputDirectory, fileName));
    }
    public unsafe Image<Rgb24> GenerateTerrainRGB(IEnumerable<float> heightMap, int width, int height)
    {
        byte[] bytes = new byte[4];

        Image<Rgb24> outputImage = new Image<Rgb24>(width, height);

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

        return outputImage;
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
    public List<float> ResizeHeightMap_RTIN(HeightMap heightMap, int width, int height, int margin)
    {
        return ResizeHeightMap_RTIN(heightMap.Coordinates.Select(c => (float)(c.Elevation ?? 0)), heightMap.Width, heightMap.Height, width, height, heightMap.Minimum, heightMap.Maximum, margin);
    }
    public List<float> ResizeHeightMap_RTIN(GeoPoint[] heightMap, int sourceWidth, int sourceHeight, int destWidth, int destHeight, float minElevation, float maxElevation, int margin)
    {
        return ResizeHeightMap_RTIN(heightMap.Select(c => (float)(c.Elevation ?? 0)), sourceWidth, sourceHeight, destWidth, destHeight, minElevation, maxElevation, margin);
    }
    public List<float> ResizeHeightMap_RTIN(IEnumerable<float> heightMap, int sourceWidth, int sourceHeight, int destWidth, int destHeight, float minElevation, float maxElevation, int margin)
    {

        List<float> outMap = new List<float>((destWidth + margin) * (destHeight + margin));

        using (Image<L16> outputImage = new Image<L16>(sourceWidth, sourceHeight))
        {

            int hMapIndex = 0;
            foreach (var coord in heightMap)
            {
                var j = hMapIndex / sourceWidth;
                var i = hMapIndex - j * sourceWidth;

                float gray = MathHelper.Map(minElevation, maxElevation, 0, (float)ushort.MaxValue, coord, true);

                outputImage[i, j] = new L16((ushort)Math.Round(gray, 0));

                hMapIndex++;
            }

            // Resize
            outputImage.Mutate(i => i.Resize(destWidth, destHeight));

            // Read heights from pixels again
            for (int j = 0; j < destHeight + margin; j++)
                for (int i = 0; i < destWidth + margin; i++)
                {
                    L16 color = outputImage[Math.Min(i, destWidth - 1), Math.Min(j, destHeight - 1)];
                    outMap[i + (j * destWidth)] = FromColorToHeight(color, minElevation, maxElevation);
                }
        }

        return outMap;
    }
    public unsafe List<float> ResizeHeightMap_RGB_RTIN(IEnumerable<float> heightMap, int sourceWidth, int sourceHeight, int destWidth, int destHeight, float minElevation, float maxElevation, int margin)
    {

        List<float> outMap = new List<float>((destWidth + margin) * (destHeight + margin));
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

    private L16 FromGeoPointToHeightMapColor(GeoPoint point, float min, float max)
    {
        float gray = MathHelper.Map(min, max, 0, (float)ushort.MaxValue, (float)(point.Elevation ?? 0f), true);
        ushort height = (ushort)Math.Round(gray, 0);
        return new L16(height);
    }
    private L16 FromElevationToHeightMapColor(float elevation, float min, float max)
    {
        float gray = MathHelper.Map(min, max, 0, (float)ushort.MaxValue, elevation, true);
        ushort height = (ushort)Math.Round(gray, 0);
        return new L16(height);
    }
    private float FromColorToHeight(L16 color, float min, float max)
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
        var bbox = textureInfo.ProjectedBounds!;
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
        List<ImageryProvider> providers = [];
        foreach (var f in predefinedStaticProviders.Value)
        {
            providers.Add((ImageryProvider)f.GetValue(null)!);
        }

        if (options.ImageryProviders != null)
        {
            providers.AddRange(options.ImageryProviders);
        }

        return providers;
    }

    private Lazy<List<FieldInfo>> predefinedStaticProviders =
        new(GetPredefinedProviders, false);

    private static List<FieldInfo> GetPredefinedProviders()
    {
        Type t = typeof(ImageryProvider);
        string? providerTypeName = t.FullName;
        // static only: ImageryProvider also has instance fields of its own type (FallbackProvider)
        var fields = t.GetRuntimeFields().Where(f => f.IsStatic && f.FieldType.FullName == providerTypeName).ToList();

        return fields;
    }
}