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

## Hot paths (v11, before the `GeoPoint` struct)

Same machine and job, 2026-11-15. Outputs are byte-identical to the baseline code (lines, points, height maps and
reprojected height maps of the HGT and AW3D30 test tiles).

| Method                                          | Mean       | Allocated  | vs baseline |
|------------------------------------------------ |-----------:|-----------:|------------ |
| 1 000 single point elevations                   |  18.691 ms |    6.39 MB | -58% memory |
| 1 000 points elevation (batch)                  |   1.911 ms |    0.20 MB | -32% time, -98% memory |
| Line elevation, 10 000 vertices                 | 318.429 ms |  129.20 MB | 3x faster, 36x less memory |
| Height map of a 0.2° bbox                       |   6.491 ms |    6.29 MB | = |
| Reproject height map to 3857, z-scale, center   |   4.391 ms |   14.81 MB | -18% time, -13% memory |
| glTF terrain mesh of the bbox, written as GLB   | 216.092 ms |  142.01 MB | = |

- HGT: a sample was read with a whole scanline copy (and a byte array per sample); the two last rows are now kept.
- Non tiled GeoTIFF: same two-row cache (a scanline was allocated and decoded for each sample).
- Line elevation: each segment walked the DEM grid lines from the tile edge (up to a tile width per segment);
  it now starts at the segment bounds, and the intersection list is sized for the segment, not for a whole tile.
- Interpolation: no array and LINQ per point to average the corners.
- Reprojection: the source is enumerated once (it was enumerated twice, re-running lazy height map pipelines),
  without one array per point; projections are parsed once per thread.

## `GeoPoint` struct (v11)

Same machine and job, 2026-11-16. Outputs are byte-identical to the previous version (same comparison, plus line
metrics and distances, Douglas-Peucker, intervisibility obstacles, height map transforms, downsampling and the GLB file).

| Method                                          | Mean       | Allocated  | vs hot paths |
|------------------------------------------------ |-----------:|-----------:|------------ |
| 1 000 single point elevations                   |  17.977 ms |    6.27 MB | = |
| 1 000 points elevation (batch)                  |   1.825 ms |    0.15 MB | -24% memory |
| Line elevation, 10 000 vertices                 | 264.242 ms |   79.03 MB | -17% time, -39% memory |
| Height map of a 0.2° bbox                       |   2.033 ms |    3.60 MB | 3.2x faster, -43% memory |
| Reproject height map to 3857, z-scale, center   |   2.184 ms |    2.70 MB | 2x faster, 5.5x less memory |
| glTF terrain mesh of the bbox, written as GLB   | 211.415 ms |  123.85 MB | -13% memory |

- A point is 32 bytes inline in its list or array (an object of 72 bytes plus an 8 bytes reference before), and
  transforms (`with`) no longer allocate per point.
- Point elevations are written in place in the input order (they were grouped by tile, then sorted back by `Id`).

## Height map arrays and async exports (v11)

Apple M1, .NET 10, default job, 2026-11-16, both versions measured the same day. The STL (binary and ASCII), OBJ
and MTL files are byte-identical to the previous version.

| Method                                          | Before     | After      | Allocated before | Allocated after |
|------------------------------------------------ |-----------:|-----------:|-----------:|-----------:|
| Height map of a 0.2° bbox                       |   1.915 ms |   2.038 ms |    3.60 MB |    3.60 MB |
| Reproject height map to 3857, z-scale, center   |   2.149 ms |   1.647 ms |    2.70 MB |    3.80 MB |
| glTF terrain mesh of the bbox, written as GLB   | 200.006 ms | 207.232 ms |  123.87 MB |  122.24 MB |
| STL binary export                               |  25.89 ms  |   4.58 ms  |   48.62 MB |   0.07 MB |
| STL ASCII export                                | 114.72 ms  |  64.23 ms  |  137.19 MB |   0.48 MB |
| OBJ export                                      |  17.01 ms  |  12.06 ms  |   11.33 MB |   0.73 MB |

- Reprojection: the benchmark clones the height map, which now copies its points (1.8 MB): the transforms change
  them in place, without allocating per point, and the result is no longer re-computed on each enumeration.
- STL: triangles are written from the mesh accessors as they are read (no list of triangle objects, no
  vertex builders), through a pooled buffer; ASCII lines are formatted without strings.
- OBJ: same line formatting, no string per line.
