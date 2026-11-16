using System;

namespace DEM.Net.Core;

/// <summary>
/// Spherical Web Mercator (EPSG:3857) tile math for XYZ tile pyramids.
/// </summary>
public static class WebMercatorTiles
{
    public const double EarthRadius = 6378137d;
    public const double OriginShift = Math.PI * EarthRadius; // 20037508.342789244
    public const double MaxLatitude = 85.0511287798066;

    /// <summary>Size of one tile edge in EPSG:3857 meters.</summary>
    public static double TileSizeMeters(int zoom) => 2 * OriginShift / (1L << zoom);

    /// <summary>Size of one pixel in EPSG:3857 meters (ground resolution at the equator).</summary>
    public static double PixelSizeMeters(int zoom, int tileSize) => TileSizeMeters(zoom) / tileSize;

    /// <summary>Tile bounds in EPSG:3857 meters.</summary>
    public static BoundingBox TileBounds(int zoom, int x, int y)
    {
        double size = TileSizeMeters(zoom);
        double xMin = -OriginShift + x * size;
        double yMax = OriginShift - y * size;
        return new BoundingBox(xMin, xMin + size, yMax - size, yMax) { SRID = Reprojection.SRID_PROJECTED_MERCATOR };
    }

    public static double LonToX(double lon) => lon * OriginShift / 180d;

    public static double LatToY(double lat)
    {
        lat = Math.Max(-MaxLatitude, Math.Min(MaxLatitude, lat));
        return Math.Log(Math.Tan((90d + lat) * Math.PI / 360d)) * EarthRadius;
    }

    /// <summary>Converts a WGS84 bbox to EPSG:3857, or returns it as is when already projected.</summary>
    public static BoundingBox ToMercator(BoundingBox bbox)
    {
        if (bbox.SRID == Reprojection.SRID_PROJECTED_MERCATOR)
            return bbox;
        if (bbox.SRID != Reprojection.SRID_GEODETIC)
            bbox = bbox.ReprojectTo(bbox.SRID, Reprojection.SRID_GEODETIC);

        return new BoundingBox(LonToX(bbox.xMin), LonToX(bbox.xMax), LatToY(bbox.yMin), LatToY(bbox.yMax), bbox.zMin, bbox.zMax)
        { SRID = Reprojection.SRID_PROJECTED_MERCATOR };
    }

    /// <summary>
    /// Range of tiles covering an EPSG:3857 bbox at given zoom (inclusive, clamped to the pyramid).
    /// </summary>
    public static (int xMin, int xMax, int yMin, int yMax) TileRange(BoundingBox mercatorBbox, int zoom)
    {
        double size = TileSizeMeters(zoom);
        int max = (int)((1L << zoom) - 1);
        int Clamp(double v) => Math.Max(0, Math.Min(max, (int)Math.Floor(v)));

        int xMin = Clamp((mercatorBbox.xMin + OriginShift) / size);
        int xMax = Clamp((mercatorBbox.xMax + OriginShift) / size);
        int yMin = Clamp((OriginShift - mercatorBbox.yMax) / size);
        int yMax = Clamp((OriginShift - mercatorBbox.yMin) / size);
        return (xMin, xMax, yMin, yMax);
    }

    /// <summary>Number of tiles covering a bbox (any SRID) at given zoom.</summary>
    public static long TileCount(BoundingBox bbox, int zoom)
    {
        var (xMin, xMax, yMin, yMax) = TileRange(ToMercator(bbox), zoom);
        return (long)(xMax - xMin + 1) * (yMax - yMin + 1);
    }
}
