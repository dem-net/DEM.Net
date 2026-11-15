using DEM.Net.Core.Datasets;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DEM.Net.Core.TileIndex
{
    /// <summary>
    /// Index for <see cref="TileIndexDataSource"/>: downloads the GeoJSON tile index once, keeps a compact
    /// local copy (bbox + url + file name per tile) and downloads tiles on demand.
    /// </summary>
    public class TileIndexFileService : IDEMDataSetIndex
    {
        private const string TilesDirectory = "Tiles";

        private readonly ILogger<TileIndexFileService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ConcurrentDictionary<string, List<DEMFileSource>> _cacheByDemName = new ConcurrentDictionary<string, List<DEMFileSource>>();
        private readonly SingleFlight _setup = new SingleFlight(keepCompleted: false); // the result lives in _cacheByDemName

        public TileIndexFileService(ILogger<TileIndexFileService> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public Task SetupAsync(DEMDataSet dataSet, string dataSetLocalDir, CancellationToken cancellationToken = default)
        {
            if (dataSet == null)
                throw new ArgumentNullException(nameof(dataSet), "Dataset is null.");
            if (!(dataSet.DataSource is TileIndexDataSource dataSource))
                throw new ArgumentException($"Dataset source is {dataSet.DataSource.DataSourceType}, only {nameof(DEMDataSourceType.TileIndex)} sources are supported by {nameof(TileIndexFileService)}.", nameof(dataSet));

            if (_cacheByDemName.ContainsKey(dataSet.Name))
                return Task.CompletedTask;

            return _setup.RunAsync(dataSet.Name, async () =>
            {
                try
                {
                    Directory.CreateDirectory(dataSetLocalDir);
                    string indexFileName = Path.Combine(dataSetLocalDir, dataSource.IndexFilePath);

                    if (!File.Exists(indexFileName))
                    {
                        _logger.LogInformation($"Downloading {dataSet.Name} tile index from {dataSource.IndexUrl}... This will be done once.");
                        var client = _httpClientFactory.CreateClient();
                        var entries = ParseGeoJsonIndex(await client.GetStringAsync(dataSource.IndexUrl).ConfigureAwait(false), dataSource);

                        // an interrupted run never leaves a truncated index
                        await HttpDownload.WriteAllTextAtomicAsync(indexFileName, JsonConvert.SerializeObject(entries), CancellationToken.None).ConfigureAwait(false);
                        _logger.LogInformation($"{entries.Count:N0} tiles written to index file {indexFileName}");
                    }

                    _cacheByDemName[dataSet.Name] = LoadSources(indexFileName, dataSetLocalDir, dataSet.SRID);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error while setting up {dataSet.Name} tile index: {ex.Message}");
                    throw;
                }
            }, cancellationToken);
        }

        internal static List<TileIndexEntry> ParseGeoJsonIndex(string geoJson, TileIndexDataSource dataSource)
        {
            var root = JObject.Parse(geoJson);
            var features = root["features"] as JArray ?? throw new InvalidDataException("Tile index is not a GeoJSON FeatureCollection.");

            var entries = new List<TileIndexEntry>(features.Count);
            foreach (JObject feature in features.OfType<JObject>())
            {
                var properties = (feature["properties"] as JObject)?.Properties()
                    .ToDictionary(p => p.Name, p => p.Value.Type == JTokenType.Null ? null : Convert.ToString(((JValue)p.Value).Value, CultureInfo.InvariantCulture))
                    ?? new Dictionary<string, string>();

                var bbox = GetBoundingBox(feature["geometry"]);
                if (bbox == null) continue;

                string url = dataSource.TileUrl(properties);
                if (string.IsNullOrWhiteSpace(url)) continue;

                string fileName = dataSource.TileFileName?.Invoke(properties) ?? Path.GetFileName(new Uri(url).LocalPath);
                entries.Add(new TileIndexEntry { FileName = fileName, Url = url, Box = bbox });
            }
            return entries;
        }

        // [xMin, yMin, xMax, yMax] over all positions of the geometry
        private static double[] GetBoundingBox(JToken geometry)
        {
            var positions = geometry?["coordinates"]?
                .SelectTokens("$..[?(@ != null)]") // flatten nested arrays
                .OfType<JArray>()
                .Where(a => a.Count >= 2 && a[0].Type != JTokenType.Array)
                .ToList();
            if (positions == null || positions.Count == 0) return null;

            double xMin = double.MaxValue, yMin = double.MaxValue, xMax = double.MinValue, yMax = double.MinValue;
            foreach (var p in positions)
            {
                double x = p[0].Value<double>(), y = p[1].Value<double>();
                xMin = Math.Min(xMin, x); xMax = Math.Max(xMax, x);
                yMin = Math.Min(yMin, y); yMax = Math.Max(yMax, y);
            }
            return new[] { xMin, yMin, xMax, yMax };
        }

        private static List<DEMFileSource> LoadSources(string indexFileName, string dataSetLocalDir, int srid)
        {
            var entries = JsonConvert.DeserializeObject<List<TileIndexEntry>>(File.ReadAllText(indexFileName));
            return entries.Select(e => new DEMFileSource
            {
                BBox = new BoundingBox(e.Box[0], e.Box[2], e.Box[1], e.Box[3]) { SRID = srid },
                SourceFileName = e.FileName,
                SourceFileNameAbsolute = e.Url,
                LocalFileName = Path.Combine(dataSetLocalDir, TilesDirectory, e.FileName)
            }).ToList();
        }

        public async Task DownloadRasterFileAsync(DemFileReport report, DEMDataSet dataset, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation($"Downloading {report.URL}...");
                await HttpDownload.DownloadToFileAsync(_httpClientFactory.CreateClient(), report.URL, report.LocalName, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, $"Error while downloading DEM file {report.URL}: {ex.Message}");
                throw;
            }
        }

        public IEnumerable<DEMFileSource> GetCoveredFileSources(DEMDataSet dataset, BoundingBox bbox)
        {
            foreach (DEMFileSource source in GetFileSources(dataset))
            {
                if (bbox == null || source.BBox.Intersects(bbox))
                    yield return source;
            }
        }

        public IEnumerable<DEMFileSource> GetFileSources(DEMDataSet dataset)
        {
            if (!_cacheByDemName.TryGetValue(dataset.Name, out var sources))
                throw new InvalidOperationException($"{nameof(SetupAsync)} must be called for dataset {dataset.Name} first.");
            return sources;
        }

        public void Reset()
        {
            _cacheByDemName.Clear();
        }

        internal class TileIndexEntry
        {
            public string FileName { get; set; }
            public string Url { get; set; }
            public double[] Box { get; set; } // xMin, yMin, xMax, yMax
        }
    }
}
