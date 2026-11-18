using System;
using System.IO;
using System.IO.Compression;
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
/// Offline elevation tests over the N43E005 SRTM tile of TestData, as a local file system dataset (nothing is downloaded).
/// </summary>
public sealed class LocalDataSetTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "DEM.Net.Tests", Guid.NewGuid().ToString("N"));
    private readonly ServiceProvider _services;
    private readonly ElevationService _elevation;
    private readonly DEMDataSet _dataSet;

    public LocalDataSetTests()
    {
        string tiles = Path.Combine(_root, "tiles");
        Directory.CreateDirectory(tiles);
        ZipFile.ExtractToDirectory(Path.Combine(AppContext.BaseDirectory, "TestData", "N43E005.hgt.zip"), tiles);

        _dataSet = new DEMDataSet
        {
            Name = "LocalHGT-" + Guid.NewGuid().ToString("N"), // catalogs are cached by dataset name
            Description = "SRTM 3 arc second tile N43E005 (tests)",
            DataSource = new LocalFileSystem(tiles),
            FileFormat = new DEMFileDefinition("Nasa SRTM HGT", DEMFileType.SRTM_HGT, ".hgt", DEMFileRegistrationMode.Grid),
            ResolutionMeters = 90,
            ResolutionArcSeconds = 3,
            PointsPerDegree = 1200,
            NoDataValue = -32768,
        };

        var services = new ServiceCollection()
            .AddLogging(b => b.SetMinimumLevel(LogLevel.Warning))
            .AddDemNetCore();
        services.Configure<DEMNetOptions>(o => o.LocalDirectory = Path.Combine(_root, "data"));
        _services = services.BuildServiceProvider();
        _elevation = _services.GetRequiredService<ElevationService>();
    }

    public void Dispose()
    {
        _services.Dispose();
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    private static GeoPoint[] Points() =>
    [
        new GeoPoint(43.30, 5.37), // Marseille
        new GeoPoint(43.53, 5.45), // Aix-en-Provence
        new GeoPoint(43.33, 5.71), // Sainte-Baume
    ];

    [Fact]
    public async Task PointsElevationIsMaterializedAndMatchesTheAsyncVersion()
    {
        var asyncPoints = await _elevation.GetPointsElevationAsync(Points(), _dataSet);
        // the rasters are closed when the method returns: the result must not be read lazily from them
        var syncPoints = _elevation.GetPointsElevation(Points().AsEnumerable().Select(p => p), _dataSet);

        Assert.Equal(3, asyncPoints.Count);
        Assert.Equal(asyncPoints.Select(p => p.Elevation), syncPoints.Select(p => p.Elevation));
        Assert.All(asyncPoints, p => Assert.InRange(p.Elevation.GetValueOrDefault(), 1, 1200));
        Assert.Equal(_elevation.GetPointElevation(43.53, 5.45, _dataSet).Elevation.Value, asyncPoints[1].Elevation.Value, 3);
    }

    [Fact]
    public async Task PointsElevationKeepsTheInputOrder()
    {
        // points outside the tile are processed apart (former versions returned them grouped by tile)
        var points = new[]
        {
            new GeoPoint(42.50, 5.50), // outside
            new GeoPoint(43.30, 5.37),
            new GeoPoint(44.50, 5.50), // outside
            new GeoPoint(43.53, 5.45),
        };

        var elevated = await _elevation.GetPointsElevationAsync(points, _dataSet);

        Assert.Equal(points, elevated); // same locations, same order
        Assert.Equal(0, elevated[0].Elevation);
        Assert.Equal(0, elevated[2].Elevation);
        Assert.Equal(_elevation.GetPointElevation(43.30, 5.37, _dataSet).Elevation, elevated[1].Elevation);
        Assert.Equal(_elevation.GetPointElevation(43.53, 5.45, _dataSet).Elevation, elevated[3].Elevation);
    }

    [Fact]
    public async Task UncoveredLocationHasNoElevation()
    {
        await _elevation.DownloadMissingFilesAsync(_dataSet, 43.5, 5.5);
        var point = _elevation.GetPointElevation(10.5, 20.5, _dataSet);
        Assert.Equal(new GeoPoint(10.5, 20.5), point);
        Assert.Null(point.Elevation);
    }

    [Fact]
    public async Task HeightMapDoesNotModifyTheRequestedBoundingBox()
    {
        var bbox = new BoundingBox(5.40, 5.50, 43.40, 43.50);
        var requested = bbox.Clone();

        var heightMap = await _elevation.GetHeightMapAsync(bbox, _dataSet);

        Assert.Equal(requested, bbox);
        Assert.Equal(heightMap.Width * heightMap.Height, heightMap.Coordinates.Count());
        Assert.InRange(heightMap.BoundingBox.xMin, 5.39, 5.41);
        Assert.InRange(heightMap.BoundingBox.yMax, 43.49, 43.51);
    }

    [Fact]
    public async Task ConcurrentRequestsOnAColdCatalogAgree()
    {
        // first use of the dataset: concurrent requests build the metadata catalog at the same time
        var bbox = new BoundingBox(5.40, 5.45, 43.40, 43.45);
        var maps = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() => _elevation.GetHeightMapAsync(bbox, _dataSet))));

        Assert.All(maps, m => Assert.Equal(maps[0].Coordinates.Select(p => p.Elevation), m.Coordinates.Select(p => p.Elevation)));
    }

    [Fact]
    public void HgtSamplesAreReadAtTheirOffset()
    {
        string file = Directory.GetFiles(Path.Combine(_root, "tiles"), "*.hgt").Single();
        byte[] raw = File.ReadAllBytes(file); // big endian int16 samples, row after row
        var rasterService = _services.GetRequiredService<RasterService>();
        var metadata = rasterService.ParseMetadata(file, _dataSet.FileFormat);

        var random = new Random(7);
        using var hgt = rasterService.OpenFile(file, DEMFileType.SRTM_HGT);
        for (int i = 0; i < 2000; i++)
        {
            int x = random.Next(metadata.Width), y = random.Next(metadata.Height);
            short expected = System.Buffers.Binary.BinaryPrimitives.ReadInt16BigEndian(raw.AsSpan((y * metadata.Width + x) * 2));
            Assert.Equal(expected, hgt.GetElevationAtPoint(metadata, x, y));
        }
    }

    [Fact]
    public void LinesBatchMatchesOneCallPerLine()
    {
        NetTopologySuite.Geometries.Geometry?[] lines =
        [
            GeometryService.ParseGeoPointAsGeometryLine([new GeoPoint(43.30, 5.37), new GeoPoint(43.53, 5.45), new GeoPoint(43.33, 5.71)]),
            null, // no line: null result, the other lines are still elevated
            GeometryService.ParseGeoPointAsGeometryLine([new GeoPoint(43.10, 5.90), new GeoPoint(43.90, 5.05)]),
            GeometryService.ParseGeoPointAsGeometryLine([new GeoPoint(43.40, 5.40), new GeoPoint(43.4005, 5.4005)]), // within a pixel
        ];

        var batch = _elevation.GetLinesGeometryElevation(lines, _dataSet);

        Assert.Equal(lines.Length, batch.Count);
        Assert.Null(batch[1]);
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i] == null) continue;
            var single = _elevation.GetLineGeometryElevation(lines[i], _dataSet);
            Assert.NotEmpty(single);
            Assert.Equal(single, batch[i]); // same points (positions and elevations)
        }

        var byKey = _elevation.GetLinesGeometryElevation(new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<GeoPoint>>
        {
            ["a"] = [new GeoPoint(43.30, 5.37), new GeoPoint(43.53, 5.45), new GeoPoint(43.33, 5.71)],
            ["b"] = [new GeoPoint(43.10, 5.90), new GeoPoint(43.90, 5.05)],
        }, _dataSet);
        Assert.Equal(batch[0], byKey["a"]);
        Assert.Equal(batch[2], byKey["b"]);
    }

    [Fact]
    public void LinePointsSkipARepeatedPosition()
    {
        // what a vertex on a grid corner produces: the vertex, its north/south and west/east crossings
        // (computed, so a few ulps off), then the next segment starting at the same vertex
        var points = new System.Collections.Generic.List<GeoPoint> { new(43.30, 5.37) };
        ElevationService.AddDistinct(points, [new GeoPoint(43.30 + 1e-15, 5.37), new GeoPoint(43.30, 5.37 - 1e-15), new GeoPoint(43.30083333, 5.37083333)]);
        ElevationService.AddDistinct(points, [new GeoPoint(43.30083333, 5.37083333), new GeoPoint(43.31, 5.38)]);

        Assert.Equal([new GeoPoint(43.30, 5.37), new GeoPoint(43.30083333, 5.37083333), new GeoPoint(43.31, 5.38)], points);
    }

    [Fact]
    public async Task LocalDatasetsCannotDownload()
    {
        var report = new DemFileReport { LocalName = Path.Combine(_root, "missing.hgt") };
        var index = _services.GetRequiredService<RasterIndexServiceResolver>()(DEMDataSourceType.LocalFileSystem);
        await Assert.ThrowsAsync<NotSupportedException>(() => index.DownloadRasterFileAsync(report, _dataSet));
    }
}
