using System;
using System.IO;
using System.IO.Compression;
using DEM.Net.Core;
using DEM.Net.Core.Configuration;
using DEM.Net.Core.Interpolation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace DEM.Net.Test;

public class RasterTests(DemNetFixture fixture) : IClassFixture<DemNetFixture>
{
    RasterService _rasterService = fixture.ServiceProvider.GetRequiredService<RasterService>();
    ElevationService _elevationService = fixture.ServiceProvider.GetRequiredService<ElevationService>();

    [Fact]
    public void Test_FileFormat_GeoTIFF()
    {

        string fileName = "N043E005_AVE_DSM.tif";
        string sourceFile = Path.Combine("TestData",  fileName + ".zip");
        bool fileOk = File.Exists(sourceFile);

        Assert.True(fileOk, "TestData is missing");

        if (!File.Exists(fileName))
        {
            ZipFile.ExtractToDirectory(sourceFile, ".");
        }

        Assert.True(File.Exists(fileName), "Unzip failed.");

        // Pass the full file name
        fileName = Path.GetFullPath(fileName);
        using IRasterFile raster = _rasterService.OpenFile(fileName, DEMFileType.GEOTIFF);
        FileMetadata metaData = raster.ParseMetaData(new DEMFileDefinition(DEMFileType.GEOTIFF, DEMFileRegistrationMode.Cell));
        Assert.NotNull(metaData);

        float elevation = raster.GetElevationAtPoint(metaData, 1000, 200);
        Assert.Equal(304f, elevation);

        elevation = raster.GetElevationAtPoint(metaData, 200, 1000);
        Assert.Equal(95f, elevation);
    }

    [Fact]
    public void Test_FileFormat_HGT()
    {
        string fileName = "N43E005.hgt";
        string sourceFile = Path.Combine("TestData", fileName + ".zip");
        bool fileOk = File.Exists(sourceFile);

        Assert.True(fileOk, "TestData is missing");

        if (!File.Exists(fileName))
        {
            ZipFile.ExtractToDirectory(sourceFile, ".");
        }

        Assert.True(File.Exists(fileName), "Unzip failed.");

        // Pass the full file name
        fileName = Path.GetFullPath(fileName);
        using IRasterFile raster = _rasterService.OpenFile(fileName, DEMFileType.SRTM_HGT);
        FileMetadata metaData = raster.ParseMetaData(new DEMFileDefinition(DEMFileType.SRTM_HGT, DEMFileRegistrationMode.Grid));
        Assert.NotNull(metaData);

        float elevation = raster.GetElevationAtPoint(metaData, 300, 10);
        Assert.Equal(751f, elevation);

        elevation = raster.GetElevationAtPoint(metaData, 10, 300);
        Assert.Equal(297f, elevation);
    }

    [Fact]
    public void Test_FileFormat_HGT_OutOfRange()
    {
        string fileName = "N43E005.hgt";
        string sourceFile = Path.Combine("TestData", fileName + ".zip");
        bool fileOk = File.Exists(sourceFile);

        Assert.True(fileOk, "TestData is missing");

        if (!File.Exists(fileName))
        {
            ZipFile.ExtractToDirectory(sourceFile, ".");
        }

        Assert.True(File.Exists(fileName), "Unzip failed.");

        // Pass the full file name
        fileName = Path.GetFullPath(fileName);
        using IRasterFile raster = _rasterService.OpenFile(fileName, DEMFileType.SRTM_HGT);
        FileMetadata metaData = raster.ParseMetaData(new DEMFileDefinition(DEMFileType.SRTM_HGT, DEMFileRegistrationMode.Grid));
        Assert.NotNull(metaData);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
       {
           float elevation = raster.GetElevationAtPoint(metaData, 10000, 10000);
       });
        // Expect exception
    }

    [Fact]
    public void RasterService_WithoutLogger_DoesNotThrow()
    {
        var resolver = fixture.ServiceProvider.GetRequiredService<RasterIndexServiceResolver>();
        string directory = Path.Combine(Path.GetTempPath(), "DEM.Net.Tests", Guid.NewGuid().ToString("N"));
        try
        {
            var service = new RasterService(resolver, Options.Create(new DEMNetOptions { LocalDirectory = directory }));
            Assert.Equal(directory, service.LocalDirectory);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void GetPointElevation_NullInterpolator_UsesBilinear()
    {
        string fileName = "N43E005.hgt";
        if (!File.Exists(fileName))
        {
            ZipFile.ExtractToDirectory(Path.Combine("TestData", fileName + ".zip"), ".");
        }

        using IRasterFile raster = _rasterService.OpenFile(Path.GetFullPath(fileName), DEMFileType.SRTM_HGT);
        FileMetadata metaData = raster.ParseMetaData(new DEMFileDefinition(DEMFileType.SRTM_HGT, DEMFileRegistrationMode.Grid));

        // A point between grid nodes, so interpolation is required
        const double lat = 43.50013, lon = 5.50021;
        float withDefault = _elevationService.GetPointElevation(raster, metaData, lat, lon);
        float withBilinear = _elevationService.GetPointElevation(raster, metaData, lat, lon, new BilinearInterpolator());

        Assert.Equal(withBilinear, withDefault);
    }
}
