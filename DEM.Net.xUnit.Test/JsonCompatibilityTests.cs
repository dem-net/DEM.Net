using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using DEM.Net.Core;
using DEM.Net.Core.Datasets;
using DEM.Net.Core.EarthData;
using DEM.Net.Core.Stac;
using DEM.Net.Core.TileIndex;
using DEM.Net.Core.XyzTiles;
using Xunit;

namespace DEM.Net.Test;

/// <summary>
/// System.Text.Json readers (v11): the files written by former versions with Newtonsoft.Json are still read,
/// and the remote payloads are parsed as before.
/// </summary>
public sealed class JsonCompatibilityTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "DEM.Net.Tests", Guid.NewGuid().ToString("N"));

    public JsonCompatibilityTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    private string WriteFile(string name, string content)
    {
        string path = Path.Combine(_dir, name);
        File.WriteAllText(path, content);
        return path;
    }

    private static readonly DEMDataSet AnyDataSet = new()
    {
        Name = "json-tests",
        FileFormat = new DEMFileDefinition("hgt", DEMFileType.SRTM_HGT, ".hgt", DEMFileRegistrationMode.Grid),
    };

    #region Files written by former versions

    // Written by DEM.Net 10 (Newtonsoft.Json, indented), NaN and Infinity as strings
    private const string NewtonsoftMetadata = """
        {
          "Version": "2.3",
          "Filename": "N43E005.hgt",
          "Height": 1201,
          "Width": 1201,
          "PixelScaleX": 0.0008333333333333334,
          "PixelScaleY": 0.0008333333333333334,
          "DataStartLat": 43.0,
          "DataStartLon": 5.0,
          "DataEndLat": 44.0,
          "DataEndLon": 6.0,
          "BitsPerSample": 16,
          "WorldUnits": null,
          "SampleFormat": "INT",
          "NoDataValue": "-32768",
          "ScanlineSize": 2402,
          "PhysicalStartLon": 4.999583333333334,
          "PhysicalStartLat": 42.999583333333334,
          "PhysicalEndLon": 6.0008333333333335,
          "PhysicalEndLat": 44.00083333333333,
          "pixelSizeX": 0.0008333333333333334,
          "pixelSizeY": -0.0008333333333333334,
          "FileFormat": {
            "Name": "Nasa SRTM HGT",
            "FileExtension": ".hgt",
            "Type": 0,
            "Registration": 0
          },
          "MinimumAltitude": "NaN",
          "MaximumAltitude": "Infinity",
          "NoDataValueFloat": -32768.0
        }
        """;

    [Fact]
    public void FormerMetadataFilesAreRead()
    {
        var metadata = RasterService.ReadMetadataFile(WriteFile("N43E005.json", NewtonsoftMetadata));

        Assert.Equal(FileMetadata.FILEMETADATA_VERSION, metadata.Version);
        Assert.Equal("N43E005.hgt", metadata.Filename);
        Assert.Equal(1201, metadata.Width);
        Assert.Equal(0.0008333333333333334, metadata.PixelScaleX);
        Assert.Equal(-0.0008333333333333334, metadata.pixelSizeY);
        Assert.Equal(44.00083333333333, metadata.PhysicalEndLat);
        Assert.Equal("-32768", metadata.NoDataValue);
        Assert.Equal(DEMFileType.SRTM_HGT, metadata.FileFormat.Type);
        Assert.Equal(".hgt", metadata.FileFormat.FileExtension);
        Assert.True(float.IsNaN(metadata.MinimumAltitude));
        Assert.Equal(float.PositiveInfinity, metadata.MaximumAltitude);
        Assert.Equal(43, metadata.BoundingBox.yMin, 3);
    }

    // Written by DEM.Net 10 next to SketchFabApi.Net, which changed the global Newtonsoft settings (camelCase, enum names, no nulls)
    private const string CamelCaseMetadata = """
        {
          "version": "2.3",
          "filename": "AW3D30/AW3D30_global/ALPSMLC30_N048E002_DSM.tif",
          "height": 3600,
          "width": 3600,
          "pixelScaleX": 0.0002777777777777778,
          "pixelScaleY": 0.0002777777777777778,
          "dataStartLat": 48.000277777777775,
          "dataStartLon": 2.0,
          "dataEndLat": 49.0,
          "dataEndLon": 2.999722222222222,
          "bitsPerSample": 16,
          "sampleFormat": "INT",
          "noDataValue": "-32767",
          "scanlineSize": 7200,
          "physicalStartLon": 2.0,
          "physicalStartLat": 48.0,
          "physicalEndLon": 3.0,
          "physicalEndLat": 49.0,
          "pixelSizeX": 0.0002777777777777778,
          "pixelSizeY": -0.0002777777777777778,
          "fileFormat": {
            "name": "GeoTiff file",
            "fileExtension": ".tif",
            "type": "GEOTIFF",
            "registration": "Cell"
          },
          "minimumAltitude": 0.0,
          "maximumAltitude": 0.0,
          "noDataValueFloat": -32767.0
        }
        """;

    [Fact]
    public void FormerCamelCaseMetadataFilesAreRead()
    {
        var metadata = RasterService.ReadMetadataFile(WriteFile("ALPSMLC30_N048E002_DSM.json", CamelCaseMetadata));

        Assert.Equal("AW3D30/AW3D30_global/ALPSMLC30_N048E002_DSM.tif", metadata.Filename);
        Assert.Equal(7200, metadata.ScanlineSize);
        Assert.Equal(48.000277777777775, metadata.DataStartLat);
        Assert.Null(metadata.WorldUnits);
        Assert.Equal(DEMFileType.GEOTIFF, metadata.FileFormat.Type);
        Assert.Equal(DEMFileRegistrationMode.Cell, metadata.FileFormat.Registration);
        Assert.Equal(-32767f, metadata.NoDataValueFloat);
    }

    [Fact]
    public void MetadataFilesRoundTrip()
    {
        var metadata = RasterService.ReadMetadataFile(WriteFile("in.json", NewtonsoftMetadata));
        string path = Path.Combine(_dir, "out.json");

        RasterService.WriteMetadataFile(path, metadata);
        var reread = RasterService.ReadMetadataFile(path);

        string json = File.ReadAllText(path);
        Assert.Contains("\"MinimumAltitude\": \"NaN\"", json);
        Assert.DoesNotContain("BoundingBox", json);
        Assert.DoesNotContain("VirtualMetadata", json);
        foreach (var property in typeof(FileMetadata).GetProperties().Where(p => p.CanWrite && p.Name != nameof(FileMetadata.FileFormat)))
            Assert.Equal(property.GetValue(metadata), property.GetValue(reread));
        Assert.Equal(metadata.FileFormat.Name, reread.FileFormat.Name);
        Assert.Equal(metadata.FileFormat.Registration, reread.FileFormat.Registration);
    }

    [Fact]
    public void FormerIndexFilesAreRead()
    {
        const BindingFlags NonPublic = BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

        string stac = WriteFile("stac.json", """
            [
              { "FileId": "a.tif", "Box": [ 5.9, 45.8, 5.93, 45.82 ], "Href": "https://data.geo.admin.ch/a.tif", "DateTime": "2019-01-01T00:00:00+00:00" },
              { "FileId": "b.tif", "Box": [ 6.1, 46.1, 6.2, 46.25 ], "Href": "https://data.geo.admin.ch/b.tif", "DateTime": "2021-06-30T12:00:00+02:00" }
            ]
            """);
        var stacSources = (List<DEMFileSource>)typeof(StacFileService).GetMethod("GetSources", NonPublic)!
            .Invoke(new StacFileService(null!, null!), [AnyDataSet, stac])!;
        Assert.Equal(2, stacSources.Count);
        Assert.Equal("https://data.geo.admin.ch/b.tif", stacSources[1].SourceFileNameAbsolute);
        Assert.Equal(46.25, stacSources[1].BBox.yMax, 5);

        string nasa = WriteFile("nasa.json", """[{"Box":"43 5 44 6","ZipFileLink":"https://e4ftl01.cr.usgs.gov/N43E005.SRTMGL1.hgt.zip","GranuleId":"SRTMGL1_N43E005"}]""");
        var nasaSources = (List<DEMFileSource>)typeof(NasaGranuleFileService).GetMethod("GetSources", NonPublic)!
            .Invoke(null, [AnyDataSet, nasa])!;
        Assert.Equal("https://e4ftl01.cr.usgs.gov/N43E005.SRTMGL1.hgt.zip", Assert.Single(nasaSources).SourceFileNameAbsolute);
        Assert.Equal("SRTMGL1_N43E005", nasaSources[0].SourceFileName);
        Assert.Equal(5, nasaSources[0].BBox.xMin);

        string tiles = WriteFile("tiles.json", """[{"FileName":"N43E005_FABDEM_V1-2.tif","Url":"https://h/N43E005_FABDEM_V1-2.tif","Box":[5.0,43.0,6.0,44.0]}]""");
        var tileSources = (List<DEMFileSource>)typeof(TileIndexFileService).GetMethod("LoadSources", NonPublic)!
            .Invoke(null, [tiles, _dir, 4326])!;
        Assert.Equal("https://h/N43E005_FABDEM_V1-2.tif", Assert.Single(tileSources).SourceFileNameAbsolute);
        Assert.Equal(44, tileSources[0].BBox.yMax);
    }

    #endregion

    #region Remote payloads

    [Fact]
    public void StacPagesAreParsed()
    {
        var page = CollectionItems.FromJson("""
            {"type":"FeatureCollection","timeStamp":"2023-04-01T10:20:30Z","features":[
             {"id":"a","collection":"ch.swisstopo.swissalti3d","type":"Feature","stac_version":"0.9.0",
              "geometry":{"type":"Polygon","coordinates":[[[5.9,45.8],[5.93,45.8],[5.9,45.8]]]},"bbox":[5.9,45.8,5.93,45.82],
              "properties":{"datetime":"2019-01-01T00:00:00Z","created":"2021-02-25T13:59:40Z","updated":"2021-02-25T13:59:40Z"},
              "links":[{"rel":"self","href":"https://s/a"},{"rel":"alternate","href":"https://s/alt"}],
              "assets":{"a_2.tif":{"type":"image/tiff; application=geotiff; profile=cloud-optimized","href":"https://s/a_2.tif","created":"2021-02-25T13:59:40","updated":"2021-02-25T13:59:40Z","eo:gsd":2.0,"proj:epsg":2056,"checksum:multihash":"1220","other":{"x":1}},
                        "a.xyz.zip":{"type":"application/x.ascii-xyz+zip","href":"https://s/a.xyz.zip","created":"2021-02-25T13:59:40Z","updated":"2021-02-25T13:59:40Z","eo:gsd":2,"proj:epsg":2056,"checksum:multihash":"1220"}}}],
             "links":[{"rel":"next","href":"https://s/items?cursor=abc"}]}
            """);

        var feature = Assert.Single(page.Features);
        Assert.Equal(Collection.ChSwisstopoSwissalti3D, feature.Collection);
        Assert.Equal(StacVersion.The090, feature.StacVersion);
        Assert.Equal(Rel.Alternate, feature.Links[1].Rel);
        Assert.Equal(new Uri("https://s/items?cursor=abc"), page.Links.Single(l => l.Rel == Rel.Next).Href);
        var cog = feature.Assets["a_2.tif"];
        Assert.Equal(AssetType.ImageTiffApplicationGeotiffProfileCloudOptimized, cog.Type);
        Assert.Equal(2056, cog.ProjEpsg);
        Assert.Equal(TimeSpan.Zero, cog.Created.Offset); // dates without offset are UTC
        Assert.Equal(AssetType.ApplicationXAsciiXyzZip, feature.Assets["a.xyz.zip"].Type);
        Assert.True(DEMDataSet.RegisteredDatasets.Single(d => d.Name == "swissALTI3D 2m").DataSource is StacDataSource source && source.Filter(cog));
    }

    [Fact]
    public void UnknownStacValuesAreRejected()
    {
        Assert.Throws<JsonException>(() => CollectionItems.FromJson("""{"features":[],"links":[{"rel":"items","href":"https://s/"}]}"""));
    }

    [Fact]
    public void CmrPagesAreParsed()
    {
        var result = NasaCmrGranuleResult.FromJson("""
            {"feed":{"updated":"2023-04-01T10:20:30.123Z","id":"https://cmr.earthdata.nasa.gov/search","title":"t","entry":[
             {"producer_granule_id":"N43E005.SRTMGL1.hgt.zip","boxes":["43 5 44 6"],"time_start":"2000-02-11T00:00:00.000Z","updated":"2015-09-02T10:50:46.433Z","time_end":"2000-02-21T23:59:59Z",
              "original_format":"ECHO10","granule_size":"8.4","browse_flag":true,"online_access_flag":true,
              "links":[{"rel":"http://esipfed.org/ns/fedsearch/1.1/data#","type":"application/x-zip","href":"https://e4ftl01/N43E005.zip"},
                       {"inherited":true,"rel":"http://esipfed.org/ns/fedsearch/1.1/metadata#","href":"https://lpdaac.usgs.gov/"}]}]}}
            """);

        var entry = Assert.Single(result.Feed.Entry);
        Assert.Equal(OriginalFormat.Echo10, entry.OriginalFormat);
        Assert.Equal("43 5 44 6", Assert.Single(entry.Boxes));
        Assert.Equal(TypeEnum.ApplicationZip, entry.Links[0].Type);
        Assert.Null(entry.Links[1].Type);
        Assert.True(entry.Links[1].Inherited);
        Assert.DoesNotContain("\"title\"", result.ToJson().Split("\"links\"")[1]); // null link values are not written
    }

    [Fact]
    public void TileIndexesAreParsed()
    {
        var source = new TileIndexDataSource("https://idx", "idx.json",
            tileUrl: p => p.TryGetValue("file", out var file) ? $"https://h/{file}" : null!);

        var entries = TileIndexFileService.ParseGeoJsonIndex("""
            {"type":"FeatureCollection","features":[
             {"type":"Feature","properties":{"file":"a.tif","id":12,"area":1.25,"whole":2.0,"exp":1e-7,"valid":true,"off":false,"missing":null},
              "geometry":{"type":"Polygon","coordinates":[[[5.0,43.0],[6.0,43.0],[6.0,44.0],[5.0,43.0]]]}},
             {"type":"Feature","properties":{"file":"b.tif"},
              "geometry":{"type":"MultiPolygon","coordinates":[[[[-1.5,-1.0],[0.0,-1.0],[0.0,0.25],[-1.5,-1.0]]],[[[2,3],[4,5],[2,5],[2,3]]]]}},
             {"type":"Feature","properties":{"file":"no-geometry.tif"},"geometry":null},
             {"type":"Feature","properties":{"file":"point.tif"},"geometry":{"type":"Point","coordinates":[7.5,8.25]}},
             {"type":"Feature","properties":{"other":"no url"},"geometry":{"type":"Polygon","coordinates":[[[0,0],[1,1],[0,1],[0,0]]]}}]}
            """, source);

        Assert.Equal(new[] { "a.tif", "b.tif" }, entries.Select(e => e.FileName));
        Assert.Equal([5.0, 43, 6, 44], entries[0].Box);
        Assert.Equal([-1.5, -1, 4, 5], entries[1].Box);

        // property values reach the url builders as former versions formatted them
        IReadOnlyDictionary<string, string> properties = null!;
        TileIndexFileService.ParseGeoJsonIndex(
            """{"features":[{"properties":{"id":12,"area":1.25,"whole":2.0,"exp":1e-7,"valid":true,"off":false,"missing":null,"name":"été"},"geometry":{"type":"Polygon","coordinates":[[[0,0],[1,1],[0,0]]]}}]}""",
            new TileIndexDataSource("u", "f", p => { properties = p; return "https://h/x.tif"; }));
        Assert.Equal("12", properties["id"]);
        Assert.Equal("1.25", properties["area"]);
        Assert.Equal("2", properties["whole"]);
        Assert.Equal("1E-07", properties["exp"]);
        Assert.Equal("True", properties["valid"]);
        Assert.Equal("False", properties["off"]);
        Assert.Null(properties["missing"]);
        Assert.Equal("été", properties["name"]);

        Assert.Throws<InvalidDataException>(() => TileIndexFileService.ParseGeoJsonIndex("""{"type":"Feature"}""", source));
    }

    [Fact]
    public void CoverageIndexesAreParsed()
    {
        var areas = XyzTileIndexService.ParseCoverageIndex("""
            {"items":[{"min_lon":-180,"min_lat":-85.0511,"max_lon":180,"max_lat":85.0511,"max_zoom":12},
                      {"min_lon":5.5,"min_lat":43.25,"max_lon":6.75,"max_lat":44.125,"max_zoom":17,"extra":[1,2]},
                      {"min_lon":1,"min_lat":1,"max_lon":2,"max_lat":2},
                      {"min_lon":1,"min_lat":1,"max_lon":2,"max_lat":2,"max_zoom":null},
                      {"max_zoom":3}]}
            """);

        Assert.Equal(new[] { 12, 17 }, areas.Select(a => a.MaxZoom));
        var expected = WebMercatorTiles.ToMercator(new BoundingBox(5.5, 6.75, 43.25, 44.125));
        Assert.Equal(expected.xMin, areas[1].Bounds.xMin);
        Assert.Equal(expected.yMax, areas[1].Bounds.yMax);
        Assert.Throws<InvalidDataException>(() => XyzTileIndexService.ParseCoverageIndex("""{"other":[]}"""));
    }

    [Fact]
    public void SensorLogExportsAreParsed()
    {
        string path = WriteFile("sensorlog.json", """
            [{"loggingTime":"2019-03-02T16:03:18.123+01:00","logSampleNr":"0","locationLatitude":"45.123456","locationLongitude":"6.5","locationAltitude":"1234.5",
              "accelerometerAccelerationX":"0.01","accelerometerAccelerationY":"","motionQuaternionW":"0.5","deviceOrientation":"1","batteryState":"2","batteryLevel":"0.8",
              "label":"","activityActivityStartDate":"2019-03-02T16:00:00Z","activityActivityConfidence":"2","altimeterReset":"0"},
             {"loggingTime":"2019-03-02 16:03:19.123","logSampleNr":1,"locationLatitude":45.2,"locationLongitude":6.6,"locationAltitude":1240,"gyroRotationX":-0.25,
              "deviceOrientation":1,"batteryState":2,"activityActivityStartDate":"2019-03-02T16:00:00","activityActivityConfidence":2,"altimeterReset":0}]
            """);

        var log = Core.IO.SensorLog.SensorLog.FromJson(path);

        Assert.Equal(2, log.Count);
        Assert.Equal(2, log.LocationCount);
        Assert.Equal(1234.5f, log.AltitudeMin);
        Assert.Equal(1240f, log.AltitudeMax);
        var first = log.Data[0];
        Assert.Equal(45.123456f, first.LocationLatitude);
        Assert.Equal(0.8f, first.BatteryLevel);
        Assert.Equal(new System.Numerics.Vector3(0.01f, 0, 0), first.AccelerometerAcceleration); // internal members, "" is no value
        Assert.Equal(0.5f, first.MotionQuaternion.W);
        Assert.Equal(-0.25f, log.Data[1].GyroRotation.X);
        Assert.Equal(new DateTimeOffset(2019, 3, 2, 15, 3, 18, 123, TimeSpan.Zero), new DateTimeOffset(first.LoggingTime));
        Assert.Equal(new DateTimeOffset(2019, 3, 2, 16, 3, 19, 123, TimeSpan.Zero), new DateTimeOffset(log.Data[1].LoggingTime)); // no offset: UTC
    }

    #endregion
}
