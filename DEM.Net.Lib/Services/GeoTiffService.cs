﻿using BitMiracle.LibTiff.Classic;
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

        public static FileMetadata ParseMetadata(Tiff tiff, string tiffPath)
        {
            FileMetadata metadata = new FileMetadata(tiffPath);
            ///
            FieldValue[] modelPixelScaleTag = tiff.GetField(TiffTag.GEOTIFF_MODELPIXELSCALETAG);
            FieldValue[] modelTiepointTag = tiff.GetField(TiffTag.GEOTIFF_MODELTIEPOINTTAG);

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

            var scanline = new byte[tiff.ScanlineSize()];
            metadata.ScanlineSize = tiff.ScanlineSize();
            //TODO: Check if band is stored in 1 byte or 2 bytes. 
            //If 2, the following code would be required
            var scanline16Bit = new ushort[tiff.ScanlineSize() / 2];
            Buffer.BlockCopy(scanline, 0, scanline16Bit, 0, scanline.Length);


            ///
            metadata.Height = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
            metadata.Width = tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();

            // Grab some raster metadata
            metadata.BitsPerSample = tiff.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();
            var sampleFormat = tiff.GetField(TiffTag.SAMPLEFORMAT);
            // Add other information about the data
            metadata.SampleFormat = sampleFormat[0].Value.ToString();
            // TODO: Read this from tiff metadata or determine after parsing
            metadata.NoDataValue = "-10000";

            metadata.WorldUnits = "meter";

            //DumpTiffTags(tiff);

            return metadata;
        }



        public static HeightMap GetHeightMap(BoundingBox bbox, string tiffPath)
        {
            // Locate which files are needed

            // Load metadata catalog
            List<FileMetadata> metadataCatalog = GetManifestMetadata(tiffPath);

            // Find files matching coords
            List<FileMetadata> bboxMetadata = metadataCatalog.Where(m => m.OriginLatitude <= Math.Round(bbox.yMax, 0)
                                && m.OriginLatitude >= Math.Round(bbox.yMin, 0)
                                && m.OriginLongitude >= Math.Round(bbox.XMin, 0)
                                && m.OriginLongitude <= Math.Round(bbox.XMax, 0)).ToList();

            if (bboxMetadata.Count == 0)
            {
                throw new Exception($"No coverage found matching provided bounding box {bbox}.");
            }
            else if (bboxMetadata.Count == 0)
            {
                throw new NotImplementedException($"Bounding box {bbox} covers more than one tile, which is not implemented yet. Consider dividing onto smaller parts covering one tile.");
            }

            //// get height map for each file at bbox
            //foreach (FileMetadata meta in bboxMetadata)
            //{
            //    HeightMap heightMap = null;
            //    using (GeoTiff tiffConverter = new GeoTiff(meta.Filename))
            //    {
            //        heightMap = tiffConverter.ConvertToHeightMap(bbox, meta);
            //    }
            //}
            HeightMap heightMap = null;
            FileMetadata meta = bboxMetadata.First();
            using (GeoTiff tiffConverter = new GeoTiff(meta.Filename))
            {
                heightMap = tiffConverter.ConvertToHeightMap(bbox, meta);
            }
            return heightMap;
        }

        private static List<FileMetadata> GetManifestMetadata(string tiffPath)
        {
            string manifestDir = Path.Combine(tiffPath, MANIFEST_DIR);
            string[] manifestFiles = Directory.GetFiles(manifestDir, "*.json");
            List<FileMetadata> metaList = new List<FileMetadata>(manifestFiles.Length);

            foreach (var file in manifestFiles)
            {
                string jsonContent = File.ReadAllText(file);
                metaList.Add(JsonConvert.DeserializeObject<FileMetadata>(jsonContent));
            }
            return metaList;
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

        public static HeightMap ParseGeoData(Tiff tiff, FileMetadata metadata)
        {
            HeightMap heightMap = new HeightMap(metadata.Width, metadata.Height);
            heightMap.FileMetadata = metadata;

            byte[] scanline = new byte[metadata.ScanlineSize];
            ushort[] scanline16Bit = new ushort[metadata.ScanlineSize / 2];
            Buffer.BlockCopy(scanline, 0, scanline16Bit, 0, scanline.Length);

            for (int y = 0; y < metadata.Height; y++)
            {
                tiff.ReadScanline(scanline, y);
                Buffer.BlockCopy(scanline, 0, scanline16Bit, 0, scanline.Length);

                double latitude = metadata.StartLat + (metadata.pixelSizeY * y);
                for (int x = 0; x < scanline16Bit.Length; x++)
                {
                    double longitude = metadata.StartLon + (metadata.pixelSizeX * x);

                    float heightValue = (float)scanline16Bit[x];
                    if (heightValue < 32768)
                    {
                        heightMap.Mininum = Math.Min(heightMap.Mininum, heightValue);
                        heightMap.Maximum = Math.Max(heightMap.Maximum, heightValue);
                    }
                    else
                    {
                        heightValue = -10000;
                    }
                    heightMap.Coordinates.Add(new GeoPoint(latitude, longitude, heightValue, x, y));

                }
            }

            return heightMap;
        }

        internal static HeightMap ParseGeoDataInBBox(Tiff tiff, BoundingBox bbox, FileMetadata metadata)
        {
            HeightMap heightMap = new HeightMap(metadata.Width, metadata.Height);
            heightMap.FileMetadata = metadata;

            byte[] scanline = new byte[metadata.ScanlineSize];
            ushort[] scanline16Bit = new ushort[metadata.ScanlineSize / 2];
            Buffer.BlockCopy(scanline, 0, scanline16Bit, 0, scanline.Length);


            int yStart = (int)Math.Floor((bbox.yMax - metadata.StartLat) / metadata.pixelSizeY);
            int yEnd = (int)Math.Ceiling((bbox.yMin - metadata.StartLat) / metadata.pixelSizeY);
            int xStart = (int)Math.Floor((bbox.XMin - metadata.StartLon) / metadata.pixelSizeX);
            int xEnd = (int)Math.Ceiling((bbox.XMax - metadata.StartLon) / metadata.pixelSizeX);

            xStart = Math.Max(0, xStart);
            xEnd = Math.Min(scanline16Bit.Length, xEnd);
            yStart = Math.Max(0, yStart);
            yEnd = Math.Min(metadata.Height, yEnd);

            for (int y = yStart; y <= yEnd; y++)
            {
                tiff.ReadScanline(scanline, y);
                Buffer.BlockCopy(scanline, 0, scanline16Bit, 0, scanline.Length);

                double latitude = metadata.StartLat + (metadata.pixelSizeY * y);
                for (int x = xStart; x <= xEnd; x++)
                {
                    double longitude = metadata.StartLon + (metadata.pixelSizeX * x);

                    float heightValue = (float)scanline16Bit[x];
                    if (heightValue < 32768)
                    {
                        heightMap.Mininum = Math.Min(heightMap.Mininum, heightValue);
                        heightMap.Maximum = Math.Max(heightMap.Maximum, heightValue);
                    }
                    else
                    {
                        heightValue = -10000;
                    }
                    heightMap.Coordinates.Add(new GeoPoint(latitude, longitude, heightValue, x, y));

                }
            }

            return heightMap;
        }

        public static void GenerateDirectoryMetadata(string directoryPath)
        {
            foreach (var file in Directory.GetFiles(directoryPath, "*.tif", SearchOption.TopDirectoryOnly))
            {
                Trace.TraceInformation($"Generating manifest for file {file}.");
                HeightMap heightMap = GeoTiffService.GetHeightMap(file);
                GeoTiffService.WriteManifestFiles(heightMap);
                GC.Collect();
            }
        }

        public static HeightMap GetHeightMap(string fileName)
        {
            fileName = Path.GetFullPath(fileName);
            string fileTitle = Path.GetFileNameWithoutExtension(fileName);

            HeightMap heightMap = null;
            using (GeoTiff tiffConverter = new GeoTiff(fileName))
            {
                heightMap = tiffConverter.ConvertToHeightMap();
            }
            return heightMap;
        }

        public static void WriteManifestFiles(HeightMap heightMap)
        {

            var fileName = heightMap.FileMetadata.Filename;
            var fileTitle = Path.GetFileNameWithoutExtension(fileName);

            string outDirPath = Path.Combine(Path.GetDirectoryName(fileName), MANIFEST_DIR);
            if (!Directory.Exists(outDirPath))
            {
                Directory.CreateDirectory(outDirPath);
            }

            // Save metadata
            var outputJsonPath = Path.Combine(Path.GetDirectoryName(fileName), MANIFEST_DIR, fileTitle + ".json");
            if (File.Exists(outputJsonPath)) File.Delete(outputJsonPath);

            var bitmapPath = Path.Combine(Path.GetDirectoryName(fileName), MANIFEST_DIR, fileTitle + ".bmp");
            if (File.Exists(bitmapPath)) File.Delete(bitmapPath);

            // Json manifest
            File.WriteAllText(outputJsonPath, JsonConvert.SerializeObject(heightMap.FileMetadata, Formatting.Indented));

            // Bitmap
            DiagnosticUtils.OutputDebugBitmap(heightMap, bitmapPath);
        }


    }
}