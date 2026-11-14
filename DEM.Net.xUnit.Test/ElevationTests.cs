using System;
using DEM.Net.Core;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Collections.Generic;

namespace DEM.Net.Test
{
    public class ElevationTests : IClassFixture<DemNetFixture>
    {
        ElevationService _elevationService;

        public ElevationTests(DemNetFixture fixture)
        {
            _elevationService = fixture.ServiceProvider.GetService<ElevationService>();
        }

        [Fact(DisplayName = "Not covered elevation check")]
        public void TestElevationWithNoCoverage()
        {
            DEMDataSet dataSet = DEMDataSet.SRTM_GL3;

            var point = _elevationService.GetPointElevation(31, -27, dataSet);
            Assert.Null(point);

        }

        [Fact(DisplayName = "Not covered download check")]
        public void TestDownloadWithNoCoverage()
        {
            DEMDataSet dataSet = DEMDataSet.SRTM_GL3;

            // This one should run without error, but generating a warning,
            // as location is not covererd by dataset
            _elevationService.DownloadMissingFiles(dataSet, 31, -27);

        }

        // Reference values below are bilinear interpolations of the dataset pixels read with GDAL
        // (pixel centers from each file georeferencing), independently of DEM.Net.

        [Theory()]
        [InlineData(nameof(DEMDataSet.AW3D30),  47.132563, -86.818075,  179)] // Lake Superior surface
        public void TestUS(string dataSetName, double lat, double lon, double expectedElevation)
        {
            DEMDataSet dataSet = DEMDataSet.RegisteredDatasets.FirstOrDefault(d => d.Name == dataSetName);
            Assert.NotNull(dataSet);

            _elevationService.DownloadMissingFiles(dataSet, lat, lon);
            GeoPoint point = _elevationService.GetPointElevation(lat, lon, dataSet);
            double elevation = point.Elevation.GetValueOrDefault(0);

            Assert.Equal(expectedElevation, elevation, 0);
        }

        [Theory()]
        [InlineData(nameof(DEMDataSet.NASADEM), 45.179337, 5.721421, 217)]  // 217.111
        [InlineData(nameof(DEMDataSet.SRTM_GL3), 45.179337, 5.721421, 217)] // 216.529
        [InlineData(nameof(DEMDataSet.AW3D30), 45.179337, 5.721421, 223)]   // 222.996
        public void TestElevationSinglePoint(string dataSetName, double lat, double lon, double expectedElevation)
        {
            DEMDataSet dataSet = DEMDataSet.RegisteredDatasets.FirstOrDefault(d => d.Name == dataSetName);
            Assert.NotNull(dataSet);

            _elevationService.DownloadMissingFiles(dataSet, lat, lon);
            GeoPoint point = _elevationService.GetPointElevation(lat, lon, dataSet);
            double elevation = point.Elevation.GetValueOrDefault(0);

            Assert.Equal(expectedElevation, elevation, 0);
        }

        [Theory()]
        [InlineData(nameof(DEMDataSet.SRTM_GL3), 46.00000000000004, 10.000000000000007, 1748)]
        [InlineData(nameof(DEMDataSet.AW3D30), 46.00000000000004, 10.000000000000007, 1743)] // 1742.75, 4 tiles corner
        public void TestElevationSinglePoint_TileEdges(string dataSetName, double lat, double lon, double expectedElevation)
        {
            DEMDataSet dataSet = DEMDataSet.RegisteredDatasets.FirstOrDefault(d => d.Name == dataSetName);
            Assert.NotNull(dataSet);

            _elevationService.DownloadMissingFiles(dataSet, lat, lon);

            _elevationService.DownloadMissingFiles(dataSet, lat - 0.5, lon);
            _elevationService.DownloadMissingFiles(dataSet, lat - 0.5, lon - 0.5);
            _elevationService.DownloadMissingFiles(dataSet, lat, lon - 0.5);
            _elevationService.DownloadMissingFiles(dataSet, lat + 0.5, lon);
            _elevationService.DownloadMissingFiles(dataSet, lat + 0.5, lon + 0.5);
            _elevationService.DownloadMissingFiles(dataSet, lat, lon + 0.5);
            GeoPoint point = _elevationService.GetPointElevation(lat, lon, dataSet);
            double elevation = point.Elevation.GetValueOrDefault(0);

            Assert.Equal(expectedElevation, elevation, 0);
        }

        /// <summary>
        /// Points close to tile borders: last pixel row / column of cell registered tiles (were not covered by any tile),
        /// interpolation across a tile seam, within half a pixel of a tile edge.
        /// </summary>
        [Theory()]
        [InlineData(nameof(DEMDataSet.AW3D30), 46.5, 10.9999, 2293.06)]        // last column of N046E010
        [InlineData(nameof(DEMDataSet.AW3D30), 46.00005, 10.5, 1488.84)]       // last row of N046E010
        [InlineData(nameof(DEMDataSet.AW3D30), 46.99995, 10.00005, 1113.3552)] // first row / column of N046E010
        [InlineData(nameof(DEMDataSet.FABDEM), 49.99, 8.9999, 130.0448)]       // between N49E008 and N49E009
        [InlineData(nameof(DEMDataSet.FABDEM), 50.00005, 8.3, 85.2464)]        // between N49E008 and N50E008
        [InlineData(nameof(DEMDataSet.FABDEM), 49.99995, 8.3, 85.1348)]        // first row of N49E008
        [InlineData(nameof(DEMDataSet.NASADEM), 45.5, 5.99995, 322.18)]        // overlapping tiles seam
        [InlineData(nameof(DEMDataSet.SRTM_GL3), 45.5, 5.9999, 322.48)]        // overlapping tiles seam
        public void TestElevationSinglePoint_TileBorders(string dataSetName, double lat, double lon, double expectedElevation)
        {
            DEMDataSet dataSet = DEMDataSet.RegisteredDatasets.FirstOrDefault(d => d.Name == dataSetName);
            Assert.NotNull(dataSet);

            _elevationService.DownloadMissingFiles(dataSet, BoundingBox.AroundPoint(lat, lon, 0.001));
            GeoPoint point = _elevationService.GetPointElevation(lat, lon, dataSet);

            Assert.NotNull(point);
            // absolute tolerance: Assert.Equal(double, double, precision) rounds both values and fails on midpoints
            Assert.True(Math.Abs(expectedElevation - point.Elevation.Value) < 0.001, $"Expected {expectedElevation}, got {point.Elevation.Value}");
        }

        [Theory()]
        // climb, descent, min and max are sums / extrema of float elevations: compared with 1 decimal
        [InlineData(nameof(DEMDataSet.SRTM_GL3), 45.179337, 5.721421, 45.212278, 5.468857, 344, 2586.41, -2617.292, 178.271, 1654.438)]
        [InlineData(nameof(DEMDataSet.AW3D30), 45.179337, 5.721421, 45.212278, 5.468857, 1029, 2974.095, -3012.914, 179.845, 1655.103)]
        public void TestElevationLine(string dataSetName, double latStart, double lonStart, double latEnd, double lonEnd,
            double expectedPointCount, double expectedClimb, double expectedDescent, double expectedMin, double expectedMax)
        {
            var elevationLine = GeometryService.ParseGeoPointAsGeometryLine(new GeoPoint(latStart, lonStart), new GeoPoint(latEnd, lonEnd));
            Assert.NotNull(elevationLine);
            Assert.Equal(2, elevationLine.Coordinates.Length);

            DEMDataSet dataSet = DEMDataSet.RegisteredDatasets.FirstOrDefault(d => d.Name == dataSetName);
            Assert.NotNull(dataSet);

            _elevationService.DownloadMissingFiles(dataSet, elevationLine.GetBoundingBox());
            var geoPoints = _elevationService.GetLineGeometryElevation(elevationLine, dataSet);
            Assert.NotNull(geoPoints);
            Assert.Equal(expectedPointCount, geoPoints.Count);

            var metrics = geoPoints.ComputeMetrics();
            Assert.NotNull(metrics);
            Assert.Equal(expectedClimb, metrics.Climb, 1);
            Assert.Equal(expectedDescent, metrics.Descent, 1);
            Assert.Equal(expectedMin, metrics.MinElevation, 1);
            Assert.Equal(expectedMax, metrics.MaxElevation, 1);
        }

        [Theory()]
        [InlineData(nameof(DEMDataSet.SRTM_GL3), -26, -25, 37, 38, true)] // fully covered (N37W026)
        [InlineData(nameof(DEMDataSet.SRTM_GL3), -26.659806263787523, -25.729350373606543, 37.73596920859053, 38.39764411353181, false)] // 1 tile missing
        [InlineData(nameof(DEMDataSet.SRTM_GL3), -37.43596931765545, -37.13861749268079, 50.33844888725473, 50.51342652633956, false)] // not covered at all
        [InlineData(nameof(DEMDataSet.SRTM_GL3), 1.5, 2.5, 44.5, 45.5, true)] // fully covered by 4 tiles
        [InlineData(nameof(DEMDataSet.SRTM_GL3), 1.5, 1.6, 44.5, 44.6, true)] // fully inside 1 tile
        [InlineData(nameof(DEMDataSet.SRTM_GL3), 3.5, 4.5, 42.5, 42.6, false)] // half inside 1 tile, half with no tile
        public void TestBboxCoverage(string dataSetName, double xmin, double xmax, double ymin, double ymax, bool isExpectedCovered)
        {
            BoundingBox bbox = new BoundingBox(xmin, xmax, ymin, ymax);
            Assert.True(bbox.IsValid(), "Bbox is not valid");

            DEMDataSet dataSet = DEMDataSet.RegisteredDatasets.FirstOrDefault(d => d.Name == dataSetName);
            Assert.NotNull(dataSet);

            _elevationService.DownloadMissingFiles(dataSet, bbox);
            List<FileMetadata> bboxMetadata = _elevationService.GetCoveringFiles(bbox, dataSet);
            bool covered = _elevationService.IsBoundingBoxCovered(bbox, bboxMetadata.Select(m => m.BoundingBox));
            Assert.Equal(isExpectedCovered, covered);

        }

        [Theory()]
        [InlineData(nameof(DEMDataSet.SRTM_GL3), 39.97052612249965, 20.178894102573395, 40.16242159876657, 20.476635396480564, 3)]
        public void TestIntervisibility(string dataSetName, double latStart, double lonStart
            , double latEnd, double lonEnd, double expectedObstacles)
        {
            DEMDataSet dataSet = DEMDataSet.RegisteredDatasets.FirstOrDefault(d => d.Name == dataSetName);
            Assert.NotNull(dataSet);

            IntervisibilityReport report = _elevationService.GetIntervisibilityReport(new GeoPoint(latStart, lonStart), new GeoPoint(latEnd, lonEnd), dataSet);

            Assert.NotNull(report);
            Assert.Equal(expectedObstacles, report.ObstacleCount, 0);
            Assert.Equal(expectedObstacles, report.Metrics.Obstacles.Count, 0);
        }

        [Theory()]
        [InlineData(nameof(DEMDataSet.SRTM_GL3), 44.9171006, 37.5636956, 9139.53, 44.4888189, 39.2923105, 7182.78, 0)]
        public void TestIntervisibilityWithInitialAltitude(string dataSetName, double latStart, double lonStart, double altitudeStart
            , double latEnd, double lonEnd, double altitudeEnd, double expectedObstacles)
        {
            DEMDataSet dataSet = DEMDataSet.RegisteredDatasets.FirstOrDefault(d => d.Name == dataSetName);
            Assert.NotNull(dataSet);

            BoundingBox bbox = new BoundingBox(lonStart, lonEnd,latStart, latEnd);
            _elevationService.DownloadMissingFiles(dataSet, bbox);
                        
            var sourcePoint = new GeoPoint(latStart, lonStart);
            var targetPoint = new GeoPoint(latEnd, lonEnd);
            var startElevation = _elevationService.GetPointElevation(sourcePoint, dataSet).Elevation ?? 0;
            var endElevation = _elevationService.GetPointElevation(targetPoint, dataSet).Elevation ?? 0;

            IntervisibilityReport report = _elevationService.GetIntervisibilityReport(sourcePoint, targetPoint, dataSet, downloadMissingFiles: true, altitudeStart-startElevation, altitudeEnd-endElevation);


            Assert.NotNull(report);
            Assert.Equal(expectedObstacles, report.ObstacleCount, 0);
            Assert.Equal(expectedObstacles, report.Metrics.Obstacles.Count, 0);

        }

        [Fact]
        public void IntervisibilityTest()
        {
            var boundingBox = new BoundingBox(-114.81670660619355, -114.79753117282172, 50.887605382989769, 50.8997005129077);
            var dataSet = DEMDataSet.RegisteredNonLocalDatasets.First(x => x.Name == "AW3D30");
            _elevationService.DownloadMissingFiles(dataSet, boundingBox);

            var start = new GeoPoint
            {
                Elevation = 1876.8404541015625,
                Latitude = 50.89365334058617,
                Longitude = -114.80711764454496
            };

            var end = new GeoPoint
            {
                Elevation = 2111.0537109375,
                Latitude = 50.885127771762058,
                Longitude = -114.80711764454496,
                DistanceFromOriginMeters = 949.06197559241423
            };

            var result = _elevationService.GetIntervisibilityReport(new List<GeoPoint> { start, end }, 0, 0);

        }

        [Theory()]
        [InlineData(nameof(DEMDataSet.AW3D30), 50.89365334058617, -114.80711764454496, 50.885127771762058, -114.80711764454496, 1)]
        [InlineData(nameof(DEMDataSet.SRTM_GL3), 50.89365334058617, -114.80711764454496, 50.885127771762058, -114.80711764454496, 1)]
        public void TestIntervisibility2(string dataSetName, double latStart, double lonStart
            , double latEnd, double lonEnd, double expectedObstacles)
        {
            DEMDataSet dataSet = DEMDataSet.RegisteredDatasets.FirstOrDefault(d => d.Name == dataSetName);
            Assert.NotNull(dataSet);

            IntervisibilityReport report = _elevationService.GetIntervisibilityReport(new GeoPoint(latStart, lonStart), new GeoPoint(latEnd, lonEnd), dataSet);

            Assert.NotNull(report);
            Assert.Equal(expectedObstacles, report.ObstacleCount, 0);
            Assert.Equal(expectedObstacles, report.Metrics.Obstacles.Count, 0);
        }
    }
}
