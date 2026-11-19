using System.IO;
using System.Linq;
using System.Text;
using DEM.Net.Core;
using Xunit;

namespace DEM.Net.Test;

/// <summary>GPX segments and routes, offline</summary>
public class GpxImportTests
{
    private static Stream Gpx(string body) => new MemoryStream(Encoding.UTF8.GetBytes(
        $"""<?xml version="1.0"?><gpx version="1.1" creator="test" xmlns="http://www.topografix.com/GPX/1/1" xmlns:gpxx="http://www.garmin.com/xmlschemas/GpxExtensions/v3">{body}</gpx>"""));

    [Fact]
    public void RoutePointsAreRead()
    {
        // a route-only file (GaiaGPS export): the rtept themselves are the route
        using var gpx = Gpx("""
            <rte><name>r</name>
              <rtept lat="33.90" lon="-112.30"><ele>550</ele></rtept>
              <rtept lat="33.91" lon="-112.31"><ele>551</ele></rtept>
              <rtept lat="33.92" lon="-112.32"><ele>552</ele></rtept>
            </rte>
            """);

        var segments = GpxImport.ReadGPX_Segments(gpx).Select(s => s.ToList()).ToList();

        var route = Assert.Single(segments);
        Assert.Equal(3, route.Count);
        Assert.Equal((33.90, -112.30, 550d), (route[0].Latitude, route[0].Longitude, route[0].Elevation!.Value));
        Assert.Equal((33.92, -112.32, 552d), (route[2].Latitude, route[2].Longitude, route[2].Elevation!.Value));
    }

    [Fact]
    public void GarminShapePointsFollowTheirRoutePoint()
    {
        using var gpx = Gpx("""
            <rte>
              <rtept lat="45.0" lon="6.0"><extensions><gpxx:RoutePointExtension>
                <gpxx:rpt lat="45.1" lon="6.1"/><gpxx:rpt lat="45.2" lon="6.2"/>
              </gpxx:RoutePointExtension></extensions></rtept>
              <rtept lat="45.3" lon="6.3"/>
            </rte>
            """);

        var route = Assert.Single(GpxImport.ReadGPX_Segments(gpx)).ToList();

        Assert.Equal([45.0, 45.1, 45.2, 45.3], route.Select(p => p.Latitude));
    }

    [Fact]
    public void TrackSegmentsStaySeparate()
    {
        using var gpx = Gpx("""
            <trk>
              <trkseg><trkpt lat="45.0" lon="6.0"/><trkpt lat="45.1" lon="6.1"/></trkseg>
              <trkseg><trkpt lat="46.0" lon="7.0"/><trkpt lat="46.1" lon="7.1"/><trkpt lat="46.2" lon="7.2"/></trkseg>
              <trkseg></trkseg>
            </trk>
            <rte><rtept lat="47.0" lon="8.0"/><rtept lat="47.1" lon="8.1"/></rte>
            """);

        var segments = GpxImport.ReadGPX_Segments(gpx).Select(s => s.Count()).ToList();

        Assert.Equal([2, 3, 2], segments); // the empty segment is skipped
    }

    [Fact]
    public void SampleTrackIsOneSegment()
    {
        var segments = GpxImport.ReadGPX_Segments(Path.Combine("TestData", "lauzannier.gpx")).ToList();

        Assert.Equal(72, Assert.Single(segments).Count());
    }
}
