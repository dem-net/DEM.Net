using DEM.Net.Core.Datasets;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace DEM.Net.Core.XyzTiles
{
    /// <summary>
    /// Index for <see cref="XyzTileDataSource"/> datasets: the tiles covering a bbox are computed from the
    /// Web Mercator tile grid (no index file), and downloaded on demand.
    /// A missing tile (HTTP 404) means the area has no data at this zoom (no high resolution source, or open ocean
    /// which only exists at low zoom levels): the tile is resampled (bilinear) from its closest available ancestor
    /// and stored as a regular tile, so that every dataset keeps a uniform resolution.
    /// </summary>
    public class XyzTileIndexService : IDEMDataSetIndex
    {
        private const string UserAgent = "DEM.Net (+https://github.com/dem-net/DEM.Net)";
        private static readonly TimeSpan AncestorCacheDuration = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan CoverageCacheDuration = TimeSpan.FromHours(24);

        /// <summary>Maximum number of tiles a single bbox query may cover (protects the tile server from mass downloads).</summary>
        public static int MaxTilesPerRequest { get; set; } = 1024;

        private readonly ILogger<XyzTileIndexService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly ConcurrentDictionary<string, string> _localDirByDemName = new ConcurrentDictionary<string, string>();

        public XyzTileIndexService(ILogger<XyzTileIndexService> logger, IHttpClientFactory httpClientFactory, IMemoryCache cache)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _cache = cache;
        }

        private static XyzTileDataSource GetDataSource(DEMDataSet dataSet)
        {
            if (dataSet == null)
                throw new ArgumentNullException(nameof(dataSet), "Dataset is null.");
            return dataSet.DataSource as XyzTileDataSource
                ?? throw new ArgumentException($"Dataset source is {dataSet.DataSource.DataSourceType}, only {nameof(DEMDataSourceType.XyzTiles)} sources are supported by {nameof(XyzTileIndexService)}.", nameof(dataSet));
        }

        public void Setup(DEMDataSet dataSet, string dataSetLocalDir)
        {
            GetDataSource(dataSet);
            Directory.CreateDirectory(dataSetLocalDir);
            _localDirByDemName[dataSet.Name] = dataSetLocalDir;
        }

        private string GetLocalDir(DEMDataSet dataSet)
        {
            if (!_localDirByDemName.TryGetValue(dataSet.Name, out var dir))
                throw new InvalidOperationException($"{nameof(Setup)} must be called for dataset {dataSet.Name} first.");
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

        public void DownloadRasterFile(DemFileReport report, DEMDataSet dataset)
        {
            var source = GetDataSource(dataset);
            var (z, x, y) = TerrariumFile.ParseTileName(report.LocalName);
            string tempFile = report.LocalName + ".download";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(report.LocalName));

                byte[] bytes = TryDownload(report.URL);
                if (bytes != null)
                {
                    File.WriteAllBytes(tempFile, bytes);
                }
                else
                {
                    // No data at this zoom (e.g. no high resolution source here, or open ocean which only exists at low zoom)
                    _logger.LogInformation($"No tile at {z}/{x}/{y}, resampling from lower zoom.");
                    float[] heights = ResampleFromAncestor(source, z, x, y, out int size);
                    TerrariumFile.EncodeWebP(heights, size, tempFile);
                }

                // a partial download must never look like a valid local tile
                File.Move(tempFile, report.LocalName, overwrite: true);
            }
            catch (Exception ex)
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
                _logger.LogError(ex, $"Error while downloading DEM tile {report.URL}: {ex.Message}");
                throw;
            }
        }

        /// <summary>Downloads a tile, returns null when it does not exist (404).</summary>
        private byte[] TryDownload(string url)
        {
            var client = _httpClientFactory.CreateClient();
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.UserAgent.ParseAdd(UserAgent);
                using (var response = client.Send(request))
                {
                    if (response.StatusCode == HttpStatusCode.NotFound)
                        return null;

                    response.EnsureSuccessStatusCode();
                    using (var stream = response.Content.ReadAsStream())
                    using (var ms = new MemoryStream())
                    {
                        stream.CopyTo(ms);
                        return ms.ToArray();
                    }
                }
            }
        }

        /// <summary>
        /// Heights of the closest available ancestor of a tile, cached in memory (sibling tiles share their ancestor).
        /// </summary>
        private (float[] heights, int size, int zoom) GetAncestor(XyzTileDataSource source, int z, int x, int y)
        {
            for (int d = 1; z - d >= 0; d++)
            {
                int az = z - d, ax = x >> d, ay = y >> d;
                string url = source.GetTileUrl(az, ax, ay);
                var ancestor = _cache.GetOrCreate("xyz-ancestor:" + url, entry =>
                {
                    entry.SlidingExpiration = AncestorCacheDuration;
                    byte[] bytes = TryDownload(url);
                    if (bytes == null)
                        return ((float[])null, 0);
                    var heights = TerrariumFile.Decode(bytes, out int size);
                    return (heights, size);
                });
                if (ancestor.Item1 != null)
                    return (ancestor.Item1, ancestor.Item2, az);
            }
            throw new FileNotFoundException($"No ancestor tile found for tile {z}/{x}/{y} ({source.UrlTemplate}).");
        }

        /// <summary>
        /// Bilinear resampling of the part of the closest ancestor tile covering tile z/x/y. Sample positions are
        /// pixel centers in both tiles (cell registration).
        /// </summary>
        internal float[] ResampleFromAncestor(XyzTileDataSource source, int z, int x, int y, out int size)
        {
            var (ancestor, ancestorSize, az) = GetAncestor(source, z, x, y);
            int d = z - az;
            int scale = 1 << d;
            size = source.TileSize;

            // child pixel (i + 0.5) / size spans [offset, offset + 1/scale] of the ancestor tile
            double offsetX = (x - ((x >> d) << d)) / (double)scale;
            double offsetY = (y - ((y >> d) << d)) / (double)scale;

            return Resample(ancestor, ancestorSize, size, scale, offsetX, offsetY);
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
        public int GetMaxZoom(DEMDataSet dataset, BoundingBox bbox)
        {
            var source = GetDataSource(dataset);
            var areas = GetCoverageAreas(source);
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
                if (TileExists(source.GetTileUrl(maxZoom, x, y)))
                    break;
                maxZoom--;
            }
            return maxZoom;
        }

        private bool TileExists(string url)
        {
            return _cache.GetOrCreate("xyz-exists:" + url, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CoverageCacheDuration;
                try
                {
                    var client = _httpClientFactory.CreateClient();
                    using (var request = new HttpRequestMessage(HttpMethod.Head, url))
                    {
                        request.Headers.UserAgent.ParseAdd(UserAgent);
                        using (var response = client.Send(request))
                        {
                            if (response.StatusCode == HttpStatusCode.NotFound)
                                return false;
                            response.EnsureSuccessStatusCode();
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                    _logger.LogWarning(ex, $"Cannot check tile {url}: {ex.Message}");
                    return true; // keep the coverage index answer
                }
            });
        }

        private IReadOnlyList<CoverageArea> GetCoverageAreas(XyzTileDataSource source)
        {
            if (string.IsNullOrEmpty(source.CoverageIndexUrl))
                return Array.Empty<CoverageArea>();

            return _cache.GetOrCreate("xyz-coverage:" + source.CoverageIndexUrl, entry =>
            {
                try
                {
                    entry.AbsoluteExpirationRelativeToNow = CoverageCacheDuration;
                    byte[] bytes = TryDownload(source.CoverageIndexUrl)
                        ?? throw new FileNotFoundException(source.CoverageIndexUrl);
                    return ParseCoverageIndex(System.Text.Encoding.UTF8.GetString(bytes));
                }
                catch (Exception ex)
                {
                    // retry soon, meanwhile only the fully covered zoom levels are used
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                    _logger.LogWarning(ex, $"Cannot read tile coverage index {source.CoverageIndexUrl}: {ex.Message}");
                    return (IReadOnlyList<CoverageArea>)Array.Empty<CoverageArea>();
                }
            });
        }

        /// <summary>Parses Mapterhorn download_urls.json: items with min_lon/min_lat/max_lon/max_lat/max_zoom.</summary>
        internal static IReadOnlyList<CoverageArea> ParseCoverageIndex(string json)
        {
            var items = JObject.Parse(json)["items"] as JArray ?? throw new InvalidDataException("Coverage index has no items.");
            return items.OfType<JObject>()
                .Where(i => i["max_zoom"] != null && i["min_lon"] != null)
                .Select(i => new CoverageArea(
                    WebMercatorTiles.ToMercator(new BoundingBox(i.Value<double>("min_lon"), i.Value<double>("max_lon"), i.Value<double>("min_lat"), i.Value<double>("max_lat"))),
                    i.Value<int>("max_zoom")))
                .ToList();
        }

        internal readonly struct CoverageArea
        {
            public CoverageArea(BoundingBox bounds, int maxZoom) { Bounds = bounds; MaxZoom = maxZoom; }
            public BoundingBox Bounds { get; }
            public int MaxZoom { get; }
        }

        public void Reset()
        {
            _localDirByDemName.Clear();
        }
    }
}
