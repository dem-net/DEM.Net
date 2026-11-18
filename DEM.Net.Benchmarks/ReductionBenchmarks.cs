using System.Linq;
using BenchmarkDotNet.Attributes;
using DEM.Net.Core;
using DEM.Net.glTF;
using Microsoft.Extensions.DependencyInjection;

namespace DEM.Net.Benchmarks;

/// <summary>
/// Terrain mesh reduction of square height maps, as the API does it: height map reprojected to 3857, Z factor,
/// centered, triangulated as a regular grid, then reduced to <see cref="Factor"/> of the triangle count.
/// </summary>
[MemoryDiagnoser]
public class ReductionBenchmarks
{
    private LocalHgtDataSet _data = null!;
    private MeshService _meshService = null!;
    private MeshReducer _reducer = null!;
    private HeightMap _heightMap = null!;

    /// <summary>Side of the bbox in degrees: 0.2° = 241², 0.5° = 601², 1° = 1201² samples at 3"</summary>
    [Params(0.2, 0.5, 1.0)]
    public double Degrees { get; set; }

    /// <summary>Kept fraction of the triangles (the API's meshReduceFactor, 0.5 by default)</summary>
    [Params(0.5f, 0.1f)]
    public float Factor { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _data = new LocalHgtDataSet();
        var elevation = _data.Services.GetRequiredService<ElevationService>();
        _meshService = _data.Services.GetRequiredService<MeshService>();
        _reducer = _data.Services.GetRequiredService<MeshReducer>();

#pragma warning disable VSTHRD002 // console host: no synchronization context to deadlock on
        elevation.DownloadMissingFilesAsync(_data.DataSet, new BoundingBox(LocalHgtDataSet.West, LocalHgtDataSet.West + 1, LocalHgtDataSet.South, LocalHgtDataSet.South + 1))
            .GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        // The 1° bbox is the whole tile; the smaller ones are centered in it
        double half = Degrees / 2, lon = LocalHgtDataSet.West + 0.5, lat = LocalHgtDataSet.South + 0.5;
        _heightMap = elevation.GetHeightMap(new BoundingBox(lon - half, lon + half, lat - half, lat + half), _data.DataSet)
            .ReprojectGeodeticToCartesian().ZScale(2f).CenterOnOrigin();
    }

    [GlobalCleanup]
    public void Cleanup() => _data.Dispose();

    [Benchmark(Description = "QEM reduction (geometry3Sharp)")]
    public int Qem()
    {
        var reduced = _reducer.Decimate(_meshService.TriangulateHeightMap(_heightMap), Factor);
        return reduced.Indices.Count();
    }

    [Benchmark(Description = "Delatin, same triangle count")]
    public int Delatin() => DelatinMesh.Reduce(_heightMap, Factor).Indices.Length;
}
