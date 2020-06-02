// HGTFile.cs
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core
{
    /// <summary>
    /// SRTM height file
    /// https://wiki.openstreetmap.org/wiki/SRTM
    /// 
    /// Official documentation : https://dds.cr.usgs.gov/srtm/version2_1/Documentation/SRTM_Topo.pdf
    /// </summary>
	public class HGTFile : IRasterFile
    {
        public const int HGT3601 = 25934402; // 3601 lines of 3601 samples of 2 bytes
        public const int HGT1201 = 2884802; // 1201 lines of 1201 samples of 2 bytes

        private Stream _hgtStream;
        private readonly string _filename;
        private readonly long _fileBytesCount;
        public HGTFile(string filename)
        {
            _filename = filename;
            _hgtStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            _fileBytesCount = (int)_hgtStream.Length;
        }
        public float GetElevationAtPoint(FileMetadata metadata, int x, int y)
        {
            float value = -32768;
            value = GetHGTValue(metadata, x, y);

            return value;
        }


        private float GetHGTValue(FileMetadata metadata, int x, int y)
        {

            int bytesPerSample = metadata.BitsPerSample / 8;
            byte[] byteScanline = new byte[metadata.ScanlineSize];

            _hgtStream.Seek(metadata.ScanlineSize * y, SeekOrigin.Begin);
            _hgtStream.Read(byteScanline, 0, metadata.ScanlineSize);

            float heightValue = 0;
            byte[] heightBytes = new byte[bytesPerSample];
            if (BitConverter.IsLittleEndian)
            {
                // reverse bytes
                for (int i = 0; i < bytesPerSample; i++)
                {
                    heightBytes[i] = byteScanline[x * bytesPerSample + bytesPerSample - i - 1];
                }
                switch (metadata.SampleFormat)
                {
                    case RasterSampleFormat.FLOATING_POINT:
                        heightValue = BitConverter.ToSingle(heightBytes, 0);
                        break;
                    case RasterSampleFormat.INTEGER:
                        heightValue = BitConverter.ToInt16(heightBytes, 0);
                        break;
                    case RasterSampleFormat.UNSIGNED_INTEGER:
                        heightValue = BitConverter.ToUInt16(heightBytes, 0);
                        break;
                    default:
                        throw new Exception("Sample format unsupported.");
                }
            }
            else
            {
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
            }

            return heightValue;
        }

        public FileMetadata ParseMetaData(DEMFileDefinition format)
        {

            FileMetadata metadata = new FileMetadata(_filename, format);

            int numPixels = _fileBytesCount == HGTFile.HGT1201 ? 1201 : 3601;

            if (format.Registration == DEMFileRegistrationMode.Grid)
            { ///
                metadata.Height = numPixels;
                metadata.Width = numPixels;
                metadata.PixelScaleX = 1d / (numPixels - 1);
                metadata.PixelScaleY = 1d / (numPixels - 1);
                metadata.pixelSizeX = metadata.PixelScaleX;
                metadata.pixelSizeY = -metadata.PixelScaleY;

                // fileName gives is coordinates of center of first lower left pixel (south west)
                // example N08E003.hgt
                // NASADEM: NASADEM_HGT_n43e005.hgt
                string fileTitle = Path.GetFileNameWithoutExtension(_filename).Split('_').Last().ToUpper();
                int latSign = fileTitle.Substring(0, 1) == "N" ? 1 : -1;
                int lonSign = fileTitle.Substring(3, 1) == "E" ? 1 : -1;
                int lat = int.Parse(fileTitle.Substring(1, 2)) * latSign;
                int lon = int.Parse(fileTitle.Substring(4, 3)) * lonSign;
                metadata.DataStartLon = lon;
                metadata.DataStartLat = lat;
                metadata.DataEndLon = lon + 1;
                metadata.DataEndLat = lat + 1;
                metadata.PhysicalStartLat = metadata.DataStartLat + (metadata.pixelSizeY / 2.0);
                metadata.PhysicalStartLon = metadata.DataStartLon - (metadata.pixelSizeX / 2.0);
                metadata.PhysicalEndLon = metadata.Width * metadata.pixelSizeX + metadata.DataStartLon;
                metadata.PhysicalEndLat = metadata.DataStartLat + metadata.Height * Math.Abs(metadata.pixelSizeY);


                metadata.ScanlineSize = numPixels * 2; // 16 bit signed integers


                metadata.BitsPerSample = 16;
                // Add other information about the data
                metadata.SampleFormat = RasterSampleFormat.INTEGER;
                // TODO: Read this from tiff metadata or determine after parsing
                metadata.NoDataValue = "-32768";
            }
            else
            {
                throw new NotSupportedException("HGT files should always be grid registered, not cell registered.");
                ///
                //metadata.Height = numPixels;
                //metadata.Width = numPixels;

                //metadata.PixelScaleX = 1d / numPixels;
                //metadata.PixelScaleY = 1d / numPixels;
                //metadata.pixelSizeX = metadata.PixelScaleX;
                //metadata.pixelSizeY = -metadata.PixelScaleY;

                //// fileName gives is coordinates of center of first lower left pixel (south west)
                //// example N08E003.hgt
                //string fileTitle = Path.GetFileNameWithoutExtension(_filename);
                //int latSign = fileTitle.Substring(0, 1) == "N" ? 1 : -1;
                //int lonSign = fileTitle.Substring(3, 1) == "E" ? 1 : -1;
                //int lat = int.Parse(fileTitle.Substring(1, 2)) * latSign;
                //int lon = int.Parse(fileTitle.Substring(4, 3)) * lonSign;
                //metadata.OriginLongitude = lon;
                //metadata.OriginLatitude = lat + 1;
                //metadata.StartLat = metadata.OriginLatitude + (metadata.pixelSizeY / 2.0);
                //metadata.StartLon = metadata.OriginLongitude + (metadata.pixelSizeX / 2.0);

                //metadata.ScanlineSize = numPixels * 2; // 16 bit signed integers


                //metadata.BitsPerSample = 16;
                //// Add other information about the data
                //metadata.SampleFormat = RasterSampleFormat.INTEGER;
                //// TODO: Read this from tiff metadata or determine after parsing
                //metadata.NoDataValue = "-32768";

                //return metadata;

            }

            return metadata;
        }

        public HeightMap GetHeightMapInBBox(BoundingBox bbox, FileMetadata metadata, float noDataValue = float.MinValue)
        {
            // metadata.BitsPerSample
            // When 16 we have 2 bytes per sample
            // When 32 we have 4 bytes per sample
            int bytesPerSample = metadata.BitsPerSample / 8;
            byte[] byteScanline = new byte[metadata.ScanlineSize];
            int registrationOffset = metadata.FileFormat.Registration == DEMFileRegistrationMode.Grid ? 1 : 0;

            int yNorth = (int)Math.Floor((bbox.yMax - metadata.PhysicalEndLat) / metadata.pixelSizeY);
            int ySouth = (int)Math.Ceiling((bbox.yMin - metadata.PhysicalEndLat) / metadata.pixelSizeY);
            int xWest = (int)Math.Floor((bbox.xMin - metadata.PhysicalStartLon) / metadata.pixelSizeX);
            int xEast = (int)Math.Ceiling((bbox.xMax - metadata.PhysicalStartLon) / metadata.pixelSizeX);

            xWest = Math.Max(0, xWest);
            xEast = Math.Min(metadata.Width - 1 - registrationOffset, xEast);
            yNorth = Math.Max(0, yNorth);
            ySouth = Math.Min(metadata.Height - 1 - registrationOffset, ySouth);

            HeightMap heightMap = new HeightMap(xEast - xWest + 1, ySouth - yNorth + 1);
            heightMap.Count = heightMap.Width * heightMap.Height;
            var coords = new List<GeoPoint>(heightMap.Count);
            heightMap.BoundingBox = new BoundingBox(0, 0, 0, 0);

            // Set position to ystart
            _hgtStream.Seek(yNorth * metadata.ScanlineSize, SeekOrigin.Begin);

            for (int y = yNorth; y <= ySouth; y++)
            {
                _hgtStream.Read(byteScanline, 0, metadata.ScanlineSize);

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

                    byte[] heightBytes = new byte[bytesPerSample];
                    float heightValue = 0;
                    if (BitConverter.IsLittleEndian)
                    {
                        // reverse bytes
                        for (int i = 0; i < bytesPerSample; i++)
                        {
                            heightBytes[i] = byteScanline[x * bytesPerSample + bytesPerSample - i - 1];
                        }
                        switch (metadata.SampleFormat)
                        {
                            case RasterSampleFormat.FLOATING_POINT:
                                heightValue = BitConverter.ToSingle(heightBytes, 0);
                                break;
                            case RasterSampleFormat.INTEGER:
                                heightValue = BitConverter.ToInt16(heightBytes, 0);
                                break;
                            case RasterSampleFormat.UNSIGNED_INTEGER:
                                heightValue = BitConverter.ToUInt16(heightBytes, 0);
                                break;
                            default:
                                throw new Exception("Sample format unsupported.");
                        }
                    }
                    else
                    {
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
                    }

                    if (heightValue < -10)
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
            heightMap.BoundingBox.zMin = heightMap.Minimum;
            heightMap.BoundingBox.zMax = heightMap.Maximum;
            Debug.Assert(heightMap.Width * heightMap.Height == coords.Count);

            heightMap.Coordinates = coords;
            return heightMap;
        }

        public HeightMap GetHeightMap(FileMetadata metadata)
        {
            HeightMap heightMap = new HeightMap(metadata.Width, metadata.Height);
            heightMap.Count = heightMap.Width * heightMap.Height;
            var coords = new List<GeoPoint>(heightMap.Count);

            _hgtStream.Seek(0, SeekOrigin.Begin);

            // metadata.BitsPerSample
            // When 16 we have 2 bytes per sample
            // When 32 we have 4 bytes per sample
            int bytesPerSample = metadata.BitsPerSample / 8;
            byte[] byteScanline = new byte[metadata.ScanlineSize];

            for (int y = 0; y < metadata.Height; y++)
            {
                _hgtStream.Read(byteScanline, 0, metadata.ScanlineSize);

                double latitude = metadata.PhysicalStartLat + (metadata.pixelSizeY * y);
                for (int x = 0; x < metadata.Width; x++)
                {
                    double longitude = metadata.PhysicalStartLon + (metadata.pixelSizeX * x);

                    float heightValue = 0;
                    byte[] heightBytes = new byte[bytesPerSample]; ;
                    if (BitConverter.IsLittleEndian)
                    {
                        // reverse bytes
                        for (int i = 0; i < bytesPerSample; i++)
                        {
                            heightBytes[i] = byteScanline[x * bytesPerSample + bytesPerSample - i - 1];
                        }
                        switch (metadata.SampleFormat)
                        {
                            case RasterSampleFormat.FLOATING_POINT:
                                heightValue = BitConverter.ToSingle(heightBytes, 0);
                                break;
                            case RasterSampleFormat.INTEGER:
                                heightValue = BitConverter.ToInt16(heightBytes, 0);
                                break;
                            case RasterSampleFormat.UNSIGNED_INTEGER:
                                heightValue = BitConverter.ToUInt16(heightBytes, 0);
                                break;
                            default:
                                throw new Exception("Sample format unsupported.");
                        }
                    }
                    else
                    {
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

        #region IDisposable Support
        private bool disposedValue = false; // Pour détecter les appels redondants

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _hgtStream?.Dispose();
                }

                // TODO: libérer les ressources non managées (objets non managés) et remplacer un finaliseur ci-dessous.
                // TODO: définir les champs de grande taille avec la valeur Null.

                disposedValue = true;
            }
        }

        // TODO: remplacer un finaliseur seulement si la fonction Dispose(bool disposing) ci-dessus a du code pour libérer les ressources non managées.
        // ~HGTFile() {
        //   // Ne modifiez pas ce code. Placez le code de nettoyage dans Dispose(bool disposing) ci-dessus.
        //   Dispose(false);
        // }

        // Ce code est ajouté pour implémenter correctement le modèle supprimable.
        public void Dispose()
        {
            // Ne modifiez pas ce code. Placez le code de nettoyage dans Dispose(bool disposing) ci-dessus.
            Dispose(true);
            // TODO: supprimer les marques de commentaire pour la ligne suivante si le finaliseur est remplacé ci-dessus.
            // GC.SuppressFinalize(this);
        }


        #endregion

    }
}
