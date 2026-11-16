using System.Linq;
using DEM.Net.Core;
using Xunit;

namespace DEM.Net.Test;

/// <summary>Since v11 the height map coordinates are an array, changed in place by the transforms (offline)</summary>
public class HeightMapTransformTests
{
    /// <summary>Grid without a bounding box: it is computed from the points</summary>
    private static HeightMap Grid(int width = 20, int height = 10)
    {
        var hm = new HeightMap(width, height)
        {
            Coordinates = Enumerable.Range(0, width * height)
                .Select(i => new GeoPoint(45.1 - (i / width) * 0.01, 6 + (i % width) * 0.01, 1000 + i))
                .ToArray(),
        };
        hm.Minimum = 1000;
        hm.Maximum = 1000 + width * height - 1;
        return hm;
    }

    private static (double, double, double?)[] Values(GeoPoint[] points) => points.Select(p => (p.Latitude, p.Longitude, p.Elevation)).ToArray();

    [Fact]
    public void TransformsChangeTheCoordinatesInPlace()
    {
        var hm = Grid();
        var source = hm.Coordinates.ToArray();
        var bbox = hm.BoundingBox.ReprojectTo(Reprojection.SRID_GEODETIC, Reprojection.SRID_PROJECTED_MERCATOR);
        // same transforms on the points
        var expected = source.ReprojectGeodeticToCartesian(source.Length).ZScale(2f).CenterOnOrigin(bbox).ToArray();
        var coordinates = hm.Coordinates;

        var result = hm.ReprojectGeodeticToCartesian().ZScale(2f).CenterOnOrigin();

        Assert.Same(hm, result);
        Assert.Same(coordinates, result.Coordinates);
        Assert.Equal(Values(expected), Values(result.Coordinates));
    }

    [Fact]
    public void ComputedBoundingBoxIsReprojectedOnce()
    {
        var hm = Grid();
        var expected = Grid().BoundingBox.ReprojectTo(Reprojection.SRID_GEODETIC, Reprojection.SRID_PROJECTED_MERCATOR);

        var bbox = hm.ReprojectGeodeticToCartesian().BoundingBox; // not computed before the reprojection

        Assert.Equal((expected.xMin, expected.xMax, expected.yMin, expected.yMax), (bbox.xMin, bbox.xMax, bbox.yMin, bbox.yMax));
    }

    [Fact]
    public void CloneKeepsTheOriginalPoints()
    {
        var hm = Grid();
        var source = Values(hm.Coordinates);

        var clone = hm.Clone().ZTranslate(10).Scale(2, 2, 2);

        Assert.Equal(source, Values(hm.Coordinates));
        Assert.Equal(hm.Coordinates.Select(p => (p.Elevation + 10) * 2), clone.Coordinates.Select(p => p.Elevation));
    }

    [Fact]
    public void ElevationTransforms()
    {
        var hm = Grid(4, 2).ZClamp(1002, 1005);
        Assert.Equal(new double?[] { 1002, 1002, 1002, 1003, 1004, 1005, 1005, 1005 }, hm.Coordinates.Select(p => p.Elevation));

        hm.Coordinates[0] = hm.Coordinates[0] with { Elevation = null };
        hm.ZTranslate(1);
        Assert.Equal(1d, hm.Coordinates[0].Elevation); // no elevation: as before, from 0
    }
}
