using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace DEM.Net.Core
{
    public class ASCIIGridFile : IRasterFile
    {
        private FileStream _fileStream;
        private StreamReader _streamReader;
        private readonly string _filename;
        private static char[] separator = new char[] { ' ' };

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

                int ncols = int.Parse(ncolsLine.Split(separator, StringSplitOptions.RemoveEmptyEntries)[1]);
                int nrows = int.Parse(nrowsLine.Split(separator, StringSplitOptions.RemoveEmptyEntries)[1]);
                double xllcorner = double.Parse(xllcornerLine.Split(separator, StringSplitOptions.RemoveEmptyEntries)[1], CultureInfo.InvariantCulture);
                double yllcorner = double.Parse(yllcornerLine.Split(separator, StringSplitOptions.RemoveEmptyEntries)[1], CultureInfo.InvariantCulture);
                double cellsize = double.Parse(cellsizeLine.Split(separator, StringSplitOptions.RemoveEmptyEntries)[1], CultureInfo.InvariantCulture);
                float NODATA_value = float.Parse(NODATA_valueLine.Split(separator, StringSplitOptions.RemoveEmptyEntries)[1], CultureInfo.InvariantCulture);

                FileMetadata metadata = new FileMetadata(_filename, fileFormat);
                metadata.BoundingBox = new 
                return metadata;

            }
            catch (Exception)
            {
                throw;
            }
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
