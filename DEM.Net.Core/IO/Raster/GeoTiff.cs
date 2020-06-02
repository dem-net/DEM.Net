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
        //static NoLogTiffErrorHandler _errorHandler = new NoLogTiffErrorHandler();
        TraceTiffErrorHandler _traceLogHandler = new TraceTiffErrorHandler();
        Dictionary<int, byte[]> tilesCache;

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
            Tiff.SetErrorHandler(_traceLogHandler);
            _tiff = Tiff.Open(tiffPath, "r");

            if (_tiff == null)
                throw new Exception($"File {tiffPath} cannot be opened !");
        }

        #region Tile info

        int tileWidth = 0;

        internal int TileWidth
        {
            get
            {
                if (tileWidth == 0)
                {
                    tileWidth = TiffFile.GetField(TiffTag.TILEWIDTH)[0].ToInt();
                }

                return tileWidth;
            }
        }
        int tileHeight = 0;

        internal int TileHeight
        {
            get
            {
                if (tileHeight == 0)
                {
                    tileHeight = TiffFile.GetField(TiffTag.TILELENGTH)[0].ToInt();
                }

                return tileHeight;
            }
        }
        int tileSize = 0;

        internal int TileSize
        {
            get
            {
                if (tileSize == 0)
                {
                    tileSize = TiffFile.TileSize();
                }

                return tileSize;
            }
        }

        bool isTiledSet = false;
        bool isTiled;
        internal bool IsTiled
        {
            get
            {
                if (isTiledSet == false)
                {
                    isTiled = TiffFile.IsTiled();
                    isTiledSet = true;
                }

                return isTiled;
            }
        }
        #endregion

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _tiff?.Dispose();
                if (tilesCache != null)
                {
                    tilesCache.Clear();
                    tilesCache = null;
                }
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

                if (this.IsTiled)
                {

                    // TODO store in metadata
                    int tileWidth = this.TileWidth;
                    int tileHeight = this.TileHeight;
                    int tileSize = this.TileSize;
                    byte[] buffer;

                    var tileX = (x / tileWidth) * tileWidth;
                    var tileY = (y / tileHeight) * tileHeight;

                    if (tilesCache == null) tilesCache = new Dictionary<int, byte[]>();
                    var tileKey = (x / tileWidth) + (y / tileHeight) * (metadata.Width / tileWidth + 1);
                    if (!tilesCache.TryGetValue(tileKey, out buffer))
                    {
                        buffer = new byte[tileSize];
                        TiffFile.ReadTile(buffer, 0, tileX, tileY, 0, 0);
                        tilesCache.Add(tileKey, buffer);
                    }
                    var offset = x - tileX + (y - tileY) * tileHeight;
                    heightValue = GetElevationAtPoint(metadata, offset, buffer);
                }
                else
                {
                    // metadata.BitsPerSample
                    // When 16 we have 2 bytes per sample
                    // When 32 we have 4 bytes per sample
                    int bytesPerSample = metadata.BitsPerSample / 8;
                    byte[] byteScanline = new byte[metadata.ScanlineSize];

                    TiffFile.ReadScanline(byteScanline, y);

                    heightValue = GetElevationAtPoint(metadata, x, byteScanline);
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Error in ParseGeoDataAtPoint: {e.Message}");
            }
            return heightValue;
        }

        public float GetElevationAtPoint(FileMetadata metadata, int offset, byte[] buffer)
        {
            float heightValue = 0;
            try
            {
                switch (metadata.SampleFormat)
                {
                    case RasterSampleFormat.FLOATING_POINT:
                        heightValue = BitConverter.ToSingle(buffer, offset * metadata.BitsPerSample / 8);
                        break;
                    case RasterSampleFormat.INTEGER:
                        heightValue = BitConverter.ToInt16(buffer, offset * metadata.BitsPerSample / 8);
                        break;
                    case RasterSampleFormat.UNSIGNED_INTEGER:
                        heightValue = BitConverter.ToUInt16(buffer, offset * metadata.BitsPerSample / 8);
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
        public float GetElevationAtPointRef(FileMetadata metadata, int offset, ref byte[] buffer)
        {
            float heightValue = 0;
            try
            {
                switch (metadata.SampleFormat)
                {
                    case RasterSampleFormat.FLOATING_POINT:
                        heightValue = BitConverter.ToSingle(buffer, offset * metadata.BitsPerSample / 8);
                        break;
                    case RasterSampleFormat.INTEGER:
                        heightValue = BitConverter.ToInt16(buffer, offset * metadata.BitsPerSample / 8);
                        break;
                    case RasterSampleFormat.UNSIGNED_INTEGER:
                        heightValue = BitConverter.ToUInt16(buffer, offset * metadata.BitsPerSample / 8);
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

        public FileMetadata ParseMetaData(DEMFileDefinition format)
        {
            FileMetadata metadata = new FileMetadata(FilePath, format);

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
            metadata.DataStartLon = BitConverter.ToDouble(modelTransformation, 24);
            metadata.DataStartLat = BitConverter.ToDouble(modelTransformation, 32);
            metadata.DataEndLon = metadata.DataStartLon + metadata.Width * pixelSizeX;
            metadata.DataEndLat = metadata.DataStartLat + metadata.Height * pixelSizeY;

            if (metadata.DataStartLon > metadata.DataEndLon)
            {
                double temp = metadata.DataStartLon;
                metadata.DataStartLon = metadata.DataEndLon;
                metadata.DataEndLon = temp;
            }
            if (metadata.DataStartLat > metadata.DataEndLat)
            {
                double temp = metadata.DataStartLat;
                metadata.DataStartLat = metadata.DataEndLat;
                metadata.DataEndLat = temp;
            }

            if (format.Registration == DEMFileRegistrationMode.Grid)
            {
                metadata.PhysicalStartLat = metadata.DataStartLat;
                metadata.PhysicalStartLon = metadata.DataStartLon;
                metadata.PhysicalEndLat = metadata.DataEndLat;
                metadata.PhysicalEndLon = metadata.DataEndLon;
                metadata.DataStartLat = Math.Round(metadata.DataStartLat + (metadata.PixelScaleY / 2.0), 10);
                metadata.DataStartLon = Math.Round(metadata.DataStartLon + (metadata.PixelScaleX / 2.0), 10);
                metadata.DataEndLat = Math.Round(metadata.DataEndLat - (metadata.PixelScaleY / 2.0), 10);
                metadata.DataEndLon = Math.Round(metadata.DataEndLon - (metadata.PixelScaleX / 2.0), 10);
            }
            else
            {
                metadata.PhysicalStartLat = metadata.DataStartLat;
                metadata.PhysicalStartLon = metadata.DataStartLon;
                metadata.PhysicalEndLat = metadata.DataEndLat;
                metadata.PhysicalEndLon = metadata.DataEndLon;
            }
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
            int yStart = 0;
            int yEnd = 0;
            int xStart = 0;
            int xEnd = 0;
            if (metadata.FileFormat.Registration == DEMFileRegistrationMode.Grid)
            {
                yStart = (int)Math.Floor((bbox.yMax - metadata.PhysicalEndLat) / metadata.pixelSizeY);
                yEnd = (int)Math.Ceiling((bbox.yMin - metadata.PhysicalEndLat) / metadata.pixelSizeY);
                xStart = (int)Math.Floor((bbox.xMin - metadata.PhysicalStartLon) / metadata.pixelSizeX);
                xEnd = (int)Math.Ceiling((bbox.xMax - metadata.PhysicalStartLon) / metadata.pixelSizeX);
            }
            else
            {
                yStart = (int)Math.Floor((bbox.yMax - metadata.DataEndLat) / metadata.pixelSizeY);
                yEnd = (int)Math.Ceiling((bbox.yMin - metadata.DataEndLat) / metadata.pixelSizeY);
                xStart = (int)Math.Floor((bbox.xMin - metadata.DataStartLon) / metadata.pixelSizeX);
                xEnd = (int)Math.Ceiling((bbox.xMax - metadata.DataStartLon) / metadata.pixelSizeX);
            }
            

            // Tiled geotiffs like aster have overlapping 1px borders
            int overlappingPixel = this.IsTiled ? 1 : 0;

            xStart = Math.Max(0, xStart);
            xEnd = Math.Min(metadata.Width - 1, xEnd) - overlappingPixel;
            yStart = Math.Max(0, yStart);
            yEnd = Math.Min(metadata.Height - 1, yEnd) - overlappingPixel;

            HeightMap heightMap = new HeightMap(xEnd - xStart + 1, yEnd - yStart + 1);
            heightMap.Count = heightMap.Width * heightMap.Height;
            var coords = new List<GeoPoint>(heightMap.Count);
            heightMap.BoundingBox = new BoundingBox(0, 0, 0, 0);

            if (this.IsTiled)
            {
                // Tiled rasters are composed of multiple "sub" images
                // TODO store in metadata
                int tileWidth = this.TileWidth;
                int tileHeight = this.TileHeight;
                int tileSize = this.TileSize;
                byte[] buffer;

                for (int y = yStart; y <= yEnd; y++)
                {
                    double latitude = metadata.DataEndLat + (metadata.pixelSizeY * y);
                    // bounding box
                    if (y == yStart)
                    {
                        heightMap.BoundingBox.yMax = latitude;
                        heightMap.BoundingBox.xMin = metadata.DataStartLon + (metadata.pixelSizeX * xStart);
                        heightMap.BoundingBox.xMax = metadata.DataStartLon + (metadata.pixelSizeX * xEnd);
                    }
                    if (y == yEnd)
                    {
                        heightMap.BoundingBox.yMin = latitude;
                    }

                    for (int x = xStart; x <= xEnd; x++)
                    {
                        double longitude = metadata.DataStartLon + (metadata.pixelSizeX * x);
                        var tileX = (x / tileWidth) * tileWidth;
                        var tileY = (y / tileHeight) * tileHeight;

                        if (tilesCache == null) tilesCache = new Dictionary<int, byte[]>();
                        var tileKey = (x / tileWidth) + (y / tileHeight) * (metadata.Width / tileWidth + 1);
                        if (!tilesCache.TryGetValue(tileKey, out buffer))
                        {
                            buffer = new byte[tileSize];
                            TiffFile.ReadTile(buffer, 0, tileX, tileY, 0, 0);
                            tilesCache.Add(tileKey, buffer);
                        }
                        var offset = x - tileX + (y - tileY) * tileHeight;
                        float heightValue = GetElevationAtPoint(metadata, offset, buffer);
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
            }
            else
            {
                // metadata.BitsPerSample
                // When 16 we have 2 bytes per sample
                // When 32 we have 4 bytes per sample
                int bytesPerSample = metadata.BitsPerSample / 8;
                byte[] byteScanline = new byte[metadata.ScanlineSize];
                double endLat = metadata.DataEndLat + metadata.pixelSizeY / 2d;
                double startLon = metadata.DataStartLon + metadata.pixelSizeX / 2d;

                for (int y = yStart; y <= yEnd; y++)
                {

                    TiffFile.ReadScanline(byteScanline, y);

                    // TODO: handle Cell registered DEMs: lat is 1/2 pixel off
                    double latitude = endLat + (metadata.pixelSizeY * y);

                    // bounding box
                    if (y == yStart)
                    {
                        heightMap.BoundingBox.yMax = latitude;
                        heightMap.BoundingBox.xMin = startLon + (metadata.pixelSizeX * xStart);
                        heightMap.BoundingBox.xMax = startLon + (metadata.pixelSizeX * xEnd);
                    }
                    else if (y == yEnd)
                    {
                        heightMap.BoundingBox.yMin = latitude;
                    }

                    for (int x = xStart; x <= xEnd; x++)
                    {
                        double longitude = startLon + (metadata.pixelSizeX * x);

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

            }
            heightMap.BoundingBox.zMin = heightMap.Minimum;
            heightMap.BoundingBox.zMax = heightMap.Maximum;
            Debug.Assert(heightMap.Width * heightMap.Height == coords.Count);

            heightMap.Coordinates = coords;
            return heightMap;
        }

        public HeightMap GetHeightMap(FileMetadata metadata)
        {
            if (this.isTiled)
                throw new NotImplementedException("Whole height map with tile geoTiff is not implemented");

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

                double latitude = metadata.DataStartLat + (metadata.pixelSizeY * y);
                for (int x = 0; x < metadata.Width; x++)
                {
                    double longitude = metadata.DataStartLon + (metadata.pixelSizeX * x);

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
