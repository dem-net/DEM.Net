[![Maintenance](https://img.shields.io/badge/Maintained%3F-yes-green.svg)](https://github.com/mdem-net/DEM.Net/graphs/commit-activity) 
[![Twitter Follow](https://img.shields.io/twitter/follow/elevationapi.svg?style=social&label=Follow)](https://twitter.com/elevationapi)

#### DEM.Net.Core [![NuGet Version](http://img.shields.io/nuget/v/DEM.Net.Core.svg?style=flat)](https://www.nuget.org/packages/DEM.Net.Core/)
#### DEM.Net.glTF [![NuGet Version](http://img.shields.io/nuget/v/DEM.Net.glTF.svg?style=flat)](https://www.nuget.org/packages/DEM.Net.glTF/)

**This library is licensed for personal use or for smal companies generating less than $100,000 annually, for any other cases, please get in touch with us.**

# DEM.Net 
Digital Elevation Model library in C#
- Elevation queries (point, polylines, heightmap, GPX)
- 3D export (glTF, STL)
- Imagery (MapBox, OSM, Stamen) : textured 3D models and normal maps
- No setup
- Automatic DEM file download from openTopography.org
- Fast and optimized queries

See samples [here](https://github.com/dem-net/Samples)

Check article on Sketchfab [API Spotlight : Elevation API](https://sketchfab.com/blogs/community/api-spotlight-elevation-api/)

 ![3D model](https://raw.githubusercontent.com/dem-net/Resources/master/videos/GPX_3D_big.gif)

# Supported formats and datasets
## Input
- GeoTIFF (JAXA AW3D, and any GeoTIFF)
- HGT (Nasa SRTM)
- netCDF
## Output
- glTF
- STL

# Current dev status

- Feel free to suggest any idea you'd like to see covered here in the issues : https://github.com/dem-net/DEM.Net/issues.

# SampleApp 
(Work in progress)
SampleApp is a Console App used for test purposes, full of samples. It's pretty messy and lacks documentation but names are self explanatory.

# How do I use the API ?
Methods that may download data are asynchronous (`...Async`, with an optional `CancellationToken`).
Their synchronous counterparts only read the files already on disk.

## Raster operations
- Use `await elevationService.DownloadMissingFilesAsync(DEMDataSet.AW3D30, <bbox>)` to download and generate metadata for a given dataset.
- Supported datasets : SRTM GL1 and GL3 (HGT files), AWD30 (GeoTIFF)
- Use `await rasterService.GenerateReportAsync(DEMDataSet.AW3D30, <bounding box>)` to list the tiles covering a bounding box (remote index downloaded once).
- Use `rasterService.GenerateFileMetadata(<path to file>, DEMFileFormat.GEOTIFF, false, false)` to generate metada for an arbitrary file.
- Use `RasterService.GenerateDirectoryMetadata(samplePath);`to generate metadata files for your raster tiles.
These metadata files will be used as an index when querying Digital Elevation Model data.

## Elevation operations
- `await GetHeightMapAsync(bbox, dataset)`: the returned `HeightMap.BoundingBox` holds the bounds of the dataset samples (v10: `GetHeightMap(ref bbox, ...)`)
- `await GetPointsElevationAsync(points, dataset)`, `await GetIntervisibilityReportAsync(source, target, dataset)`
- `GetLineGeometryElevation`, `GetPointElevation`, `GetPointsElevation`, `GetHeightMap(bbox, dataset)`: from local files (download them first)

## Imagery
- `await imageryService.DownloadTilesAsync(tiles, provider)`

## Migrating from v10
| v10 | v11 |
|---|---|
| `DownloadMissingFiles(...)` | `await DownloadMissingFilesAsync(...)` |
| `GetHeightMap(ref bbox, dataset, downloadMissingFiles)` | `await GetHeightMapAsync(bbox, dataset, downloadMissingFiles)`, then `bbox = heightMap.BoundingBox` if needed |
| `GetPointsElevation(points, dataset, ..., downloadMissingFiles)` (lazy) | `await GetPointsElevationAsync(...)` (a `List<GeoPoint>`) |
| `GetIntervisibilityReport(source, target, dataset, ...)` | `await GetIntervisibilityReportAsync(...)` |
| `RasterService.GenerateReport(dataset, bbox)` / `GenerateReportForLocation(...)` | `await GenerateReportAsync(dataset, bbox)` / `await GenerateReportForLocationAsync(...)` |
| `ImageryService.DownloadTiles(...)` | `await DownloadTilesAsync(...)` |
| `XyzTileIndexService.GetMaxZoom(...)` | `await GetMaxZoomAsync(...)` |
| `IDEMDataSetIndex.Setup` / `IRasterDownloader.DownloadRasterFile` | `SetupAsync` / `DownloadRasterFileAsync` |

`GeoPoint` is now an immutable `readonly struct` (32 bytes, was a class of ~80 bytes per point with its reference):

| v10 | v11 |
|---|---|
| `point.Elevation = z` / `point.Clone(z)` | `point = point with { Elevation = z }` (in a list: `list[i] = list[i] with { ... }`) |
| `point == null`, `GeoPoint x = null` | `GeoPoint?` where a point is optional; `default(GeoPoint)` is (0, 0) without elevation |
| `point.Id` (used to restore the order of `GetPointsElevation`) | removed: `GetPointsElevation` returns the points in the input order |
| `GetPointElevation(...)` returns `null` outside of the dataset | returns the location with `Elevation == null` |
| `ComputeMetrics()` sets `DistanceFromOriginMeters` on the point objects | sets it on the points of the given list (`IList<GeoPoint>`) |
| `GeoPointRays : GeoPoint` | holds a `Point` (implicit conversion to `GeoPoint`) |
| `IntervisibilityObstacle.PeakPoint` / `ExitPoint` | `GeoPoint?` |

DEM.Net.Core no longer depends on Newtonsoft.Json: manifests, dataset indexes and the STAC / NASA CMR payloads use System.Text.Json (source generated).
The files written by former versions are read as is (PascalCase or camelCase names, enum numbers or names, `"NaN"`), nothing has to be regenerated.

| v10 | v11 |
|---|---|
| Newtonsoft `[JsonIgnore]` on `FileMetadata`, `StacDataSource`, `TileIndexDataSource` | `System.Text.Json.Serialization.JsonIgnore`: an application serializing these types with Newtonsoft must honor it (see `ApiContractResolver` in DEM.Net.WebAPI), the data source delegates are not serializable |
| Newtonsoft.Json available transitively | reference it in the application when it is used there |
| Tile index feature properties: ISO date strings reformatted by Newtonsoft (`06/15/2020 00:00:00`) | passed as written in the GeoJSON |

`HeightMap.Coordinates` is a `GeoPoint[]`, and the height map transforms (`ReprojectTo`, `ReprojectGeodeticToCartesian`,
`CenterOnOrigin`, `ZScale`, `Scale`, `FitInto`, `ZTranslate`, `ZClamp`, `ApplyGeometryMask`...) change the points in place.
Before, each transform added a lazy step that ran again on every enumeration of the coordinates.

| v10 | v11 |
|---|---|
| `IEnumerable<GeoPoint> Coordinates` (lazy pipeline) | `GeoPoint[] Coordinates`: index it, `Length` instead of `Count()` |
| `heightMap.BakeCoordinates()` | not needed (obsolete, does nothing) |
| `heightMap.Clone()` shares the source points, transforms of the clone build a new pipeline | `Clone()` copies the points: clone a height map to keep the original points |
| a reference to `heightMap.Coordinates` kept before a transform still enumerates the former points | it sees the transformed points (same array) |
| `ISTLExportService.STLExport(mesh, fileName, ...)` | `await STLExportAsync(mesh, fileName, ..., cancellationToken)` (written through a temporary file), `WriteSTLAsync(mesh, stream, ...)` |
| `OBJExportService.ExportGlTFModelToWaveFrontObj(...)` | `await ExportGlTFModelToWaveFrontObjAsync(..., cancellationToken)` |

The STL and OBJ files are byte-identical to v10.

## glTF export
- `glTFService` can generate triangulated MeshPrimitives from height maps
- Export to .gtlf or .glb
- Export to STL (ASCII or binary) and Wavefront OBJ, streamed from the mesh

# Sample data
- Rasters from http://www.opentopography.org
Dataset used is "ALOS World 3D - 30m" : http://opentopo.sdsc.edu/lidar?format=sd&platform=Satellite%20Data&collector=JAXA
*For development and tests, files covering France were used.*
- Not used yet but worth mentionning :
For sea bed elevation : ETOPO1 Global Relief Model https://www.ngdc.noaa.gov/mgg/global/global.html

# Acknowledgements / Sources
- https://github.com/stefangordon/GeoTiffSharp from @stefangordon which provided a good starting point.
- Pedro Sousa : http://build-failed.blogspot.fr/2014/12/processing-geotiff-files-in-net-without.html for good explanations.
- Mathieu Leplatre for http://blog.mathieu-leplatre.info/drape-lines-on-a-dem-with-postgis.html
- Andy9FromSpace : HGT file reader in https://github.com/Andy9FromSpace/map-elevation

# Third party code and librairies
- Image processing: Six Labors Image Sharp: https://sixlabors.com/products/imagesharp/
- glTF : glTF2Loader and AssetGenerator : https://github.com/KhronosGroup/glTF
- Tiff support : https://github.com/BitMiracle/libtiff.net
- Serialization : https://github.com/protobuf-net/protobuf-net and System.Text.Json
- System.Numerics.Vectors for Vector support
- GPX reader from dlg.krakow.pl

