using System;
using System.Linq;
using DEM.Net.Core;
using Xunit;

namespace DEM.Net.Test;

/// <summary>Scaled Web Mercator (trueScale) in <see cref="ModelGenerationTransform"/>, offline</summary>
public class TrueScaleTransformTests
{
    private const double Lat0 = 40.75; // Midtown Manhattan
    private const double Lon0 = -73.985;
    private const double EarthRadius = 6378137d; // Web Mercator sphere
    private static readonly double CosLat0 = Math.Cos(Lat0 * Math.PI / 180);

    private static readonly BoundingBox Bbox = new(Lon0 - 0.02, Lon0 + 0.02, Lat0 - 0.015, Lat0 + 0.015);

    /// <summary>The centre, then 1 km east and 1 km north of it</summary>
    private static readonly GeoPoint[] Points =
    [
        new(Lat0, Lon0, 100),
        new(Lat0, Lon0 + 1000 / (EarthRadius * CosLat0) * 180 / Math.PI, 100),
        new(Lat0 + 1000 / EarthRadius * 180 / Math.PI, Lon0, 100),
    ];

    private static ModelGenerationTransform Transform(bool trueScale, bool centerOnOrigin = true, float zFactor = 2f)
        => new(Bbox, Reprojection.SRID_GEODETIC, Reprojection.SRID_PROJECTED_MERCATOR, centerOnOrigin, zFactor, centerOnZOrigin: false, trueScale);

    [Fact]
    public void TrueScaleGivesTrueMetresAtTheCentre()
    {
        var transform = Transform(trueScale: true);
        var p = transform.TransformPoints(Points).ToArray();

        Assert.True(transform.IsTrueScale);
        Assert.Equal(1d, transform.ModelUnitsPerMeter);
        Assert.Equal(1000, p[1].Longitude - p[0].Longitude, 0.5);
        Assert.Equal(1000, p[2].Latitude - p[0].Latitude, 2.0);
    }

    [Fact]
    public void WithoutTrueScaleTheOutputIsPureWebMercator()
    {
        var transform = Transform(trueScale: false);
        var bboxMercator = Bbox.ReprojectTo(Reprojection.SRID_GEODETIC, Reprojection.SRID_PROJECTED_MERCATOR);
        var expected = Points.ReprojectTo(Reprojection.SRID_GEODETIC, Reprojection.SRID_PROJECTED_MERCATOR).CenterOnOrigin(bboxMercator).ZScale(2f).ToArray();

        var p = transform.TransformPoints(Points).ToArray();

        Assert.False(transform.IsTrueScale);
        Assert.Equal(1 / CosLat0, transform.ModelUnitsPerMeter, 6);
        Assert.Equal(1.32, transform.ModelUnitsPerMeter, 2);
        Assert.Equal(expected.Select(e => (e.Longitude, e.Latitude, e.Elevation)), p.Select(e => (e.Longitude, e.Latitude, e.Elevation)));
        Assert.Equal(1000 / CosLat0, p[1].Longitude - p[0].Longitude, 0.5);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ElevationIsOnlyScaledByZFactor(bool trueScale)
    {
        var p = Transform(trueScale).TransformPoints(Points).ToArray();

        Assert.All(p, pt => Assert.Equal(200, pt.Elevation!.Value, 6));
    }

    [Fact]
    public void UncentredModelsIgnoreTrueScale()
    {
        var transform = Transform(trueScale: true, centerOnOrigin: false);
        var expected = Points.ReprojectTo(Reprojection.SRID_GEODETIC, Reprojection.SRID_PROJECTED_MERCATOR).ToArray();

        var p = transform.TransformPoints(Points).ToArray();

        Assert.False(transform.IsTrueScale);
        Assert.Equal(1 / CosLat0, transform.ModelUnitsPerMeter, 6);
        Assert.Equal(expected[1].Longitude, p[1].Longitude, 6);
    }

    [Fact]
    public void HeightMapAndPointsShareTheScale()
    {
        // a height map sample and a point at the same place end up at the same model position
        var hMap = new HeightMap(2, 2)
        {
            Coordinates = [new(Bbox.yMax, Bbox.xMin, 10), new(Bbox.yMax, Bbox.xMax, 20), new(Bbox.yMin, Bbox.xMin, 30), new(Bbox.yMin, Bbox.xMax, 40)],
            BoundingBox = Bbox,
            Minimum = 10,
            Maximum = 40,
        };
        var corner = new GeoPoint(Bbox.yMin, Bbox.xMax, 40);
        var transform = Transform(trueScale: true);

        var mapped = transform.TransformHeightMap(hMap);
        var point = transform.TransformPoints([corner]).Single();

        Assert.Equal(point.Longitude, mapped.Coordinates[3].Longitude, 3);
        Assert.Equal(point.Latitude, mapped.Coordinates[3].Latitude, 3);
        Assert.Equal(point.Elevation!.Value, mapped.Coordinates[3].Elevation!.Value, 3);
        Assert.Equal(mapped.BoundingBox.xMax, mapped.Coordinates[3].Longitude, 3);
        // the model is cos(lat₀) narrower than in Web Mercator
        Assert.Equal(0.04 * Math.PI / 180 * EarthRadius * CosLat0, mapped.BoundingBox.Width, 1);
    }
}
