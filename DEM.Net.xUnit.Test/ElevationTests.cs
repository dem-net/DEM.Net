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

        [Theory()]
        [InlineData(nameof(DEMDataSet.ASTER_GDEMV3), 45.179337, 5.721421, 222)]
        [InlineData(nameof(DEMDataSet.SRTM_GL3), 45.179337, 5.721421, 217)]
        [InlineData(nameof(DEMDataSet.SRTM_GL1), 45.179337, 5.721421, 217)]
        [InlineData(nameof(DEMDataSet.AW3D30), 45.179337, 5.721421, 221)]
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
        [InlineData(nameof(DEMDataSet.SRTM_GL1), 46.00000000000004, 10.000000000000007, 1744)]
        [InlineData(nameof(DEMDataSet.AW3D30), 46.00000000000004, 10.000000000000007, 1741)]
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

        [Theory()]
        [InlineData(nameof(DEMDataSet.ASTER_GDEMV3), 45.179337, 5.721421, 45.212278, 5.468857, 1031, 2837.678, -2878.576, 172.776, 1648.313)]
        [InlineData(nameof(DEMDataSet.SRTM_GL3), 45.179337, 5.721421, 45.212278, 5.468857, 344, 2586.41, -2617.292, 178.271, 1654.438)]
        [InlineData(nameof(DEMDataSet.SRTM_GL1), 45.179337, 5.721421, 45.212278, 5.468857, 1031, 2755.597, -2789.424, 178, 1655.313)]
        [InlineData(nameof(DEMDataSet.AW3D30), 45.179337, 5.721421, 45.212278, 5.468857, 911, 2973.321, -3012.14, 177.845, 1653.103)]
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
            Assert.Equal(expectedClimb, metrics.Climb, 3);
            Assert.Equal(expectedDescent, metrics.Descent, 3);
            Assert.Equal(expectedMin, metrics.MinElevation, 3);
            Assert.Equal(expectedMax, metrics.MaxElevation, 3);
        }

        [Theory()]
        [InlineData(nameof(DEMDataSet.SRTM_GL3), -26, -25, 37, 38, true)] // fully covered
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
        [InlineData(nameof(DEMDataSet.SRTM_GL3), 39.97052612249965, 20.178894102573395, 40.16242159876657, 20.476635396480564, 3)]
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
    }
}
