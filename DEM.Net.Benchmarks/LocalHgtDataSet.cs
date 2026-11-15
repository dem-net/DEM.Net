using System;
using System.IO;
using System.IO.Compression;
using DEM.Net.Core;
using DEM.Net.Core.Configuration;
using DEM.Net.Core.Datasets;
using DEM.Net.glTF;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DEM.Net.Benchmarks;

/// <summary>
/// Offline benchmark data: the N43E005 SRTM 3" tile of the test project (Marseille area, 1201 x 1201 samples),
/// unzipped into a temporary folder and exposed as a local file system dataset. Nothing is downloaded.
/// </summary>
public sealed class LocalHgtDataSet : IDisposable
{
    /// <summary>Tile covers lat 43..44, lon 5..6.</summary>
    public const double South = 43, West = 5;

    private readonly string _root;

    public ServiceProvider Services { get; }
    public DEMDataSet DataSet { get; }

    public LocalHgtDataSet()
    {
        _root = Path.Combine(Path.GetTempPath(), "DEM.Net.Benchmarks", Guid.NewGuid().ToString("N"));
        string tiles = Path.Combine(_root, "tiles");
        Directory.CreateDirectory(tiles);
        ZipFile.ExtractToDirectory(Path.Combine(AppContext.BaseDirectory, "TestData", "N43E005.hgt.zip"), tiles);

        DataSet = new DEMDataSet
        {
            Name = "BenchmarkHGT",
            Description = "SRTM 3 arc second tile N43E005 (benchmarks)",
            DataSource = new LocalFileSystem(tiles),
            FileFormat = new DEMFileDefinition("Nasa SRTM HGT", DEMFileType.SRTM_HGT, ".hgt", DEMFileRegistrationMode.Grid),
            ResolutionMeters = 90,
            ResolutionArcSeconds = 3,
            PointsPerDegree = 1200,
            NoDataValue = -32768,
        };

        var services = new ServiceCollection()
            .AddLogging(b => b.SetMinimumLevel(LogLevel.Warning))
            .AddDemNetCore()
            .AddDemNetglTF();
        services.Configure<DEMNetOptions>(o => o.LocalDirectory = Path.Combine(_root, "data"));
        Services = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        Services.Dispose();
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
}
