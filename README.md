[![Build status](https://ci.appveyor.com/api/projects/status/github/xfischer/DEM.Net)](https://ci.appveyor.com/project/xfischer/dem-net)


# DEM.Net 
Digital Elevation Model samples in C#. GeoTiff file parsing and DEM querying.
The goal is to provide on the fly DEM querying from GeoJSON or WKT geometries :
- No big RAM cache
- Fast and optimized data retrieval as precise as possible

# Coming soon...
- All incoming features are listed in the Issues here : https://github.com/xfischer/DEM.Net/issues. Feel free to suggest any idea you'd like to see covered here.
- Provide a simple sample for getting Height Maps (Unity based samples) and vizualising DEM as 3D meshes
- Provide a simple sample to drape a line onto a DEM
- If you have specific needs, let me know (create an issue)

# SampleApp 
(Work in progress)
SampleApp is a Console App used for test purposes.

- Use `new GeoTiffService().GenerateReport(DEMDataSet.AW3D30, <bounding box>)` to download only necessary tiles using remote VRT file.

- Use `GeoTiffService.GenerateDirectoryMetadata(samplePath);`to generate metadata files for your GeoTIFF tiles.
These metadata files will be used as an index when querying Digital Elevation Model data.

# Sample data
- GeoTiff from http://www.opentopography.org
Dataset used is "ALOS World 3D - 30m" : http://opentopo.sdsc.edu/lidar?format=sd&platform=Satellite%20Data&collector=JAXA
*For development and tests, files covering France were used.*
- Not used yet but worth mentionning :
For sea bed elevation : ETOPO1 Global Relief Model https://www.ngdc.noaa.gov/mgg/global/global.html

# Acknowledgements / Sources
- https://github.com/stefangordon/GeoTiffSharp from @stefangordon which provided a good starting point.
- Pedro Sousa : http://build-failed.blogspot.fr/2014/12/processing-geotiff-files-in-net-without.html for good explanations.
- Mathieu Leplatre for http://blog.mathieu-leplatre.info/drape-lines-on-a-dem-with-postgis.html
- Andy9FromSpace : HGT file reader in https://github.com/Andy9FromSpace/map-elevation
