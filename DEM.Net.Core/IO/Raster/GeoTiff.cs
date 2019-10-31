// GeoTiff.cs
//
// Author:
//       Xavier Fischer 
//
// Copyright (c) 2019 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
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

using BitMiracle.LibTiff.Classic;
using DEM.Net.Core.IO;
using DEM.Net.Core;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace DEM.Net.Core
{

    public class GeoTiff : IRasterFile
    {
        Tiff _tiff;
        string _tiffPath;
        static NoLogTiffErrorHandler _errorHandler = new NoLogTiffErrorHandler();

        internal Tiff TiffFile
        {
            get { return _tiff; }
        }

        public string FilePath
        {
            get { return _tiffPath; }
        }



        public GeoTiff(string tiffPath)
        {
            if (!File.Exists(tiffPath))
                throw new Exception($"File {tiffPath} does not exists !");

            _tiffPath = tiffPath;
            Tiff.SetErrorHandler(new ConsoleLogTiffErrorHandler());
            _tiff = Tiff.Open(tiffPath, "r");

            if (_tiff == null)
                throw new Exception($"File {tiffPath} cannot be opened !");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _tiff?.Dispose();
            }
        }

        ~GeoTiff()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public float GetElevationAtPoint(FileMetadata metadata, int x, int y)
        {
            float heightValue = 0;
            try
            {
                // metadata.BitsPerSample
                // When 16 we have 2 bytes per sample
                // When 32 we have 4 bytes per sample
                int bytesPerSample = metadata.BitsPerSample / 8;
                byte[] byteScanline = new byte[metadata.ScanlineSize];

                Test(TiffFile);
                TiffFile.ReadScanline(byteScanline, y);

                heightValue = GetElevationAtPoint(metadata, x, byteScanline);
            }
            catch (Exception e)
            {
                throw new Exception($"Error in ParseGeoDataAtPoint: {e.Message}");
            }
            return heightValue;
        }


        public void Test(Tiff image)
        {

            FieldValue[] value = image.GetField(TiffTag.IMAGELENGTH);
            int imageLength = value[0].ToInt();

            value = image.GetField(TiffTag.PLANARCONFIG);
            PlanarConfig config = (PlanarConfig)value[0].ToInt();

            byte[] buf = new byte[image.ScanlineSize()];

            if (config == PlanarConfig.CONTIG)
            {
                for (int row = 0; row < imageLength; row++)
                    image.ReadScanline(buf, row);
            }
            else if (config == PlanarConfig.SEPARATE)
            {
                value = image.GetField(TiffTag.SAMPLESPERPIXEL);
                short spp = value[0].ToShort();

                for (short s = 0; s < spp; s++)
                {
                    for (int row = 0; row < imageLength; row++)
                        image.ReadScanline(buf, row, s);
                }
            }
        }


        public float GetElevationAtPoint(FileMetadata metadata, int x, byte[] byteScanline)
        {
            float heightValue = 0;
            try
            {
                switch (metadata.SampleFormat)
                {
                    case RasterSampleFormat.FLOATING_POINT:
                        heightValue = BitConverter.ToSingle(byteScanline, x * metadata.BitsPerSample / 8);
                        break;
                    case RasterSampleFormat.INTEGER:
                        heightValue = BitConverter.ToInt16(byteScanline, x * metadata.BitsPerSample / 8);
                        break;
                    case RasterSampleFormat.UNSIGNED_INTEGER:
                        heightValue = BitConverter.ToUInt16(byteScanline, x * metadata.BitsPerSample / 8);
                        break;
                    default:
                        throw new Exception("Sample format unsupported.");
                }
                if (heightValue > 32768)
                {
                    heightValue = metadata.NoDataValueFloat;
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Error in ParseGeoDataAtPoint: {e.Message}");
            }

            return heightValue;
        }

        public FileMetadata ParseMetaData()
        {
            FileMetadata metadata = new FileMetadata(FilePath, DEMFileFormat.GEOTIFF);

            ///
            metadata.Height = TiffFile.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
            metadata.Width = TiffFile.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();

            ///
            FieldValue[] modelPixelScaleTag = TiffFile.GetField(TiffTag.GEOTIFF_MODELPIXELSCALETAG);
            FieldValue[] modelTiepointTag = TiffFile.GetField(TiffTag.GEOTIFF_MODELTIEPOINTTAG);

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

            var scanline = new byte[TiffFile.ScanlineSize()];
            metadata.ScanlineSize = TiffFile.ScanlineSize();

            // Grab some raster metadata
            metadata.BitsPerSample = TiffFile.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();
            var sampleFormat = TiffFile.GetField(TiffTag.SAMPLEFORMAT);
            // Add other information about the data
            metadata.SampleFormat = sampleFormat[0].Value.ToString();
            // TODO: Read this from tiff metadata or determine after parsing
            metadata.NoDataValue = "-10000";

            metadata.WorldUnits = "meter";

            return metadata;
        }

        public HeightMap GetHeightMapInBBox(BoundingBox bbox, FileMetadata metadata, float noDataValue = 0)
        {
            // metadata.BitsPerSample
            // When 16 we have 2 bytes per sample
            // When 32 we have 4 bytes per sample
            int bytesPerSample = metadata.BitsPerSample / 8;
            byte[] byteScanline = new byte[metadata.ScanlineSize];


            int yStart = (int)Math.Floor((bbox.yMax - metadata.StartLat) / metadata.pixelSizeY);
            int yEnd = (int)Math.Ceiling((bbox.yMin - metadata.StartLat) / metadata.pixelSizeY);
            int xStart = (int)Math.Floor((bbox.xMin - metadata.StartLon) / metadata.pixelSizeX);
            int xEnd = (int)Math.Ceiling((bbox.xMax - metadata.StartLon) / metadata.pixelSizeX);

            xStart = Math.Max(0, xStart);
            xEnd = Math.Min(metadata.Width - 1, xEnd);
            yStart = Math.Max(0, yStart);
            yEnd = Math.Min(metadata.Height - 1, yEnd);

            HeightMap heightMap = new HeightMap(xEnd - xStart + 1, yEnd - yStart + 1);
            heightMap.Count = heightMap.Width * heightMap.Height;
            var coords = new List<GeoPoint>(heightMap.Count);
            heightMap.BoundingBox = new BoundingBox(0, 0, 0, 0);

            for (int y = yStart; y <= yEnd; y++)
            {
                TiffFile.ReadScanline(byteScanline, y);

                double latitude = metadata.StartLat + (metadata.pixelSizeY * y);

                // bounding box
                if (y == yStart)
                {
                    heightMap.BoundingBox.yMax = latitude;
                    heightMap.BoundingBox.xMin = metadata.StartLon + (metadata.pixelSizeX * xStart);
                    heightMap.BoundingBox.xMax = metadata.StartLon + (metadata.pixelSizeX * xEnd);
                }
                else if (y == yEnd)
                {
                    heightMap.BoundingBox.yMin = latitude;
                }

                for (int x = xStart; x <= xEnd; x++)
                {
                    double longitude = metadata.StartLon + (metadata.pixelSizeX * x);

                    float heightValue = 0;
                    switch (metadata.SampleFormat)
                    {
                        case RasterSampleFormat.FLOATING_POINT:
                            heightValue = BitConverter.ToSingle(byteScanline, x * bytesPerSample);
                            break;
                        case RasterSampleFormat.INTEGER:
                            heightValue = BitConverter.ToInt16(byteScanline, x * bytesPerSample);
                            break;
                        case RasterSampleFormat.UNSIGNED_INTEGER:
                            heightValue = BitConverter.ToUInt16(byteScanline, x * bytesPerSample);
                            break;
                        default:
                            throw new Exception("Sample format unsupported.");
                    }
                    if (heightValue <= 0)
                    {
                        heightMap.Minimum = Math.Min(heightMap.Minimum, heightValue);
                        heightMap.Maximum = Math.Max(heightMap.Maximum, heightValue);
                    }
                    else if (heightValue < 32768)
                    {
                        heightMap.Minimum = Math.Min(heightMap.Minimum, heightValue);
                        heightMap.Maximum = Math.Max(heightMap.Maximum, heightValue);
                    }

                    else
                    {
                        heightValue = (float)noDataValue;
                    }
                    coords.Add(new GeoPoint(latitude, longitude, heightValue));

                }
            }
            Debug.Assert(heightMap.Width * heightMap.Height == coords.Count);

            heightMap.Coordinates = coords;
            return heightMap;
        }

        public HeightMap GetHeightMap(FileMetadata metadata)
        {
            HeightMap heightMap = new HeightMap(metadata.Width, metadata.Height);
            heightMap.Count = heightMap.Width * heightMap.Height;
            var coords = new List<GeoPoint>(heightMap.Count);

            // metadata.BitsPerSample
            // When 16 we have 2 bytes per sample
            // When 32 we have 4 bytes per sample
            int bytesPerSample = metadata.BitsPerSample / 8;
            byte[] byteScanline = new byte[metadata.ScanlineSize];

            for (int y = 0; y < metadata.Height; y++)
            {
                TiffFile.ReadScanline(byteScanline, y);

                double latitude = metadata.StartLat + (metadata.pixelSizeY * y);
                for (int x = 0; x < metadata.Width; x++)
                {
                    double longitude = metadata.StartLon + (metadata.pixelSizeX * x);

                    float heightValue = 0;
                    switch (metadata.SampleFormat)
                    {
                        case RasterSampleFormat.FLOATING_POINT:
                            heightValue = BitConverter.ToSingle(byteScanline, x * metadata.BitsPerSample / 8);
                            break;
                        case RasterSampleFormat.INTEGER:
                            heightValue = BitConverter.ToInt16(byteScanline, x * metadata.BitsPerSample / 8);
                            break;
                        case RasterSampleFormat.UNSIGNED_INTEGER:
                            heightValue = BitConverter.ToUInt16(byteScanline, x * metadata.BitsPerSample / 8);
                            break;
                        default:
                            throw new Exception("Sample format unsupported.");
                    }
                    if (heightValue < 32768)
                    {
                        heightMap.Minimum = Math.Min(metadata.MinimumAltitude, heightValue);
                        heightMap.Maximum = Math.Max(metadata.MaximumAltitude, heightValue);
                    }
                    else
                    {
                        heightValue = 0;
                    }
                    coords.Add(new GeoPoint(latitude, longitude, heightValue));

                }
            }

            heightMap.Coordinates = coords;
            return heightMap;
        }

    }
}
