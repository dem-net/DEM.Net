using DEM.Net.Core;
using Microsoft.Research.Science.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Importers.netCDF
{
    public class NetCdfFile : IRasterFile
    {
        private readonly string _filename;
        private DataSet _dataset;
        private const string LAT = "lat";
        private const string LONG = "lon";
        private const string ELEV = "elevation";

        #region Lifecycle
        public NetCdfFile(string fileName)
        {
            _filename = fileName;

            OpenDataset();
        }
        private void OpenDataset()
        {
            try
            {
                _dataset = DataSet.Open(_filename, ResourceOpenMode.ReadOnly);
                var varNames = new HashSet<string>(_dataset.Variables.Select(v=>v.Name));

                void CheckVarName(string varName)
                {
                    if (!varNames.Contains(varName))
                    {
                        throw new KeyNotFoundException($"NetCDF file must contain ${varName} variable.");
                    }
                }

                CheckVarName(LAT);
                CheckVarName(LONG);
                CheckVarName(ELEV);

            }
            catch (Exception ex)
            {
                throw new FileLoadException($"{nameof(NetCdfFile)}: Cannot open file {_filename}. Check if file exists and if netCdf binaries are installed on your system. See https://www.unidata.ucar.edu/software/netcdf/docs/winbin.html for installation instructions, or the repo ReadMe here: https://github.com/predictionmachines/SDSlite."
                    , _filename, ex);
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
                    _dataset?.Dispose();
                    _dataset = null;
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
        #endregion

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
            throw new NotImplementedException();
            //try
            //{
                
            //    string ncolsLine = _streamReader.ReadLine();
            //    string nrowsLine = _streamReader.ReadLine();
            //    string xllcornerLine = _streamReader.ReadLine();
            //    string yllcornerLine = _streamReader.ReadLine();
            //    string cellsizeLine = _streamReader.ReadLine();
            //    string NODATA_valueLine = _streamReader.ReadLine();

            //    DebugCheckRegistrationType(fileFormat.Registration, xllcornerLine, yllcornerLine);

            //    int ncols = int.Parse(ncolsLine.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries)[1]);
            //    int nrows = int.Parse(nrowsLine.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries)[1]);
            //    double xllcorner = double.Parse(xllcornerLine.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries)[1], CultureInfo.InvariantCulture);
            //    double yllcorner = double.Parse(yllcornerLine.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries)[1], CultureInfo.InvariantCulture);
            //    double cellsize = double.Parse(cellsizeLine.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries)[1], CultureInfo.InvariantCulture);
            //    float NODATA_value = float.Parse(NODATA_valueLine.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries)[1], CultureInfo.InvariantCulture);

            //    FileMetadata metadata = new FileMetadata(_filename, fileFormat);
            //    metadata.Height = nrows;
            //    metadata.Width = ncols;
            //    metadata.PixelScaleX = cellsize;
            //    metadata.PixelScaleY = cellsize;
            //    metadata.pixelSizeX = metadata.PixelScaleX;
            //    metadata.pixelSizeY = metadata.PixelScaleY;

            //    if (fileFormat.Registration == DEMFileRegistrationMode.Grid)
            //    {
            //        metadata.DataStartLat = yllcorner;
            //        metadata.DataStartLon = xllcorner;
            //        metadata.DataEndLat = yllcorner + metadata.Height * metadata.pixelSizeY;
            //        metadata.DataEndLon = xllcorner + metadata.Width * metadata.pixelSizeX;

            //        metadata.PhysicalStartLat = yllcorner;
            //        metadata.PhysicalStartLon = xllcorner;
            //        metadata.PhysicalEndLat = metadata.DataEndLat;
            //        metadata.PhysicalEndLon = metadata.DataEndLon;
            //    }
            //    else
            //    {
            //        metadata.DataStartLat = Math.Round(yllcorner + (metadata.PixelScaleY / 2.0), 10);
            //        metadata.DataStartLon = Math.Round(xllcorner + (metadata.PixelScaleX / 2.0), 10);
            //        metadata.DataEndLat = Math.Round(metadata.DataEndLat - (metadata.PixelScaleY / 2.0), 10);
            //        metadata.DataEndLon = Math.Round(metadata.DataEndLon - (metadata.PixelScaleX / 2.0), 10);

            //        metadata.PhysicalStartLat = metadata.DataStartLat;
            //        metadata.PhysicalStartLon = metadata.DataStartLon;
            //        metadata.DataEndLat = yllcorner + metadata.Height * metadata.pixelSizeY;
            //        metadata.DataEndLon = xllcorner + metadata.Width * metadata.pixelSizeX;
            //    }

            //    metadata.SampleFormat = RasterSampleFormat.FLOATING_POINT;
            //    metadata.NoDataValue = NODATA_value.ToString();
            //    return metadata;

            //}
            //catch (Exception)
            //{
            //    throw;
            //}
        }

        public string GetMetadataReport()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"=================== Dataset {_dataset.Name}");
            sb.AppendLine("Metadata:");
            foreach (var data in _dataset.Metadata)
            {
                sb.AppendLine($"{data.Key}:");
                sb.AppendLine($"{data.Value}");
            }
            sb.AppendLine($"=================== Variables ");
            foreach (var v in _dataset.Variables)
            {
                sb.AppendLine($"{v.Name}:");
                sb.AppendLine(v.ToString());
            }

            return sb.ToString();
        }
    }
}
