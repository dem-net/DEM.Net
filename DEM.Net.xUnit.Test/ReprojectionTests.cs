using System.Linq;
using DEM.Net.Core;
using Xunit;

namespace DEM.Net.Test;

public class ReprojectionTests
{
    private const int SwissGrid = 2056; // CH1903+ / LV95 (swissALTI3D): a datum shift from WGS84

    [Fact]
    public void ElevatedPointsComeBackWhereTheyWere()
    {
        // The API projects request points to the dataset grid without elevation, then back once elevated
        var request = new GeoPoint(45.975, 7.65);
        var projected = new[] { request }.ReprojectTo(Reprojection.SRID_GEODETIC, SwissGrid).Single();
        var back = new[] { projected with { Elevation = 3876.9 } }.ReprojectTo(SwissGrid, Reprojection.SRID_GEODETIC).Single();

        Assert.Equal(request.Latitude, back.Latitude, 1e-8); // was 7e-7 (8 cm): the elevation was used as the datum shift height
        Assert.Equal(request.Longitude, back.Longitude, 1e-8);
        Assert.Equal(3876.9, back.Elevation);
    }

    [Fact]
    public void SingleAndBatchReprojectionsAgree()
    {
        var points = new[] { new GeoPoint(45.975, 7.65, 3876.9), new GeoPoint(46.5, 8.0, 1200) };

        var single = points.ReprojectTo(Reprojection.SRID_GEODETIC, SwissGrid).ToList();
        var batch = points.ReprojectTo(Reprojection.SRID_GEODETIC, SwissGrid, points.Length).ToList();

        for (int i = 0; i < points.Length; i++)
        {
            Assert.Equal(batch[i].Longitude, single[i].Longitude, 1e-6);
            Assert.Equal(batch[i].Latitude, single[i].Latitude, 1e-6);
        }
        // PROJ (gdaltransform -s_srs EPSG:4326 -t_srs EPSG:2056): 2616381.78086119 1091514.08019585
        Assert.Equal(2616381.78086119, single[0].Longitude, 1e-3);
        Assert.Equal(1091514.08019585, single[0].Latitude, 1e-3);
    }
}
