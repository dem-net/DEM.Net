using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DEM.Net.Core.Datasets;
using Microsoft.Extensions.Logging;

namespace DEM.Net.Core.RemoteFile;

/// <summary>
/// Index for <see cref="RemoteFileDataSource"/>: a single file source covering the declared extent, downloaded on
/// demand. Concurrent requests share one download (<see cref="RasterService.DownloadRasterFileAsync"/>).
/// </summary>
public class RemoteFileService(ILogger<RemoteFileService> logger, IHttpClientFactory httpClientFactory) : IDEMDataSetIndex
{
    private readonly ILogger<RemoteFileService> _logger = logger;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ConcurrentDictionary<string, DEMFileSource> _sourceByDemName = new();

    public Task SetupAsync(DEMDataSet dataSet, string dataSetLocalDir, CancellationToken cancellationToken = default)
    {
        var dataSource = GetDataSource(dataSet);
        _sourceByDemName.GetOrAdd(dataSet.Name, _ => new DEMFileSource
        {
            BBox = new BoundingBox(dataSource.Extent.xMin, dataSource.Extent.xMax, dataSource.Extent.yMin, dataSource.Extent.yMax) { SRID = dataSet.SRID },
            SourceFileName = dataSource.FileName,
            SourceFileNameAbsolute = dataSource.Url,
            LocalFileName = Path.Combine(dataSetLocalDir, dataSource.FileName)
        });
        return Task.CompletedTask;
    }

    private static RemoteFileDataSource GetDataSource(DEMDataSet dataSet)
    {
        if (dataSet == null)
            throw new ArgumentNullException(nameof(dataSet), "Dataset is null.");
        return dataSet.DataSource as RemoteFileDataSource
            ?? throw new ArgumentException($"Dataset source is {dataSet.DataSource.DataSourceType}, only {nameof(DEMDataSourceType.RemoteFile)} sources are supported by {nameof(RemoteFileService)}.", nameof(dataSet));
    }

    public async Task DownloadRasterFileAsync(DemFileReport report, DEMDataSet dataset, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation($"Downloading {dataset.Name} from {report.URL}... This will be done once, the file can be several GB.");
            var client = _httpClientFactory.CreateClient();
            // a multi GB file: HttpClient's default 100 s timeout must not abort the transfer
            client.Timeout = Timeout.InfiniteTimeSpan;
            await HttpDownload.DownloadToFileAsync(client, report.URL, report.LocalName, cancellationToken,
                request => request.Headers.UserAgent.ParseAdd(HttpDownload.UserAgent)).ConfigureAwait(false);
            _logger.LogInformation($"{dataset.Name} downloaded to {report.LocalName}.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, $"Error while downloading DEM file {report.URL}: {ex.Message}");
            throw;
        }
    }

    public IEnumerable<DEMFileSource> GetFileSources(DEMDataSet dataset)
    {
        if (!_sourceByDemName.TryGetValue(dataset.Name, out var source))
            throw new InvalidOperationException($"{nameof(SetupAsync)} must be called for dataset {dataset.Name} first.");
        yield return source;
    }

    public IEnumerable<DEMFileSource> GetCoveredFileSources(DEMDataSet dataset, BoundingBox bbox)
    {
        foreach (DEMFileSource source in GetFileSources(dataset))
        {
            if (bbox == null || source.BBox.Intersects(bbox))
                yield return source;
        }
    }

    public void Reset()
    {
        _sourceByDemName.Clear();
    }
}
