using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using DEM.Net.Core;
using DEM.Net.glTF.SharpglTF;
using Microsoft.Extensions.DependencyInjection;

namespace DEM.Net.Benchmarks;

[MemoryDiagnoser]
public class ElevationBenchmarks
{
    private LocalHgtDataSet _data = null!;
    private ElevationService _elevation = null!;
    private SharpGltfService _gltf = null!;
    private List<GeoPoint> _points = null!;
    private List<GeoPoint> _line = null!;
    private HeightMap _heightMap = null!;

    /// <summary>~0.2° x 0.2° (about 240 x 240 samples at 3")</summary>
    private static BoundingBox Bbox => new BoundingBox(5.30, 5.50, 43.30, 43.50);

    [GlobalSetup]
    public void Setup()
    {
        _data = new LocalHgtDataSet();
        _elevation = _data.Services.GetRequiredService<ElevationService>();
        _gltf = _data.Services.GetRequiredService<SharpGltfService>();

        var random = new Random(42);
        _points = Enumerable.Range(0, 1_000)
            .Select(_ => new GeoPoint(LocalHgtDataSet.South + 0.05 + random.NextDouble() * 0.9, LocalHgtDataSet.West + 0.05 + random.NextDouble() * 0.9))
            .ToList();
        // 10 000 vertices zig-zagging across the tile
        _line = Enumerable.Range(0, 10_000)
            .Select(i => new GeoPoint(43.1 + (i % 100) * 0.008, 5.1 + i * 0.00008))
            .ToList();

        // Warm up: generates and loads the file metadata (local dataset: nothing is downloaded), opens the raster once.
        // The measured calls then skip DownloadMissingFiles (downloadMissingFiles: false) where the API allows it.
        _elevation.DownloadMissingFiles(_data.DataSet, new BoundingBox(LocalHgtDataSet.West, LocalHgtDataSet.West + 1, LocalHgtDataSet.South, LocalHgtDataSet.South + 1));
        _elevation.GetPointElevation(43.5, 5.5, _data.DataSet);
        var bbox = Bbox;
        _heightMap = _elevation.GetHeightMap(ref bbox, _data.DataSet, downloadMissingFiles: false);
    }

    [GlobalCleanup]
    public void Cleanup() => _data.Dispose();

    [Benchmark(Description = "1 000 single point elevations")]
    public double PointElevations()
    {
        double sum = 0;
        foreach (var p in _points)
            sum += _elevation.GetPointElevation(p.Latitude, p.Longitude, _data.DataSet).Elevation ?? 0;
        return sum;
    }

    [Benchmark(Description = "1 000 points elevation (batch)")]
    public int PointsElevationBatch()
        => _elevation.GetPointsElevation(_points, _data.DataSet, downloadMissingFiles: false).Count();

    [Benchmark(Description = "Line elevation, 10 000 vertices")]
    public int LineElevation()
        => _elevation.GetLineGeometryElevation(_line, _data.DataSet).Count;

    [Benchmark(Description = "Height map of a 0.2° bbox")]
    public int HeightMap()
    {
        var bbox = Bbox;
        return _elevation.GetHeightMap(ref bbox, _data.DataSet, downloadMissingFiles: false).Count;
    }

    [Benchmark(Description = "Reproject height map to 3857, z-scale, center")]
    public int ReprojectHeightMap()
        => _heightMap.Clone().ReprojectGeodeticToCartesian().ZScale(2f).CenterOnOrigin().Coordinates.Count();

    [Benchmark(Description = "glTF terrain mesh of the bbox, written as GLB")]
    public long TerrainMeshGlb()
    {
        var model = _gltf.CreateTerrainMesh(_heightMap.Clone().ReprojectGeodeticToCartesian().CenterOnOrigin());
        using var stream = new MemoryStream();
        model.WriteGLB(stream);
        return stream.Length;
    }
}
