using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DEM.Net.Core;
using DEM.Net.Core.Configuration;
using DEM.Net.Core.Datasets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DEM.Net.Test;

/// <summary>
/// GEBCO_2026, a local file system dataset (one COG), offline: the dataset directory holds a sample of the published
/// file instead of the 3.3 GB global grid. TestData/GEBCO_2026_Gibraltar.tif is a 3°x2° window (7°W-4°W, 35°N-37°N)
/// cut from the published COG with the same encoding (BigTIFF, Int16, Deflate + predictor 2, 512px tiles, PixelIsArea).
/// Expected values were read with GDAL (gdallocationinfo) at pixel centers.
/// </summary>
public sealed class Gebco2026Tests : IDisposable
{
    private const string SampleFile = "GEBCO_2026_Gibraltar.tif";

    private readonly string _root = Path.Combine(Path.GetTempPath(), "DEM.Net.Tests", Guid.NewGuid().ToString("N"));
    private readonly ServiceProvider _services;
    private readonly ElevationService _elevation;
    private readonly RasterService _raster;
    private readonly DEMDataSet _dataSet;

    public Gebco2026Tests()
    {
        _dataSet = (DEMDataSet)DEMDataSet.GEBCO_2026.Clone();
        _dataSet.Name = "GEBCO-" + Guid.NewGuid().ToString("N"); // catalogs are cached by dataset name

        var services = new ServiceCollection()
            .AddLogging(b => b.SetMinimumLevel(LogLevel.Warning))
            .AddDemNetCore();
        services.Configure<DEMNetOptions>(o => o.LocalDirectory = Path.Combine(_root, "data"));
        _services = services.BuildServiceProvider();
        _elevation = _services.GetRequiredService<ElevationService>();
        _raster = _services.GetRequiredService<RasterService>();

        string dataSetDir = _raster.GetLocalDEMPath(_dataSet);
        Directory.CreateDirectory(dataSetDir);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "TestData", SampleFile), Path.Combine(dataSetDir, SampleFile));
    }

    public void Dispose()
    {
        _services.Dispose();
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    [Fact]
    public void Gebco2026IsListedAndGebco2019IsArchived()
    {
        Assert.Contains(DEMDataSet.RegisteredListedDatasets, d => d.Name == nameof(DEMDataSet.GEBCO_2026));
        Assert.DoesNotContain(DEMDataSet.RegisteredListedDatasets, d => d.Name == nameof(DEMDataSet.GEBCO_2019));
        // archived, still usable by name
        Assert.Contains(DEMDataSet.RegisteredDatasets, d => d.Name == nameof(DEMDataSet.GEBCO_2019));

        Assert.IsType<LocalFileSystem>(DEMDataSet.GEBCO_2026.DataSource);
        Assert.Equal(DEMFileType.GEOTIFF, DEMDataSet.GEBCO_2026.FileFormat.Type);
        Assert.Equal(DEMFileRegistrationMode.Cell, DEMDataSet.GEBCO_2026.FileFormat.Registration);
    }

    [Fact]
    public async Task ReportListsTheFileOnlyInsideItsExtent()
    {
        var inside = await _raster.GenerateReportAsync(_dataSet, new BoundingBox(-5.5, -5.2, 36.0, 36.2));
        var report = Assert.Single(inside);
        Assert.True(report.IsExistingLocally);
        Assert.Equal(Path.Combine(_raster.GetLocalDEMPath(_dataSet), SampleFile), report.LocalName);

        Assert.Empty(await _raster.GenerateReportAsync(_dataSet, new BoundingBox(10, 11, 45, 46)));
    }

    [Theory]
    [InlineData(-5.335416667, 36.139583333, -4)]     // Strait of Gibraltar, coast
    [InlineData(-4.497916667, 35.914583333, -1310)]  // Alboran Sea
    [InlineData(-4.789583333, 36.581250000, 561)]    // Andalusia
    [InlineData(-6.581250000, 35.331250000, -410)]   // Atlantic, off Larache
    [InlineData(-4.868750000, 35.747916667, -555)]   // last column of the first 512px tile
    [InlineData(-4.864583333, 35.747916667, -564)]   // first column of the second tile
    public async Task ElevationAtPixelCentersMatchesGdal(double lon, double lat, double expected)
    {
        var point = Assert.Single((await _elevation.GetPointsElevationAsync([new GeoPoint(lat, lon)], _dataSet))!);
        Assert.NotNull(point.Elevation);
        Assert.Equal(expected, point.Elevation!.Value, 3);
    }

    [Fact]
    public async Task HeightMapKeepsTheSeaFloor()
    {
        var heightMap = await _elevation.GetHeightMapAsync(new BoundingBox(-5.8, -4.2, 35.5, 36.5) { SRID = _dataSet.SRID }, _dataSet);

        Assert.True(heightMap.Minimum < -1000, $"Minimum {heightMap.Minimum} m: the Alboran Sea floor is below -1000 m");
        Assert.True(heightMap.Maximum > 500, $"Maximum {heightMap.Maximum} m");
        Assert.Equal(heightMap.Width * heightMap.Height, heightMap.Coordinates.Count());
    }

    /// <summary>
    /// A file copied into a local dataset directory has no manifest yet: the startup preload (WebAPI PreloadService)
    /// must index it rather than cache (and save to globalmanifest.bin) an empty catalog.
    /// </summary>
    [Fact]
    public async Task PreloadIndexesAFileWithoutManifest()
    {
        var catalog = _raster.LoadManifestMetadata(_dataSet, force: false);
        Assert.Single(catalog);

        var heightMap = await _elevation.GetHeightMapAsync(new BoundingBox(-5.5, -5.2, 36.0, 36.2) { SRID = _dataSet.SRID }, _dataSet);
        Assert.True(heightMap.Count > 0);
    }

    /// <summary>An empty index saved by a former version is rebuilt, not trusted.</summary>
    [Fact]
    public void EmptyIndexOnDiskIsRebuilt()
    {
        string dataSetDir = _raster.GetLocalDEMPath(_dataSet);
        File.Move(Path.Combine(dataSetDir, SampleFile), Path.Combine(_root, SampleFile));
        Assert.Empty(_raster.LoadManifestMetadata(_dataSet, force: false)); // no file: empty index written

        File.Move(Path.Combine(_root, SampleFile), Path.Combine(dataSetDir, SampleFile));
        using var restarted = new ServiceCollection().AddLogging().AddDemNetCore()
            .Configure<DEMNetOptions>(o => o.LocalDirectory = Path.Combine(_root, "data"))
            .BuildServiceProvider();
        Assert.Single(restarted.GetRequiredService<RasterService>().LoadManifestMetadata(_dataSet, force: false));
    }
}
