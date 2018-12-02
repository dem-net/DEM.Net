using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib
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
            switch (_fileBytesCount)
            {
                case HGTFile.HGT1201:
                    value = 0;// GetHGTValue(x, y, latAdj, lonAdj, 1200, 2402);
                    break;
                case HGTFile.HGT3601:
                    value = 0;// GetHGTValue(x, y, latAdj, lonAdj, 3600, 7202);
                    break;
            }

            return value;
        }


        //private float GetHGTValue(int x, int y, Stream hgtDataStream, int latAdj, int lonAdj, int width, int stride)
        //{
        //    hgtDataStream.Seek(0, SeekOrigin.Begin);

        //    double y = node.Latitude;
        //    double x = node.Longitude;
        //    var offset = ((int)((x - (int)x + lonAdj) * width) * 2 + (width - (int)((y - (int)y + latAdj) * width)) * stride);
        //    var h1 = hgtData[offset + 1] + hgtData[offset + 0] * 256;
        //    var h2 = hgtData[offset + 3] + hgtData[offset + 2] * 256;
        //    var h3 = hgtData[offset - stride + 1] + hgtData[offset - stride + 0] * 256;
        //    var h4 = hgtData[offset - stride + 3] + hgtData[offset - stride + 2] * 256;

        //    var m = Math.Max(h1, Math.Max(h2, Math.Max(h3, h4)));
        //    if (h1 == -32768)
        //        h1 = m;
        //    if (h2 == -32768)
        //        h2 = m;
        //    if (h3 == -32768)
        //        h3 = m;
        //    if (h4 == -32768)
        //        h4 = m;

        //    var fx = node.Longitude - (int)(node.Longitude);
        //    var fy = node.Latitude - (int)(node.Latitude);

        //    var elevation = (int)Math.Round((h1 * (1 - fx) + h2 * fx) * (1 - fy) + (h3 * (1 - fx) + h4 * fx) * fy);

        //    node.Elevation = elevation < -1000 ? 0 : elevation;
        //}
        //private float GetHGTValue(int x, int y, byte[] hgtData, int latAdj, int lonAdj, int width, int stride)
        //{

        //    double y = node.Latitude;
        //    double x = node.Longitude;
        //    var offset = ((int)((x - (int)x + lonAdj) * width) * 2 + (width - (int)((y - (int)y + latAdj) * width)) * stride);
        //    var h1 = hgtData[offset + 1] + hgtData[offset + 0] * 256;
        //    var h2 = hgtData[offset + 3] + hgtData[offset + 2] * 256;
        //    var h3 = hgtData[offset - stride + 1] + hgtData[offset - stride + 0] * 256;
        //    var h4 = hgtData[offset - stride + 3] + hgtData[offset - stride + 2] * 256;

        //    var m = Math.Max(h1, Math.Max(h2, Math.Max(h3, h4)));
        //    if (h1 == -32768)
        //        h1 = m;
        //    if (h2 == -32768)
        //        h2 = m;
        //    if (h3 == -32768)
        //        h3 = m;
        //    if (h4 == -32768)
        //        h4 = m;

        //    var fx = node.Longitude - (int)(node.Longitude);
        //    var fy = node.Latitude - (int)(node.Latitude);

        //    var elevation = (int)Math.Round((h1 * (1 - fx) + h2 * fx) * (1 - fy) + (h3 * (1 - fx) + h4 * fx) * fy);

        //    node.Elevation = elevation < -1000 ? 0 : elevation;
        //}

        public FileMetadata ParseMetaData()
        {
            FileMetadata metadata = new FileMetadata(_filename, DEMFileFormat.SRTM_HGT);

            int numPixels = _fileBytesCount == HGTFile.HGT1201 ? 1201 : 3601;

            ///
            metadata.Height = numPixels;
            metadata.Width = numPixels;

            metadata.PixelScaleX = 1f / numPixels;
            metadata.PixelScaleY = 1f / numPixels;
            metadata.pixelSizeX = metadata.PixelScaleX;
            metadata.pixelSizeY = -metadata.PixelScaleY;

            // fileName gives is coordinates of center of first lower left pixel (south west)
            // example N08E003.hgt
            string fileTitle = Path.GetFileNameWithoutExtension(_filename);
            int latSign = fileTitle.Substring(0, 1) == "N" ? 1 : -1;
            int lonSign = fileTitle.Substring(3, 1) == "E" ? 1 : -1;
            int lat = int.Parse(fileTitle.Substring(1, 2)) * latSign;
            int lon = int.Parse(fileTitle.Substring(4, 3)) * lonSign;
            metadata.OriginLongitude = lon;
            metadata.OriginLatitude = lat + 1;
            metadata.StartLat = metadata.OriginLatitude + (metadata.pixelSizeY / 2.0);
            metadata.StartLon = metadata.OriginLongitude + (metadata.pixelSizeX / 2.0);

            metadata.ScanlineSize = numPixels * 2; // 16 bit signed integers


            metadata.BitsPerSample = 16;
            // Add other information about the data
            metadata.SampleFormat = RasterSampleFormat.INTEGER;
            // TODO: Read this from tiff metadata or determine after parsing
            metadata.NoDataValue = "-32768";

            return metadata;
        }

        public HeightMap GetHeightMapInBBox(BoundingBox bbox, FileMetadata metadata, float noDataValue = float.MinValue)
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

            // Set position to ystart
            _hgtStream.Seek(yStart * metadata.ScanlineSize, SeekOrigin.Begin);

            for (int y = yStart; y <= yEnd; y++)
            {
                _hgtStream.Read(byteScanline, 0, metadata.ScanlineSize);

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

                    byte[] heightBytes = new byte[bytesPerSample];
                    float heightValue = 0;
                    if (BitConverter.IsLittleEndian)
                    {
                        // reverse bytes
                        for (int i = 0; i < bytesPerSample; i++)
                        {
                            heightBytes[i] = byteScanline[x * bytesPerSample + bytesPerSample - i-1];
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
                        heightMap.Mininum = Math.Min(heightMap.Mininum, heightValue);
                        heightMap.Maximum = Math.Max(heightMap.Maximum, heightValue);
                    }
                    else if (heightValue < 32768)
                    {
                        heightMap.Mininum = Math.Min(heightMap.Mininum, heightValue);
                        heightMap.Maximum = Math.Max(heightMap.Maximum, heightValue);
                    }
                    else
                    {
                        heightValue = (float)noDataValue;
                    }
                    coords.Add(new GeoPoint(latitude, longitude, heightValue, x, y));

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

            _hgtStream.Seek(0, SeekOrigin.Begin);

            // metadata.BitsPerSample
            // When 16 we have 2 bytes per sample
            // When 32 we have 4 bytes per sample
            int bytesPerSample = metadata.BitsPerSample / 8;
            byte[] byteScanline = new byte[metadata.ScanlineSize];

            for (int y = 0; y < metadata.Height; y++)
            {
                _hgtStream.Read(byteScanline, 0, metadata.ScanlineSize);

                double latitude = metadata.StartLat + (metadata.pixelSizeY * y);
                for (int x = 0; x < metadata.Width; x++)
                {
                    double longitude = metadata.StartLon + (metadata.pixelSizeX * x);

                    float heightValue = 0;
                    byte[] heightBytes = new byte[bytesPerSample];;
                    if (BitConverter.IsLittleEndian)
                    {
                        // reverse bytes
                        for (int i = 0; i < bytesPerSample; i++)
                        {
                            heightBytes[i] = byteScanline[x * bytesPerSample + bytesPerSample - i-1];
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
                        heightMap.Mininum = Math.Min(metadata.MininumAltitude, heightValue);
                        heightMap.Maximum = Math.Max(metadata.MaximumAltitude, heightValue);
                    }
                    else
                    {
                        heightValue = 0;
                    }
                    coords.Add(new GeoPoint(latitude, longitude, heightValue, x, y));

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
