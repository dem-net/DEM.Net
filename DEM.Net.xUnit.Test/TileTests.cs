using System;
using System.Linq;
using DEM.Net.Core;
using DEM.Net.Test;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using DEM.Net.Core.Interpolation;

namespace DEM.Net.xUnit.Test
{
    public class TileTests : IClassFixture<DemNetFixture>
    {
        readonly ElevationService _elevationService;
        readonly RasterService _rasterService;
        readonly RasterIndexServiceResolver _rasterResolver;

        public TileTests(DemNetFixture fixture)
        {
            _elevationService = fixture.ServiceProvider.GetService<ElevationService>();
            _rasterService = fixture.ServiceProvider.GetService<RasterService>();
            _rasterResolver = fixture.ServiceProvider.GetService<RasterIndexServiceResolver>();
        }

        [Theory()]
        [InlineData(nameof(DEMDataSet.SRTM_GL3), 46.5, 10.5, "N46E010.hgt")]
        [InlineData(nameof(DEMDataSet.SRTM_GL3), 46.00000000000004, 10.000000000000007, "N46E010.hgt")]
        [InlineData(nameof(DEMDataSet.SRTM_GL3), 46.5, 10.000000000000007, "N46E010.hgt")]
        public void SourceBboxCheck(string dataSetName, double lat, double lon, string expectedFileName)
        {
            DEMDataSet dataSet = DEMDataSet.RegisteredDatasets.FirstOrDefault(d => d.Name == dataSetName);
            Assert.NotNull(dataSet);

            var indexService = _rasterResolver(dataSet.DataSource.DataSourceType);
            Assert.NotNull(dataSet);

            indexService.Setup(dataSet, _rasterService.LocalDirectory);
            var intersectingTiles = indexService.GetFileSources(dataSet).Where(tile => tile.BBox.Intersects(lat, lon)).ToList();

            var report = _rasterService.GenerateReportForLocation(dataSet, lat, lon);

            Assert.NotNull(report);
            Assert.True(report.Count == 1);


            Assert.Equal(expectedFileName, Path.GetFileName(report.First().LocalName));

        }

        [Theory()]
        [InlineData(nameof(DEMDataSet.ASTER_GDEMV3), 46, 10, "ASTGTMV003_N45E009_dem.tif", "ASTGTMV003_N45E010_dem.tif", "ASTGTMV003_N46E009_dem.tif", "ASTGTMV003_N46E010_dem.tif")]
        [InlineData(nameof(DEMDataSet.SRTM_GL3), 46, 10, "N45E009.hgt", "N45E010.hgt", "N46E009.hgt", "N46E010.hgt")]
        [InlineData(nameof(DEMDataSet.SRTM_GL1), 46, 10, "N45E009.hgt", "N45E010.hgt", "N46E009.hgt", "N46E010.hgt")]
        [InlineData(nameof(DEMDataSet.AW3D30), 46, 10, "N045E009_AVE_DSM.tif", "N045E010_AVE_DSM.tif", "N046E009_AVE_DSM.tif", "N046E010_AVE_DSM.tif")]
        void TestEdges(string dataSetName, double lat, double lon
            , string rasterSouthWestName, string rasterSouthEastName
            , string rasterNorthWestName, string rasterNorthEastName)
        {
            DEMDataSet dataSet = DEMDataSet.RegisteredDatasets.FirstOrDefault(d => d.Name == dataSetName);
            Assert.NotNull(dataSet);
            DEMFileType fileType = dataSet.FileFormat.Type;
            int rasterSize = dataSet.PointsPerDegree;
            double amountx = (1d / rasterSize) / 4d;
            double amounty = (1d / rasterSize) / 4d;

            // Regenerates all metadata            
            //_rasterService.GenerateDirectoryMetadata(dataSet
            //                                        , force: true
            //                                        , deleteOnError: false
            //                                        , maxDegreeOfParallelism: 1);
            _elevationService.DownloadMissingFiles(dataSet, lat, lon);
            var tiles = _rasterService.GenerateReportForLocation(dataSet, lat, lon);
            Assert.True(tiles.Count == 4);
            Assert.Single(tiles, t => string.Equals(Path.GetFileName(t.LocalName), rasterSouthWestName, StringComparison.OrdinalIgnoreCase));
            Assert.Single(tiles, t => string.Equals(Path.GetFileName(t.LocalName), rasterSouthEastName, StringComparison.OrdinalIgnoreCase));
            Assert.Single(tiles, t => string.Equals(Path.GetFileName(t.LocalName), rasterNorthWestName, StringComparison.OrdinalIgnoreCase));
            Assert.Single(tiles, t => string.Equals(Path.GetFileName(t.LocalName), rasterNorthEastName, StringComparison.OrdinalIgnoreCase));

            if (dataSet.FileFormat.Registration == DEMFileRegistrationMode.Cell)
            {
                using (var rasterNW = _rasterService.OpenFile(tiles.First(t => string.Equals(rasterNorthWestName, Path.GetFileName(t.LocalName))).LocalName, fileType))
                using (var rasterNE = _rasterService.OpenFile(tiles.First(t => string.Equals(rasterNorthEastName, Path.GetFileName(t.LocalName))).LocalName, fileType))
                using (var rasterSW = _rasterService.OpenFile(tiles.First(t => string.Equals(rasterSouthWestName, Path.GetFileName(t.LocalName))).LocalName, fileType))
                using (var rasterSE = _rasterService.OpenFile(tiles.First(t => string.Equals(rasterSouthEastName, Path.GetFileName(t.LocalName))).LocalName, fileType))
                {
                    var elevNW = rasterNW.GetElevationAtPoint(rasterNW.ParseMetaData(dataSet.FileFormat), rasterSize - 1, rasterSize - 1);
                    var elevNE = rasterNE.GetElevationAtPoint(rasterNE.ParseMetaData(dataSet.FileFormat), 0, rasterSize - 1);
                    var elevSW = rasterSW.GetElevationAtPoint(rasterSW.ParseMetaData(dataSet.FileFormat), rasterSize - 1, 0);
                    var elevSE = rasterSE.GetElevationAtPoint(rasterSE.ParseMetaData(dataSet.FileFormat), 0, 0);

                    BilinearInterpolator interpolator = new BilinearInterpolator();
                    var elev0 = interpolator.Interpolate(elevSW, elevSE, elevNW, elevNE, 0.25, 0.25);
                    var apiElev0 = _elevationService.GetPointElevation(lat + amounty, lon - amountx, dataSet);
                    Assert.True((elev0 - apiElev0.Elevation.Value) < double.Epsilon);

                    var elev1 = interpolator.Interpolate(elevSW, elevSE, elevNW, elevNE, 0.75, 0.25);
                    var apiElev1 = _elevationService.GetPointElevation(lat + amounty, lon + amountx, dataSet);
                    Assert.True((elev1 - apiElev1.Elevation.Value) < double.Epsilon);

                    var elev2 = interpolator.Interpolate(elevSW, elevSE, elevNW, elevNE, 0.25, 0.75);
                    var apiElev2 = _elevationService.GetPointElevation(lat - amounty, lon - amountx, dataSet);
                    Assert.True((elev2 - apiElev2.Elevation.Value) < double.Epsilon);

                    var elev3 = interpolator.Interpolate(elevSW, elevSE, elevNW, elevNE, 0.75, 0.75);
                    var apiElev3 = _elevationService.GetPointElevation(lat - amounty, lon + amountx, dataSet);
                    Assert.True((elev3 - apiElev3.Elevation.Value) < double.Epsilon);
                }
            }
            else
            {
                using (var rasterNW = _rasterService.OpenFile(tiles.First(t => string.Equals(rasterNorthWestName, Path.GetFileName(t.LocalName))).LocalName, fileType))
                using (var rasterNE = _rasterService.OpenFile(tiles.First(t => string.Equals(rasterNorthEastName, Path.GetFileName(t.LocalName))).LocalName, fileType))
                using (var rasterSW = _rasterService.OpenFile(tiles.First(t => string.Equals(rasterSouthWestName, Path.GetFileName(t.LocalName))).LocalName, fileType))
                using (var rasterSE = _rasterService.OpenFile(tiles.First(t => string.Equals(rasterSouthEastName, Path.GetFileName(t.LocalName))).LocalName, fileType))
                {
                    // Northen row, west to east
                    var elevN0 = rasterNW.GetElevationAtPoint(rasterNW.ParseMetaData(dataSet.FileFormat), rasterSize - 1, rasterSize - 1);
                    var elevN1 = rasterNW.GetElevationAtPoint(rasterNW.ParseMetaData(dataSet.FileFormat), rasterSize, rasterSize - 1);
                    var elevN2 = rasterNE.GetElevationAtPoint(rasterNE.ParseMetaData(dataSet.FileFormat), 1, rasterSize - 1);

                    // middle row, west to east
                    var elevM0 = rasterNW.GetElevationAtPoint(rasterNW.ParseMetaData(dataSet.FileFormat), rasterSize - 1, rasterSize);
                    var elevM1 = rasterNW.GetElevationAtPoint(rasterNW.ParseMetaData(dataSet.FileFormat), rasterSize, rasterSize);
                    var elevM2 = rasterNE.GetElevationAtPoint(rasterNE.ParseMetaData(dataSet.FileFormat), 1, rasterSize);

                    // Sourthen row, west to east
                    var elevS0 = rasterSW.GetElevationAtPoint(rasterSW.ParseMetaData(dataSet.FileFormat), rasterSize - 1, 1);
                    var elevS1 = rasterSW.GetElevationAtPoint(rasterSW.ParseMetaData(dataSet.FileFormat), rasterSize, 1);
                    var elevS2 = rasterSE.GetElevationAtPoint(rasterSE.ParseMetaData(dataSet.FileFormat), 1, 1);

                    BilinearInterpolator interpolator = new BilinearInterpolator();
                    var elev0 = interpolator.Interpolate(elevM0, elevM1, elevN0, elevN1, 0.75, 0.75);
                    var apiElev0 = _elevationService.GetPointElevation(lat + amounty, lon - amountx, dataSet);
                    Assert.True((elev0 - apiElev0.Elevation.Value) < double.Epsilon);

                    var elev1 = interpolator.Interpolate(elevM1, elevM2, elevN1, elevN2, 0.25, 0.75);
                    var apiElev1 = _elevationService.GetPointElevation(lat + amounty, lon + amountx, dataSet);
                    Assert.True((elev1 - apiElev1.Elevation.Value) < double.Epsilon);

                    var elev2 = interpolator.Interpolate(elevS0, elevS1, elevM0, elevM1, 0.75, 0.25);
                    var apiElev2 = _elevationService.GetPointElevation(lat - amounty, lon - amountx, dataSet);
                    Assert.True((elev2 - apiElev2.Elevation.Value) < double.Epsilon);

                    var elev3 = interpolator.Interpolate(elevS1, elevS2, elevM1, elevM2, 0.25, 0.25);
                    var apiElev3 = _elevationService.GetPointElevation(lat - amounty, lon + amountx, dataSet);
                    Assert.True((elev3 - apiElev3.Elevation.Value) < double.Epsilon);
                }

            }
        }


        [Theory()]
        [InlineData(nameof(DEMDataSet.SRTM_GL3))]
        [InlineData(nameof(DEMDataSet.ASTER_GDEMV3))]
        public void BoudingBoxConservationTest(string datasetName)
        {

            var dataset = DEMDataSet.RegisteredDatasets.First(d => d.Name == datasetName);

            string bboxWKT = "POLYGON((5.54888 43.519525, 5.61209 43.519525, 5.61209 43.565225, 5.54888 43.565225, 5.54888 43.519525))";


            BoundingBox bbox = GeometryService.GetBoundingBox(bboxWKT);

            Assert.NotNull(bbox);
            Assert.Equal(bboxWKT, bbox.WKT);

            HeightMap heightMap = _elevationService.GetHeightMap(ref bbox, dataset);

            heightMap = heightMap.ReprojectGeodeticToCartesian().BakeCoordinates();
            Assert.True(heightMap.BoundingBox == heightMap.Coordinates.GetBoundingBox());

            heightMap = heightMap.ZScale(2.5f).BakeCoordinates();
            Assert.True(heightMap.BoundingBox == heightMap.Coordinates.GetBoundingBox());

            heightMap = heightMap.CenterOnOrigin().BakeCoordinates();
            Assert.True(heightMap.BoundingBox == heightMap.Coordinates.GetBoundingBox());

            heightMap = heightMap.FitInto(30f).BakeCoordinates();
            Assert.True(heightMap.BoundingBox == heightMap.Coordinates.GetBoundingBox());

        }
    }
}
