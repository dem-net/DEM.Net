using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DEM.Net.Core.Datasets;
using Microsoft.Extensions.Logging;

namespace DEM.Net.Core.EarthData;

public class NasaGranuleFileService(ILogger<NasaGranuleFileService> logger, EarthdataLoginConnector rasterDownloader, IHttpClientFactory httpClientFactory) : IDEMDataSetIndex
{
    private readonly ILogger<NasaGranuleFileService> logger = logger;
    private readonly ConcurrentDictionary<string, List<DEMFileSource>> _cacheByDemName = new();
    private readonly SingleFlight _setup = new(keepCompleted: false); // the result lives in _cacheByDemName
    private readonly IHttpClientFactory httpClientFactory = httpClientFactory;
    private readonly EarthdataLoginConnector rasterDownloader = rasterDownloader;

    public Task SetupAsync(DEMDataSet dataSet, string dataSetLocalDir, CancellationToken cancellationToken = default)
    {
        if (dataSet == null)
            throw new ArgumentNullException(nameof(dataSet), "Dataset is null.");
        if (dataSet.DataSource is not NasaGranuleDataSource dataSource)
            throw new ArgumentException($"Dataset source is {dataSet.DataSource.DataSourceType}, only {nameof(DEMDataSourceType.NasaEarthData)} sources are supported by {nameof(NasaGranuleFileService)}.", nameof(dataSet));

        if (_cacheByDemName.ContainsKey(dataSet.Name))
            return Task.CompletedTask;

        return _setup.RunAsync(dataSet.Name, async () =>
        {
            try
            {
                Directory.CreateDirectory(dataSetLocalDir);
                string indexFileName = Path.Combine(dataSetLocalDir, UrlHelper.GetFileNameFromUrl(dataSource.IndexFilePath));

                if (!File.Exists(indexFileName))
                {
                    var links = await FetchGranulesAsync(dataSource).ConfigureAwait(false);
                    // an interrupted run never leaves a truncated index
                    await HttpDownload.WriteAllTextAtomicAsync(indexFileName, JsonSerializer.Serialize(links, CoreJsonContext.Default.ListNasaDemFile), CancellationToken.None).ConfigureAwait(false);
                    logger.LogInformation($"{links.Count} entries written to index file {indexFileName}");
                }

                _cacheByDemName[dataSet.Name] = GetSources(dataSet, indexFileName);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, $"Error while setting up {dataSet.Name} granule index: {ex.Message}");
                throw;
            }
        }, cancellationToken);
    }

    /// <summary>Fetches the Earth data collection page by page until no result is left.</summary>
    private async Task<List<NasaDemFile>> FetchGranulesAsync(NasaGranuleDataSource dataSource)
    {
        const int PAGE_SIZE = 2000;
        logger.LogInformation($"Fetching granules from collection {dataSource.CollectionId} to disk... This will be done once.");
        var links = new List<NasaDemFile>(30000);
        var httpClient = httpClientFactory.CreateClient();
        for (int pageIndex = 1; ; pageIndex++)
        {
            logger.LogInformation($"Getting entries on page {pageIndex} with page size of {PAGE_SIZE} ({(pageIndex - 1) * PAGE_SIZE} entries so far)...");
            var json = await httpClient.GetStringAsync(dataSource.GetUrl(PAGE_SIZE, pageIndex)).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(json))
                break;

            var result = NasaCmrGranuleResult.FromJson(json);
            if (!result.Feed.Entry.Any())
                break;

            // Only retrieve bbox and dem file link (zip file)
            links.AddRange(result.Feed.Entry.Select(GetNasaDemFile).OfType<NasaDemFile>());
        }
        return links;
    }

    private NasaDemFile? GetNasaDemFile(Entry entry)
    {
        try
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry), "Entry is mandatory.");
            if (entry.Boxes == null || entry.Boxes.Count == 0)
                throw new ArgumentNullException(nameof(entry.Boxes), "Boxes should contain at least an element.");
            if (entry.Links == null || entry.Links.Count == 0)
                throw new ArgumentNullException(nameof(entry.Links), "Links should contain at least an element.");

            var link = entry.Links.FirstOrDefault(l => l.Type == TypeEnum.ApplicationZip);
            if (link == null)
            {
                // fallack for LPCLOUD datasets (see https://cmr.earthdata.nasa.gov/search/granules.json?collection_concept_id=C1711961296-LPCLOUD&page_num=1&page_size=2000&sort_key=-entry_title)
                link = entry.Links.FirstOrDefault(l => l.Href.AbsolutePath.EndsWith("_dem.tif"));
                if (link == null)
                    throw new InvalidDataException("Cannot find Zip entry or dem.tif entry");
            }


            return new NasaDemFile(entry.ProducerGranuleId, entry.Boxes.First(), link.Href.AbsoluteUri);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Error parsing Nasa entry. File will be ignored: " + ex.Message);
            return null;
        }

    }

    private static List<DEMFileSource> GetSources(DEMDataSet dataSet, string indexFileName)
    {
        List<NasaDemFile> nasaDemFiles;
        using (var stream = File.OpenRead(indexFileName))
            nasaDemFiles = JsonSerializer.Deserialize(stream, CoreJsonContext.Default.ListNasaDemFile)
                ?? throw new InvalidDataException($"Empty granule index {indexFileName}.");
        var dataSetLocalDir = Path.GetDirectoryName(indexFileName)!;

        static BoundingBox GetBBox(string box)
        {
            // box is ymin xmin ymax xmax
            var coords = box.Split(' ').Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToArray();

            return new BoundingBox(coords[1], coords[3], coords[0], coords[2]);
        };

        return [.. nasaDemFiles.Select(file => new DEMFileSource()
        {
            BBox = GetBBox(file.Box),
            SourceFileName = file.GranuleId,
            SourceFileNameAbsolute = file.ZipFileLink,
            LocalFileName = Path.Combine(dataSetLocalDir, "Granules", FileNameFromGranuleId(file.GranuleId, dataSet))
        })];
    }

    public IEnumerable<DEMFileSource> GetCoveredFileSources(DEMDataSet dataset, BoundingBox bbox)
    {
        foreach (DEMFileSource source in this.GetFileSources(dataset))
        {
            if (bbox == null || source.BBox.Intersects(bbox))
            {
                yield return source;
            }
        }
    }

    public IEnumerable<DEMFileSource> GetFileSources(DEMDataSet dataSet)
    {
        if (!_cacheByDemName.TryGetValue(dataSet.Name, out var sources))
            throw new InvalidOperationException($"{nameof(SetupAsync)} must be called for dataset {dataSet.Name} first.");
        return sources;
    }

    public void Reset()
    {
        _cacheByDemName.Clear();
    }

    public async Task DownloadRasterFileAsync(DemFileReport report, DEMDataSet dataset, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!report.LocalName.EndsWith(".zip"))
            {
                await rasterDownloader.DownloadAsync(report.URL, report.LocalName, cancellationToken).ConfigureAwait(false);
                return;
            }

            // Zipped granule: keep only the DEM file of the archive (e.g. *_dem.tif), renamed as the local file
            var zipFileName = Path.Combine(Path.GetDirectoryName(report.LocalName)!, report.Source.SourceFileName);
            await rasterDownloader.DownloadAsync(report.URL, zipFileName, cancellationToken).ConfigureAwait(false);
            try
            {
                await using var archive = await ZipFile.OpenAsync(zipFileName, ZipArchiveMode.Read, cancellationToken).ConfigureAwait(false);
                var geoTiffFile = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(dataset.FileFormat.FileExtension, StringComparison.OrdinalIgnoreCase));
                if (geoTiffFile == null)
                {
                    this.logger.LogError($"Cannot find any {dataset.FileFormat.FileExtension} file into archive {report.LocalName}");
                    return;
                }
                // extracted next to the target first: a partial extraction never looks like a valid file
                string tempFile = HttpDownload.TempFileName(report.LocalName);
                await geoTiffFile.ExtractToFileAsync(tempFile, overwrite: true, cancellationToken).ConfigureAwait(false);
                File.Move(tempFile, report.LocalName, overwrite: true);
            }
            finally
            {
                File.Delete(zipFileName);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            this.logger.LogError(ex, $"Error while downloading raster {report.URL}: {ex.Message}");
            throw;
        }
    }

    private static string FileNameFromGranuleId(string granuleId, DEMDataSet dataSet)
    {
        var fileName = string.Concat(Path.GetFileNameWithoutExtension(granuleId), dataSet.FileFormat.FileExtension);
        return fileName;
    }
}
