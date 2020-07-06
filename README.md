[![](https://github.com/xscode-resources/promotion-materials/blob/master/assets/xscode-banner-s.svg?raw=true)](https://xscode.com/xfischer/DEM.Net)


[![xscode](https://img.shields.io/badge/Available%20on-xs%3Acode-blue?style=?style=plastic&logo=appveyor&logo=data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAEAAAABACAMAAACdt4HsAAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJbWFnZVJlYWR5ccllPAAAAAZQTFRF////////VXz1bAAAAAJ0Uk5T/wDltzBKAAAAlUlEQVR42uzXSwqAMAwE0Mn9L+3Ggtgkk35QwcnSJo9S+yGwM9DCooCbgn4YrJ4CIPUcQF7/XSBbx2TEz4sAZ2q1RAECBAiYBlCtvwN+KiYAlG7UDGj59MViT9hOwEqAhYCtAsUZvL6I6W8c2wcbd+LIWSCHSTeSAAECngN4xxIDSK9f4B9t377Wd7H5Nt7/Xz8eAgwAvesLRjYYPuUAAAAASUVORK5CYII=)](https://xscode.com/xfischer/DEM.Net)
[![Build Status](https://dev.azure.com/dem-net/elevationapi/_apis/build/status/DEM.Net%20Core?branchName=master)](https://dev.azure.com/dem-net/elevationapi/_build/latest?definitionId=4&branchName=master)
[![Board Status](https://dev.azure.com/dem-net/fb0d4f55-28ad-4931-91c1-3be40dc42d96/97ece37c-c459-4aa0-a394-99b37b2af215/_apis/work/boardbadge/5f246f28-ee97-4849-bc40-654c06ec1bc1)](https://dev.azure.com/dem-net/fb0d4f55-28ad-4931-91c1-3be40dc42d96/_boards/board/t/97ece37c-c459-4aa0-a394-99b37b2af215/Microsoft.RequirementCategory) 
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

Supported by [![JetBrainSupport](https://raw.githubusercontent.com/dem-net/Resources/master/images/ext/jetbrains-variant-2.svg?sanitize=true)](https://www.jetbrains.com/?from=DemNetElevationAPI)

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
- All incoming features are listed in the project board here : https://dev.azure.com/dem-net/elevationapi/_workitems/recentlyupdated/.
- Feel free to suggest any idea you'd like to see covered here in the issues : https://github.com/dem-net/DEM.Net/issues.

# SampleApp 
(Work in progress)
SampleApp is a Console App used for test purposes, full of samples. It's pretty messy and lacks documentation but names are self explanatory.

# How do I use the API ?
## Raster operations
- Use `elevationService.DownloadMissingFiles(DEMDataSet.AW3D30, <bbox>)` to download and generate metadata for a given dataset.
- Supported datasets : SRTM GL1 and GL3 (HGT files), AWD30 (GeoTIFF)
- Use `new RasterService().GenerateReport(DEMDataSet.AW3D30, <bounding box>)` to download only necessary tiles using remote VRT file.
- Use `rasterService.GenerateFileMetadata(<path to file>, DEMFileFormat.GEOTIFF, false, false)` to generate metada for an arbitrary file.
- Use `RasterService.GenerateDirectoryMetadata(samplePath);`to generate metadata files for your raster tiles.
These metadata files will be used as an index when querying Digital Elevation Model data.

## Elevation operations
- GetLineGeometryElevation
- GetPointElevation

## glTF export
- `glTFService` can generate triangulated MeshPrimitives from height maps
- Export to .gtlf or .glb

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
- glTF : glTF2Loader and AssetGenerator : https://github.com/KhronosGroup/glTF
- Tiff support : https://github.com/BitMiracle/libtiff.net
- Serialization : https://github.com/neuecc/ZeroFormatter and https://github.com/JamesNK/Newtonsoft.Json
- System.Numerics.Vectors for Vector support
- GPX reader from dlg.krakow.pl

