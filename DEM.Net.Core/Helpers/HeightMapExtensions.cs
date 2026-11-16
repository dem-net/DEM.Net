// HeightMapExtensions.cs
//
// Author:
//       Xavier Fischer 
//
// Copyright (c) 2019 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using DEM.Net.Core.Gpx;
using SixLabors.ImageSharp.ColorSpaces;

namespace DEM.Net.Core;

/// <summary>
/// Various extensions methods for height maps
/// </summary>
public static class HeightMapExtensions
{
    /// <summary>
    /// Centers height map on origin
    /// </summary>
    /// <param name="heightMap"></param>
    /// <returns></returns>
    /// <remarks>Coordinates are changed in place</remarks>
    public static HeightMap CenterOnOrigin(this HeightMap heightMap, out Matrix4x4 transform)
    {
        //Logger.Info("CenterOnOrigin...");
        var bbox = heightMap.BoundingBox;

        double xOriginOffset = bbox.xMax - (bbox.xMax - bbox.xMin) / 2d;
        double yOriginOffset = bbox.yMax - (bbox.yMax - bbox.yMin) / 2d;
        double zOriginOffset = bbox.zMax - (bbox.zMax - bbox.zMin) / 2d;
        TranslateInPlace(heightMap.Coordinates, -xOriginOffset, -yOriginOffset, -zOriginOffset);

        // world to model is (x,y,z) -> (x,z,-y)
        // translate (X,Y,0) -> (X,0,-Y)
        transform = System.Numerics.Matrix4x4.CreateTranslation(-(float)xOriginOffset, 0, (float)yOriginOffset);

        heightMap.BoundingBox = new BoundingBox(bbox.xMin - xOriginOffset, bbox.xMax - xOriginOffset
                                                , bbox.yMin - yOriginOffset, bbox.yMax - yOriginOffset
                                                , bbox.zMin - zOriginOffset, bbox.zMax - zOriginOffset);
        return heightMap;
    }
    public static HeightMap CenterOnOrigin(this HeightMap heightMap, GeoPoint origin)
    {
        //Logger.Info("CenterOnOrigin...");
        var bbox = heightMap.BoundingBox;

        TranslateInPlace(heightMap.Coordinates, -origin.Longitude, -origin.Latitude, -origin.Elevation ?? 0);



        heightMap.BoundingBox = new BoundingBox(bbox.xMin - origin.Longitude, bbox.xMax - origin.Longitude
                                                , bbox.yMin - origin.Latitude, bbox.yMax - origin.Latitude
                                                , bbox.zMin - origin.Elevation ?? 0, bbox.zMax - origin.Elevation ?? 0);
        return heightMap;
    }
    /// <summary>
    /// Centers height map on origin
    /// </summary>
    /// <param name="heightMap"></param>
    /// <param name="centerOnZ"></param>
    /// <returns></returns>
    /// <remarks>Coordinates are changed in place</remarks>
    public static HeightMap CenterOnOrigin(this HeightMap heightMap, bool centerOnZ = false)
    {
        //Logger.Info("CenterOnOrigin...");
        var bbox = heightMap.BoundingBox;

        double xOriginOffset = bbox.xMax - (bbox.xMax - bbox.xMin) / 2d;
        double yOriginOffset = bbox.yMax - (bbox.yMax - bbox.yMin) / 2d;
        TranslateInPlace(heightMap.Coordinates, -xOriginOffset, -yOriginOffset, centerOnZ ? -bbox.zMin : 0);

        heightMap.BoundingBox = new BoundingBox(bbox.xMin - xOriginOffset, bbox.xMax - xOriginOffset
                                                , bbox.yMin - yOriginOffset, bbox.yMax - yOriginOffset
                                                , 0, bbox.zMax - bbox.zMin);
        return heightMap;
    }
    public static BoundingBox CenterOnOrigin(this BoundingBox bbox, bool centerOnZ = false)
    {
        double xOriginOffset = bbox.xMax - (bbox.xMax - bbox.xMin) / 2d;
        double yOriginOffset = bbox.yMax - (bbox.yMax - bbox.yMin) / 2d;
        return new BoundingBox(bbox.xMin - xOriginOffset, bbox.xMax - xOriginOffset
                                                 , bbox.yMin - yOriginOffset, bbox.yMax - yOriginOffset
                                                 , 0, bbox.zMax - bbox.zMin);

    }

    public static HeightMap CenterOnOrigin(this HeightMap heightMap, BoundingBox bbox, bool centerOnZ = false)
    {
        //Logger.Info("CenterOnOrigin...");

        double xOriginOffset = bbox.xMax - (bbox.xMax - bbox.xMin) / 2d;
        double yOriginOffset = bbox.yMax - (bbox.yMax - bbox.yMin) / 2d;
        TranslateInPlace(heightMap.Coordinates, -xOriginOffset, -yOriginOffset, centerOnZ ? -bbox.zMin : 0);

        heightMap.BoundingBox = new BoundingBox(bbox.xMin - xOriginOffset, bbox.xMax - xOriginOffset
                                                , bbox.yMin - yOriginOffset, bbox.yMax - yOriginOffset
                                                , 0, bbox.zMax - bbox.zMin)
        { SRID = bbox.SRID };
        return heightMap;
    }

    /// <summary>
    /// Centers a set of points on origin, when their bbox is not known
    /// </summary>
    /// <param name="points"></param>
    /// <returns></returns>
    /// <remarks>Warning : The bounding box will be computed (points will be enumerated). Prefer the <see cref="CenterOnOrigin(IEnumerable{GeoPoint}, BoundingBox)"/> method when the bbox is known
    /// </remarks>
    public static IEnumerable<GeoPoint> CenterOnOrigin(this IEnumerable<GeoPoint> points)
    {
        //Logger.Info("CenterOnOrigin...");
        var bbox = points.GetBoundingBox();

        return points.CenterOnOrigin(bbox);
    }
    public static IEnumerable<GeoPoint> CenterOnOrigin(this IEnumerable<GeoPoint> points, GeoPoint origin)
    {
        return points.Translate(-origin.Longitude, -origin.Latitude, -origin.Elevation ?? 0);
    }

    /// <summary>
    /// Centers a set of points on origin, when their bbox is known
    /// </summary>
    /// <param name="points"></param>
    /// <param name="bbox"></param>
    /// <returns></returns>
    public static IEnumerable<GeoPoint> CenterOnOrigin(this IEnumerable<GeoPoint> points, BoundingBox bbox, bool centerOnZ = false)
    {
        //Logger.Info("CenterOnOrigin...");
        double xOriginOffset = bbox.xMax - (bbox.xMax - bbox.xMin) / 2d;
        double yOriginOffset = bbox.yMax - (bbox.yMax - bbox.yMin) / 2d;
        //double zOriginOffset = bbox.zMax - (bbox.zMax - bbox.zMin) / 2d;
        points = points.Translate(-xOriginOffset, -yOriginOffset, centerOnZ ? -bbox.zMin : 0); // Set minZ = 0

        return points;
    }

    public static GeoPoint CenterOnOrigin(this GeoPoint point, BoundingBox bbox, bool centerOnZ = false)
    {
        //Logger.Info("CenterOnOrigin...");
        double xOriginOffset = bbox.xMax - (bbox.xMax - bbox.xMin) / 2d;
        double yOriginOffset = bbox.yMax - (bbox.yMax - bbox.yMin) / 2d;
        //double zOriginOffset = bbox.zMax - (bbox.zMax - bbox.zMin) / 2d;
        point = point.Translate(-xOriginOffset, -yOriginOffset, centerOnZ ? -bbox.zMin : 0); // Set minZ = 0

        return point;
    }
    /// <summary>
    /// Translate points
    /// </summary>
    /// <param name="points"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public static IEnumerable<GeoPoint> Translate(this IEnumerable<GeoPoint> points, double x, double y, double z = 0)
    {
        //Logger.Info("Translate...");
        foreach (var pt in points)
        {
            yield return pt.Translate(x, y, z);
        }
        //Logger.Info("Translate done...");
    }
    public static GeoPoint Translate(this GeoPoint point, double x, double y, double z = 0)
    {
        //Logger.Info("Translate...");

        return point with { Latitude = point.Latitude + y, Longitude = point.Longitude + x, Elevation = point.Elevation + z };

        //Logger.Info("Translate done...");
    }

    private static void TranslateInPlace(Span<GeoPoint> points, double x, double y, double z)
    {
        foreach (ref var pt in points)
            pt = pt.Translate(x, y, z);
    }

    private static void ScaleInPlace(Span<GeoPoint> points, float x, float y, float z)
    {
        foreach (ref var pt in points)
            pt = pt.Scale(x, y, z);
    }

    /// <summary>
    /// Before v11: turned the lazy coordinates into a list, they are an array now
    /// </summary>
    /// <returns></returns>
    [Obsolete("Height map coordinates are an array since v11, the transforms change them in place: there is nothing to bake.")]
    public static HeightMap BakeCoordinates(this HeightMap heightMap) => heightMap;

    /// <summary>
    /// Scale height map verticaly for relief exageration
    /// </summary>
    /// <param name="heightMap"></param>
    /// <param name="zFactor"></param>
    /// <returns></returns>
    public static HeightMap ZScale(this HeightMap heightMap, float zFactor = 1f)
    {
        ScaleInPlace(heightMap.Coordinates, 1f, 1f, zFactor);
        heightMap.Minimum *= zFactor;
        heightMap.Maximum *= zFactor;

        return heightMap;
    }

    public static HeightMap ZClamp(this HeightMap heightMap, float? min, float? max)
    {
        foreach (ref var pt in heightMap.Coordinates.AsSpan())
            pt = pt with { Elevation = MathHelper.Clamp(pt.Elevation ?? 0, min ?? float.MinValue, max ?? float.MaxValue) };
        heightMap.Minimum = MathHelper.Clamp(heightMap.Minimum, min ?? float.MinValue, max ?? float.MaxValue);
        heightMap.Maximum = MathHelper.Clamp(heightMap.Maximum, min ?? float.MinValue, max ?? float.MaxValue);

        return heightMap;
    }

    /// <summary>
    /// Scale height map
    /// </summary>
    /// <param name="heightMap"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    /// <remarks>Bounding box will be updated accordingly</remarks>
    public static HeightMap Scale(this HeightMap heightMap, float x = 1f, float y = 1f, float z = 1f)
    {
        var bbox = heightMap.BoundingBox; // before the points are scaled, when it is computed from them
        ScaleInPlace(heightMap.Coordinates, x, y, z);
        heightMap.BoundingBox = bbox.Scale(x, y, z);
        heightMap.Minimum *= z;
        heightMap.Maximum *= z;

        return heightMap;
    }

    /// <summary>
    /// Rescale the height map in order that it fits into the specified size
    /// </summary>
    /// <param name="heightMap"></param>
    /// <param name="maxSize"></param>
    /// <returns></returns>
    public static HeightMap FitInto(this HeightMap heightMap, float maxSize)
    {
        return FitInto(heightMap, maxSize, out float scale);
    }
    public static HeightMap FitInto(this HeightMap heightMap, float maxSize, out float scale)
    {
        scale = 1f;
        if (heightMap.BoundingBox.Width > heightMap.BoundingBox.Height)
        {
            scale = (float)(maxSize / heightMap.BoundingBox.Width);
        }
        else
        {
            scale = (float)(maxSize / heightMap.BoundingBox.Height);
        }
        ScaleInPlace(heightMap.Coordinates, scale, scale, scale);
        heightMap.BoundingBox = heightMap.BoundingBox.ScaleAbsolute(scale, scale, scale);
        return heightMap;
    }
    /// <summary>
    /// Scales elevation of give points
    /// </summary>
    /// <param name="points"></param>
    /// <param name="zFactor"></param>
    /// <returns></returns>
    public static IEnumerable<GeoPoint> ZScale(this IEnumerable<GeoPoint> points, float zFactor = 1f)
    {
        return points.Scale(1, 1, zFactor);
    }
    public static IEnumerable<GeoPoint> ZClamp(this IEnumerable<GeoPoint> points, float? min, float? max)
    {
        return points.Select(pt => pt with { Elevation = MathHelper.Clamp(pt.Elevation ?? 0, min ?? float.MinValue, max ?? float.MaxValue) });
    }
    
    public static List<Gpx.GpxTrackPoint> ZScale(this List<Gpx.GpxTrackPoint> points, float zFactor = 1f)
    {
        points.ForEach(p => p.Elevation *= zFactor);
        return points;
    }
    /// <summary>
    /// Scale given points
    /// </summary>
    /// <param name="points"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public static IEnumerable<GeoPoint> Scale(this IEnumerable<GeoPoint> points, float x = 1f, float y = 1f, float z = 1f)
    {
        //Logger.Info("Scale...");
        foreach (var pt in points)
        {
            yield return pt.Scale(x, y, z);
        }
        //Logger.Info("Scale done...");

    }/// <summary>
     /// Scale given points
     /// </summary>
     /// <param name="points"></param>
     /// <param name="x"></param>
     /// <param name="y"></param>
     /// <param name="z"></param>
     /// <returns></returns>
    public static GeoPoint Scale(this GeoPoint pt, float x = 1f, float y = 1f, float z = 1f)
    {
        return pt with { Longitude = pt.Longitude * x, Latitude = pt.Latitude * y, Elevation = pt.Elevation * z };
    }

    /// <summary>
    /// Verticaly translates a height map
    /// </summary>
    /// <param name="heightMap"></param>
    /// <param name="distance"></param>
    /// <returns></returns>
    public static HeightMap ZTranslate(this HeightMap heightMap, float distance)
    {
        foreach (ref var pt in heightMap.Coordinates.AsSpan())
            pt = pt with { Elevation = (pt.Elevation ?? 0) + distance };
        heightMap.Minimum += distance;
        heightMap.Maximum += distance;

        return heightMap;
    }
    public static HeightMap Translate(this HeightMap heightMap, GeoPoint pt)
    {
        TranslateInPlace(heightMap.Coordinates, pt.Longitude, pt.Latitude, pt.Elevation ?? 0);
        heightMap.Minimum += (float)(pt.Elevation ?? 0);
        heightMap.Maximum += (float)(pt.Elevation ?? 0);

        return heightMap;
    }

    /// <summary>
    /// Verticaly translates points
    /// </summary>
    /// <param name="points"></param>
    /// <param name="distance"></param>
    /// <returns></returns>
    public static IEnumerable<GeoPoint> ZTranslate(this IEnumerable<GeoPoint> points, float distance)
    {
        //Logger.Info("ZTranslate...");
        foreach (var pt in points)
        {
            yield return pt with { Elevation = (pt.Elevation ?? 0) + distance };
        }
        //Logger.Info("ZTranslate done...");

    }
    public static IEnumerable<GeoPoint> Translate(this IEnumerable<GeoPoint> points, GeoPoint vector)
    {
        foreach (var pt in points)
        {
            yield return pt with { Latitude = pt.Latitude + vector.Latitude, Longitude = pt.Longitude + vector.Longitude, Elevation = pt.Elevation + (vector.Elevation ?? 0) };
        }
    }


    /// <summary>
    /// Sort height map coordinates (lat descending, then lon)
    /// </summary>
    /// <param name="heightMap"></param>
    /// <returns></returns>
    public static HeightMap Sort(this HeightMap heightMap)
    {
        heightMap.Coordinates = heightMap.Coordinates.Sort().ToArray();

        return heightMap;
    }
    /// <summary>
    /// Sort coordinates (lat descending, then lon)
    /// </summary>
    /// <param name="coords"></param>
    /// <returns></returns>
    public static IEnumerable<GeoPoint> Sort(this IEnumerable<GeoPoint> coords)
    {
        // Rows from adjacent tiles can differ by floating point noise (e.g. AW3D30 tie points like 46.00000000000003):
        // compare latitudes with a tolerance so a row is never split and reordered east-then-west.
        coords = coords.OrderByDescending(pt => RoundCoordinate(pt.Latitude))
                       .ThenBy(pt => pt.Longitude);

        return coords;
    }

    /// <summary>
    /// Rounds a coordinate to absorb floating point noise between tiles (1e-9 ≈ 0.1 mm in degrees), far below any pixel size.
    /// </summary>
    internal static double RoundCoordinate(double value) => Math.Round(value, 9);

    /// <summary>
    /// Merges per-tile height maps into a single grid, in O(n) and without sorting.
    /// Each tile must be a row-major grid (north to south, west to east), as returned by the raster readers.
    /// Tiles are laid out as rows (by northern edge) of columns (by western edge); rows or columns that
    /// repeat the neighbour's edge (overlapping grid-registered tiles) are skipped.
    /// </summary>
    /// <exception cref="InvalidOperationException">Tiles do not form a complete rectangular grid.</exception>
    public static HeightMap MergeTiles(this IEnumerable<HeightMap> tiles)
    {
        var grids = tiles.Select(t => new TileGrid(t)).ToList();
        if (grids.Count == 0) throw new ArgumentException("No height map to merge.", nameof(tiles));
        if (grids.Count == 1) return grids[0].HeightMap;

        List<TileGrid[]> tileRows = grids
            .GroupBy(g => RoundCoordinate(g.North))
            .OrderByDescending(g => g.Key)
            .Select(g => g.OrderBy(t => t.West).ToArray())
            .ToList();

        // Skip overlapping columns (west edge repeating the previous tile's east edge) and rows (north edge repeating the row above)
        for (int r = 0; r < tileRows.Count; r++)
        {
            var row = tileRows[r];
            for (int c = 0; c < row.Length; c++)
            {
                if (c > 0) row[c].SkipColumnsUpTo(row[c - 1].East);
                if (r > 0) row[c].SkipRowsDownTo(tileRows[r - 1][0].South);
            }
        }

        int width = tileRows[0].Sum(t => t.Columns);
        int height = tileRows.Sum(row => row[0].Rows);
        foreach (var row in tileRows)
        {
            if (row.Any(t => t.Rows != row[0].Rows) || row.Sum(t => t.Columns) != width)
            {
                throw new InvalidOperationException("Cannot merge height maps, tiles do not form a rectangular grid: "
                    + string.Join(" | ", grids.Select(t => $"{t.HeightMap.Width}x{t.HeightMap.Height} [{t.West}, {t.South}, {t.East}, {t.North}]")));
            }
        }

        var coords = new GeoPoint[width * height];
        int coordIndex = 0;
        foreach (var row in tileRows)
        {
            for (int y = row[0].FirstRow; y < row[0].FirstRow + row[0].Rows; y++)
            {
                foreach (var tile in row)
                {
                    int start = y * tile.HeightMap.Width + tile.FirstColumn;
                    for (int x = 0; x < tile.Columns; x++)
                        coords[coordIndex++] = tile.Points[start + x];
                }
            }
        }

        var all = grids.Select(g => g.HeightMap).ToList();
        return new HeightMap(width, height)
        {
            Coordinates = coords,
            Count = coords.Length,
            Minimum = all.Min(h => h.Minimum),
            Maximum = all.Max(h => h.Maximum),
            BoundingBox = new BoundingBox(xmin: all.Min(h => h.BoundingBox.xMin), xmax: all.Max(h => h.BoundingBox.xMax),
                                          ymin: all.Min(h => h.BoundingBox.yMin), ymax: all.Max(h => h.BoundingBox.yMax),
                                          zmin: all.Min(h => h.BoundingBox.zMin), zmax: all.Max(h => h.BoundingBox.zMax))
            { SRID = all[0].BoundingBox.SRID },
        };
    }

    private sealed class TileGrid
    {
        public TileGrid(HeightMap heightMap)
        {
            HeightMap = heightMap;
            Points = heightMap.Coordinates;
            if (Points.Length != heightMap.Width * heightMap.Height)
                throw new InvalidOperationException($"Height map has {Points.Length} points, expected {heightMap.Width}x{heightMap.Height}.");
            Columns = heightMap.Width;
            Rows = heightMap.Height;
        }

        public HeightMap HeightMap { get; }
        public GeoPoint[] Points { get; }
        public int FirstColumn { get; private set; }
        public int FirstRow { get; private set; }
        public int Columns { get; private set; }
        public int Rows { get; private set; }

        public double North => Points[FirstRow * HeightMap.Width].Latitude;
        public double South => Points[(HeightMap.Height - 1) * HeightMap.Width].Latitude;
        public double West => Points[FirstColumn].Longitude;
        public double East => Points[HeightMap.Width - 1].Longitude;

        // half a pixel: positions closer than that to the neighbour's edge are the same sample (1e-9 for single pixel tiles)
        private double HalfStepX => HeightMap.Width > 1 ? Math.Abs(Points[1].Longitude - Points[0].Longitude) / 2 : 1e-9;
        private double HalfStepY => HeightMap.Height > 1 ? Math.Abs(Points[0].Latitude - Points[HeightMap.Width].Latitude) / 2 : 1e-9;

        public void SkipColumnsUpTo(double previousEast)
        {
            while (Columns > 0 && Points[FirstColumn].Longitude < previousEast + HalfStepX)
            {
                FirstColumn++;
                Columns--;
            }
        }

        public void SkipRowsDownTo(double previousSouth)
        {
            while (Rows > 0 && Points[FirstRow * HeightMap.Width].Latitude > previousSouth - HalfStepY)
            {
                FirstRow++;
                Rows--;
            }
        }
    }

    /// <summary>
    /// Downsample a given height map
    /// </summary>
    /// <param name="heightMap"></param>
    /// <param name="step">Must be a factor of two</param>
    /// <returns></returns>
    public static HeightMap Downsample(this HeightMap heightMap, int step)
    {
        if (step == 0 || step % 2 != 0)
            throw new ArgumentOutOfRangeException("step", "Step must be a factor of 2");

        HeightMap hMap = new HeightMap(heightMap.Width / step, heightMap.Height / step)
        {
            Maximum = heightMap.Maximum,
            Minimum = heightMap.Minimum,
            BoundingBox = heightMap.BoundingBox,
            Coordinates = DownsampleCoordinates(heightMap.Coordinates, heightMap.Width, heightMap.Height, step).ToArray()
        };

        return hMap;
    }

    private static IEnumerable<GeoPoint> DownsampleCoordinates(GeoPoint[] input, int w, int h, int step)
    {
        for (int lat = 0; lat <= h; lat += step)
        {
            for (int lon = 0; lon <= w; lon += step)
            {
                yield return input[lon + lat * h];
            }
        }
    }

    public static IEnumerable<(GeoPoint point, List<GeoPoint> source)> WarpHeightMap(IEnumerable<GeoPoint> coords, int sourceWidth, int sourceHeight, int destWidth, int destHeight)
    {
        // But parcourir chaque pixel de l'image source (coords)
        // attribuer aux pixels de destination la valeur interpolée

        // Algo : Add weighted elevation destination points

        int numPoints = destWidth * destHeight;
        var enume = coords.GetEnumerator();
        double radius = 1.4;
        List<List<(double Elevation, double Weight, GeoPoint source)>> outPoints = Enumerable.Range(1, numPoints).Select(_ => new List<(double elevation, double weigh, GeoPoint sourcet)>()).ToList();
        for (int y = 0; y < sourceHeight; y++)
        {
            for (int x = 0; x < sourceWidth; x++)
            {
                // where in dest ?
                enume.MoveNext();
                GeoPoint sourcePt = enume.Current;// coords[x + y * sourceWidth];
                // Warning : sourcePt is GridRegistered. It sits at the center of a 4 pixel grid.
                // Value must be spread across those pixels

                double sourceZ = sourcePt.Elevation ?? 0;
                double sourceX = sourcePt.Longitude - 0.5;
                double sourceY = sourcePt.Latitude - 0.5;

                int xWest = (int)Math.Floor(sourceX);
                int yNorth = (int)Math.Floor(sourceY);
                int xEast = xWest+1;
                int ySouth = yNorth+1;

                AddData(sourceZ, xWest, yNorth, sourceX, sourceY, radius, destWidth, destHeight, sourcePt);
                AddData(sourceZ, xEast, yNorth, sourceX, sourceY, radius, destWidth, destHeight, sourcePt);
                AddData(sourceZ, xEast, ySouth, sourceX, sourceY, radius, destWidth, destHeight, sourcePt);
                AddData(sourceZ, xWest, ySouth, sourceX, sourceY, radius, destWidth, destHeight, sourcePt);
            }
        }

        for (int y = 0; y < destHeight; y++)
        {
            for (int x = 0; x < destWidth; x++)
            {
                var pts = outPoints[x + y * destWidth];
                (GeoPoint point, List<GeoPoint> source) outPt =  (new GeoPoint(y, x, 0), new List<GeoPoint>());
                if (pts.Count > 0)
                {
                    double outVal = 0;
                    double weights = 0;
                    foreach (var pt in pts)
                    {
                        outVal += pt.Elevation * pt.Weight;
                        weights += pt.Weight;
                        outPt.source.Add(pt.source);
                    }
                    outPt.point = outPt.point with { Elevation = outVal / weights };
                }

                yield return outPt;
            }

        }

        void AddData(double elevation, int x, int y, double x1, double y1, double radius, int width, int height, GeoPoint sourcePt)
        {
            if (x < width && y < height && x >= 0 && y >= 0)
            {
                int index = x + y * width;

                var w = (1-Math.Abs(x1-x) + 1-Math.Abs(y1-y)) / 2d;
                //var w = WeightedDistance(x1, x, y1, y, radius);
                outPoints[index].Add((elevation, w, sourcePt));
            }
        }
        //double WeightedDistance(double x1, double x2, double y1, double y2, double radius)
        //    => (radius - Distance(x1, x2, y1, y2))/radius;
        //double Distance(double x1, double x2, double y1, double y2)
        //    => Math.Sqrt((x2 - x1) * (x2 - x1)+ (y2 - y1) * (y2 - y1));
    }
    public static IEnumerable<GeoPoint> WarpHeightMapOld(IReadOnlyList<GeoPoint> coords, int sourceWidth, int sourceHeight, int destWidth, int destHeight)
    {
        // But parcourir chaque pixel de l'image source (coords)
        // attribuer aux pixels de destination la valeur interpolée

        int numPoints = destWidth * destHeight;
        List<List<double>> outPoints = Enumerable.Range(1, numPoints).Select(_ => new List<double>()).ToList();
        var enume = coords.GetEnumerator();
        double lastXz = 0, lastYz = 0;
        for (int y = 0; y < sourceHeight; y++)
        {
            for (int x = 0; x < sourceWidth; x++)
            {
                enume.MoveNext();
                GeoPoint sourcePt = enume.Current;// coords[x + y * sourceWidth];

                if (lastXz == 0 && lastYz == 0)
                {
                    lastXz = sourcePt.Elevation ?? 0;
                    lastYz = sourcePt.Elevation ?? 0;
                }

                double sourceX = sourcePt.Longitude;
                double sourceY = sourcePt.Latitude;
                int outXmin = (int)Math.Floor(sourceX);
                int outYmin = (int)Math.Floor(sourceY);
                int outXmax = (int)Math.Ceiling(sourceX);
                int outYmax = (int)Math.Ceiling(sourceY);

                double currentZ = sourcePt.Elevation ?? 0;
                int slopeX = Math.Sign(currentZ - lastXz);
                int slopeY = Math.Sign(currentZ - lastYz);

                int indexX0Y0 = outXmin + outYmin * destWidth;
                int indexX1Y0 = outXmax + outYmin * destWidth;
                int indexX0Y1 = outXmin + outYmax * destWidth;
                int indexX1Y1 = outXmax + outYmax * destWidth;

                if (indexX0Y0<0 ||indexX0Y1<0)
                {
                    // should not happen
                }
                else
                {
                    if (indexX0Y0 < numPoints && outXmin < destWidth)
                    {
                        outPoints[indexX0Y0].Add(slopeX > 0
                            ? MathHelper.Map(0, 1, lastXz, currentZ, sourceX - outXmin, true)
                            : MathHelper.Map(0, 1, lastXz, currentZ, 1 - (sourceX - outXmin), true));
                    }
                    if (indexX1Y0 < numPoints && outXmax < destWidth)
                    {
                        outPoints[indexX0Y0].Add(slopeX < 0
                            ? MathHelper.Map(0, 1, lastXz, currentZ, sourceX - outXmin, true)
                            : MathHelper.Map(0, 1, lastXz, currentZ, 1 - (sourceX - outXmin), true));
                    }
                    if (indexX0Y1 < numPoints && outYmin < destHeight)
                    {
                        outPoints[indexX0Y1].Add(slopeY > 0
                            ? MathHelper.Map(0, 1, lastYz, currentZ, sourceY - outYmin, true)
                            : MathHelper.Map(0, 1, lastYz, currentZ, 1 - (sourceY - outYmin), true));
                    }
                }


                lastXz = sourcePt.Elevation ?? 0;
                lastYz = lastXz;
            }
        }

        for (int y = 0; y < destHeight; y++)
        {
            for (int x = 0; x < destWidth; x++)
            {
                var pts = outPoints[x + y * destWidth];
                GeoPoint outPt = new GeoPoint(y, x, 0);
                if (pts.Count > 0)
                    outPt = outPt with { Elevation = pts.Average() };// (pts.Max() - pts.Min())/2+pts.Min();
                //outPt.Elevation = pts.Average();

                yield return outPt;
            }

        }
    }
}
