using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DEM.Net.Core;

public class LocalFileSystemIndex : IDEMDataSetIndex
{
    private ConcurrentDictionary<string, List<DEMFileSource>> _cacheByDemName;

    private ConcurrentDictionary<string, string> _directoryPerDataset;
    private readonly RasterService _rasterService;
    private readonly ILogger<LocalFileSystemIndex> _logger;
    public LocalFileSystemIndex(ILogger<LocalFileSystemIndex> logger, RasterService rasterService)
    {
        _logger = logger;
        _cacheByDemName = new ConcurrentDictionary<string, List<DEMFileSource>>();
        _directoryPerDataset = new ConcurrentDictionary<string, string>();
        _rasterService = rasterService;
    }


    public Task DownloadRasterFileAsync(DemFileReport report, DEMDataSet dataset, CancellationToken cancellationToken = default)
        => throw new NotSupportedException($"{dataset.Name} is a local dataset: its files cannot be downloaded ({report.LocalName} is missing).");

    public IEnumerable<DEMFileSource> GetCoveredFileSources(DEMDataSet dataset, BoundingBox bbox)
    {
        foreach (DEMFileSource source in _cacheByDemName[dataset.Name])
        {
            if (bbox == null || source.BBox.Intersects(bbox))
            {
                yield return source;
            }
        }
    }

    public IEnumerable<DEMFileSource> GetFileSources(DEMDataSet dataset)
    {
        string datasetPath = _rasterService.GetLocalDEMPath(dataset);
        foreach (var file in Directory.GetFiles(datasetPath, "*" + dataset.FileFormat.FileExtension, SearchOption.AllDirectories))
        {
            var metadataPath = Path.Combine(Path.GetDirectoryName(file), RasterService.MANIFEST_DIR, Path.ChangeExtension(Path.GetFileName(file), ".json"));
            if (!File.Exists(metadataPath))
            {
                _rasterService.GenerateFileMetadata(file, dataset.FileFormat, false);
            }


            FileMetadata metadata = RasterService.ReadMetadataFile(metadataPath);
            metadata.BoundingBox.SRID = dataset.SRID;
            yield return new DEMFileSource()
            {
                BBox = metadata.BoundingBox
                 ,
                LocalFileName = file
            };


        }
    }

    public void Reset()
    {
        _cacheByDemName.Clear();
        _directoryPerDataset.Clear();
    }

    /// <remarks>Local files only: runs synchronously.</remarks>
    public Task SetupAsync(DEMDataSet dataSet, string dataSetLocalDir, CancellationToken cancellationToken = default)
    {
        Setup(dataSet, dataSetLocalDir);
        return Task.CompletedTask;
    }

    private void Setup(DEMDataSet dataSet, string dataSetLocalDir)
    {
        try
        {

            if (dataSet == null)
                throw new ArgumentNullException(nameof(dataSet), "Dataset is null.");
            if (!Directory.Exists(dataSetLocalDir))
                throw new ArgumentNullException(nameof(dataSet), $"Directory {dataSetLocalDir} does not exist.");

            if (_cacheByDemName == null)
            {
                _cacheByDemName = new ConcurrentDictionary<string, List<DEMFileSource>>();
            }
            if (_cacheByDemName.ContainsKey(dataSet.Name))
                return;

            _cacheByDemName[dataSet.Name] = GetFileSources(dataSet).ToList();

            _directoryPerDataset[dataSet.Name] = dataSetLocalDir;

            _logger?.LogInformation($"Setup for {dataSet.Name} dataset.");


        }
        catch (Exception ex)
        {
            _logger?.LogError("Unhandled exception: " + ex.Message);
            _logger?.LogInformation(ex.ToString());
            throw;
        }
    }
}
