using System;
using System.IO;
using System.Linq;
using DEM.Net.Core;
using DEM.Net.Core.XyzTiles;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DEM.Net.Test
{
    public class MapterhornTests : IClassFixture<DemNetFixture>
    {
        private readonly ElevationService _elevationService;
        private readonly RasterService _rasterService;
        private readonly XyzTileIndexService _xyzIndex;

        public MapterhornTests(DemNetFixture fixture)
        {
            _elevationService = fixture.ServiceProvider.GetService<ElevationService>();
            _rasterService = fixture.ServiceProvider.GetService<RasterService>();
            _xyzIndex = fixture.ServiceProvider.GetService<XyzTileIndexService>();
        }

        [Fact(DisplayName = "Mapterhorn datasets are registered, only zoom 12 is listed")]
        public void DatasetsRegistered()
        {
            var listed = DEMDataSet.RegisteredListedDatasets.Where(d => d.IsMapterhorn).ToList();
            Assert.Single(listed);
            Assert.Equal(DEMDataSet.MapterhornName, listed[0].Name);
            Assert.Equal(DEMDataSet.MapterhornDefaultZoom, ((Core.Datasets.XyzTileDataSource)listed[0].DataSource).Zoom);

            for (int z = DEMDataSet.MapterhornMinZoom; z <= DEMDataSet.MapterhornMaxZoom; z++)
            {
                var dataSet = DEMDataSet.GetMapterhorn(z);
                Assert.Equal(z, ((Core.Datasets.XyzTileDataSource)dataSet.DataSource).Zoom);
                Assert.Equal(Reprojection.SRID_PROJECTED_MERCATOR, dataSet.SRID);
            }
            Assert.Equal(19.109, DEMDataSet.Mapterhorn.ResolutionMeters, 2);
        }

        [Fact(DisplayName = "Terrarium encode / decode round trip")]
        public void TerrariumRoundTrip()
        {
            const int size = 8;
            var heights = Enumerable.Range(0, size * size).Select(i => -431.5f + i * 137.25f).ToArray();
            string file = Path.Combine(Path.GetTempPath(), $"12_1_{Guid.NewGuid().GetHashCode() & 0xFFFF}.webp");
            try
            {
                TerrariumFile.EncodeWebP(heights, size, file);
                var decoded = TerrariumFile.Decode(File.ReadAllBytes(file), out int decodedSize);
                Assert.Equal(size, decodedSize);
                for (int i = 0; i < heights.Length; i++)
                    Assert.Equal(heights[i], decoded[i], 2);
            }
            finally
            {
                File.Delete(file);
            }
        }

        [Fact(DisplayName = "Decoded tiles are shared, and decoded again when the file changes")]
        public void TerrariumDecodedTileCache()
        {
            const int size = 8;
            string file = Path.Combine(Path.GetTempPath(), $"12_2_{Guid.NewGuid().GetHashCode() & 0xFFFF}.webp");
            try
            {
                TerrariumFile.EncodeWebP(Enumerable.Repeat(100f, size * size).ToArray(), size, file);
                using (var first = new TerrariumFile(file))
                using (var second = new TerrariumFile(file))
                {
                    Assert.Equal(100f, first.GetElevationAtPoint(null, 3, 3), 2);
                    Assert.Equal(100f, second.GetElevationAtPoint(null, 3, 3), 2);
                }

                TerrariumFile.EncodeWebP(Enumerable.Repeat(250f, size * size).ToArray(), size, file);
                File.SetLastWriteTimeUtc(file, DateTime.UtcNow.AddMinutes(1));
                using (var updated = new TerrariumFile(file))
                    Assert.Equal(250f, updated.GetElevationAtPoint(null, 3, 3), 2);
            }
            finally
            {
                File.Delete(file);
            }
        }

        [Fact(DisplayName = "Web Mercator tile math")]
        public void TileMath()
        {
            var world = WebMercatorTiles.TileBounds(0, 0, 0);
            Assert.Equal(-WebMercatorTiles.OriginShift, world.xMin, 3);
            Assert.Equal(WebMercatorTiles.OriginShift, world.yMax, 3);

            // Matterhorn summit
            var point = WebMercatorTiles.ToMercator(BoundingBox.AroundPoint(45.97637, 7.65861, 1e-7));
            var (xMin, xMax, yMin, yMax) = WebMercatorTiles.TileRange(point, 12);
            Assert.Equal((2135, 2135, 1457, 1457), (xMin, xMax, yMin, yMax));
            Assert.Equal(1, WebMercatorTiles.TileCount(BoundingBox.AroundPoint(45.97637, 7.65861, 1e-7), 12));
        }

        [Fact(DisplayName = "Resampling at scale 1 is the identity")]
        public void ResampleIdentity()
        {
            const int size = 4;
            var heights = Enumerable.Range(0, size * size).Select(i => (float)i).ToArray();
            var resampled = XyzTileIndexService.Resample(heights, size, size, 1, 0, 0);
            Assert.Equal(heights, resampled);

            // upper left quarter at scale 2: first sample is at ancestor pixel (-0.25 clamped to 0)
            var quarter = XyzTileIndexService.Resample(heights, size, size, 2, 0, 0);
            Assert.Equal(0f, quarter[0], 3);
            Assert.Equal(0.25f, quarter[1], 3); // u = 0.25
        }

        [Theory(DisplayName = "Mapterhorn point elevation")]
        [InlineData(12, 45.97637, 7.65861, 4440, 50)] // Matterhorn summit (19 m pixels smooth the peak)
        [InlineData(15, 45.179337, 5.721421, 217, 3)] // Grenoble
        public void PointElevation(int zoom, double lat, double lon, double expected, double tolerance)
        {
            var dataSet = DEMDataSet.GetMapterhorn(zoom);
            _elevationService.DownloadMissingFiles(dataSet, lat, lon);
            var point = _elevationService.GetPointElevation(lat, lon, dataSet);
            Assert.NotNull(point);
            Assert.InRange(point.Elevation.GetValueOrDefault(), expected - tolerance, expected + tolerance);
        }

        [Fact(DisplayName = "Mapterhorn zoom 17 resolves the Matterhorn summit")]
        public void SummitAtMaxZoom()
        {
            var dataSet = DEMDataSet.GetMapterhorn(17);
            // ~100 m around the summit (official height 4478 m, swissALTI3D 4477.5 m)
            var bbox = BoundingBox.AroundPoint(45.97637, 7.65861, 0.0007).ReprojectTo(Reprojection.SRID_GEODETIC, dataSet.SRID);
            var hMap = _elevationService.GetHeightMap(ref bbox, dataSet);
            Assert.InRange(hMap.Maximum, 4470, 4480);
        }

        [Fact(DisplayName = "Mapterhorn height map spans tile seams without gaps")]
        public void HeightMapAcrossTiles()
        {
            var dataSet = DEMDataSet.GetMapterhorn(14);
            // straddles tile boundaries at zoom 14
            var bbox = new BoundingBox(7.64, 7.68, 45.96, 45.99).ReprojectTo(Reprojection.SRID_GEODETIC, dataSet.SRID);
            var hMap = _elevationService.GetHeightMap(ref bbox, dataSet);

            Assert.True(WebMercatorTiles.TileCount(bbox, 14) > 1);
            Assert.Equal(hMap.Width * hMap.Height, hMap.Coordinates.Count());
            Assert.DoesNotContain(hMap.Coordinates, p => p.Elevation < 1000 || p.Elevation > 4500);
            Assert.InRange(hMap.Maximum, 4400, 4480);
        }

        [Fact(DisplayName = "Mapterhorn line elevation is sampled at dataset resolution, in WGS84")]
        public void LineElevation()
        {
            var dataSet = DEMDataSet.GetMapterhorn(14);
            var start = new GeoPoint(45.9763, 7.6586);
            var end = new GeoPoint(45.95, 7.70);
            var line = GeometryService.ParseGeoPointAsGeometryLine(start, end);
            _elevationService.DownloadMissingFiles(dataSet, line.GetBoundingBox());

            var points = _elevationService.GetLineGeometryElevation(line, dataSet);

            double step = dataSet.ResolutionMeters * Math.Cos(45.96 * Math.PI / 180);
            Assert.InRange(points.Count, start.DistanceTo(end) / step, start.DistanceTo(end) / step + 2);
            Assert.Equal(start.Latitude, points.First().Latitude, 6);
            Assert.Equal(end.Longitude, points.Last().Longitude, 6);
            Assert.InRange(points.First().Elevation.Value, 4300, 4480);
            Assert.DoesNotContain(points, p => p.Elevation < 1500);
        }

        [Fact(DisplayName = "Missing high zoom tile is resampled from a lower zoom")]
        public void MissingTileFallback()
        {
            // Middle of the Pacific: no data above zoom 12
            var dataSet = DEMDataSet.GetMapterhorn(15);
            double lat = 0.5, lon = -150.5;
            _elevationService.DownloadMissingFiles(dataSet, lat, lon);
            var point = _elevationService.GetPointElevation(lat, lon, dataSet);
            Assert.NotNull(point);
        }

        [Fact(DisplayName = "Extents covering too many tiles are refused before any download")]
        public void TooManyTilesRefused()
        {
            var dataSet = DEMDataSet.GetMapterhorn(17);
            var bbox = new BoundingBox(5.7, 7.7, 45.1, 46.0); // ~290 000 tiles at zoom 17
            Assert.Throws<InvalidOperationException>(() => _rasterService.GenerateReport(dataSet, bbox));
        }

        [Fact(DisplayName = "Mapterhorn max zoom from coverage index")]
        public void MaxZoom()
        {
            var swiss = DEMDataSet.GetMapterhorn(15);
            _rasterService.GenerateReport(swiss, BoundingBox.AroundPoint(45.97637, 7.65861, 0.001)); // Setup
            Assert.True(_xyzIndex.GetMaxZoom(swiss, BoundingBox.AroundPoint(45.97637, 7.65861, 0.001)) >= 16);
            Assert.Equal(DEMDataSet.MapterhornDefaultZoom, _xyzIndex.GetMaxZoom(swiss, BoundingBox.AroundPoint(0.5, -150.5, 0.001)));
        }
    }
}
