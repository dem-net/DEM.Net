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

using DEM.Net.Core.Configuration;
using DEM.Net.Core.Helpers;
using DEM.Net.Core.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core
{
    public class RasterService : IRasterDownloader
    {
        public const string APP_NAME = "DEM.Net";
        internal const string MANIFEST_DIR = "manifest";
        private readonly RasterIndexServiceResolver _rasterIndexServiceResolver;
        private readonly ILogger<RasterService> _logger;
        private readonly NamedMonitor monitor = new NamedMonitor();

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

            switch (fileFormat)
            {
                case DEMFileType.GEOTIFF: return new GeoTiff(filePath);
                case DEMFileType.SRTM_HGT: return new HGTFile(filePath);
                case DEMFileType.ASCIIGrid: return new ASCIIGridFile(filePath, gzip: false);
                case DEMFileType.ASCIIGridGzip: return new ASCIIGridFile(filePath, gzip: true);
                case DEMFileType.CF_NetCDF: return new NetCdfFile(filePath);
                default:
                    throw new NotImplementedException($"{fileFormat} file format not implemented.");
            }

        }

        public string GetLocalDEMPath(DEMDataSet dataset)
        {
            string path = null;
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
        public FileMetadata ParseMetadata(IRasterFile rasterFile, DEMFileDefinition format)
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

            if (force && _metadataCatalogCache.ContainsKey(localPath))
            {
                _metadataCatalogCache.TryRemove(localPath, out List<FileMetadata> removed);
            }
            Stopwatch stopwatch = Stopwatch.StartNew();

            if (_metadataCatalogCache.ContainsKey(localPath) == false)
            {
                var manifestDirectories = Directory.EnumerateDirectories(localPath, MANIFEST_DIR, SearchOption.AllDirectories);

                ConcurrentBag<FileMetadata> metaList = new ConcurrentBag<FileMetadata>();
                foreach (var manifestDirectory in manifestDirectories)
                {
                    var manifestFiles = Directory.EnumerateFiles(manifestDirectory, "*.json");

                    //foreach (var file in manifestFiles)
                    Parallel.ForEach(manifestFiles, file =>
                    {
                        string jsonContent = File.ReadAllText(file);

                        FileMetadata metadata = JsonConvert.DeserializeObject<FileMetadata>(jsonContent);
                        metadata.BoundingBox.SRID = dataset.SRID;
                        if (metadata.Version != FileMetadata.FILEMETADATA_VERSION)
                        {
                            metadata = FileMetadataMigrations.Migrate(this, _logger, metadata, _localDirectory, dataset);
                            File.WriteAllText(file, JsonConvert.SerializeObject(metadata, Formatting.Indented));
                        }
                        metaList.Add(metadata);
                    }
                    );

                }
                _metadataCatalogCache[localPath] = metaList.ToList();

            }

            if (logTimeSpent) // we avoid logging each time the data is requested, only needed on preload
                _logger.LogWarning($"{dataset.Name} metadata loaded in {stopwatch.ElapsedMilliseconds} ms");

            return _metadataCatalogCache[localPath];
        }

        /// <summary>
        /// Generate metadata files for fast in-memory indexing
        /// </summary>
        /// <param name="dataset">Dataset</param>
        /// <param name="deleteOnError">Deletes raster files on error</param>
        /// <param name="force">If true, force regeneration of all files. If false, only missing files will be generated.</param>
        /// <param name="maxDegreeOfParallelism">Set to 1 to force single thread execution (for debug purposes)</param>
        public void GenerateDirectoryMetadata(DEMDataSet dataset, bool force, bool deleteOnError = false, int maxDegreeOfParallelism = -1)
        {
            string directoryPath = GetLocalDEMPath(dataset);
            var files = Directory.EnumerateFiles(directoryPath, "*" + dataset.FileFormat.FileExtension, SearchOption.AllDirectories);
            ParallelOptions options = new ParallelOptions() { MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, maxDegreeOfParallelism == 0 ? -1 : maxDegreeOfParallelism) };
            //ParallelOptions options = new ParallelOptions() { MaxDegreeOfParallelism = 1 };
            Parallel.ForEach(files, options, file =>
             {
                 try
                 {
                     GenerateFileMetadata(file, dataset.FileFormat, force);
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
        }

        private string GetMetadataFileName(string rasterFileName, string outDirPath, string extension = ".json")
        {
            var fileTitle = Path.GetFileNameWithoutExtension(rasterFileName);
            return Path.Combine(outDirPath, fileTitle + extension);
        }
        private string GetManifestDirectory(string rasterFileName)
        {
            return Path.Combine(Path.GetDirectoryName(rasterFileName), MANIFEST_DIR);
        }
        private string GetMetadataFileName(string rasterFileName, string extension = ".json")
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
            string bmpPath = GetMetadataFileName(rasterFileName, outDirPath, ".bmp");
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
                if (File.Exists(bmpPath))
                {
                    File.Delete(bmpPath);
                }
            }

            // Json manifest
            if (File.Exists(jsonPath) == false)
            {
                Trace.TraceInformation($"Generating manifest for file {rasterFileName}.");

                FileMetadata metadata = this.ParseMetadata(rasterFileName, fileFormat);
                File.WriteAllText(jsonPath, JsonConvert.SerializeObject(metadata, Formatting.Indented));

                Trace.TraceInformation($"Manifest generated for file {rasterFileName}.");
            }

        }

        /// <summary>
        /// Generates a full report of all datasets to check size and number of downloaded tiles
        /// </summary>
        /// <returns>
        /// A string containing the report
        /// </returns>
        public string GenerateReportAsString()
        {
            StringBuilder sb = new StringBuilder();
            var reports = GenerateReport();

            // Get report for downloaded files
            foreach (var report in reports)
            {
                sb.AppendLine(report.ToString());
            }
            return sb.ToString();
        }

        /// <summary>
        /// Generates a full report of all datasets to check size and number of downloaded tiles
        /// </summary>
        /// <returns>
        /// A string containing the report
        /// </returns>
        public async Task<List<DatasetReport>> GenerateReportAsync()
        {
            StringBuilder sb = new StringBuilder();
            var tasks = new List<Task<DatasetReport>>();

            // Get report for downloaded files
            foreach (DEMDataSet dataset in DEMDataSet.RegisteredDatasets)
            {
                tasks.Add(Task.Run(() => GenerateReportSummary(dataset)));
            }

            var reports = await Task.WhenAll(tasks.ToArray());

            return reports.ToList();
        }
        /// <summary>
        /// Generates a full report of all datasets to check size and number of downloaded tiles
        /// </summary>
        /// <returns>
        /// A string containing the report
        /// </returns>
        public IEnumerable<DatasetReport> GenerateReport()
        {
            StringBuilder sb = new StringBuilder();

            // Get report for downloaded files
            foreach (DEMDataSet dataset in DEMDataSet.RegisteredDatasets)
            {
                yield return GenerateReportSummary(dataset);
            }
        }
        private DatasetReport GenerateReportSummary(DEMDataSet dataset)
        {
            List<DemFileReport> report = GenerateReport(dataset);
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
        public List<DemFileReport> GenerateReport(DEMDataSet dataSet, BoundingBox bbox = null)
        {
            List<DemFileReport> statusByFile = new List<DemFileReport>();

            var indexService = this._rasterIndexServiceResolver(dataSet.DataSource.DataSourceType);
            indexService.Setup(dataSet, GetLocalDEMPath(dataSet));

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

        public List<DemFileReport> GenerateReportForLocation(DEMDataSet dataSet, double lat, double lon)
        {

            var indexService = this._rasterIndexServiceResolver(dataSet.DataSource.DataSourceType);
            indexService.Setup(dataSet, GetLocalDEMPath(dataSet));

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


        /// <summary>
        /// Downloads a raster file using downloader configured for the dataset
        /// </summary>
        /// <param name="report">Report item return by GenerateReport methods</param>
        /// <param name="dataset"></param>
        public void DownloadRasterFile(DemFileReport report, DEMDataSet dataset)
        {
            var downloader = _rasterIndexServiceResolver(dataset.DataSource.DataSourceType);

            lock (monitor[report.URL])
            {
                if (!File.Exists(report.LocalName))
                {
                    _logger?.LogInformation($"Downloading file {report.URL}...");

                    downloader.DownloadRasterFile(report, dataset);

                    this.GenerateFileMetadata(report.LocalName, dataset.FileFormat, false);
                }
            }

        }


        /// <summary>
        /// Copy / paste from ASC similar function getHeightMap in bbox
        /// Goal : generate Hmap fitted to bbox full of no data values
        /// </summary>
        /// <param name="bbox"></param>
        /// <param name="metadata"></param>
        /// <param name="noDataValue"></param>
        /// <returns></returns>
        internal HeightMap GetVirtualHeightMapInBBox(BoundingBox bbox, FileMetadata metadata, float? noDataValue)
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
            var coords = new List<GeoPoint>(heightMap.Count);
            heightMap.BoundingBox = new BoundingBox(0, 0, 0, 0);

            for (int y = yNorth; y <= ySouth; y++)
            {
                double latitude = metadata.DataEndLat + (metadata.pixelSizeY * y);

                // bounding box
                if (y == yNorth)
                {
                    heightMap.BoundingBox.yMax = latitude;
                    heightMap.BoundingBox.xMin = metadata.DataStartLon + (metadata.pixelSizeX * xWest);
                    heightMap.BoundingBox.xMax = metadata.DataStartLon + (metadata.pixelSizeX * xEast);
                }
                if (y == ySouth)
                {
                    heightMap.BoundingBox.yMin = latitude;
                }

                for (int x = xWest; x <= xEast; x++)
                {
                    double longitude = metadata.DataStartLon + (metadata.pixelSizeX * x);

                    float heightValue = noDataValue ?? metadata.NoDataValueFloat;
                    heightMap.Minimum = Math.Min(heightMap.Minimum, heightValue);
                    heightMap.Maximum = Math.Max(heightMap.Maximum, heightValue);

                    coords.Add(new GeoPoint(latitude, longitude, heightValue));

                }
            }
            heightMap.BoundingBox.zMin = heightMap.Minimum;
            heightMap.BoundingBox.zMax = heightMap.Maximum;
            Debug.Assert(heightMap.Width * heightMap.Height == coords.Count);

            heightMap.Coordinates = coords;
            return heightMap;
        }
    }


}
