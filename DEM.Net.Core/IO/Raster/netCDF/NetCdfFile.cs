using Microsoft.Research.Science.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DEM.Net.Core.netCDF;

namespace DEM.Net.Core
{
    /// <summary>
    /// https://www.gebco.net/data_and_products/gridded_bathymetry_data/gebco_2019/gebco_2019_info.html
    /// The netCDF storage is arranged as contiguous latitudinal bands.
    /// </summary>
    public class NetCdfFile : IRasterFile
    {
        private readonly string _filename;
        private DataSet _dataset;
        private netCDFField _lat = netCDFField.Create<double>("lat");
        private netCDFField _long = netCDFField.Create<double>("lon");
        private netCDFField _elevation = netCDFField.Create<float>("elevation");
        private Variable _latVariable;
        private Variable _longVariable;
        private Variable _elevationVariable;

        #region Lifecycle
        public NetCdfFile(string fileName, netCDFField latitudeField = null, netCDFField longitudeField = null, netCDFField elevationField = null)
        {
            _filename = fileName;

            _lat = latitudeField ?? _lat;
            _long = longitudeField ?? _long;
            _elevation = elevationField ?? _elevation;

            OpenDataset();
        }
        private void OpenDataset()
        {
            try
            {
                _dataset = DataSet.Open(_filename, ResourceOpenMode.ReadOnly);
                var varNamesAndTypes = _dataset.Variables.ToDictionary(v => v.Name, v => v.TypeOfData);

                void CheckVariable(netCDFField field)
                {
                    if (!varNamesAndTypes.ContainsKey(field.FieldName))
                    {
                        throw new KeyNotFoundException($"NetCDF file must contain ${field.FieldName} variable.");
                    }
                    else if (!varNamesAndTypes[field.FieldName].Equals(field.Type))
                    {
                        throw new InvalidCastException($"NetCDF variable {field.FieldName} is of type {varNamesAndTypes[field.FieldName].Name} and doesn't match excpected type {field.Type.Name}.");
                    }
                }

                CheckVariable(_lat);
                CheckVariable(_long);
                CheckVariable(_elevation);
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
            float height = (float)_elevationVariable.GetData(new int[] { x, y }, new int[] { 1, 1 }).GetValue(0, 0);
            return height;
        }

        public HeightMap GetHeightMap(FileMetadata metadata)
        {
            HeightMap heightMap = new HeightMap(metadata.Width, metadata.Height);
            heightMap.Count = heightMap.Width * heightMap.Height;
            var coords = new List<GeoPoint>(heightMap.Count);

            MultipleDataResponse response = _dataset.GetMultipleData(
                DataRequest.GetData(_elevationVariable),
                DataRequest.GetData(_latVariable),
                DataRequest.GetData(_longVariable));

            Array latitudes = response[_latVariable.ID].Data;
            Array longitudes = response[_longVariable.ID].Data;
            Array elevations = response[_elevationVariable.ID].Data;

            int index = 0;

            var elevationsEnumerator = elevations.GetEnumerator();
            foreach (double longitude in longitudes)
            {
                foreach (double latitude in latitudes)
                {
                    elevationsEnumerator.MoveNext();
                    float heightValue = (float)elevationsEnumerator.Current;

                    coords.Add(new GeoPoint(latitude, longitude, heightValue));

                    index++;
                }
            }

            Debug.Assert(index == heightMap.Count);
            heightMap.Coordinates = coords;
            return heightMap;
        }

        public HeightMap GetHeightMapInBBox(BoundingBox bbox, FileMetadata metadata, float noDataValue = float.MinValue)
        {
            int registrationOffset = metadata.FileFormat.Registration == DEMFileRegistrationMode.Grid ? 1 : 0;

            int yNorth = (int)Math.Floor((bbox.yMax - metadata.PhysicalStartLat) / metadata.pixelSizeY);
            int ySouth = (int)Math.Ceiling((bbox.yMin - metadata.PhysicalStartLat) / metadata.pixelSizeY);
            int xWest = (int)Math.Floor((bbox.xMin - metadata.PhysicalStartLon) / metadata.pixelSizeX);
            int xEast = (int)Math.Ceiling((bbox.xMax - metadata.PhysicalStartLon) / metadata.pixelSizeX);

            xWest = Math.Max(0, xWest);
            xEast = Math.Min(metadata.Width - 1 - registrationOffset, xEast);
            yNorth = Math.Max(0, yNorth);
            ySouth = Math.Min(metadata.Height - 1 - registrationOffset, ySouth);

            HeightMap heightMap = new HeightMap(xEast - xWest + 1, yNorth - ySouth + 1);
            heightMap.Count = heightMap.Width * heightMap.Height;
            var coords = new List<GeoPoint>(heightMap.Count);
            heightMap.BoundingBox = new BoundingBox(0, 0, 0, 0);

            // The netCDF storage is arranged as contiguous latitudinal bands.
            MultipleDataResponse response = _dataset.GetMultipleData(
                DataRequest.GetData(_elevationVariable, new int[] { xWest, ySouth }, new int[] { heightMap.Width, heightMap.Height }),
                DataRequest.GetData(_latVariable, new int[] { ySouth }, new int[] { heightMap.Height }),
                DataRequest.GetData(_longVariable, new int[] { xWest }, new int[] { heightMap.Width }));

            Array latitudes = response[_latVariable.ID].Data;
            Array longitudes = response[_longVariable.ID].Data;
            Array elevations = response[_elevationVariable.ID].Data;

            int index = 0;

            var elevationsEnumerator = elevations.GetEnumerator();
            foreach (double longitude in longitudes)
            {
                foreach (double latitude in latitudes)
                {
                    elevationsEnumerator.MoveNext();
                    float heightValue = (float)elevationsEnumerator.Current;

                    coords.Add(new GeoPoint(latitude, longitude, heightValue));

                    index++;
                }
            }

            Debug.Assert(index == heightMap.Count);
            heightMap.Coordinates = coords;
            return heightMap;
        }

        public FileMetadata ParseMetaData(DEMFileDefinition fileFormat)
        {
            // Data origin is lower left corner
            try
            {
                int[] shape = new int[1];
                shape[0] = 2;
                int ncols = _longVariable.Dimensions.First().Length;
                int nrows = _latVariable.Dimensions.First().Length;

                Array longValues = _longVariable.GetData(null, shape);
                Array latValues = _latVariable.GetData(null, shape);
                double xllcorner = (double)longValues.GetValue(0);
                double yllcorner = (double)latValues.GetValue(0);
                double cellsizex = (double)longValues.GetValue(1) - (double)longValues.GetValue(0);
                double cellsizey = (double)latValues.GetValue(1) - (double)latValues.GetValue(0);
                float NODATA_value = -9999f;

                FileMetadata metadata = new FileMetadata(_filename, fileFormat)
                {
                    Height = nrows,
                    Width = ncols,
                    PixelScaleX = cellsizex,
                    PixelScaleY = cellsizey
                };
                metadata.pixelSizeX = metadata.PixelScaleX;
                metadata.pixelSizeY = metadata.PixelScaleY;

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

                    metadata.PhysicalStartLat = metadata.DataStartLat;
                    metadata.PhysicalStartLon = metadata.DataStartLon;
                    metadata.PhysicalEndLat = Math.Round(yllcorner + metadata.Height * metadata.pixelSizeY + (metadata.PixelScaleY / 2.0), 10);
                    metadata.PhysicalEndLon = Math.Round(xllcorner + metadata.Width * metadata.pixelSizeX + (metadata.PixelScaleX / 2.0), 10);
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
