using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DEM.Net.Core.Datasets;
using Microsoft.Extensions.Logging;

namespace DEM.Net.Core.Stac;

public class StacFileService(ILogger<StacFileService> logger, IHttpClientFactory httpClientFactory) : IDEMDataSetIndex
{
    private readonly ILogger<StacFileService> logger = logger;
    private readonly ConcurrentDictionary<string, List<DEMFileSource>> _cacheByDemName = new();
    private readonly SingleFlight _setup = new(keepCompleted: false); // the result lives in _cacheByDemName
    private readonly IHttpClientFactory httpClientFactory = httpClientFactory;

    public Task SetupAsync(DEMDataSet dataSet, string dataSetLocalDir, CancellationToken cancellationToken = default)
    {
        if (dataSet == null)
            throw new ArgumentNullException(nameof(dataSet), "Dataset is null.");
        if (dataSet.DataSource is not StacDataSource dataSource)
            throw new ArgumentException($"Dataset source is {dataSet.DataSource.DataSourceType}, only {nameof(DEMDataSourceType.Stac)} sources are supported by {nameof(StacFileService)}.", nameof(dataSet));

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
                    var links = await FetchCollectionAsync(dataSource).ConfigureAwait(false);
                    // an interrupted run never leaves a truncated index
                    await HttpDownload.WriteAllTextAtomicAsync(indexFileName, JsonSerializer.Serialize([.. links.Values], CoreJsonContext.Indented.ListStacDemFile), CancellationToken.None).ConfigureAwait(false);
                    logger.LogInformation($"{links.Count} entries written to index file {indexFileName}");
                }

                _cacheByDemName[dataSet.Name] = GetSources(dataSet, indexFileName);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, $"Error while setting up {dataSet.Name} STAC index: {ex.Message}");
                throw;
            }
        }, cancellationToken);
    }

    /// <summary>Fetches the collection page by page until no result is left.</summary>
    private async Task<Dictionary<StacDemFile, StacDemFile>> FetchCollectionAsync(StacDataSource dataSource)
    {
        logger.LogInformation($"Fetching items from collection {dataSource.Collection} to disk... This will be done once.");
        Uri baseUrl = new Uri(dataSource.Url);
        string? url = string.Concat(baseUrl, $"/collections/{dataSource.Collection}/items");
        var links = new Dictionary<StacDemFile, StacDemFile>(30000);
        var httpClient = httpClientFactory.CreateClient();
        int pageIndex = 0;
        while (url != null)
        {
            pageIndex++;
            logger.LogInformation($"Getting entries on page {pageIndex}... ({links.Count} assets collected so far)");

            var json = await httpClient.GetStringAsync(url).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(json))
                break;

            var result = CollectionItems.FromJson(json);
            if (result.Features.Count == 0)
                break;

            // Only retrieve bbox and dem file link (zip file), keep the most recent item of each file
            foreach (var newFile in result.Features.Select(f => GetStacDemFile(dataSource, f)).OfType<StacDemFile>())
            {
                if (!links.TryGetValue(newFile, out var existing) || existing.DateTime.CompareTo(newFile.DateTime) < 0)
                    links[newFile] = newFile;
            }

            url = result.Links.FirstOrDefault(l => l.Rel == Rel.Next)?.Href?.ToString();
        }
        return links;
    }

    private List<DEMFileSource> GetSources(DEMDataSet dataSet, string indexFileName)
    {
        List<StacDemFile> stacDemFiles;
        using (var stream = File.OpenRead(indexFileName))
            stacDemFiles = JsonSerializer.Deserialize(stream, CoreJsonContext.Default.ListStacDemFile)
                ?? throw new InvalidDataException($"Empty STAC index {indexFileName}.");
        var dataSetLocalDir = Path.GetDirectoryName(indexFileName)!;

        return [.. stacDemFiles.Select(file => new DEMFileSource()
        {
            BBox = new BoundingBox(file.Box[0], file.Box[2], file.Box[1], file.Box[3]),
            SourceFileName = file.FileId,
            SourceFileNameAbsolute = file.Href,
            LocalFileName = Path.Combine(dataSetLocalDir, "Features", file.FileId)
        })];
    }

    private StacDemFile? GetStacDemFile(StacDataSource dataSource, Feature f)
    {
        foreach (var asset in f.Assets)
        {
            if (dataSource.Filter(asset.Value))
            {
                return new StacDemFile(asset.Key, f.Bbox, asset.Value.Href.ToString(), f.Properties.Datetime);
            }
        }
        return null;
    }

    public async Task DownloadRasterFileAsync(DemFileReport report, DEMDataSet dataset, CancellationToken cancellationToken = default)
    {
        try
        {
            await HttpDownload.DownloadToFileAsync(httpClientFactory.CreateClient(), report.URL, report.LocalName, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, $"Error while downloading DEM file {report.URL}: {ex.Message}");
            throw;
        }
    }

    public IEnumerable<DEMFileSource> GetCoveredFileSources(DEMDataSet dataset, BoundingBox bbox)
    {
        foreach (DEMFileSource source in this.GetFileSources(dataset))
        {
            if (bbox == null || source.BBox.ReprojectTo(source.BBox.SRID, dataset.SRID).Intersects(bbox))
            {
                yield return source;
            }
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

}
