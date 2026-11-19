using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DEM.Net.Core.netCDF;
using Microsoft.Research.Science.Data;

namespace DEM.Net.Core;

/// <summary>
/// https://www.gebco.net/data_and_products/gridded_bathymetry_data/gebco_2019/gebco_2019_info.html
/// The netCDF storage is arranged as contiguous latitudinal bands.
/// </summary>
public class NetCdfFile : IRasterFile
{
    private readonly string _filename;
    private DataSet _dataset = null!;
    private netCDFField _lat = netCDFField.Create<double>("lat");
    private netCDFField _long = netCDFField.Create<double>("lon");
    private netCDFField _elevation = netCDFField.Create<float>("elevation");
    private Variable _latVariable = null!;
    private Variable _longVariable = null!;
    private Variable _elevationVariable = null!;

    public NetCdfFile(string fileName, netCDFField? latitudeField = null, netCDFField? longitudeField = null, netCDFField? elevationField = null)
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
            var variablesByName = _dataset.Variables.ToDictionary(v => v.Name, v => v);

            Variable CheckVariable(netCDFField field)
            {
                if (!variablesByName.ContainsKey(field.FieldName))
                {
                    throw new KeyNotFoundException($"NetCDF file must contain ${field.FieldName} variable.");
                }
                else if (!variablesByName[field.FieldName].TypeOfData.Equals(field.Type))
                {
                    throw new InvalidCastException($"NetCDF variable {field.FieldName} is of type {variablesByName[field.FieldName].Name} and doesn't match excpected type {field.Type.Name}.");
                }
                return variablesByName[field.FieldName];
            }

            _latVariable = CheckVariable(_lat);
            _longVariable = CheckVariable(_long);
            _elevationVariable = CheckVariable(_elevation);
        }
        catch (Exception ex)
        {
            throw new FileLoadException($"{nameof(NetCdfFile)}: Cannot open file {_filename}. Check if file exists and if netCdf binaries are installed on your system. See https://www.unidata.ucar.edu/software/netcdf/docs/winbin.html for installation instructions, or the repo ReadMe here: https://github.com/predictionmachines/SDSlite."
                , _filename, ex);
        }
    }

    private bool disposedValue = false; // Pour détecter les appels redondants


    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _dataset?.Dispose();
                _dataset = null!;
            }

            disposedValue = true;
        }
    }

    // Ce code est ajouté pour implémenter correctement le modèle supprimable.
    public void Dispose()
    {
        Dispose(true);
    }

    /// <summary>
    /// netCDF row index of a metadata row: metadata rows count from the north (see <see cref="CreateMetadata"/>),
    /// netCDF rows follow the latitude variable, which is ascending (south to north) in GEBCO files.
    /// </summary>
    private int StorageRow(FileMetadata metadata, int northUpRow) => IsSouthUp ? metadata.Height - 1 - northUpRow : northUpRow;

    /// <summary>True when the latitude variable is ascending (first row is the southernmost one), as in GEBCO files.</summary>
    private bool IsSouthUp
    {
        get
        {
            if (_southUp is null)
            {
                Array latValues = _latVariable.GetData(null, [2]);
                _southUp = (double)latValues.GetValue(1)! > (double)latValues.GetValue(0)!;
            }
            return _southUp.Value;
        }
    }
    private bool? _southUp;

    public float GetElevationAtPoint(FileMetadata metadata, int x, int y)
    {
        float height = Convert.ToSingle(_elevationVariable.GetData([StorageRow(metadata, y), x], [1, 1]).GetValue(0, 0));
        return height;
    }

    public HeightMap GetHeightMap(FileMetadata metadata)
        => ReadBlock(0, metadata.Height - 1, 0, metadata.Width - 1, metadata);

    public HeightMap GetHeightMapInBBox(BoundingBox bbox, FileMetadata metadata, float noDataValue = float.MinValue)
    {
        try
        {
            // Same rows and columns as GeoTiff.GetHeightMapInBBox: metadata rows count from the north (pixelSizeY < 0)
            int yStart, yEnd, xStart, xEnd;
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

            return ReadBlock(Math.Max(0, yStart), Math.Min(metadata.Height - 1, yEnd),
                             Math.Max(0, xStart), Math.Min(metadata.Width - 1, xEnd), metadata);
        }
        catch (Exception ex)
        {
            Trace.TraceError($"{nameof(NetCdfFile)} error: {ex.Message}");
            return null!;
        }
    }

    /// <summary>
    /// Reads metadata rows <paramref name="yNorth"/> to <paramref name="ySouth"/> (counted from the north) and columns
    /// <paramref name="xWest"/> to <paramref name="xEast"/>, as a height map with rows in latitude descending order.
    /// </summary>
    private HeightMap ReadBlock(int yNorth, int ySouth, int xWest, int xEast, FileMetadata metadata)
    {
        var heightMap = new HeightMap(xEast - xWest + 1, ySouth - yNorth + 1);
        heightMap.Count = heightMap.Width * heightMap.Height;
        heightMap.Minimum = float.MaxValue;
        heightMap.Maximum = float.MinValue;

        // The netCDF storage is arranged as contiguous latitudinal bands, in the latitude variable order
        int storageStart = IsSouthUp ? StorageRow(metadata, ySouth) : yNorth;
        MultipleDataResponse response = _dataset.GetMultipleData(
            DataRequest.GetData(_elevationVariable, [storageStart, xWest], [heightMap.Height, heightMap.Width]),
            DataRequest.GetData(_latVariable, [storageStart], [heightMap.Height]),
            DataRequest.GetData(_longVariable, [xWest], [heightMap.Width]));

        Array latitudes = response[_latVariable.ID].Data;
        Array longitudes = response[_longVariable.ID].Data;
        Array elevations = response[_elevationVariable.ID].Data;

        // Height map rows are in latitude descending order: a south-up file fills them from the last one
        var coords = new GeoPoint[heightMap.Count];
        int index = 0;
        int row = IsSouthUp ? heightMap.Height - 1 : 0;
        int rowStep = IsSouthUp ? -1 : 1;
        var elevationsEnumerator = elevations.GetEnumerator();
        foreach (double latitude in latitudes)
        {
            int coordIndex = row * heightMap.Width;
            foreach (double longitude in longitudes)
            {
                elevationsEnumerator.MoveNext();
                float heightValue = Convert.ToSingle(elevationsEnumerator.Current);

                coords[coordIndex++] = new GeoPoint(latitude, longitude, heightValue);
                heightMap.Minimum = Math.Min(heightMap.Minimum, heightValue);
                heightMap.Maximum = Math.Max(heightMap.Maximum, heightValue);
                index++;
            }
            row += rowStep;
        }

        Debug.Assert(index == heightMap.Count);
        heightMap.Coordinates = coords;
        heightMap.BoundingBox.zMin = heightMap.Minimum;
        heightMap.BoundingBox.zMax = heightMap.Maximum;
        return heightMap;
    }

    public FileMetadata ParseMetaData(DEMFileDefinition fileFormat)
    {
        int ncols = _longVariable.Dimensions.First().Length;
        int nrows = _latVariable.Dimensions.First().Length;

        Array longValues = _longVariable.GetData(null, [2]);
        Array latValues = _latVariable.GetData(null, [2]);
        double firstLon = (double)longValues.GetValue(0)!;
        double firstLat = (double)latValues.GetValue(0)!;
        double stepLon = (double)longValues.GetValue(1)! - firstLon;
        double stepLat = (double)latValues.GetValue(1)! - firstLat;

        return CreateMetadata(_filename, fileFormat, ncols, nrows, firstLon, firstLat, stepLon, stepLat);
    }

    /// <summary>
    /// Metadata of a netCDF grid, given its coordinate variables (cell centers): first values and steps.
    /// Bounds follow the GeoTIFF conventions (<see cref="GeoTiff.ParseMetaData"/>), north up whatever the storage
    /// order: <see cref="FileMetadata.pixelSizeY"/> is negative and rows count from the north edge, so the generic
    /// sampling and line code works on GEBCO files, whose latitudes are stored ascending (south to north).
    /// </summary>
    internal static FileMetadata CreateMetadata(string fileName, DEMFileDefinition fileFormat, int width, int height,
        double firstLon, double firstLat, double stepLon, double stepLat)
    {
        double cellX = Math.Abs(stepLon);
        double cellY = Math.Abs(stepLat);
        double westCenter = Math.Min(firstLon, firstLon + (width - 1) * stepLon);
        double northCenter = Math.Max(firstLat, firstLat + (height - 1) * stepLat);

        var metadata = new FileMetadata(fileName, fileFormat)
        {
            Height = height,
            Width = width,
            PixelScaleX = cellX,
            PixelScaleY = cellY,
            pixelSizeX = cellX,
            pixelSizeY = -cellY,
            SampleFormat = RasterSampleFormat.FLOATING_POINT,
            NoDataValue = (-9999f).ToString(System.Globalization.CultureInfo.InvariantCulture),
        };

        if (fileFormat.Registration == DEMFileRegistrationMode.Grid)
        {
            // Data bounds are the outer sample positions, the physical extent is half a pixel larger
            metadata.DataStartLon = westCenter;
            metadata.DataEndLon = westCenter + (width - 1) * cellX;
            metadata.DataEndLat = northCenter;
            metadata.DataStartLat = northCenter - (height - 1) * cellY;

            metadata.PhysicalStartLon = Math.Round(metadata.DataStartLon - cellX / 2d, 10);
            metadata.PhysicalEndLon = Math.Round(metadata.DataEndLon + cellX / 2d, 10);
            metadata.PhysicalStartLat = Math.Round(metadata.DataStartLat - cellY / 2d, 10);
            metadata.PhysicalEndLat = Math.Round(metadata.DataEndLat + cellY / 2d, 10);
        }
        else
        {
            // Cell registration: Data* bounds are pixel corners (first pixel to last pixel), the physical extent is
            // Width x Height pixels from the north west corner
            double north = northCenter + cellY / 2d;
            double west = westCenter - cellX / 2d;
            metadata.DataStartLon = west;
            metadata.DataEndLon = west + (width - 1) * cellX;
            metadata.DataEndLat = north;
            metadata.DataStartLat = north - (height - 1) * cellY;

            metadata.PhysicalStartLon = west;
            metadata.PhysicalEndLon = west + width * cellX;
            metadata.PhysicalEndLat = north;
            metadata.PhysicalStartLat = north - height * cellY;
        }

        return metadata;
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
