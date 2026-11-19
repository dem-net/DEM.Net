using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DEM.Net.Core;
using Xunit;

namespace DEM.Net.Test;

/// <summary>Ribbon meshes along lines (roads, GPX tracks), offline</summary>
public class LineRibbonTests
{
    private const float Width = 10f;

    /// <summary>Line points in model coordinates: (x, y) horizontal, z elevation</summary>
    private static List<GeoPoint> Line(params (double x, double y, double z)[] pts)
        => [.. pts.Select(p => new GeoPoint(p.y, p.x, p.z))];

    private static TriangulationList<Vector3> Ribbon(List<GeoPoint> line, float simplifyTolerance = 0f)
        => new MeshService().GenerateTriangleMesh_Line(line, Width, default, simplifyTolerance);

    public static TheoryData<string, List<GeoPoint>> Lines => new()
    {
        { "straight", Line((0, 0, 0), (100, 0, 0), (200, 0, 0)) },
        { "90° turn", Line((0, 0, 0), (100, 0, 0), (100, 100, 0)) },
        { "45° turn", Line((0, 0, 0), (100, 0, 0), (200, 100, 0)) },
        { "hairpin", Line((0, 0, 0), (100, 0, 0), (100, 20, 0), (0, 20, 0)) },
        { "steep", Line((0, 0, 0), (10, 0, 100), (20, 0, 0)) },
        { "zigzag", Line([.. Enumerable.Range(0, 20).Select(i => (i * 30d, (i % 2) * 30d, i * 5d))]) },
    };

    /// <summary>With a U-turn, the ribbon overlaps itself</summary>
    public static TheoryData<string, List<GeoPoint>> LinesWithUTurn
    {
        get
        {
            var lines = Lines;
            lines.Add("U-turn", Line((0, 0, 0), (100, 0, 0), (0, 1, 0)));
            return lines;
        }
    }

    [Theory]
    [MemberData(nameof(LinesWithUTurn))]
    public void EveryTriangleFacesUp(string name, List<GeoPoint> line)
    {
        var ribbon = Ribbon(line);

        Assert.NotEmpty(ribbon.Indices);
        for (int i = 0; i < ribbon.Indices.Count; i += 3)
        {
            var (a, b, c) = (ribbon.Positions[ribbon.Indices[i]], ribbon.Positions[ribbon.Indices[i + 1]], ribbon.Positions[ribbon.Indices[i + 2]]);
            var normal = Vector3.Cross(b - a, c - a);
            Assert.True(normal.Y > 0, $"{name}: triangle {i / 3} faces down or is degenerate ({normal})");
        }
    }

    [Theory]
    [MemberData(nameof(Lines))]
    public void RibbonKeepsItsWidthAlongEverySegment(string name, List<GeoPoint> line)
    {
        var centre = line.Select(p => p.ToVector3GlTFSpace()).ToList();
        var ribbon = Ribbon(line);

        // every ribbon vertex but the bevel centres is half a width away from the nearest segment's centre line
        // (a miter vertex is half a width from both adjacent segments' lines, further from their ends)
        foreach (var v in ribbon.Positions)
        {
            var near = Enumerable.Range(0, centre.Count - 1).Where(i => HorizontalDistanceToLine(v, centre[i], centre[i + 1], clamp: true) <= Width).ToList();
            float distance = near.Min(i => HorizontalDistanceToLine(v, centre[i], centre[i + 1], clamp: false));
            if (distance < 1e-3f) continue; // bevel centre
            Assert.True(Math.Abs(distance - Width / 2) <= Width / 2 * 0.01f, $"{name}: vertex {v} is {distance} from the line");
        }
    }

    [Fact]
    public void StraightRibbonHasTheFullWidth()
    {
        var ribbon = Ribbon(Line((0, 0, 0), (100, 0, 0)));

        Assert.Equal(4, ribbon.Positions.Count);
        Assert.Equal(Width, Vector3.Distance(ribbon.Positions[0], ribbon.Positions[1]), 3);
    }

    [Fact]
    public void SteepSegmentDoesNotNarrow()
    {
        // 45° slope: the old side vector, Cross(dir, Y) of the 3D direction, was cos(45°) shorter
        var ribbon = Ribbon(Line((0, 0, 0), (100, 0, 100)));

        Assert.Equal(Width, Vector3.Distance(ribbon.Positions[0], ribbon.Positions[1]), 3);
        Assert.All(ribbon.Positions, p => Assert.True(p.Y is 0 or 100));
    }

    [Fact]
    public void RightAngleUsesAMiter()
    {
        var ribbon = Ribbon(Line((0, 0, 0), (100, 0, 0), (100, 100, 0)));

        Assert.Equal(6, ribbon.Positions.Count); // three cross sections, no bevel
        Assert.Equal(Width * MathF.Sqrt(2), Vector3.Distance(ribbon.Positions[2], ribbon.Positions[3]), 3);
    }

    [Fact]
    public void HairpinWithRoomUsesMiters()
    {
        // two 90° turns: their miters recede 5 m along the 20 m leg
        var ribbon = Ribbon(Line((0, 0, 0), (100, 0, 0), (100, 20, 0), (0, 20, 0)));

        Assert.Equal(8, ribbon.Positions.Count);
    }

    [Theory]
    [InlineData(0, 1)] // U-turn
    [InlineData(0, 30)] // 163° turn: the miter would be longer than two half widths
    [InlineData(100, 4)] // 90° turn onto a 4 m leg: the miter recedes 5 m
    public void SharpTurnUsesABevel(double x, double y)
    {
        var ribbon = Ribbon(Line((0, 0, 0), (100, 0, 0), (x, y, 0)));

        Assert.Equal(2 + 2 + 2 + 1 + 2, ribbon.Positions.Count); // start, bevel in and out, bevel centre, end
    }

    [Fact]
    public void ClosePointsAreDropped()
    {
        // jitter under a quarter of the width
        var ribbon = Ribbon(Line((0, 0, 0), (1, 0.5, 0), (2, -0.5, 0), (100, 0, 0)));

        Assert.Equal(4, ribbon.Positions.Count);
    }

    [Fact]
    public void VerticalSegmentsAreDropped()
    {
        var ribbon = Ribbon(Line((0, 0, 0), (0, 0, 50), (100, 0, 50)));

        Assert.Equal(4, ribbon.Positions.Count);
        Assert.All(ribbon.Indices.Chunk(3), t => Assert.True(Vector3.Cross(ribbon.Positions[t[1]] - ribbon.Positions[t[0]], ribbon.Positions[t[2]] - ribbon.Positions[t[0]]).Y > 0));
    }

    [Fact]
    public void ShortOrEmptyLinesGiveNoTriangles()
    {
        Assert.Empty(Ribbon(Line((0, 0, 0))).Indices);
        Assert.Empty(Ribbon([]).Indices);
        Assert.Empty(Ribbon(Line((0, 0, 0), (0, 0, 10))).Indices);
    }

    [Fact]
    public void SimplificationKeepsElevationChanges()
    {
        // straight in plan, over a 50 m ridge: a 2D simplification would cut through it
        var line = Line([.. Enumerable.Range(0, 101).Select(i => (i * 10d, 0d, 50 - Math.Abs(i - 50d)))]);

        var simplified = LineHelpers.SimplifyDouglasPeucker([.. line.Select(p => p.ToVector3GlTFSpace())], 2.5f);

        Assert.Equal(3, simplified.Count);
        Assert.Equal(50f, simplified[1].Y, 3);
    }

    [Fact]
    public void SimplifiedRibbonHasFewerVertices()
    {
        var line = Line([.. Enumerable.Range(0, 200).Select(i => (i * 5d, Math.Sin(i / 20d) * 0.5, 0d))]);

        Assert.True(Ribbon(line, simplifyTolerance: Width / 4).Positions.Count < Ribbon(line).Positions.Count / 4);
    }

    private static float HorizontalDistanceToLine(Vector3 p, Vector3 a, Vector3 b, bool clamp)
    {
        var ab = new Vector2(b.X - a.X, b.Z - a.Z);
        var ap = new Vector2(p.X - a.X, p.Z - a.Z);
        float t = Vector2.Dot(ap, ab) / ab.LengthSquared();
        if (clamp) t = Math.Clamp(t, 0, 1);
        return Vector2.Distance(ap, ab * t);
    }
}
