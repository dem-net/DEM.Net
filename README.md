#DEM.Net [![Build status](https://ci.appveyor.com/api/projects/status/github/xfischer/DEM.Net)](https://ci.appveyor.com/project/xfischer/dem-net)
Digital Elevation Model samples in C#. GeoTiff file parsing and DEM querying.
The goal is to provide on the fly DEM querying from GeoJSON or WKT geometries.

# How to run the SampleApp 
(Work in progress)
SampleApp is a Console App used for test purposes.
Use `GeoTiffService.GenerateDirectoryMetadata(samplePath);`to generate metadata files for your GeoTIFF tiles.
These metadata files will be used as an index when querying Digital Elevation Model data.

# Sample data
GeoTiff from http://www.opentopography.org
Dataset used is "ALOS World 3D - 30m" : http://opentopo.sdsc.edu/lidar?format=sd&platform=Satellite%20Data&collector=JAXA
*For development and tests, files covering France were used.*

# Acknowledgements / Sources
- https://github.com/stefangordon/GeoTiffSharp from @stefangordon which provided a good starting point.
- Pedro Sousa : http://build-failed.blogspot.fr/2014/12/processing-geotiff-files-in-net-without.html for good explanations.
- Mathieu Leplatre for http://blog.mathieu-leplatre.info/drape-lines-on-a-dem-with-postgis.html
