using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace DEM.Net.Core
{
    /// <summary>
    /// An Esri grid is a raster GIS file format developed by Esri, which has two formats:
    /// 
    /// A proprietary binary format, also known as an ARC/INFO GRID, ARC GRID and many other variations
    /// A non-proprietary ASCII format, also known as an ARC/INFO ASCII GRID
    /// The formats were introduced for ARC/INFO. The binary format is widely used within Esri programs, such /// as ArcGIS, while the ASCII format is used as an exchange, or export format, due to the simple and portable ASCII file structure.
    /// 
    /// The grid defines geographic space as an array of equally sized square grid points arranged in rows and /// columns. Each grid point stores a numeric value that represents a geographic attribute (such as elevation or surface slope) for that unit of space. Each grid cell is referenced by its x,y coordinate location.
    /// https://en.wikipedia.org/wiki/Esri_grid
    /// Spec: http://help.arcgis.com/en/arcgisdesktop/10.0/help/index.html#//009t0000000w000000
    /// </summary>
    public class ASCIIGridFile : IRasterFile
    {
        private FileStream _fileStream;
        private StreamReader _streamReader;
        private GZipStream _gzipStream;
        private readonly string _filename;
        private static char[] SEPARATOR = new char[] { ' ' };

        List<List<string>> _data = null;
        private static Dictionary<string, List<List<string>>> _tempCache = new Dictionary<string, List<List<string>>>();

        public ASCIIGridFile(string fileName, bool gzip)
        {
            this._filename = fileName;
            _fileStream = new FileStream(_filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (gzip)
            {
                _gzipStream = new GZipStream(_fileStream, CompressionMode.Decompress);
                _streamReader = new StreamReader(_gzipStream, Encoding.ASCII);
            }
            else
            {
                _streamReader = new StreamReader(_fileStream, Encoding.ASCII);
            }
        }
        public float GetElevationAtPoint(FileMetadata metadata, int x, int y)
        {
            if (_data == null)
            {
                ReadAllFile(metadata);
            }

            string strXValue = _data[y][x];

            float elevation = float.Parse(strXValue, CultureInfo.InvariantCulture);
            return elevation;

        }

        private void ReadAllFile(FileMetadata metadata)
        {
            if (_tempCache.ContainsKey(_filename))
            {
                _data = _tempCache[_filename];
                return;
            }
            string curLine = null;
            _fileStream.Seek(0, SeekOrigin.Begin);

            // skip header
            for (int i = 1; i <= 6 /* + (y - 1)*/; i++)
            {
                curLine = _streamReader.ReadLine();
            }

            _data = new List<List<string>>(metadata.Height);
            while (!_streamReader.EndOfStream)
            {
                var line = _streamReader.ReadLine().Trim();

                var values = new List<string>(metadata.Width);
                var current = string.Empty;
                foreach (char c in line)
                {
                    if (c == ' ')
                    {
                        values.Add(current);
                        current = string.Empty;
                    }
                    else
                    {
                        current += c;
                    }
                }
                values.Add(current);
                //Debug.Assert(values.Count == metadata.Width);
                _data.Add(values);
            }
            _tempCache[_filename] = _data;
        }

        public HeightMap GetHeightMap(FileMetadata metadata)
        {
            throw new NotImplementedException();
        }

        public HeightMap GetHeightMapInBBox(BoundingBox bbox, FileMetadata metadata, float noDataValue = float.MinValue)
        {
            if (_data == null)
            {
                ReadAllFile(metadata);
            }
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

                    float heightValue = float.Parse(_data[y][x], CultureInfo.InvariantCulture);
                    if (heightValue == metadata.NoDataValueFloat) heightValue = noDataValue;
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

        public FileMetadata ParseMetaData(DEMFileDefinition fileFormat)
        {
            try
            {

                string ncolsLine = _streamReader.ReadLine();
                string nrowsLine = _streamReader.ReadLine();
                string xllcornerLine = _streamReader.ReadLine();
                string yllcornerLine = _streamReader.ReadLine();
                string cellsizeLine = _streamReader.ReadLine();
                string NODATA_valueLine = _streamReader.ReadLine();

                DebugCheckRegistrationType(fileFormat.Registration, xllcornerLine, yllcornerLine);

                int ncols = int.Parse(ncolsLine.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries)[1]);
                int nrows = int.Parse(nrowsLine.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries)[1]);
                double xllcorner = double.Parse(xllcornerLine.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries)[1], CultureInfo.InvariantCulture);
                double yllcorner = double.Parse(yllcornerLine.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries)[1], CultureInfo.InvariantCulture);
                double cellsize = double.Parse(cellsizeLine.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries)[1], CultureInfo.InvariantCulture);
                float NODATA_value = float.Parse(NODATA_valueLine.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries)[1], CultureInfo.InvariantCulture);

                FileMetadata metadata = new FileMetadata(_filename, fileFormat);
                metadata.Height = nrows;
                metadata.Width = ncols;
                metadata.PixelScaleX = cellsize;
                metadata.PixelScaleY = cellsize;
                metadata.pixelSizeX = cellsize;
                metadata.pixelSizeY = -cellsize;

                if (fileFormat.Registration == DEMFileRegistrationMode.Grid)
                {
                    metadata.DataStartLat = yllcorner;
                    metadata.DataStartLon = xllcorner;
                    metadata.DataEndLat = yllcorner + metadata.Height * metadata.pixelSizeY;
                    metadata.DataEndLon = xllcorner + metadata.Width * metadata.pixelSizeX;

                    metadata.PhysicalStartLat = yllcorner;
                    metadata.PhysicalStartLon = xllcorner;
                    metadata.PhysicalEndLat = metadata.DataEndLat;
                    metadata.PhysicalEndLon = metadata.DataEndLon;

                }
                else
                {
                    metadata.DataStartLat = Math.Round(yllcorner + (metadata.pixelSizeY / 2.0), 10);
                    metadata.DataStartLon = Math.Round(xllcorner - (metadata.PixelScaleX / 2.0), 10);
                    metadata.DataEndLat = yllcorner + metadata.Height * cellsize;
                    metadata.DataEndLon = xllcorner + metadata.Width * cellsize;
                    metadata.DataEndLat = Math.Round(metadata.DataEndLat - (metadata.PixelScaleY / 2.0), 10);
                    metadata.DataEndLon = Math.Round(metadata.DataEndLon - (metadata.PixelScaleX / 2.0), 10);

                    metadata.PhysicalStartLat = metadata.DataStartLat;
                    metadata.PhysicalStartLon = metadata.DataStartLon;
                    metadata.PhysicalEndLat = metadata.DataEndLat;
                    metadata.PhysicalEndLon = metadata.DataEndLon;
                }

                metadata.SampleFormat = RasterSampleFormat.FLOATING_POINT;
                metadata.NoDataValue = NODATA_value.ToString();
                return metadata;

            }
            catch (Exception)
            {
                throw;
            }
        }

        [Conditional("DEBUG")]
        private void DebugCheckRegistrationType(DEMFileRegistrationMode registration, string xllcornerLine, string yllcornerLine)
        {
            string xRegFromFile = xllcornerLine.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries)[0];
            string yRegFromFile = yllcornerLine.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries)[0];

            DEMFileRegistrationMode xModeFromFile = xRegFromFile.ToLower().EndsWith("corner") ? DEMFileRegistrationMode.Grid : DEMFileRegistrationMode.Cell;
            DEMFileRegistrationMode yModeFromFile = yRegFromFile.ToLower().EndsWith("corner") ? DEMFileRegistrationMode.Grid : DEMFileRegistrationMode.Cell;

            //Debug.Assert(registration == xModeFromFile && registration == yModeFromFile, "Registration mode mismatch between file and dataset.");

        }

        #region IDisposable Support
        private bool disposedValue = false; // Pour détecter les appels redondants

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    //_data?.Clear();
                    //_data = null;
                    _streamReader?.Dispose();
                    _fileStream?.Dispose();
                    _gzipStream?.Dispose();
                }

                disposedValue = true;
            }
        }

        // Ce code est ajouté pour implémenter correctement le modèle supprimable.
        public void Dispose()
        {
            Dispose(true);
        }


        #endregion
    }
}
