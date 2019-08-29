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

using DEM.Net.Core.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core
{
    public class RasterService : IRasterService
    {
        const string APP_NAME = "DEM.Net";
        const string MANIFEST_DIR = "manifest";
        private readonly IGDALVRTFileService _gdalVrtService;
        private readonly ILogger<RasterService> _logger;

        private readonly string _localDirectory;
        private Dictionary<string, List<FileMetadata>> _metadataCatalogCache = new Dictionary<string, List<FileMetadata>>();

        public string LocalDirectory
        {
            get { return _localDirectory; }
        }

        public RasterService(IGDALVRTFileService gdalVrtService, ILogger<RasterService> logger = null)
        {
            _logger = logger;
            _gdalVrtService = gdalVrtService;
            //_localDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), APP_NAME);
            _localDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), APP_NAME);
            if (!Directory.Exists(_localDirectory))
                Directory.CreateDirectory(_localDirectory);

            _metadataCatalogCache = new Dictionary<string, List<FileMetadata>>();
        }

        /// <summary>
        /// Open specified file
        /// </summary>
        /// <param name="filePath">If path is rooted (full file name), the specified file will be openened,
        /// otherwise the file path will be relative to <see cref="LocalDirectory"/></param>
        /// <param name="fileFormat"><see cref="DEMFileFormat"/> enumeration indicating the file type</param>
        /// <returns><see cref="IRasterFile"/> interface for accessing file contents</returns>
        public IRasterFile OpenFile(string filePath, DEMFileFormat fileFormat)
        {

            if (!Path.IsPathRooted(filePath))
            {
                filePath = Path.Combine(_localDirectory, filePath);
            }

            if (fileFormat.Name == DEMFileFormat.GEOTIFF.Name)
            {
                return new GeoTiff(filePath);
            }
            else if (fileFormat.Name == DEMFileFormat.SRTM_HGT.Name)
            {
                return new HGTFile(filePath);
            }
            else
                throw new NotImplementedException($"{fileFormat} file format not implemented.");

        }

        public string GetLocalDEMPath(DEMDataSet dataset)
        {
            return dataset.DataSource.IsGlobalFile ?
                        Path.GetDirectoryName(dataset.DataSource.IndexFilePath)
                        : Path.Combine(_localDirectory, dataset.Name);
        }
        public string GetLocalDEMFilePath(DEMDataSet dataset, string fileTitle)
        {
            return Path.Combine(GetLocalDEMPath(dataset), fileTitle);
        }
        public FileMetadata ParseMetadata(IRasterFile rasterFile, bool makeRelativePath = false)
        {
            return rasterFile.ParseMetaData();


        }
        public FileMetadata ParseMetadata(string fileName, DEMFileFormat fileFormat, bool makeRelativePath = true)
        {
            FileMetadata metadata = null;

            fileName = Path.GetFullPath(fileName);

            using (IRasterFile rasterFile = OpenFile(fileName, fileFormat))
            {
                metadata = rasterFile.ParseMetaData();
            }

            Uri fullPath = new Uri(metadata.Filename, UriKind.Absolute);
            Uri relRoot = new Uri(Path.GetFullPath(_localDirectory) + Path.DirectorySeparatorChar, UriKind.Absolute);

            metadata.Filename = Uri.UnescapeDataString(relRoot.MakeRelativeUri(fullPath).ToString());
            return metadata;
        }

        public List<FileMetadata> LoadManifestMetadata(DEMDataSet dataset, bool force)
        {
            string localPath = GetLocalDEMPath(dataset);

            if (force && _metadataCatalogCache.ContainsKey(localPath))
            {
                _metadataCatalogCache.Remove(localPath);
            }
            if (_metadataCatalogCache.ContainsKey(localPath) == false)
            {
                var manifestDirectories = Directory.EnumerateDirectories(localPath, MANIFEST_DIR, SearchOption.AllDirectories);

                List<FileMetadata> metaList = new List<FileMetadata>(32000);
                foreach (var manifestDirectory in manifestDirectories)
                {
                    var manifestFiles = Directory.EnumerateFiles(manifestDirectory, "*.json");

                    foreach (var file in manifestFiles)
                    {
                        string jsonContent = File.ReadAllText(file);
                        FileMetadata metadata = JsonConvert.DeserializeObject<FileMetadata>(jsonContent);
                        if (metadata.Version != FileMetadata.FILEMETADATA_VERSION)
                        {
                            metadata = FileMetadataMigrations.Migrate(_logger, metadata, _localDirectory, dataset);
                            File.WriteAllText(file, JsonConvert.SerializeObject(metadata, Formatting.Indented));
                        }
                        metaList.Add(metadata);
                    }
                    
                }
                _metadataCatalogCache[localPath] = metaList;

            }
            return _metadataCatalogCache[localPath];
        }

        /// <summary>
        /// Generate metadata files for fast in-memory indexing
        /// </summary>
        /// <param name="directoryPath">Raster files directory</param>
        /// <param name="force">If true, force regeneration of all files. If false, only missing files will be generated.</param>
        public void GenerateDirectoryMetadata(DEMDataSet dataset, bool force, bool deleteOnError = false)
        {
            string directoryPath = GetLocalDEMPath(dataset);
            var files = Directory.EnumerateFiles(directoryPath, "*" + dataset.FileFormat.FileExtension, SearchOption.AllDirectories);
            ParallelOptions options = new ParallelOptions();
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


        public void GenerateFileMetadata(string rasterFileName, DEMFileFormat fileFormat, bool force)
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
            var reports = GenerateReportAsync().GetAwaiter().GetResult();

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
                DowloadedPercent = Math.Round(downloadedCount*100d/totalFiles, 2)
            };

            return reportSummary;
        }


        bool BoundingBoxIntersects(BoundingBox bbox1, BoundingBox bbox2)
        {
            return (bbox1.xMax >= bbox2.xMin && bbox1.xMin <= bbox2.xMax) && (bbox1.yMax >= bbox2.yMin && bbox1.yMin <= bbox2.yMax);
        }
        bool BoundingBoxIntersects(BoundingBox bbox1, double lat, double lon)
        {
            return (bbox1.xMax >= lon && bbox1.xMin <= lon) && (bbox1.yMax >= lat && bbox1.yMin <= lat);
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

            if (dataSet.DataSource.IsGlobalFile)
            {
                statusByFile.Add(new DemFileReport()
                {
                    IsExistingLocally = File.Exists(dataSet.DataSource.IndexFilePath),
                    IsMetadataGenerated = File.Exists(GetMetadataFileName(dataSet.DataSource.IndexFilePath, ".json")),
                    LocalName = dataSet.DataSource.IndexFilePath,
                    URL = dataSet.DataSource.IndexFilePath
                });
            }
            else
            {
                _gdalVrtService.Setup(dataSet, GetLocalDEMPath(dataSet));

                foreach (GDALSource source in _gdalVrtService.Sources(dataSet))
                {

                    if (bbox == null || BoundingBoxIntersects(source.BBox, bbox))
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
                    //Trace.TraceInformation($"Source {source.SourceFileName}");
                }
            }

            return statusByFile;
        }

        public DemFileReport GenerateReportForLocation(DEMDataSet dataSet, double lat, double lon)
        {
            if (dataSet.DataSource.IsGlobalFile)
            {
                return new DemFileReport()
                {
                    IsExistingLocally = File.Exists(dataSet.DataSource.IndexFilePath),
                    IsMetadataGenerated = File.Exists(GetMetadataFileName(dataSet.DataSource.IndexFilePath, ".json")),
                    LocalName = dataSet.DataSource.IndexFilePath,
                    URL = dataSet.DataSource.IndexFilePath
                };
            }
            else
            {
                _gdalVrtService.Setup(dataSet, GetLocalDEMPath(dataSet));

                foreach (GDALSource source in _gdalVrtService.Sources(dataSet))
                {
                    if (BoundingBoxIntersects(source.BBox, lat, lon))
                    {

                        return new DemFileReport()
                        {
                            IsExistingLocally = File.Exists(source.LocalFileName),
                            IsMetadataGenerated = File.Exists(GetMetadataFileName(source.LocalFileName, ".json")),
                            LocalName = source.LocalFileName,
                            URL = source.SourceFileNameAbsolute,
                            Source = source
                        };

                    }
                    //Trace.TraceInformation($"Source {source.SourceFileName}");
                }
            }

            return null;
        }


    }


}
