using DEM.Net.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace DEM.Net.Test
{
    public class HeightMapMergeTests
    {
        private const double Step = 1 / 3600d;
        private readonly ITestOutputHelper _output;

        public HeightMapMergeTests(ITestOutputHelper output) => _output = output;

        // Elevation encodes the global (row, col) so any misplaced sample is detected
        private static float Id(int row, int col) => row * 10000 + col;

        /// <summary>Row-major tile (north to south, west to east) starting at global pixel (row0, col0).</summary>
        private static HeightMap Tile(int row0, int col0, int rows, int cols, double noise = 0)
        {
            var coords = new List<GeoPoint>(rows * cols);
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    coords.Add(new GeoPoint(46 - (row0 + r) * Step + noise, 5 + (col0 + c) * Step + noise, Id(row0 + r, col0 + c)));

            return new HeightMap(cols, rows)
            {
                Coordinates = coords,
                Count = coords.Count,
                Minimum = coords.Min(p => (float)p.Elevation.Value),
                Maximum = coords.Max(p => (float)p.Elevation.Value),
                BoundingBox = new BoundingBox(coords.Min(p => p.Longitude), coords.Max(p => p.Longitude),
                                              coords.Min(p => p.Latitude), coords.Max(p => p.Latitude),
                                              coords.Min(p => p.Elevation.Value), coords.Max(p => p.Elevation.Value)),
            };
        }

        private static void AssertGrid(HeightMap merged, int rows, int cols)
        {
            Assert.Equal(cols, merged.Width);
            Assert.Equal(rows, merged.Height);
            var points = merged.Coordinates.ToList();
            Assert.Equal(rows * cols, points.Count);
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    Assert.Equal(Id(r, c), points[r * cols + c].Elevation.Value);
        }

        [Fact]
        public void TwoByTwoTilesWithoutOverlap()
        {
            var tiles = new[] { Tile(0, 0, 3, 4), Tile(0, 4, 3, 5), Tile(3, 0, 2, 4), Tile(3, 4, 2, 5) };
            AssertGrid(tiles.MergeTiles(), rows: 5, cols: 9);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public void OverlappingSeamsAreDeduplicated(int overlap)
        {
            // neighbours repeat the last `overlap` rows / columns of the previous tile
            var tiles = new[] { Tile(0, 0, 4, 5), Tile(0, 5 - overlap, 4, 6), Tile(4 - overlap, 0, 3, 5), Tile(4 - overlap, 5 - overlap, 3, 6) };
            AssertGrid(tiles.MergeTiles(), rows: 7 - overlap, cols: 11 - overlap);
        }

        [Fact]
        public void TileOrderDoesNotMatter()
        {
            var tiles = new[] { Tile(3, 4, 2, 5), Tile(0, 0, 3, 4), Tile(3, 0, 2, 4), Tile(0, 4, 3, 5) };
            AssertGrid(tiles.MergeTiles(), rows: 5, cols: 9);
        }

        [Fact]
        public void FloatingPointNoiseBetweenTilesIsTolerated()
        {
            // like AW3D30 tie points (46.00000000000003)
            var tiles = new[] { Tile(0, 0, 3, 4, noise: 3e-14), Tile(0, 4, 3, 5, noise: -3e-14) };
            AssertGrid(tiles.MergeTiles(), rows: 3, cols: 9);
        }

        [Fact]
        public void TilesNotFormingARectangleThrow()
        {
            // second tile of the row is one row shorter
            var tiles = new[] { Tile(0, 0, 3, 4), Tile(0, 4, 2, 5) };
            Assert.Throws<InvalidOperationException>(() => tiles.MergeTiles());
        }

        [Fact]
        public void SameResultAsDistinctAndSortForOverlappingTiles()
        {
            // former TINItaly merge
            var tiles = new[] { Tile(0, 0, 40, 50), Tile(0, 49, 40, 60), Tile(39, 0, 30, 50), Tile(39, 49, 30, 60) };
            var legacy = tiles.SelectMany(t => t.Coordinates).Distinct().Sort().ToList();
            var merged = tiles.MergeTiles().Coordinates.ToList();
            Assert.Equal(legacy.Select(p => (p.Latitude, p.Longitude, p.Elevation)), merged.Select(p => (p.Latitude, p.Longitude, p.Elevation)));
        }

        [Fact]
        public void MergeIsFasterThanSorting()
        {
            // 4 tiles of 1000x1000 (a 2000x2000 terrain)
            var tiles = new[] { Tile(0, 0, 1000, 1000), Tile(0, 1000, 1000, 1000), Tile(1000, 0, 1000, 1000), Tile(1000, 1000, 1000, 1000) };

            var sw = Stopwatch.StartNew();
            var legacy = tiles.SelectMany(t => t.Coordinates).Sort().ToList();
            long legacyMs = sw.ElapsedMilliseconds;

            sw.Restart();
            var merged = tiles.MergeTiles().Coordinates.ToList();
            long mergeMs = sw.ElapsedMilliseconds;

            _output.WriteLine($"4M points: sort {legacyMs} ms (and again on every enumeration, the result was lazy), stitch {mergeMs} ms");
            Assert.Equal(legacy.Count, merged.Count);
            Assert.True(mergeMs < legacyMs);
        }
    }
}
