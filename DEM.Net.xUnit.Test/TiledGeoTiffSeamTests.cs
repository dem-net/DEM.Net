using DEM.Net.Core;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace DEM.Net.Test
{
    /// <summary>
    /// Height maps built across tile seams must be regular grids sampled at the true pixel centers,
    /// whatever the dataset georeferencing (PixelIsArea / PixelIsPoint, overlapping or not).
    /// All these datasets are tiled GeoTIFFs. Downloads missing tiles on first run.
    /// </summary>
    public class TiledGeoTiffSeamTests : IClassFixture<DemNetFixture>
    {
        private readonly ElevationService _elevationService;
        private readonly IServiceProvider _services;
        private readonly ITestOutputHelper _output;

        public TiledGeoTiffSeamTests(DemNetFixture fixture, ITestOutputHelper output)
        {
            _services = fixture.ServiceProvider;
            _elevationService = fixture.ServiceProvider.GetService<ElevationService>();
            _output = output;
        }

        public static IEnumerable<object[]> Cases => new List<object[]>
        {
            // dataset, xMin, xMax, yMin, yMax, pixel size, pixel centers offset from tile origin (0.5 = PixelIsArea, 0 = PixelIsPoint)
            new object[] { nameof(DEMDataSet.AW3D30), 5.95, 6.05, 45.40, 45.50, 1 / 3600d, 0.5 },     // E-W seam at 6°
            new object[] { nameof(DEMDataSet.SRTM_GL3), 5.95, 6.05, 45.40, 45.50, 1 / 1200d, 0.0 },   // E-W seam at 6°
            new object[] { nameof(DEMDataSet.NASADEM), 5.95, 6.05, 45.40, 45.50, 1 / 3600d, 0.0 },    // E-W seam at 6°
            new object[] { nameof(DEMDataSet.FABDEM), 8.223893, 8.35273, 49.964561, 50.042325, 1 / 3600d, 0.0 }, // N-S seam at 50° (PixelIsPoint, no overlap)
            new object[] { nameof(DEMDataSet.swissALTI3D2m), 2616501, 2617499, 1090501, 1091499, 2d, 0.5 },     // both seams (LV95)
            new object[] { nameof(DEMDataSet.SRTM_GL3), 1.95, 2.05, 44.95, 45.05, 1 / 1200d, 0.0 },   // 2x2 tiles corner at 45°N 2°E
            new object[] { nameof(DEMDataSet.SRTM_GL3), -25.05, -24.95, 36.95, 37.05, 1 / 1200d, 0.0 }, // Azores: N36W025 is ocean => virtual tile
            new object[] { nameof(DEMDataSet.FABDEM), -6.05, -5.95, 48.40, 48.50, 1 / 3600d, 0.0 },   // Ouessant: N48W007 is ocean => virtual tile
        };

        [Theory]
        [MemberData(nameof(Cases))]
        public void HeightMapAcrossTileSeamIsARegularGridOfPixelCenters(string datasetName, double xMin, double xMax, double yMin, double yMax, double pixelSize, double centerOffset)
        {
            var dataset = (DEMDataSet)typeof(DEMDataSet).GetProperty(datasetName).GetValue(null);
            var bbox = new BoundingBox(xMin, xMax, yMin, yMax) { SRID = dataset.SRID };

            HeightMap heightMap = _elevationService.GetHeightMap(ref bbox, dataset);
            var points = heightMap.Coordinates.ToList();

            // column / row index of each coordinate, in pixels from the global origin
            var cols = points.Select(p => p.Longitude / pixelSize - centerOffset).ToList();
            var rows = points.Select(p => p.Latitude / pixelSize - centerOffset).ToList();

            var distinctCols = cols.Select(c => Math.Round(c)).Distinct().OrderBy(c => c).ToList();
            var distinctRows = rows.Select(r => Math.Round(r)).Distinct().OrderBy(r => r).ToList();
            double worstCenterError = cols.Concat(rows).Max(v => Math.Abs(v - Math.Round(v)));
            int colGaps = distinctCols.Zip(distinctCols.Skip(1), (a, b) => b - a).Count(d => d != 1);
            int rowGaps = distinctRows.Zip(distinctRows.Skip(1), (a, b) => b - a).Count(d => d != 1);
            int duplicates = points.Count - points.Select(p => (Math.Round(p.Longitude / pixelSize, 3), Math.Round(p.Latitude / pixelSize, 3))).Distinct().Count();

            _output.WriteLine($"{datasetName}: {heightMap.Width}x{heightMap.Height}, {points.Count} points, {distinctCols.Count} cols, {distinctRows.Count} rows, " +
                $"col gaps {colGaps}, row gaps {rowGaps}, duplicates {duplicates}, worst center error {worstCenterError:F3} px");

            Assert.Equal(heightMap.Width * heightMap.Height, points.Count);
            Assert.Equal(0, duplicates);
            Assert.Equal(0, colGaps);
            Assert.Equal(0, rowGaps);
            Assert.Equal(heightMap.Width, distinctCols.Count);
            Assert.Equal(heightMap.Height, distinctRows.Count);
            Assert.True(worstCenterError < 1e-3, $"Coordinates are {worstCenterError:F3} px away from the pixel centers");
        }

        /// <summary>
        /// FABDEM terrain across the 50°N tile seam, decimated like the Web API does (was failing with
        /// "SmallListSet.MoveTo: list at ... is empty" when tiles were missing).
        /// </summary>
        [Fact]
        public void FabdemTerrainAcrossTileSeamCanBeDecimated()
        {
            var gltf = _services.GetService<DEM.Net.glTF.SharpglTF.SharpGltfService>();
            var dataset = DEMDataSet.FABDEM;
            var bbox = new BoundingBox(8.223893, 8.35273, 49.964561, 50.042325) { SRID = dataset.SRID };

            var heightMap = _elevationService.GetHeightMap(ref bbox, dataset)
                .ReprojectGeodeticToCartesian()
                .ZScale(1.5f);
            var model = gltf.CreateTerrainMesh(heightMap, meshReduceFactor: 0.5f);

            int triangles = model.LogicalMeshes.Sum(m => m.Primitives.Sum(p => p.GetTriangleIndices().Count()));
            _output.WriteLine($"FABDEM terrain: {heightMap.Width}x{heightMap.Height} grid, {triangles:N0} triangles after decimation");
            Assert.True(triangles > 0);
        }
    }
}
