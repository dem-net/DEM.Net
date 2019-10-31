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
        IRasterService _rasterService;
        IElevationService _elevationService;

        public ElevationTests(DemNetFixture fixture)
        {

            _rasterService = fixture.ServiceProvider.GetService<IRasterService>();
            _elevationService = fixture.ServiceProvider.GetService<IElevationService>();
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
        [InlineData(nameof(DEMDataSet.ASTER_GDEMV3), 45.179337, 5.721421, 220.99562072753906)]
        [InlineData(nameof(DEMDataSet.SRTM_GL3), 45.179337, 5.721421, 216.57283020019531)]
        [InlineData(nameof(DEMDataSet.SRTM_GL1), 45.179337, 5.721421, 216.71719360351562)]
        [InlineData(nameof(DEMDataSet.AW3D30), 45.179337, 5.721421, 220.99562072753906)]
        public void TestElevationSinglePoint(string dataSetName, double lat, double lon, double expectedElevation)
        {
            DEMDataSet dataSet = DEMDataSet.RegisteredDatasets.FirstOrDefault(d => d.Name == dataSetName);
            Assert.NotNull(dataSet);

            _elevationService.DownloadMissingFiles(dataSet, lat, lon);
            GeoPoint point = _elevationService.GetPointElevation(lat, lon, dataSet);
            double elevation = point.Elevation.GetValueOrDefault(0);

            Assert.Equal(expectedElevation, elevation, 5);
        }

        [Theory()]
        [InlineData("SRTM_GL3", 45.179337, 5.721421, 45.212278, 5.468857, 345, 2799.6234436035156, -2831.7227172851562, 178.56304931640625, 1656.548583984375)]
        [InlineData("SRTM_GL1", 45.179337, 5.721421, 45.212278, 5.468857, 1030, 3029.673828125, -3063.4037170410156, 178, 1657.423828125)]
        [InlineData("AW3D30", 45.179337, 5.721421, 45.212278, 5.468857, 1029, 3290.001708984375, -3328.8204193115234, 177.8447265625, 1653.1025390625)]
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
            Assert.Equal(expectedClimb, metrics.Climb);
            Assert.Equal(expectedDescent, metrics.Descent);
            Assert.Equal(expectedMin, metrics.MinElevation);
            Assert.Equal(expectedMax, metrics.MaxElevation);
        }

        [Theory()]
        [InlineData("SRTM_GL3", -25, -26, 37, 38, true)] // fully covered
        [InlineData("SRTM_GL3", -26.659806263787523, -25.729350373606543, 37.73596920859053, 38.39764411353181, false)] // 1 tile missing
        [InlineData("SRTM_GL3", -37.43596931765545, -37.13861749268079, 50.33844888725473, 50.51342652633956, false)] // not covered at all
        [InlineData("SRTM_GL3", 1.5, 2.5, 44.5, 45.5, true)] // fully covered by 4 tiles
        [InlineData("SRTM_GL3", 1.5, 1.6, 44.5, 44.6, true)] // fully inside 1 tile
        [InlineData("SRTM_GL3", 3.5, 4.5, 42.5, 42.6, false)] // half inside 1 tile, half with no tile
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
    }
}
