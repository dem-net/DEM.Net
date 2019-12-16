using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
        private readonly string _filename;
        private static char[] SEPARATOR = new char[] { ' ' };

        public ASCIIGridFile(string fileName)
        {
            this._filename = fileName;
            _fileStream = new FileStream(_filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            _streamReader = new StreamReader(_fileStream, Encoding.ASCII);
        }
        public float GetElevationAtPoint(FileMetadata metadata, int x, int y)
        {
            throw new NotImplementedException();
        }

        public HeightMap GetHeightMap(FileMetadata metadata)
        {
            throw new NotImplementedException();
        }

        public HeightMap GetHeightMapInBBox(BoundingBox bbox, FileMetadata metadata, float noDataValue = float.MinValue)
        {
            throw new NotImplementedException();
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
                metadata.pixelSizeX = metadata.PixelScaleX;
                metadata.pixelSizeY = -metadata.PixelScaleY;

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
                    metadata.DataStartLat = Math.Round(yllcorner + (metadata.PixelScaleY / 2.0), 10);
                    metadata.DataStartLon = Math.Round(xllcorner + (metadata.PixelScaleX / 2.0), 10);
                    metadata.DataEndLat = Math.Round(metadata.DataEndLat - (metadata.PixelScaleY / 2.0), 10);
                    metadata.DataEndLon = Math.Round(metadata.DataEndLon - (metadata.PixelScaleX / 2.0), 10);

                    metadata.PhysicalStartLat = metadata.DataStartLat;
                    metadata.PhysicalStartLon = metadata.DataStartLon;
                    metadata.DataEndLat = yllcorner + metadata.Height * metadata.pixelSizeY;
                    metadata.DataEndLon = xllcorner + metadata.Width * metadata.pixelSizeX;
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

            Debug.Assert(registration == xModeFromFile && registration == yModeFromFile, "Registration mode mismatch between file and dataset.");

        }

        #region IDisposable Support
        private bool disposedValue = false; // Pour détecter les appels redondants

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _streamReader?.Dispose();
                    _fileStream?.Dispose();
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
