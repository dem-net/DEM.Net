using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib
{
    public class RasterService : IRasterService
    {
        const string APP_NAME = "DEM.Net";
        const string MANIFEST_DIR = "manifest";
        const int EARTH_CIRCUMFERENCE_METERS = 40075017;
        GDALVRTFileService _gdalService;


        private static string _localDirectory;
        private static Dictionary<string, List<FileMetadata>> _metadataCatalogCache = null;

        public string LocalDirectory
        {
            get { return _localDirectory; }
        }

        static RasterService()
        {

            _localDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), APP_NAME);
            if (!Directory.Exists(_localDirectory))
                Directory.CreateDirectory(_localDirectory);

            _metadataCatalogCache = new Dictionary<string, List<FileMetadata>>();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dataDirectory">Path to data directory. If null, ApplicationData folder will be used.</param>
        public RasterService(string dataDirectory = null)
        {
            if (dataDirectory != null)
            {
                Directory.CreateDirectory(dataDirectory);
                _localDirectory = dataDirectory;
            }
        }

        public IRasterFile OpenFile(string filePath, DEMFileFormat fileFormat)
        {
            if (fileFormat.Name == DEMFileFormat.GEOTIFF.Name)
            {
                return new GeoTiff(Path.Combine(_localDirectory, filePath));
            }
            else if (fileFormat.Name == DEMFileFormat.SRTM_HGT.Name)
            {
                return new HGTFile(Path.Combine(_localDirectory, filePath));
            }
            else
                throw new NotImplementedException($"{fileFormat} file format not implemented.");

        }

        public string GetLocalDEMPath(DEMDataSet dataset)
        {
            return Path.Combine(_localDirectory, dataset.Name);
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
                string manifestDir = Path.Combine(localPath, MANIFEST_DIR);
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
                            metadata = FileMetadataMigrations.Migrate(metadata, _localDirectory, dataset);
                            File.WriteAllText(file, JsonConvert.SerializeObject(metadata, Formatting.Indented));
                        }
                        metaList.Add(metadata);
                    }

                    _metadataCatalogCache[localPath] = metaList;
                }

            }
            return _metadataCatalogCache[localPath];
        }


        public static int GetResolutionMeters(FileMetadata metadata)
        {
            double preciseRes = metadata.pixelSizeX * EARTH_CIRCUMFERENCE_METERS / 360d;
            return (int)Math.Floor(preciseRes);
        }

        /// <summary>
        /// Generate metadata files for fast in-memory indexing
        /// </summary>
        /// <param name="directoryPath">Raster files directory</param>
        /// <param name="generateBitmaps">If true, bitmaps with height map will be generated (heavy memory usage and waaaay slower)</param>
        /// <param name="force">If true, force regeneration of all files. If false, only missing files will be generated.</param>
        public void GenerateDirectoryMetadata(DEMDataSet dataset, bool generateBitmaps, bool force, bool deleteOnError = false)
        {
            string directoryPath = GetLocalDEMPath(dataset);
            var files = Directory.EnumerateFiles(directoryPath, "*" + dataset.FileFormat.FileExtension, SearchOption.AllDirectories);
            ParallelOptions options = new ParallelOptions();
            if (generateBitmaps)
            {
                options.MaxDegreeOfParallelism = 2; // heavy memory usage, so let's do in parallel, but not too much
            }
            Parallel.ForEach(files, options, file =>
            {
                try
                {
                    GenerateFileMetadata(file, dataset.FileFormat, generateBitmaps, force);
                }
                catch (Exception exFile)
                {
                    Logger.Error($"Error while generating metadata for file {file} : {exFile.Message}");
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


        public void GenerateFileMetadata(string rasterFileName, DEMFileFormat fileFormat, bool generateBitmap, bool force)
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

            // Debug bitmap
            if (File.Exists(bmpPath) == false && generateBitmap)
            {
                Trace.TraceInformation($"Generating bitmap for file {rasterFileName}.");
                FileMetadata metadata = this.ParseMetadata(rasterFileName, fileFormat);
                HeightMap heightMap = GetHeightMap(rasterFileName, metadata);
                DiagnosticUtils.OutputDebugBitmap(heightMap, bmpPath);

                Trace.TraceInformation($"Bitmap generated for file {rasterFileName}.");
            }

        }

        private HeightMap GetHeightMap(string fileName, FileMetadata metadata)
        {
            fileName = Path.GetFullPath(fileName);

            HeightMap heightMap = null;
            using (IRasterFile raster = OpenFile(fileName, metadata.fileFormat))
            {
                heightMap = raster.GetHeightMap(metadata);
            }
            return heightMap;
        }
        public string GenerateReportAsString()
        {
            StringBuilder sb = new StringBuilder();
            // Get report for downloaded files
            foreach (DEMDataSet dataset in DEMDataSet.RegisteredDatasets)
            {
                Dictionary<string, DemFileReport> report = GenerateReport(dataset);
                int totalFiles = report.Count;
                int downloadedCount = report.Count(kvp => kvp.Value.IsExistingLocally);
                int isMetadataGeneratedCount = report.Count(kvp => kvp.Value.IsMetadataGenerated);
                int isnotMetadataGeneratedCount = report.Count(kvp => !kvp.Value.IsMetadataGenerated);

                var fileSizeBytes = FileSystem.GetDirectorySize(GetLocalDEMPath(dataset));
                var fileSizeMB = fileSizeBytes / 1024f / 1024f;

                sb.AppendLine($"Dataset : {dataset.Name} report :");
                sb.AppendLine($"> {totalFiles} file(s) in dataset.");
                sb.AppendLine($"> {downloadedCount} file(s) dowloaded ({fileSizeMB:F2} MB total).");
                sb.AppendLine($"> {isMetadataGeneratedCount} file(s) with DEM.Net metadata.");
            }
            return sb.ToString();
        }
       

        public bool BoundingBoxIntersects(BoundingBox bbox1, BoundingBox bbox2)
        {
            return (bbox1.xMax >= bbox2.xMin && bbox1.xMin <= bbox2.xMax) && (bbox1.yMax >= bbox2.yMin && bbox1.yMin <= bbox2.yMax);
        }
        public bool BoundingBoxIntersects(BoundingBox bbox1, double lat, double lon)
        {
            return (bbox1.xMax >= lon && bbox1.xMin <= lon) && (bbox1.yMax >= lat && bbox1.yMin <= lat);
        }

        public Dictionary<string, DemFileReport> GenerateReport(DEMDataSet dataSet, BoundingBox bbox = null)
        {
            Dictionary<string, DemFileReport> statusByFile = new Dictionary<string, DemFileReport>();
            if (_gdalService == null || _gdalService.Dataset.Name != dataSet.Name)
            {
                _gdalService = new GDALVRTFileService(GetLocalDEMPath(dataSet), dataSet);
                _gdalService.Setup(true);
            }

            foreach (GDALSource source in _gdalService.Sources())
            {

                if (bbox == null || BoundingBoxIntersects(source.BBox, bbox))
                {

                    statusByFile.Add(source.SourceFileNameAbsolute, new DemFileReport()
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


            return statusByFile;
        }

        public Dictionary<string, DemFileReport> GenerateReportForLocation(DEMDataSet dataSet, double lat, double lon)
        {
            Dictionary<string, DemFileReport> statusByFile = new Dictionary<string, DemFileReport>();
            if (_gdalService == null || _gdalService.Dataset.Name != dataSet.Name)
            {
                _gdalService = new GDALVRTFileService(GetLocalDEMPath(dataSet), dataSet);
                _gdalService.Setup(true);
            }

            foreach (GDALSource source in _gdalService.Sources())
            {

                if (BoundingBoxIntersects(source.BBox, lat, lon))
                {

                    statusByFile.Add(source.SourceFileNameAbsolute, new DemFileReport()
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


            return statusByFile;
        }


    }


}
