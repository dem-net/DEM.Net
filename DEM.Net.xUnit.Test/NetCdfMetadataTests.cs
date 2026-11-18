using DEM.Net.Core;
using Xunit;

namespace DEM.Net.Test;

/// <summary>
/// GEBCO netCDF files store latitudes ascending (south to north). Their metadata must describe the grid north up,
/// like GeoTIFF rasters, since the line elevation code walks rows from the north edge with a negative pixelSizeY
/// (it used to throw "DEM with y axis upwards not supported" on GEBCO).
/// </summary>
public class NetCdfMetadataTests
{
    private const double Cell = 1d / 240; // GEBCO: 15 arc seconds
    private static readonly DEMFileDefinition CellFormat = new("netCDF file", DEMFileType.CF_NetCDF, ".nc", DEMFileRegistrationMode.Cell);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GridIsDescribedNorthUp_WhateverTheStorageOrder(bool latitudesAscending)
    {
        // 1° x 1° grid of cell centers covering 6..7°E, 45..46°N
        double firstLat = latitudesAscending ? 45 + Cell / 2 : 46 - Cell / 2;
        var metadata = NetCdfFile.CreateMetadata("gebco.nc", CellFormat, 240, 240,
            firstLon: 6 + Cell / 2, firstLat: firstLat, stepLon: Cell, stepLat: latitudesAscending ? Cell : -Cell);

        Assert.True(metadata.pixelSizeY < 0);
        Assert.Equal(Cell, metadata.PixelScaleY, 12);
        Assert.Equal(46, metadata.PhysicalEndLat, 9);
        Assert.Equal(45, metadata.PhysicalStartLat, 9);
        Assert.Equal(6, metadata.PhysicalStartLon, 9);
        Assert.Equal(7, metadata.PhysicalEndLon, 9);
    }

    [Fact]
    public void CellBoundsMatchTheGeoTiffConventions()
    {
        // GeoTiff.ParseMetaData, cell registration, tie point at the north west corner (6°E, 46°N)
        var metadata = NetCdfFile.CreateMetadata("gebco.nc", CellFormat, 240, 240, 6 + Cell / 2, 45 + Cell / 2, Cell, Cell);

        Assert.Equal(6, metadata.DataStartLon, 9);
        Assert.Equal(6 + 239 * Cell, metadata.DataEndLon, 9);
        Assert.Equal(46, metadata.DataEndLat, 9);
        Assert.Equal(46 - 239 * Cell, metadata.DataStartLat, 9);
    }

    [Fact]
    public void NorthRowCenterIsRowZero()
    {
        // The pixel row the elevation service computes for a cell registered raster (GetElevationAtPoint)
        var metadata = NetCdfFile.CreateMetadata("gebco.nc", CellFormat, 240, 240, 6 + Cell / 2, 45 + Cell / 2, Cell, Cell);

        double northRowCenter = 46 - Cell / 2;
        double southRowCenter = 45 + Cell / 2;
        double Row(double lat) => (lat - (metadata.PhysicalEndLat + metadata.pixelSizeY / 2)) / metadata.pixelSizeY;

        Assert.Equal(0, Row(northRowCenter), 9);
        Assert.Equal(239, Row(southRowCenter), 9);
    }
}
