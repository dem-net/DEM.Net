// RasterService.cs
//
// Author:
//       Xavier Fischer
//
// Copyright (c) 2019 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the right
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
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DEM.Net.Core.Configuration;
using DEM.Net.Core.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProtoBuf;

namespace DEM.Net.Core;

public class RasterService : IRasterDownloader
{
    public const string APP_NAME = "DEM.Net";
    internal const string MANIFEST_DIR = "manifest";
    private readonly RasterIndexServiceResolver _rasterIndexServiceResolver;
    private readonly ILogger<RasterService> _logger;
    // concurrent requests for the same missing file share one download
    private readonly SingleFlight _downloads = new SingleFlight(keepCompleted: false);
    // one manifest catalog build at a time per dataset directory
    private readonly ConcurrentDictionary<string, Lock> _manifestLocks = new ConcurrentDictionary<string, Lock>(StringComparer.Ordinal);

    private string _localDirectory;
    private ConcurrentDictionary<string, List<FileMetadata>> _metadataCatalogCache = new ConcurrentDictionary<string, List<FileMetadata>>();

    public string LocalDirectory
    {
        get { return _localDirectory; }
    }

    public RasterService(RasterIndexServiceResolver rasterResolver, IOptions<DEMNetOptions> options, ILogger<RasterService> logger = null)
    {
        this._logger = logger;
        this._rasterIndexServiceResolver = rasterResolver;
        string directoryFromOptions = options?.Value?.LocalDirectory;
        _localDirectory = string.IsNullOrWhiteSpace(directoryFromOptions) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), APP_NAME)
            : directoryFromOptions;
        if (!Directory.Exists(_localDirectory))
            Directory.CreateDirectory(_localDirectory);

        _logger.LogInformation($"Local data directory : {_localDirectory}");

        _metadataCatalogCache = new ConcurrentDictionary<string, List<FileMetadata>>();
    }

    /// <summary>
    /// Change directory to user specified directory. Causes local caches to reset.
    /// Directory will be created if not existing
    /// </summary>
    /// <param name="localDirectory"></param>
    public void SetLocalDirectory(string localDirectory)
    {
        localDirectory = Path.Combine(localDirectory, APP_NAME);
        if (_localDirectory != null && _localDirectory != localDirectory)
        {
            _localDirectory = localDirectory;
            if (!Directory.Exists(_localDirectory))
                Directory.CreateDirectory(_localDirectory);

            _metadataCatalogCache = new ConcurrentDictionary<string, List<FileMetadata>>();
            foreach (var value in Enum.GetValues(typeof(Datasets.DEMDataSourceType)))
            {
                _rasterIndexServiceResolver((Datasets.DEMDataSourceType)value).Reset();
            }

        }
    }

    /// <summary>
    /// Open specified file
    /// </summary>
    /// <param name="filePath">If path is rooted (full file name), the specified file will be openened,
    /// otherwise the file path will be relative to <see cref="LocalDirectory"/></param>
    /// <param name="fileFormat"><see cref="DEMFileType"/> enumeration indicating the file type</param>
    /// <returns><see cref="IRasterFile"/> interface for accessing file contents</returns>
    public IRasterFile OpenFile(string filePath, DEMFileType fileFormat)
    {

        if (!Path.IsPathRooted(filePath))
        {
            filePath = Path.Combine(_localDirectory, filePath);
        }

        return fileFormat switch
        {
            DEMFileType.GEOTIFF => new GeoTiff(filePath),
            DEMFileType.SRTM_HGT => new HGTFile(filePath),
            DEMFileType.ASCIIGrid => new ASCIIGridFile(filePath, gzip: false),
            DEMFileType.ASCIIGridGzip => new ASCIIGridFile(filePath, gzip: true),
            DEMFileType.CF_NetCDF => new NetCdfFile(filePath),
            DEMFileType.TerrainRGB => new TerrainRGBFile(filePath),
            DEMFileType.Terrarium => new TerrariumFile(filePath),
            _ => throw new NotImplementedException($"{fileFormat} file format not implemented."),
        };
    }

    public string GetLocalDEMPath(DEMDataSet dataset)
    {
        string path;
        if (dataset.DataSource.DataSourceType == Datasets.DEMDataSourceType.LocalFileSystem)
        {
            if (Path.IsPathRooted(dataset.DataSource.IndexFilePath))
            {
                path = dataset.DataSource.IndexFilePath;
            }
            else
            {
                path = Path.Combine(_localDirectory, dataset.Name);
            }
        }
        else
        {
            path = Path.Combine(_localDirectory, dataset.Name);
        }

        return path;
    }
    public string GetLocalDEMFilePath(DEMDataSet dataset, string fileTitle)
    {
        return Path.Combine(GetLocalDEMPath(dataset), fileTitle);
    }

    /// <summary>Reads a json manifest (also the ones written by former versions)</summary>
    internal static FileMetadata ReadMetadataFile(string path)
    {
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize(stream, CoreJsonContext.Default.FileMetadata)
            ?? throw new InvalidDataException($"Empty metadata file {path}.");
    }

    internal static void WriteMetadataFile(string path, FileMetadata metadata)
    {
        using var stream = File.Create(path);
        JsonSerializer.Serialize(stream, metadata, CoreJsonContext.Indented.FileMetadata);
    }

    public static FileMetadata ParseMetadata(IRasterFile rasterFile, DEMFileDefinition format)
    {
        return rasterFile.ParseMetaData(format);
    }
    public FileMetadata ParseMetadata(string fileName, DEMFileDefinition fileFormat)
    {
        FileMetadata metadata = null;

        fileName = Path.GetFullPath(fileName);

        using (IRasterFile rasterFile = OpenFile(fileName, fileFormat.Type))
        {
            metadata = rasterFile.ParseMetaData(fileFormat);
        }

        Uri fullPath = new Uri(metadata.Filename, UriKind.Absolute);
        Uri relRoot = new Uri(Path.GetFullPath(_localDirectory) + Path.DirectorySeparatorChar, UriKind.Absolute);

        metadata.Filename = Uri.UnescapeDataString(relRoot.MakeRelativeUri(fullPath).ToString());
        return metadata;
    }

    public List<FileMetadata> LoadManifestMetadata(DEMDataSet dataset, bool force, bool logTimeSpent = false)
    {
        string localPath = GetLocalDEMPath(dataset);
        var globalManifestPath = Path.Combine(localPath, "globalmanifest.bin");

        Stopwatch stopwatch = Stopwatch.StartNew();

        if (!force && _metadataCatalogCache.TryGetValue(localPath, out var cached))
            return cached;

        // Building the catalog reads (and may migrate) every manifest, then writes the protobuf index:
        // concurrent builds would race on these files.
        lock (_manifestLocks.GetOrAdd(localPath, _ => new Lock()))
        {
            if (force)
            {
                // also when not loaded in memory yet: the protobuf index on disk may be stale
                _metadataCatalogCache.TryRemove(localPath, out _);
                if (File.Exists(globalManifestPath))
                {
                    File.Delete(globalManifestPath);
                }
            }
            else if (_metadataCatalogCache.TryGetValue(localPath, out cached))
                return cached;

            if (File.Exists(globalManifestPath))
            {
                List<FileMetadata> indexedMetadata;
                using (var inStream = new FileStream(globalManifestPath, FileMode.Open, FileAccess.Read))
                {
                    indexedMetadata = Serializer.Deserialize<List<FileMetadata>>(inStream);
                }

                if (indexedMetadata.All(m => !IsMetadataStale(m, dataset)))
                {
                    _logger.LogInformation($"{dataset.Name} metadata read from protobuf index");
                    _metadataCatalogCache[localPath] = indexedMetadata;
                    return _metadataCatalogCache[localPath];
                }

                // Bounds were computed with another registration mode: rebuild from the json manifests (migrated below)
                _logger.LogWarning($"{dataset.Name} protobuf index is out of date (dataset registration is {dataset.FileFormat.Registration}), rebuilding it.");
                File.Delete(globalManifestPath);
            }

            var manifestDirectories = Directory.EnumerateDirectories(localPath, MANIFEST_DIR, SearchOption.AllDirectories);

            ConcurrentBag<FileMetadata> metaList = new ConcurrentBag<FileMetadata>();
            foreach (var manifestDirectory in manifestDirectories)
            {
                var manifestFiles = Directory.EnumerateFiles(manifestDirectory, "*.json");

                //foreach (var file in manifestFiles)
                Parallel.ForEach(manifestFiles, file =>
                {
                    FileMetadata metadata = ReadMetadataFile(file);
                    metadata.BoundingBox.SRID = dataset.SRID;
                    if (IsMetadataStale(metadata, dataset))
                    {
                        metadata = FileMetadataMigrations.Migrate(this, _logger, metadata, _localDirectory, dataset);
                        WriteMetadataFile(file, metadata);
                    }
                    metaList.Add(metadata);
                }
                );

            }

            var catalog = metaList.ToList();
            string tempIndexPath = HttpDownload.TempFileName(globalManifestPath);
            using (var outStream = new FileStream(tempIndexPath, FileMode.Create, FileAccess.Write))
            {
                Serializer.Serialize(outStream, catalog);
            }
            File.Move(tempIndexPath, globalManifestPath, overwrite: true);
            _logger.LogInformation($"{dataset.Name} metadata written to protobuf index");
            _metadataCatalogCache[localPath] = catalog;
        }

        if (logTimeSpent) // we avoid logging each time the data is requested, only needed on preload
            _logger.LogWarning($"{dataset.Name} metadata loaded in {stopwatch.ElapsedMilliseconds} ms");

        return _metadataCatalogCache[localPath];
    }

    /// <summary>
    /// Metadata must be regenerated when its version is outdated, or when the dataset registration mode changed
    /// (data bounds depend on it, see <see cref="GeoTiff.ParseMetaData"/>).
    /// </summary>
    private static bool IsMetadataStale(FileMetadata metadata, DEMDataSet dataset)
        => metadata.Version != FileMetadata.FILEMETADATA_VERSION
        || metadata.FileFormat == null
        || metadata.FileFormat.Registration != dataset.FileFormat.Registration;

    /// <summary>
    /// Generate metadata files for fast in-memory indexing
    /// </summary>
    /// <param name="dataset">Dataset</param>
    /// <param name="deleteOnError">Deletes raster files on error</param>
    /// <param name="force">If true, force regeneration of all files. If false, only missing files will be generated.</param>
    public void GenerateDirectoryMetadata(DEMDataSet dataset, bool force, bool deleteOnError = false)
    {
        string directoryPath = GetLocalDEMPath(dataset);
        var files = Directory.GetFiles(directoryPath, "*" + dataset.FileFormat.FileExtension, SearchOption.AllDirectories);
        ParallelOptions options = new ParallelOptions() { MaxDegreeOfParallelism = 2 };
        //ParallelOptions options = new ParallelOptions() { MaxDegreeOfParallelism = 1 };
        int count = 0;
        Parallel.ForEach(files, options, file =>
         {
             try
             {
                 GenerateFileMetadata(file, dataset.FileFormat, force);
                 Interlocked.Increment(ref count);

                 if (count % 100 == 0)
                 {
                     _logger?.LogInformation($"Generating metadata {count}/{files.Length} ({count * 1d / files.Length:P1})");
                 }
             }
             catch (Exception exFile)
             {
                 _logger?.LogError(exFile, $"Error while generating metadata for file {file} : {exFile.Message}");
                 try
                 {
                     if (deleteOnError)
                     {
                         var jsonFile = GetMetadataFileName(file, GetManifestDirectory(file), ".json");
                         File.Delete(jsonFile);
                         File.Delete(file);
                     }
                 }
                 catch (Exception)
                 {

                     throw;
                 }
             }
         });

        LoadManifestMetadata(dataset, force);
    }

    private static string GetMetadataFileName(string rasterFileName, string outDirPath, string extension = ".json")
    {
        var fileTitle = Path.GetFileNameWithoutExtension(rasterFileName);
        return Path.Combine(outDirPath, fileTitle + extension);
    }
    private static string GetManifestDirectory(string rasterFileName)
    {
        return Path.Combine(Path.GetDirectoryName(rasterFileName), MANIFEST_DIR);
    }
    private static string GetMetadataFileName(string rasterFileName, string extension = ".json")
    {
        string outDirPath = GetManifestDirectory(rasterFileName);
        return GetMetadataFileName(rasterFileName, outDirPath, extension);
    }

    /// <summary>
    /// Generates a <see cref="FileMetadata"/> as JSON file containing raster file information.
    /// This metadata is used for fast indexing, preventing to open every raster file when performing spatial queries
    /// </summary>
    /// <param name="rasterFileName">Local file name</param>
    /// <param name="fileFormat">File format definition, see <see cref="DEMFileDefinition"/></param>
    /// <param name="force">If true, metadata will be replaced, if false the metadata will be generated only if the JSON file does not exists</param>
    public void GenerateFileMetadata(string rasterFileName, DEMFileDefinition fileFormat, bool force)
    {
        if (!File.Exists(rasterFileName))
            throw new FileNotFoundException($"File {rasterFileName} does not exists !");
        string outDirPath = GetManifestDirectory(rasterFileName);
        string jsonPath = GetMetadataFileName(rasterFileName, outDirPath, ".json");


        // Output directory "manifest"
        if (!Directory.Exists(outDirPath))
        {
            Directory.CreateDirectory(outDirPath);
        }

        if (force)
        {
            if (File.Exists(jsonPath))
            {
                File.Delete(jsonPath);
            }
        }

        // Json manifest
        if (File.Exists(jsonPath) == false)
        {
            Trace.TraceInformation($"Generating manifest for file {rasterFileName}.");

            FileMetadata metadata = this.ParseMetadata(rasterFileName, fileFormat);
            // a concurrent catalog build must never read a partial manifest
            string tempJsonPath = HttpDownload.TempFileName(jsonPath); // the same file may be indexed concurrently
            WriteMetadataFile(tempJsonPath, metadata);
            File.Move(tempJsonPath, jsonPath, overwrite: true);

            Trace.TraceInformation($"Manifest generated for file {rasterFileName}.");
        }

    }

    /// <summary>
    /// Generates a full report of all datasets to check size and number of downloaded tiles
    /// </summary>
    /// <returns>
    /// A string containing the report
    /// </returns>
    public async Task<string> GenerateReportAsStringAsync(CancellationToken cancellationToken = default)
    {
        StringBuilder sb = new StringBuilder();
        foreach (var report in await GenerateReportAsync(cancellationToken).ConfigureAwait(false))
        {
            sb.AppendLine(report.ToString());
        }
        return sb.ToString();
    }

    /// <summary>
    /// Generates a full report of all datasets to check size and number of downloaded tiles
    /// </summary>
    public async Task<List<DatasetReport>> GenerateReportAsync(CancellationToken cancellationToken = default)
    {
        var reports = await Task.WhenAll(DEMDataSet.RegisteredDatasets
            .Select(dataset => GenerateReportSummaryAsync(dataset, cancellationToken))).ConfigureAwait(false);
        return reports.ToList();
    }

    private async Task<DatasetReport> GenerateReportSummaryAsync(DEMDataSet dataset, CancellationToken cancellationToken)
    {
        List<DemFileReport> report = await GenerateReportAsync(dataset, null, cancellationToken).ConfigureAwait(false);
        int totalFiles = report.Count;
        int downloadedCount = report.Count(rpt => rpt.IsExistingLocally);
        int isMetadataGeneratedCount = report.Count(rpt => rpt.IsMetadataGenerated);
        int isnotMetadataGeneratedCount = report.Count(rpt => !rpt.IsMetadataGenerated);

        var fileSizeBytes = FileSystem.GetDirectorySize(GetLocalDEMPath(dataset), "*" + dataset.FileFormat.FileExtension);
        var fileSizeMB = Math.Round(fileSizeBytes / 1024f / 1024f, 2);

        // rule of 3 to evaluate total size

        var totalfileSizeGB = Math.Round((totalFiles * fileSizeMB / downloadedCount) / 1024f, 2);
        var remainingfileSizeGB = Math.Round(totalfileSizeGB - fileSizeMB / 1024f, 2);

        DatasetReport reportSummary = new DatasetReport()
        {
            DatasetName = dataset.Name
            ,
            TotalFiles = totalFiles
            ,
            DownloadedFiles = downloadedCount
            ,
            DownloadedSizeMB = fileSizeMB
            ,
            FilesWithMetadata = isMetadataGeneratedCount
            ,
            RemainingSizeGB = remainingfileSizeGB
            ,
            TotalSizeGB = totalfileSizeGB
            ,
            DowloadedPercent = Math.Round(downloadedCount * 100d / totalFiles, 2)
        };

        return reportSummary;
    }

    /// <summary>
    /// Compare LST file and local directory and generates dictionary with key : remoteFile and value = true if file is present and false if it is not downloaded
    /// </summary>
    /// <param name="dataSet">DEM dataset information</param>
    /// <param name="bbox">Bbox for filtering</param>
    /// <returns>A Dictionnary</returns>
    public async Task<List<DemFileReport>> GenerateReportAsync(DEMDataSet dataSet, BoundingBox bbox = null, CancellationToken cancellationToken = default)
    {
        List<DemFileReport> statusByFile = new List<DemFileReport>();

        var indexService = await GetIndexServiceAsync(dataSet, cancellationToken).ConfigureAwait(false);

        if (bbox == null)
        {
            // All sources
            foreach (DEMFileSource source in indexService.GetFileSources(dataSet))
            {
                statusByFile.Add(new DemFileReport()
                {
                    IsExistingLocally = File.Exists(source.LocalFileName),
                    IsMetadataGenerated = File.Exists(GetMetadataFileName(source.LocalFileName, ".json")),
                    LocalName = source.LocalFileName,
                    URL = source.SourceFileNameAbsolute,
                    Source = source
                });

            }
        }
        else
        {
            // only sources intersecting bbox
            foreach (DEMFileSource source in indexService.GetCoveredFileSources(dataSet, bbox))
            {
                statusByFile.Add(new DemFileReport()
                {
                    IsExistingLocally = File.Exists(source.LocalFileName),
                    IsMetadataGenerated = File.Exists(GetMetadataFileName(source.LocalFileName, ".json")),
                    LocalName = source.LocalFileName,
                    URL = source.SourceFileNameAbsolute,
                    Source = source
                });

            }
        }

        return statusByFile;
    }

    public async Task<List<DemFileReport>> GenerateReportForLocationAsync(DEMDataSet dataSet, double lat, double lon, CancellationToken cancellationToken = default)
    {
        var indexService = await GetIndexServiceAsync(dataSet, cancellationToken).ConfigureAwait(false);

        if (dataSet.DataSource.DataSourceType == Datasets.DEMDataSourceType.XyzTiles)
        {
            // Tile pyramids only list downloaded tiles: compute the tiles around the location instead
            // (one pixel around, adjacent tiles are needed for interpolation at tile edges).
            var pixelDegrees = 360d / ((1L << ((Datasets.XyzTileDataSource)dataSet.DataSource).Zoom) * ((Datasets.XyzTileDataSource)dataSet.DataSource).TileSize);
            return indexService.GetCoveredFileSources(dataSet, BoundingBox.AroundPoint(lat, lon, pixelDegrees))
                .Select(source => new DemFileReport()
                {
                    IsExistingLocally = File.Exists(source.LocalFileName),
                    IsMetadataGenerated = File.Exists(GetMetadataFileName(source.LocalFileName, ".json")),
                    LocalName = source.LocalFileName,
                    URL = source.SourceFileNameAbsolute,
                    Source = source
                })
                .ToList();
        }

        if (dataSet.FileFormat.Registration == DEMFileRegistrationMode.Cell)
        {
            var size = 1d / dataSet.PointsPerDegree;
            var bbox = BoundingBox.AroundPoint(lat, lon, size);
            var sources = indexService.GetFileSources(dataSet).Where(source => source.BBox.Intersects(bbox))
                .Select(source => new DemFileReport()
                {
                    IsExistingLocally = File.Exists(source.LocalFileName),
                    IsMetadataGenerated = File.Exists(GetMetadataFileName(source.LocalFileName, ".json")),
                    LocalName = source.LocalFileName,
                    URL = source.SourceFileNameAbsolute,
                    Source = source
                })
                .ToList();
            return sources;
        }
        else
        {
            var sources = indexService.GetFileSources(dataSet)
                        .Where(source => source.BBox.Intersects(lat, lon))
                        .Select(source => new DemFileReport()
                        {
                            IsExistingLocally = File.Exists(source.LocalFileName),
                            IsMetadataGenerated = File.Exists(GetMetadataFileName(source.LocalFileName, ".json")),
                            LocalName = source.LocalFileName,
                            URL = source.SourceFileNameAbsolute,
                            Source = source
                        })
                        .ToList();
            return sources;
        }
    }


    /// <summary>Index service of the dataset, set up (its remote index is downloaded once).</summary>
    private async Task<IDEMDataSetIndex> GetIndexServiceAsync(DEMDataSet dataSet, CancellationToken cancellationToken)
    {
        var indexService = this._rasterIndexServiceResolver(dataSet.DataSource.DataSourceType);
        await indexService.SetupAsync(dataSet, GetLocalDEMPath(dataSet), cancellationToken).ConfigureAwait(false);
        return indexService;
    }

    /// <summary>
    /// Downloads a raster file using downloader configured for the dataset, and generates its metadata.
    /// Concurrent calls for the same file share one download.
    /// </summary>
    /// <param name="report">Report item return by GenerateReport methods</param>
    /// <param name="dataset"></param>
    public Task DownloadRasterFileAsync(DemFileReport report, DEMDataSet dataset, CancellationToken cancellationToken = default)
    {
        if (File.Exists(report.LocalName))
            return Task.CompletedTask;

        var downloader = _rasterIndexServiceResolver(dataset.DataSource.DataSourceType);
        // the shared download is not cancelled by one caller: other requests may be waiting for the same file
        return _downloads.RunAsync(report.LocalName, async () =>
        {
            if (!File.Exists(report.LocalName))
            {
                await downloader.DownloadRasterFileAsync(report, dataset, CancellationToken.None).ConfigureAwait(false);
                this.GenerateFileMetadata(report.LocalName, dataset.FileFormat, false);
            }
        }, cancellationToken);
    }


    /// <summary>
    /// Copy / paste from ASC similar function getHeightMap in bbox
    /// Goal : generate Hmap fitted to bbox full of no data values
    /// </summary>
    /// <param name="bbox"></param>
    /// <param name="metadata"></param>
    /// <param name="noDataValue"></param>
    /// <returns></returns>
    internal static HeightMap GetVirtualHeightMapInBBox(BoundingBox bbox, FileMetadata metadata, float? noDataValue)
    {

        int registrationOffset = metadata.FileFormat.Registration == DEMFileRegistrationMode.Grid ? 1 : 0;

        int yNorth = (int)Math.Floor((bbox.yMax - metadata.PhysicalEndLat) / metadata.pixelSizeY);
        int ySouth = (int)Math.Ceiling((bbox.yMin - metadata.PhysicalEndLat) / metadata.pixelSizeY);
        int xWest = (int)Math.Floor((bbox.xMin - metadata.PhysicalStartLon) / metadata.pixelSizeX);
        int xEast = (int)Math.Ceiling((bbox.xMax - metadata.PhysicalStartLon) / metadata.pixelSizeX);

        xWest = Math.Max(0, xWest);
        xEast = Math.Min(metadata.Width - 1, xEast) - registrationOffset;
        yNorth = Math.Max(0, yNorth);
        ySouth = Math.Min(metadata.Height - 1, ySouth) - registrationOffset;

        HeightMap heightMap = new HeightMap(xEast - xWest + 1, ySouth - yNorth + 1);
        heightMap.Count = heightMap.Width * heightMap.Height;
        var coords = new GeoPoint[heightMap.Count];
        int coordIndex = 0;
        heightMap.BoundingBox = new BoundingBox(0, 0, 0, 0);

        // Same sample positions as real tiles (GeoTiff.GetHeightMapInBBox): cell-registered rasters are sampled
        // at pixel centers, otherwise virtual and real tiles are half a pixel apart and cannot be stitched.
        bool isCell = metadata.FileFormat.Registration == DEMFileRegistrationMode.Cell;
        double endLat = isCell ? metadata.DataEndLat + metadata.pixelSizeY / 2d : metadata.DataEndLat;
        double startLon = isCell ? metadata.DataStartLon + metadata.pixelSizeX / 2d : metadata.DataStartLon;

        for (int y = yNorth; y <= ySouth; y++)
        {
            double latitude = endLat + (metadata.pixelSizeY * y);

            // bounding box
            if (y == yNorth)
            {
                heightMap.BoundingBox.yMax = latitude;
                heightMap.BoundingBox.xMin = startLon + (metadata.pixelSizeX * xWest);
                heightMap.BoundingBox.xMax = startLon + (metadata.pixelSizeX * xEast);
            }
            if (y == ySouth)
            {
                heightMap.BoundingBox.yMin = latitude;
            }

            for (int x = xWest; x <= xEast; x++)
            {
                double longitude = startLon + (metadata.pixelSizeX * x);

                float heightValue = noDataValue ?? metadata.NoDataValueFloat;
                heightMap.Minimum = Math.Min(heightMap.Minimum, heightValue);
                heightMap.Maximum = Math.Max(heightMap.Maximum, heightValue);

                coords[coordIndex++] = new GeoPoint(latitude, longitude, heightValue);

            }
        }
        heightMap.BoundingBox.zMin = heightMap.Minimum;
        heightMap.BoundingBox.zMax = heightMap.Maximum;
        Debug.Assert(coordIndex == coords.Length);

        heightMap.Coordinates = coords;
        return heightMap;
    }
}
