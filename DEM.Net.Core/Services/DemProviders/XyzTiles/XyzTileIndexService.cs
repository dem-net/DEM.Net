using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DEM.Net.Core.Datasets;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DEM.Net.Core.XyzTiles;

/// <summary>
/// Index for <see cref="XyzTileDataSource"/> datasets: the tiles covering a bbox are computed from the
/// Web Mercator tile grid (no index file), and downloaded on demand.
/// A missing tile (HTTP 404) means the area has no data at this zoom (no high resolution source, or open ocean
/// which only exists at low zoom levels): the tile is resampled (bilinear) from its closest available ancestor
/// and stored as a regular tile, so that every dataset keeps a uniform resolution.
/// </summary>
public class XyzTileIndexService(ILogger<XyzTileIndexService> logger, IHttpClientFactory httpClientFactory, IMemoryCache cache) : IDEMDataSetIndex
{
    private const string UserAgent = "DEM.Net (+https://github.com/dem-net/DEM.Net)";
    private static readonly TimeSpan AncestorCacheDuration = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan CoverageCacheDuration = TimeSpan.FromHours(24);

    /// <summary>Maximum number of tiles a single bbox query may cover (protects the tile server from mass downloads).</summary>
    public static int MaxTilesPerRequest { get; set; } = 1024;

    private readonly ILogger<XyzTileIndexService> _logger = logger;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IMemoryCache _cache = cache;
    private readonly ConcurrentDictionary<string, string> _localDirByDemName = new();

    private static XyzTileDataSource GetDataSource(DEMDataSet dataSet)
    {
        if (dataSet == null)
            throw new ArgumentNullException(nameof(dataSet), "Dataset is null.");
        return dataSet.DataSource as XyzTileDataSource
            ?? throw new ArgumentException($"Dataset source is {dataSet.DataSource.DataSourceType}, only {nameof(DEMDataSourceType.XyzTiles)} sources are supported by {nameof(XyzTileIndexService)}.", nameof(dataSet));
    }

    /// <remarks>No index to download: runs synchronously.</remarks>
    public Task SetupAsync(DEMDataSet dataSet, string dataSetLocalDir, CancellationToken cancellationToken = default)
    {
        GetDataSource(dataSet);
        Directory.CreateDirectory(dataSetLocalDir);
        _localDirByDemName[dataSet.Name] = dataSetLocalDir;
        return Task.CompletedTask;
    }

    private string GetLocalDir(DEMDataSet dataSet)
    {
        if (!_localDirByDemName.TryGetValue(dataSet.Name, out var dir))
            throw new InvalidOperationException($"{nameof(SetupAsync)} must be called for dataset {dataSet.Name} first.");
        return dir;
    }

    private DEMFileSource CreateSource(DEMDataSet dataSet, XyzTileDataSource source, string localDir, int x, int y)
    {
        int z = source.Zoom;
        string fileName = TerrariumFile.GetTileFileName(z, x, y, dataSet.FileFormat.FileExtension);
        return new DEMFileSource
        {
            BBox = WebMercatorTiles.TileBounds(z, x, y),
            SourceFileName = fileName,
            SourceFileNameAbsolute = source.GetTileUrl(z, x, y),
            // one directory per column keeps directories small (manifests are stored next to the tiles)
            LocalFileName = Path.Combine(localDir, x.ToString(), fileName)
        };
    }

    /// <summary>
    /// Only the tiles already downloaded: a tile pyramid has no finite list of sources worth enumerating.
    /// </summary>
    public IEnumerable<DEMFileSource> GetFileSources(DEMDataSet dataset)
    {
        var source = GetDataSource(dataset);
        string localDir = GetLocalDir(dataset);
        if (!Directory.Exists(localDir))
            yield break;

        foreach (var file in Directory.EnumerateFiles(localDir, "*" + dataset.FileFormat.FileExtension, SearchOption.AllDirectories))
        {
            (int z, int x, int y) tile;
            try { tile = TerrariumFile.ParseTileName(file); }
            catch (ArgumentException) { continue; }
            if (tile.z == source.Zoom)
                yield return CreateSource(dataset, source, localDir, tile.x, tile.y);
        }
    }

    /// <param name="bbox">Bounding box, WGS84 or EPSG:3857 (see <see cref="BoundingBox.SRID"/>)</param>
    public IEnumerable<DEMFileSource> GetCoveredFileSources(DEMDataSet dataset, BoundingBox bbox)
    {
        if (bbox == null)
            return GetFileSources(dataset);

        var source = GetDataSource(dataset);
        string localDir = GetLocalDir(dataset);
        var (xMin, xMax, yMin, yMax) = WebMercatorTiles.TileRange(WebMercatorTiles.ToMercator(bbox), source.Zoom);

        // Safety net against mass downloads from the tile server: callers must pick a zoom level fitting the extent
        long count = (long)(xMax - xMin + 1) * (yMax - yMin + 1);
        if (count > MaxTilesPerRequest)
            throw new InvalidOperationException($"Extent covers {count:N0} tiles of {dataset.Name} (maximum {MaxTilesPerRequest:N0}): use a lower zoom level or a smaller extent.");

        var sources = new List<DEMFileSource>((xMax - xMin + 1) * (yMax - yMin + 1));
        for (int x = xMin; x <= xMax; x++)
            for (int y = yMin; y <= yMax; y++)
                sources.Add(CreateSource(dataset, source, localDir, x, y));
        return sources;
    }

    public async Task DownloadRasterFileAsync(DemFileReport report, DEMDataSet dataset, CancellationToken cancellationToken = default)
    {
        var source = GetDataSource(dataset);
        var (z, x, y) = TerrariumFile.ParseTileName(report.LocalName);
        try
        {
            bool downloaded = await HttpDownload.DownloadToFileAsync(_httpClientFactory.CreateClient(), report.URL, report.LocalName, cancellationToken,
                SetUserAgent, notFoundIsMissing: true).ConfigureAwait(false);
            if (!downloaded)
            {
                // No data at this zoom (e.g. no high resolution source here, or open ocean which only exists at low zoom)
                _logger.LogInformation($"No tile at {z}/{x}/{y}, resampling from lower zoom.");
                var (heights, size) = await ResampleFromAncestorAsync(source, z, x, y, cancellationToken).ConfigureAwait(false);

                // a partial file must never look like a valid local tile
                string tempFile = HttpDownload.TempFileName(report.LocalName);
                try
                {
                    TerrariumFile.EncodeWebP(heights, size, tempFile);
                    File.Move(tempFile, report.LocalName, overwrite: true);
                }
                finally
                {
                    if (File.Exists(tempFile)) File.Delete(tempFile);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, $"Error while downloading DEM tile {report.URL}: {ex.Message}");
            throw;
        }
    }

    private static void SetUserAgent(HttpRequestMessage request) => request.Headers.UserAgent.ParseAdd(UserAgent);

    /// <summary>Downloads a tile, returns null when it does not exist (404).</summary>
    private Task<byte[]> TryDownloadAsync(string url, CancellationToken cancellationToken)
        => HttpDownload.TryGetBytesAsync(_httpClientFactory.CreateClient(), url, cancellationToken, SetUserAgent);

    /// <summary>
    /// Heights of the closest available ancestor of a tile, cached in memory (sibling tiles share their ancestor).
    /// </summary>
    private async Task<(float[] heights, int size, int zoom)> GetAncestorAsync(XyzTileDataSource source, int z, int x, int y, CancellationToken cancellationToken)
    {
        for (int d = 1; z - d >= 0; d++)
        {
            int az = z - d, ax = x >> d, ay = y >> d;
            string url = source.GetTileUrl(az, ax, ay);
            string key = "xyz-ancestor:" + url;
            if (!_cache.TryGetValue(key, out (float[] heights, int size) ancestor))
            {
                byte[] bytes = await TryDownloadAsync(url, cancellationToken).ConfigureAwait(false);
                ancestor = bytes == null ? (null, 0) : (TerrariumFile.Decode(bytes, out int size), size);
                _cache.Set(key, ancestor, new MemoryCacheEntryOptions { SlidingExpiration = AncestorCacheDuration });
            }
            if (ancestor.heights != null)
                return (ancestor.heights, ancestor.size, az);
        }
        throw new FileNotFoundException($"No ancestor tile found for tile {z}/{x}/{y} ({source.UrlTemplate}).");
    }

    /// <summary>
    /// Bilinear resampling of the part of the closest ancestor tile covering tile z/x/y. Sample positions are
    /// pixel centers in both tiles (cell registration).
    /// </summary>
    internal async Task<(float[] heights, int size)> ResampleFromAncestorAsync(XyzTileDataSource source, int z, int x, int y, CancellationToken cancellationToken = default)
    {
        var (ancestor, ancestorSize, az) = await GetAncestorAsync(source, z, x, y, cancellationToken).ConfigureAwait(false);
        int d = z - az;
        int scale = 1 << d;
        int size = source.TileSize;

        // child pixel (i + 0.5) / size spans [offset, offset + 1/scale] of the ancestor tile
        double offsetX = (x - ((x >> d) << d)) / (double)scale;
        double offsetY = (y - ((y >> d) << d)) / (double)scale;

        return (Resample(ancestor, ancestorSize, size, scale, offsetX, offsetY), size);
    }

    public static float[] Resample(float[] ancestor, int ancestorSize, int size, int scale, double offsetX, double offsetY)
    {
        var heights = new float[size * size];
        int max = ancestorSize - 1;
        for (int j = 0; j < size; j++)
        {
            double v = (offsetY + (j + 0.5) / size / scale) * ancestorSize - 0.5;
            v = Math.Max(0, Math.Min(max, v));
            int v0 = (int)Math.Floor(v), v1 = Math.Min(max, v0 + 1);
            double fv = v - v0;

            for (int i = 0; i < size; i++)
            {
                double u = (offsetX + (i + 0.5) / size / scale) * ancestorSize - 0.5;
                u = Math.Max(0, Math.Min(max, u));
                int u0 = (int)Math.Floor(u), u1 = Math.Min(max, u0 + 1);
                double fu = u - u0;

                double top = ancestor[v0 * ancestorSize + u0] * (1 - fu) + ancestor[v0 * ancestorSize + u1] * fu;
                double bottom = ancestor[v1 * ancestorSize + u0] * (1 - fu) + ancestor[v1 * ancestorSize + u1] * fu;
                heights[j * size + i] = (float)(top * (1 - fv) + bottom * fv);
            }
        }
        return heights;
    }

    /// <summary>
    /// Highest zoom level with data intersecting the bbox, from the coverage index of the data source
    /// (<see cref="XyzTileDataSource.CoverageIndexUrl"/>). Falls back to <see cref="XyzTileDataSource.FullCoverageMaxZoom"/>.
    /// </summary>
    /// <param name="bbox">Bounding box, WGS84 or EPSG:3857 (see <see cref="BoundingBox.SRID"/>)</param>
    public async Task<int> GetMaxZoomAsync(DEMDataSet dataset, BoundingBox bbox, CancellationToken cancellationToken = default)
    {
        var source = GetDataSource(dataset);
        var areas = await GetCoverageAreasAsync(source, cancellationToken).ConfigureAwait(false);
        var mercator = WebMercatorTiles.ToMercator(bbox);

        int maxZoom = source.FullCoverageMaxZoom;
        foreach (var area in areas)
        {
            if (area.MaxZoom > maxZoom && area.Bounds.Intersects(mercator))
                maxZoom = area.MaxZoom;
        }

        // Coverage areas are coarse (zoom 6 tiles, max zoom of their best source): check that the tile at the
        // center of the bbox exists, otherwise the highest zoom would only be resampled from a lower one.
        double centerX = (mercator.xMin + mercator.xMax) / 2d, centerY = (mercator.yMin + mercator.yMax) / 2d;
        while (maxZoom > source.FullCoverageMaxZoom)
        {
            var (x, _, y, _) = WebMercatorTiles.TileRange(new BoundingBox(centerX, centerX, centerY, centerY) { SRID = Reprojection.SRID_PROJECTED_MERCATOR }, maxZoom);
            if (await TileExistsAsync(source.GetTileUrl(maxZoom, x, y), cancellationToken).ConfigureAwait(false))
                break;
            maxZoom--;
        }
        return maxZoom;
    }

    private async Task<bool> TileExistsAsync(string url, CancellationToken cancellationToken)
    {
        string key = "xyz-exists:" + url;
        if (_cache.TryGetValue(key, out bool exists))
            return exists;

        TimeSpan duration = CoverageCacheDuration;
        try
        {
            var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            SetUserAgent(request);
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NotFound)
                exists = false;
            else
            {
                response.EnsureSuccessStatusCode();
                exists = true;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            duration = TimeSpan.FromMinutes(5);
            _logger.LogWarning(ex, $"Cannot check tile {url}: {ex.Message}");
            exists = true; // keep the coverage index answer
        }
        _cache.Set(key, exists, duration);
        return exists;
    }

    private async Task<IReadOnlyList<CoverageArea>> GetCoverageAreasAsync(XyzTileDataSource source, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(source.CoverageIndexUrl))
            return Array.Empty<CoverageArea>();

        string key = "xyz-coverage:" + source.CoverageIndexUrl;
        if (_cache.TryGetValue(key, out IReadOnlyList<CoverageArea> areas))
            return areas;

        TimeSpan duration = CoverageCacheDuration;
        try
        {
            byte[] bytes = await TryDownloadAsync(source.CoverageIndexUrl, cancellationToken).ConfigureAwait(false)
                ?? throw new FileNotFoundException(source.CoverageIndexUrl);
            areas = ParseCoverageIndex(System.Text.Encoding.UTF8.GetString(bytes));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // retry soon, meanwhile only the fully covered zoom levels are used
            duration = TimeSpan.FromMinutes(5);
            _logger.LogWarning(ex, $"Cannot read tile coverage index {source.CoverageIndexUrl}: {ex.Message}");
            areas = Array.Empty<CoverageArea>();
        }
        _cache.Set(key, areas, duration);
        return areas;
    }

    /// <summary>Parses Mapterhorn download_urls.json: items with min_lon/min_lat/max_lon/max_lat/max_zoom.</summary>
    internal static IReadOnlyList<CoverageArea> ParseCoverageIndex(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object
            || !document.RootElement.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("Coverage index has no items.");

        var areas = new List<CoverageArea>(items.GetArrayLength());
        foreach (var item in items.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object || !Has(item, "max_zoom") || !Has(item, "min_lon"))
                continue;
            areas.Add(new CoverageArea(
                WebMercatorTiles.ToMercator(new BoundingBox(item.GetProperty("min_lon").GetDouble(), item.GetProperty("max_lon").GetDouble(),
                    item.GetProperty("min_lat").GetDouble(), item.GetProperty("max_lat").GetDouble())),
                item.GetProperty("max_zoom").GetInt32()));
        }
        return areas;

        static bool Has(JsonElement item, string name) => item.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null;
    }

    internal readonly struct CoverageArea(BoundingBox bounds, int maxZoom)
    {
        public BoundingBox Bounds { get; } = bounds; public int MaxZoom { get; } = maxZoom;
    }

    public void Reset()
    {
        _localDirByDemName.Clear();
    }
}
