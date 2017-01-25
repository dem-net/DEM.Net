using BitMiracle.LibTiff.Classic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Services
{
    public static class GeoTiffService
    {

        private const string MANIFEST_DIR = "manifest";
        private const int EARTH_CIRCUMFERENCE_METERS = 40075017;


        public static FileMetadata ParseMetadata(GeoTiff tiff, string tiffPath)
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
        public static FileMetadata ParseMetadata(string fileName)
        {
            FileMetadata metadata = null;

            fileName = Path.GetFullPath(fileName);
            string fileTitle = Path.GetFileNameWithoutExtension(fileName);

            using (GeoTiff tiff = new GeoTiff(fileName))
            {
                metadata = GeoTiffService.ParseMetadata(tiff, fileName);
            }
            return metadata;
        }

        private static List<FileMetadata> _metadataCatalogCache = null;
        public static List<FileMetadata> LoadManifestMetadata(string tiffPath)
        {
            if (_metadataCatalogCache == null)
            {
                string manifestDir = Path.Combine(tiffPath, MANIFEST_DIR);
                string[] manifestFiles = Directory.GetFiles(manifestDir, "*.json");
                List<FileMetadata> metaList = new List<FileMetadata>(manifestFiles.Length);

                foreach (var file in manifestFiles)
                {
                    string jsonContent = File.ReadAllText(file);
                    metaList.Add(JsonConvert.DeserializeObject<FileMetadata>(jsonContent));
                }

                _metadataCatalogCache = metaList;
            }
            return _metadataCatalogCache;
        }

        public static void DumpTiffTags(Tiff tiff)
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
        public static void GenerateDirectoryMetadata(string directoryPath, bool generateBitmaps)
        {
            string[] files = Directory.GetFiles(directoryPath, "*.tif", SearchOption.TopDirectoryOnly);
            //ParallelOptions options = new ParallelOptions() { MaxDegreeOfParallelism = 2 };
            //Parallel.ForEach(files, options, file => GenerateFileMetadata(file, generateBitmaps));
            Parallel.ForEach(files, file => GenerateFileMetadata(file, generateBitmaps));
        }

        private static void GenerateFileMetadata(string geoTiffFileName, bool generateBitmap)
        {
            Trace.TraceInformation($"Generating manifest for file {geoTiffFileName}.");

            FileMetadata metadata = GeoTiffService.ParseMetadata(geoTiffFileName);
            HeightMap heightMap = null;
            if (generateBitmap)
            {
                heightMap = ElevationService.GetHeightMap(geoTiffFileName);
            }
            GeoTiffService.WriteManifestFiles(metadata, heightMap);
            //GC.Collect()
            Trace.TraceInformation($"Manifest generated for file {geoTiffFileName}.");
        }

        public static void WriteManifestFiles(FileMetadata fileMetadata, HeightMap heightMap)
        {

            var fileName = fileMetadata.Filename;
            var fileTitle = Path.GetFileNameWithoutExtension(fileName);

            string outDirPath = Path.Combine(Path.GetDirectoryName(fileName), MANIFEST_DIR);
            if (!Directory.Exists(outDirPath))
            {
                Directory.CreateDirectory(outDirPath);
            }

            // Save metadata
            var outputJsonPath = Path.Combine(outDirPath, fileTitle + ".json");
            if (File.Exists(outputJsonPath)) File.Delete(outputJsonPath);

            // Generate bitmap
            if (heightMap != null)
            {
                var bitmapPath = Path.Combine(outDirPath, fileTitle + ".bmp");
                if (File.Exists(bitmapPath)) File.Delete(bitmapPath);
                // Bitmap
                DiagnosticUtils.OutputDebugBitmap(heightMap, bitmapPath);
            }

            // Json manifest
            File.WriteAllText(outputJsonPath, JsonConvert.SerializeObject(fileMetadata, Formatting.Indented));


        }



    }
}
