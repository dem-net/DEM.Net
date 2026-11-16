using System;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using DEM.Net.Core;
using DEM.Net.glTF.Export;
using DEM.Net.glTF.SharpglTF;
using Microsoft.Extensions.DependencyInjection;
using SharpGLTF.Schema2;

namespace DEM.Net.Benchmarks;

/// <summary>STL and OBJ files of the terrain mesh of the bbox (written to a temporary folder, or EXPORT_DIR)</summary>
[MemoryDiagnoser]
public class ExportBenchmarks
{
    private LocalHgtDataSet _data = null!;
    private ModelRoot _model = null!;
    private ISTLExportService _stl = null!;
    private OBJExportService _obj = null!;
    private string _dir = null!;

    [GlobalSetup]
    public void Setup()
    {
        _data = new LocalHgtDataSet();
        var elevation = _data.Services.GetRequiredService<ElevationService>();
        var gltf = _data.Services.GetRequiredService<SharpGltfService>();
        _stl = _data.Services.GetRequiredService<ISTLExportService>();
        _obj = _data.Services.GetRequiredService<OBJExportService>();
        _dir = Environment.GetEnvironmentVariable("EXPORT_DIR") ?? Path.Combine(Path.GetTempPath(), "DEM.Net.Benchmarks");
        Directory.CreateDirectory(_dir);

#pragma warning disable VSTHRD002 // console host: no synchronization context to deadlock on
        elevation.DownloadMissingFilesAsync(_data.DataSet, new BoundingBox(LocalHgtDataSet.West, LocalHgtDataSet.West + 1, LocalHgtDataSet.South, LocalHgtDataSet.South + 1))
            .GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        var heightMap = elevation.GetHeightMap(new BoundingBox(5.30, 5.50, 43.30, 43.50), _data.DataSet);
        _model = gltf.CreateTerrainMesh(heightMap.ReprojectGeodeticToCartesian().CenterOnOrigin());
    }

    [GlobalCleanup]
    public void Cleanup() => _data.Dispose();

    [Benchmark(Description = "STL binary export")]
    public async Task<long> StlBinary()
    {
        var path = Path.Combine(_dir, "terrain_bin.stl");
        await _stl.STLExportAsync(_model.LogicalMeshes[0].Primitives[0], path, ascii: false);
        return new FileInfo(path).Length;
    }

    [Benchmark(Description = "STL ASCII export")]
    public async Task<long> StlAscii()
    {
        var path = Path.Combine(_dir, "terrain_ascii.stl");
        await _stl.STLExportAsync(_model.LogicalMeshes[0].Primitives[0], path, ascii: true, [new Attribution("Dataset", "Test", "", "Test data")]);
        return new FileInfo(path).Length;
    }

    [Benchmark(Description = "OBJ export")]
    public async Task<long> Obj()
        => new FileInfo(await _obj.ExportGlTFModelToWaveFrontObjAsync(_model, Path.Combine(_dir, "obj"), "terrain", overwrite: true)).Length;
}
