using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DEM.Net.Core;
using Xunit;

namespace DEM.Net.Test;

/// <summary>GeoPoint value semantics (a readonly struct since v11)</summary>
public class GeoPointTests
{
    [Fact]
    public void PointsAreSmall() => Assert.Equal(32, Unsafe.SizeOf<GeoPoint>());

    [Fact]
    public void DefaultPointHasNoElevation()
    {
        GeoPoint point = default;
        Assert.Equal(0, point.Latitude);
        Assert.Equal(0, point.Longitude);
        Assert.Null(point.Elevation);
        Assert.Null(point.DistanceFromOriginMeters);
        Assert.Null((new GeoPoint[1])[0].Elevation);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-0d)]
    [InlineData(-32768d)]
    [InlineData(8848.86)]
    [InlineData(double.MaxValue)]
    [InlineData(double.PositiveInfinity)]
    public void OptionalValuesAreKeptExactly(double value)
    {
        var point = new GeoPoint(1, 2, value) with { DistanceFromOriginMeters = value };
        Assert.Equal(value, point.Elevation!.Value);
        Assert.Equal(value, point.DistanceFromOriginMeters!.Value);
        Assert.Equal(double.IsNegative(value), double.IsNegative(point.Elevation.Value));
    }

    [Fact]
    public void NaNIsNoValue()
    {
        Assert.Null(new GeoPoint(1, 2, double.NaN).Elevation);
        Assert.Null((new GeoPoint(1, 2, 3) with { Elevation = double.NaN }).Elevation);
        Assert.Null((new GeoPoint(1, 2, 3) with { Elevation = null }).Elevation);
    }

    [Fact]
    public void WithCreatesAnIndependentCopy()
    {
        var points = new List<GeoPoint> { new GeoPoint(1, 2, 3) };
        var copy = points[0] with { Elevation = 10 };
        Assert.Equal(3, points[0].Elevation);
        Assert.Equal(10, copy.Elevation);
        Assert.Equal(1, copy.Latitude);
    }

    [Fact]
    public void EqualityIsOnTheLocation()
    {
        Assert.Equal(new GeoPoint(1, 2, 3), new GeoPoint(1, 2, 100));
        Assert.NotEqual(new GeoPoint(1, 2), new GeoPoint(2, 1));
        Assert.Equal(new GeoPoint(1, 2).GetHashCode(), new GeoPoint(1, 2, 5).GetHashCode());
    }

    [Fact]
    public void MetricsWriteTheDistancesInTheList()
    {
        var line = new List<GeoPoint> { new GeoPoint(45, 6, 100), new GeoPoint(45.001, 6, 110), new GeoPoint(45.002, 6, 105) };
        var metrics = line.ComputeMetrics();

        Assert.Equal(0, line[0].DistanceFromOriginMeters);
        Assert.InRange(line[1].DistanceFromOriginMeters!.Value, 110, 112);
        Assert.Equal(metrics.Distance, line[2].DistanceFromOriginMeters!.Value, 6);
        Assert.Equal(10, metrics.Climb, 6);
    }
}
