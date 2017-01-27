using BitMiracle.LibTiff.Classic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Services
{
    public class GeoTiffService : IGeoTiffService
    {
        private const string MANIFEST_DIR = "manifest";
        private const int EARTH_CIRCUMFERENCE_METERS = 40075017;
        private static Dictionary<string, List<FileMetadata>> _metadataCatalogCache = null;

        static GeoTiffService()
        {
            _metadataCatalogCache = new Dictionary<string, List<FileMetadata>>();
        }

        public FileMetadata ParseMetadata(GeoTiff tiff, string tiffPath)
        {
            FileMetadata metadata = new FileMetadata(tiffPath);

            ///
            metadata.Height = tiff.TiffFile.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
            metadata.Width = tiff.TiffFile.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();

            ///
            FieldValue[] modelPixelScaleTag = tiff.TiffFile.GetField(TiffTag.GEOTIFF_MODELPIXELSCALETAG);
            FieldValue[] modelTiepointTag = tiff.TiffFile.GetField(TiffTag.GEOTIFF_MODELTIEPOINTTAG);

            byte[] modelPixelScale = modelPixelScaleTag[1].GetBytes();
            double pixelSizeX = BitConverter.ToDouble(modelPixelScale, 0);
            double pixelSizeY = BitConverter.ToDouble(modelPixelScale, 8) * -1;
            metadata.pixelSizeX = pixelSizeX;
            metadata.pixelSizeY = pixelSizeY;
            metadata.PixelScaleX = BitConverter.ToDouble(modelPixelScale, 0);
            metadata.PixelScaleY = BitConverter.ToDouble(modelPixelScale, 8);

            // Ignores first set of model points (3 bytes) and assumes they are 0's...
            byte[] modelTransformation = modelTiepointTag[1].GetBytes();
            metadata.OriginLongitude = BitConverter.ToDouble(modelTransformation, 24);
            metadata.OriginLatitude = BitConverter.ToDouble(modelTransformation, 32);


            double startLat = metadata.OriginLatitude + (pixelSizeY / 2.0);
            double startLon = metadata.OriginLongitude + (pixelSizeX / 2.0);
            metadata.StartLat = startLat;
            metadata.StartLon = startLon;

            var scanline = new byte[tiff.TiffFile.ScanlineSize()];
            metadata.ScanlineSize = tiff.TiffFile.ScanlineSize();
            //TODO: Check if band is stored in 1 byte or 2 bytes. 
            //If 2, the following code would be required
            var scanline16Bit = new ushort[tiff.TiffFile.ScanlineSize() / 2];
            Buffer.BlockCopy(scanline, 0, scanline16Bit, 0, scanline.Length);


            // Grab some raster metadata
            metadata.BitsPerSample = tiff.TiffFile.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();
            var sampleFormat = tiff.TiffFile.GetField(TiffTag.SAMPLEFORMAT);
            // Add other information about the data
            metadata.SampleFormat = sampleFormat[0].Value.ToString();
            // TODO: Read this from tiff metadata or determine after parsing
            metadata.NoDataValue = "-10000";

            metadata.WorldUnits = "meter";

            //DumpTiffTags(tiff);

            return metadata;
        }
        public FileMetadata ParseMetadata(string fileName)
        {
            FileMetadata metadata = null;

            fileName = Path.GetFullPath(fileName);
            string fileTitle = Path.GetFileNameWithoutExtension(fileName);

            using (GeoTiff tiff = new GeoTiff(fileName))
            {
                metadata = this.ParseMetadata(tiff, fileName);
            }
            return metadata;
        }

        public List<FileMetadata> LoadManifestMetadata(string tiffPath)
        {
            if (_metadataCatalogCache.ContainsKey(tiffPath) == false)
            {
                string manifestDir = Path.Combine(tiffPath, MANIFEST_DIR);
                string[] manifestFiles = Directory.GetFiles(manifestDir, "*.json");
                List<FileMetadata> metaList = new List<FileMetadata>(manifestFiles.Length);

                foreach (var file in manifestFiles)
                {
                    string jsonContent = File.ReadAllText(file);
                    metaList.Add(JsonConvert.DeserializeObject<FileMetadata>(jsonContent));
                }

                _metadataCatalogCache[tiffPath] = metaList;
            }
            return _metadataCatalogCache[tiffPath];
        }

        public void DumpTiffTags(Tiff tiff)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var value in Enum.GetValues(typeof(TiffTag)))
            {
                TiffTag tag = (TiffTag)value;
                FieldValue[] values = tiff.GetField(tag);
                if (values != null)
                {
                    sb.AppendLine(value + ": ");
                    foreach (var fieldValue in values)
                    {
                        sb.Append("\t");
                        sb.AppendLine(fieldValue.Value.ToString());
                    }
                }
            }
            Console.WriteLine(sb.ToString());
        }

        public static int GetResolutionMeters(FileMetadata metadata)
        {
            double preciseRes = metadata.pixelSizeX * EARTH_CIRCUMFERENCE_METERS / 360d;
            return (int)Math.Floor(preciseRes);
        }

        /// <summary>
        /// Generate metadata files for fast in-memory indexing
        /// </summary>
        /// <param name="directoryPath">GeoTIFF files directory</param>
        /// <param name="generateBitmaps">If true, bitmaps with height map will be generated (heavy memory usage and waaaay slower)</param>
        /// <param name="force">If true, force regeneration of all files. If false, only missing files will be generated.</param>
        public void GenerateDirectoryMetadata(string directoryPath, bool generateBitmaps, bool force)
        {
            string[] files = Directory.GetFiles(directoryPath, "*.tif", SearchOption.TopDirectoryOnly);
            ParallelOptions options = new ParallelOptions();
            if (generateBitmaps)
            {
                options.MaxDegreeOfParallelism = 2; // heavy memory usage, so let's do in parallel, but not too much
            }
            Parallel.ForEach(files, options, file => GenerateFileMetadata(file, generateBitmaps, force));
        }

        public void GenerateFileMetadata(string geoTiffFileName, bool generateBitmap, bool force)
        {

            var fileName = geoTiffFileName;
            var fileTitle = Path.GetFileNameWithoutExtension(fileName);
            string outDirPath = Path.Combine(Path.GetDirectoryName(fileName), MANIFEST_DIR);
            string bmpPath = Path.Combine(outDirPath, fileTitle + ".bmp");
            string jsonPath = Path.Combine(outDirPath, fileTitle + ".json");


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
                Trace.TraceInformation($"Generating manifest for file {geoTiffFileName}.");

                FileMetadata metadata = this.ParseMetadata(geoTiffFileName);
                File.WriteAllText(jsonPath, JsonConvert.SerializeObject(metadata, Formatting.Indented));

                Trace.TraceInformation($"Manifest generated for file {geoTiffFileName}.");
            }

            // Debug bitmap
            if (File.Exists(bmpPath) == false && generateBitmap)
            {
                Trace.TraceInformation($"Generating bitmap for file {geoTiffFileName}.");
                FileMetadata metadata = this.ParseMetadata(geoTiffFileName);
                HeightMap heightMap = ElevationService.GetHeightMap(geoTiffFileName, metadata);
                DiagnosticUtils.OutputDebugBitmap(heightMap, bmpPath);

                Trace.TraceInformation($"Bitmap generated for file {geoTiffFileName}.");
            }

        }

        public string GenerateReport(string directoryPath, string urlToLstFile, string remoteFileExtension, string newRemoteFileExtension)
        {
            WebClient webClient = new WebClient();
            Uri lstUri = new Uri(urlToLstFile);
            string lstContent = webClient.DownloadString(lstUri);
            IEnumerable<string> remoteFilesQuery = lstContent.Split('\n');
            remoteFilesQuery = remoteFilesQuery.Where(f => f.EndsWith(remoteFileExtension));
            if (!string.IsNullOrWhiteSpace(newRemoteFileExtension))
            {
                remoteFilesQuery = remoteFilesQuery.Select(f => f.Replace(remoteFileExtension, newRemoteFileExtension));
            }
            HashSet<string> remoteFiles = new HashSet<string>(remoteFilesQuery);
            HashSet<string> localFiles = new HashSet<string>(Directory.GetFiles(directoryPath, "*" + newRemoteFileExtension, SearchOption.TopDirectoryOnly)
                                                                       .Select(f => Path.GetFileName(f)));
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("RemoteURL\tIsDownloaded");
            foreach (string remoteFile in remoteFiles)
            {
                string fileTitle = remoteFile.Split('/').Last();
                Uri remoteFileUri = null;
                Uri.TryCreate(lstUri, remoteFile, out remoteFileUri);
                bool isDownloaded = localFiles.Contains(fileTitle);

                sb.AppendLine(string.Concat(remoteFileUri.AbsoluteUri, '\t', isDownloaded));
            }
            return sb.ToString();
        }
    }
}
