# DEM.Net.Benchmarks

[BenchmarkDotNet](https://benchmarkdotnet.org) benchmarks of the elevation, height map, reprojection and glTF paths, with
allocations (`[MemoryDiagnoser]`). They run offline on the SRTM 3" tile of the test project (`N43E005.hgt`, Marseille area),
exposed as a local file system dataset in a temporary folder.

```sh
dotnet run -c Release --project DEM.Net.Benchmarks -- --filter '*'
# quicker, less precise
dotnet run -c Release --project DEM.Net.Benchmarks -- --filter '*' --job short
```

Release builds need the Six Labors license (see `Directory.Build.props`).

## Baseline

Before the async / `GeoPoint` / hot path work. `--job short`, Apple M1, .NET 10.0.9, 2026-11-15.

| Method                                          | Mean       | Allocated  |
|------------------------------------------------ |-----------:|-----------:|
| 1 000 single point elevations                   |  23.764 ms |   15.22 MB |
| 1 000 points elevation (batch)                  |   2.808 ms |    9.75 MB |
| Line elevation, 10 000 vertices                 | 907.150 ms | 4699.13 MB |
| Height map of a 0.2° bbox                       |   6.592 ms |    6.29 MB |
| Reproject height map to 3857, z-scale, center   |   5.341 ms |   17.07 MB |
| glTF terrain mesh of the bbox, written as GLB   | 209.662 ms |  146.51 MB |
