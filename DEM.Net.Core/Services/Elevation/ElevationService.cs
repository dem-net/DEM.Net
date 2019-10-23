// ElevationService.cs
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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DEM.Net.Core.Interpolation;
using GeoAPI.Geometries;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;

namespace DEM.Net.Core
{
    public class ElevationService : IElevationService
    {
        public const float NO_DATA_OUT = 0;
        private readonly IRasterService _IRasterService;
        private readonly ILogger<ElevationService> _logger;

        public ElevationService(IRasterService rasterService, ILogger<ElevationService> logger = null)
        {
            _IRasterService = rasterService;
            _logger = logger;
        }


        public string GetDEMLocalPath(DEMDataSet dataSet)
        {
            return _IRasterService.GetLocalDEMPath(dataSet);
        }

        public void DownloadMissingFiles(DEMDataSet dataSet, BoundingBox bbox = null)
        {
            var report = _IRasterService.GenerateReport(dataSet, bbox);

            if (report == null)
            {
                _logger?.LogWarning($"No coverage for bbox {bbox} in {dataSet.Name} dataset.");
                return;
            }

            DownloadMissingFiles_FromReport(report, dataSet);

        }

        public void DownloadMissingFiles(DEMDataSet dataSet, double lat, double lon)
        {
            var report = _IRasterService.GenerateReportForLocation(dataSet, lat, lon);

            if (report == null)
            {
                _logger?.LogWarning($"No coverage for lat/lon {lat}/{lon} in {dataSet.Name} dataset.");
                return;
            }

            DownloadMissingFiles_FromReport(Enumerable.Repeat(report, 1), dataSet);

        }

        public void DownloadMissingFiles(DEMDataSet dataSet, GeoPoint geoPoint)
        {
            if (geoPoint == null)
                return;

            DownloadMissingFiles(dataSet, geoPoint.Latitude, geoPoint.Longitude);
        }
        private void DownloadMissingFiles_FromReport(IEnumerable<DemFileReport> report, DEMDataSet dataSet)
        {
            // Generate metadata files if missing
            foreach (var file in report.Where(r => r.IsMetadataGenerated == false && r.IsExistingLocally == true))
            {
                _IRasterService.GenerateFileMetadata(file.LocalName, dataSet.FileFormat, false);
            }
            List<DemFileReport> filesToDownload = new List<DemFileReport>(report.Where(kvp => kvp.IsExistingLocally == false));

            if (filesToDownload.Count == 0)
            {
                _logger?.LogTrace("No missing file(s).");
            }
            else
            {
                _logger?.LogInformation($"Downloading {filesToDownload.Count} missing file(s).");

                try
                {
                    ParallelOptions options = new ParallelOptions() { MaxDegreeOfParallelism = 2 };
                    Parallel.ForEach(filesToDownload, options, file =>
                   {

                       DownloadDEMTile_HttpClient(file.URL, dataSet.FileFormat, file.LocalName);

                   }
                    );

                    _IRasterService.GenerateDirectoryMetadata(dataSet, false, false);
                    _IRasterService.LoadManifestMetadata(dataSet, true);

                }
                catch (AggregateException ex)
                {
                    _logger?.LogError(ex, $"Error downloading missing files. Check internet connection or retry later. {ex.GetInnerMostException().Message}");
                }


            }
        }


        private void DownloadDEMTile_HttpClient(string url, DEMFileFormat fileFormat, string localFileName)
        {

            // Create directories if not existing
            new FileInfo(localFileName).Directory.Create();

            _logger?.LogInformation($"Downloading file {url}...");

            using (HttpClient client = new HttpClient())
            {
                //using (HttpResponseMessage response = client.GetAsync(url).Result)
                //{
                //    if (response.IsSuccessStatusCode)
                //    {
                //        using (HttpContent content = response.Content)
                //        {
                //            using (FileStream fs = new FileStream(localFileName, FileMode.Create, FileAccess.Write))
                //            {
                //                var contentbytes = content.ReadAsByteArrayAsync().Result;
                //                fs.Write(contentbytes, 0, contentbytes.Length);
                //            }
                //        }
                //    }
                //}
                var contentbytes = client.GetByteArrayAsync(url).Result;
                using (FileStream fs = new FileStream(localFileName, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(contentbytes, 0, contentbytes.Length);
                }

            }

            _IRasterService.GenerateFileMetadata(localFileName, fileFormat, false);


        }

        /// <summary>
        /// Extract elevation data along line path
        /// </summary>
        /// <param name="lineWKT"></param>
        /// <returns></returns>
        public List<GeoPoint> GetLineGeometryElevation(string lineWKT, DEMDataSet dataSet, InterpolationMode interpolationMode = InterpolationMode.Bilinear)
        {
            IGeometry geom = GeometryService.ParseWKTAsGeometry(lineWKT);

            if (geom.OgcGeometryType == OgcGeometryType.MultiLineString)
            {
                _logger?.LogWarning("Geometry is a multi line string. Only the longest segment will be processed.");
                geom = geom.Geometries().OrderByDescending(g => g.NumPoints).First();
            }
            return GetLineGeometryElevation(geom, dataSet, interpolationMode);
        }

        public List<GeoPoint> GetLineGeometryElevation(IGeometry lineStringGeometry, DEMDataSet dataSet, InterpolationMode interpolationMode = InterpolationMode.Bilinear)
        {
            if (lineStringGeometry == null || lineStringGeometry.IsEmpty)
                return null;
            if (lineStringGeometry.OgcGeometryType != OgcGeometryType.LineString)
            {
                throw new Exception("Geometry must be a linestring");
            }
            if (lineStringGeometry.SRID != 4326)
            {
                throw new Exception("Geometry SRID must be set to 4326 (WGS 84)");
            }

            BoundingBox bbox = lineStringGeometry.GetBoundingBox();
            List<FileMetadata> tiles = this.GetCoveringFiles(bbox, dataSet);

            // Init interpolator
            IInterpolator interpolator = GetInterpolator(interpolationMode);

            var ptStart = lineStringGeometry.Coordinates[0];
            var ptEnd = lineStringGeometry.Coordinates.Last();
            GeoPoint start = new GeoPoint(ptStart.Y, ptStart.X);
            GeoPoint end = new GeoPoint(ptEnd.Y, ptEnd.X);
            double lengthMeters = start.DistanceTo(end);
            int demResolution = dataSet.ResolutionMeters;
            int totalCapacity = 2 * (int)(lengthMeters / demResolution);

            List<GeoPoint> geoPoints = new List<GeoPoint>(totalCapacity);

            using (RasterFileDictionary adjacentRasters = new RasterFileDictionary())
            {
                bool isFirstSegment = true; // used to return first point only for first segments, for all other segments last point will be returned
                foreach (GeoSegment segment in lineStringGeometry.Segments())
                {
                    List<FileMetadata> segTiles = this.GetCoveringFiles(segment.GetBoundingBox(), dataSet, tiles);

                    // Find all intersection with segment and DEM grid
                    IEnumerable<GeoPoint> intersections = this.FindSegmentIntersections(segment.Start.Longitude
                        , segment.Start.Latitude
                        , segment.End.Longitude
                        , segment.End.Latitude
                        , segTiles
                        , isFirstSegment
                        , true);

                    // Get elevation for each point
                    intersections = this.GetElevationData(intersections, adjacentRasters, segTiles, interpolator);

                    // Add to output list
                    geoPoints.AddRange(intersections);

                    isFirstSegment = false;
                }
                //Debug.WriteLine(adjacentRasters.Count);
            }  // Ensures all rasters are properly closed

            return geoPoints;
        }
        public List<GeoPoint> GetLineGeometryElevation(IEnumerable<GeoPoint> lineGeoPoints, DEMDataSet dataSet, InterpolationMode interpolationMode = InterpolationMode.Bilinear)
        {
            if (lineGeoPoints == null)
                throw new ArgumentNullException(nameof(lineGeoPoints), "Point list is null");

            IGeometry geometry = GeometryService.ParseGeoPointAsGeometryLine(lineGeoPoints);

            return GetLineGeometryElevation(geometry, dataSet, interpolationMode);
        }

        public float GetPointElevation(FileMetadata metadata, double lat, double lon, IInterpolator interpolator = null)
        {
            float heightValue = 0;
            try
            {
                using (IRasterFile raster = _IRasterService.OpenFile(metadata.Filename, metadata.fileFormat))
                {
                    heightValue = GetPointElevation(raster, metadata, lat, lon, interpolator);
                }
            }
            catch (Exception e)
            {
                _logger?.LogError(e, $"Error while getting elevation data : {e.Message}{Environment.NewLine}{e.ToString()}");
            }
            return heightValue;
        }
        public float GetPointElevation(IRasterFile raster, FileMetadata metadata, double lat, double lon, IInterpolator interpolator = null)
        {
            float heightValue = 0;
            try
            {
                if (interpolator == null)
                    interpolator = GetInterpolator(InterpolationMode.Bilinear);

                float noData = metadata.NoDataValueFloat;


                // precise position on the grid (with commas)
                double ypos = (lat - metadata.StartLat) / metadata.pixelSizeY;
                double xpos = (lon - metadata.StartLon) / metadata.pixelSizeX;

                // If pure integers, then it's on the grid
                float xInterpolationAmount = (float)xpos % 1;
                float yInterpolationAmount = (float)ypos % 1;

                bool xOnGrid = Math.Abs(xInterpolationAmount) < float.Epsilon;
                bool yOnGrid = Math.Abs(yInterpolationAmount) < float.Epsilon;

                // If xOnGrid and yOnGrid, we are on a grid intersection, and that's all
                if (xOnGrid && yOnGrid)
                {
                    int x = (int)Math.Round(xpos, 0);
                    int y = (int)Math.Round(ypos, 0);
                    heightValue = raster.GetElevationAtPoint(metadata, x, y);
                }
                else
                {
                    int xCeiling = (int)Math.Ceiling(xpos);
                    int xFloor = (int)Math.Floor(xpos);
                    int yCeiling = (int)Math.Ceiling(ypos);
                    int yFloor = (int)Math.Floor(ypos);
                    // Get 4 grid nearest points (DEM grid corners)

                    // If not yOnGrid and not xOnGrid we are on grid horizontal line
                    // We need elevations for top, bottom, left and right grid points (along x axis and y axis)
                    float northWest = raster.GetElevationAtPoint(metadata, xFloor, yFloor);
                    float northEast = raster.GetElevationAtPoint(metadata, xCeiling, yFloor);
                    float southWest = raster.GetElevationAtPoint(metadata, xFloor, yCeiling);
                    float southEast = raster.GetElevationAtPoint(metadata, xCeiling, yCeiling);

                    float avgHeight = GetAverageExceptForNoDataValue(noData, NO_DATA_OUT, southWest, southEast, northWest, northEast);

                    if (Math.Abs(northWest - noData) < float.Epsilon) northWest = avgHeight;
                    if (Math.Abs(northEast - noData) < float.Epsilon) northEast = avgHeight;
                    if (Math.Abs(southWest - noData) < float.Epsilon) southWest = avgHeight;
                    if (Math.Abs(southEast - noData) < float.Epsilon) southEast = avgHeight;

                    heightValue = interpolator.Interpolate(southWest, southEast, northWest, northEast, xInterpolationAmount, yInterpolationAmount);
                }


            }
            catch (Exception e)
            {
                _logger?.LogError(e, $"Error while getting elevation data : {e.Message}{Environment.NewLine}{e.ToString()}");
            }
            return heightValue;
        }
        public float GetPointsElevation(IRasterFile raster, FileMetadata metadata, IEnumerable<GeoPoint> points, IInterpolator interpolator = null)
        {
            float heightValue = 0;
            try
            {
                if (interpolator == null)
                    interpolator = GetInterpolator(InterpolationMode.Bilinear);

                float noData = metadata.NoDataValueFloat;

                foreach (var pointsByLat in points.GroupBy(p => p.Latitude))
                {
                    double lat = pointsByLat.Key;
                    double ypos = (lat - metadata.StartLat) / metadata.pixelSizeY;
                    foreach (GeoPoint point in pointsByLat)
                    {
                        // precise position on the grid (with commas)
                        double lon = point.Longitude;
                        double xpos = (lon - metadata.StartLon) / metadata.pixelSizeX;

                        // If pure integers, then it's on the grid
                        float xInterpolationAmount = (float)xpos % 1;
                        float yInterpolationAmount = (float)ypos % 1;

                        bool xOnGrid = Math.Abs(xInterpolationAmount) < float.Epsilon;
                        bool yOnGrid = Math.Abs(yInterpolationAmount) < float.Epsilon;

                        // If xOnGrid and yOnGrid, we are on a grid intersection, and that's all
                        if (xOnGrid && yOnGrid)
                        {
                            int x = (int)Math.Round(xpos, 0);
                            int y = (int)Math.Round(ypos, 0);
                            heightValue = raster.GetElevationAtPoint(metadata, x, y);
                        }
                        else
                        {
                            int xCeiling = (int)Math.Ceiling(xpos);
                            int xFloor = (int)Math.Floor(xpos);
                            int yCeiling = (int)Math.Ceiling(ypos);
                            int yFloor = (int)Math.Floor(ypos);
                            // Get 4 grid nearest points (DEM grid corners)

                            // If not yOnGrid and not xOnGrid we are on grid horizontal line
                            // We need elevations for top, bottom, left and right grid points (along x axis and y axis)
                            float northWest = raster.GetElevationAtPoint(metadata, xFloor, yFloor);
                            float northEast = raster.GetElevationAtPoint(metadata, xCeiling, yFloor);
                            float southWest = raster.GetElevationAtPoint(metadata, xFloor, yCeiling);
                            float southEast = raster.GetElevationAtPoint(metadata, xCeiling, yCeiling);

                            float avgHeight = GetAverageExceptForNoDataValue(noData, NO_DATA_OUT, southWest, southEast, northWest, northEast);

                            if (Math.Abs(northWest - noData) < float.Epsilon) northWest = avgHeight;
                            if (Math.Abs(northEast - noData) < float.Epsilon) northEast = avgHeight;
                            if (Math.Abs(southWest - noData) < float.Epsilon) southWest = avgHeight;
                            if (Math.Abs(southEast - noData) < float.Epsilon) southEast = avgHeight;

                            heightValue = interpolator.Interpolate(southWest, southEast, northWest, northEast, xInterpolationAmount, yInterpolationAmount);
                        }

                        point.Elevation = heightValue;
                    }
                }
            }
            catch (Exception e)
            {
                _logger?.LogError(e, $"Error while getting elevation data : {e.Message}{Environment.NewLine}{e.ToString()}");
            }
            return heightValue;
        }
        public GeoPoint GetPointElevation(double lat, double lon, DEMDataSet dataSet, InterpolationMode interpolationMode = InterpolationMode.Bilinear)
        {
            GeoPoint geoPoint = new GeoPoint(lat, lon);
            List<FileMetadata> tiles = this.GetCoveringFiles(lat, lon, dataSet);

            if (tiles.Count == 0)
            {
                _logger?.LogWarning($"No coverage found matching provided point {geoPoint} for dataset {dataSet.Name}");
                return null;
            }
            else
            {
                // Init interpolator
                IInterpolator interpolator = GetInterpolator(interpolationMode);

                using (RasterFileDictionary adjacentRasters = new RasterFileDictionary())
                {
                    PopulateRasterFileDictionary(adjacentRasters, tiles.First(), _IRasterService, tiles);

                    geoPoint.Elevation = GetElevationAtPoint(adjacentRasters, tiles.First(), lat, lon, 0, interpolator);


                    //Debug.WriteLine(adjacentRasters.Count);
                }  // Ensures all geotifs are properly closed
            }

            return geoPoint;
        }
        public IEnumerable<GeoPoint> GetPointsElevation(IEnumerable<GeoPoint> points, DEMDataSet dataSet, InterpolationMode interpolationMode = InterpolationMode.Bilinear)
        {
            if (points == null)
                return null;
            IEnumerable<GeoPoint> pointsWithElevation;
            BoundingBox bbox = points.GetBoundingBox();
            DownloadMissingFiles(dataSet, bbox);
            List<FileMetadata> tiles = this.GetCoveringFiles(bbox, dataSet);

            if (tiles.Count == 0)
            {
                return null;
            }
            else
            {
                // Init interpolator
                IInterpolator interpolator = GetInterpolator(interpolationMode);

                using (RasterFileDictionary adjacentRasters = new RasterFileDictionary())
                {

                    // Get elevation for each point
                    pointsWithElevation = this.GetElevationData(points, adjacentRasters, tiles, interpolator);

                    //Debug.WriteLine(adjacentRasters.Count);
                }  // Ensures all rasters are properly closed
            }

            return pointsWithElevation;
        }

        public IInterpolator GetInterpolator(InterpolationMode interpolationMode)
        {
            switch (interpolationMode)
            {
                case InterpolationMode.Hyperbolic:
                    return new HyperbolicInterpolator();
                case InterpolationMode.Bilinear:
                    return new BilinearInterpolator();
                default:
                    throw new NotImplementedException($"Interpolator {interpolationMode} is not implemented.");
            }
        }

        public string ExportElevationTable(List<GeoPoint> lineElevationData)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Lon\tLat\tDistance (meters)\tZ");
            foreach (GeoPoint pt in lineElevationData)
            {
                sb.AppendLine($"{pt.Longitude.ToString(CultureInfo.InvariantCulture)}\t{pt.Latitude.ToString(CultureInfo.InvariantCulture)}\t{(pt.DistanceFromOriginMeters ?? 0d).ToString("F2")}\t{pt.Elevation}");
            }
            return sb.ToString();
        }

        public HeightMap GetHeightMap(BoundingBox bbox, DEMDataSet dataSet)
        {
            DownloadMissingFiles(dataSet, bbox);

            // Locate which files are needed
            // Find files matching coords
            List<FileMetadata> bboxMetadata = GetCoveringFiles(bbox, dataSet);
            if (bboxMetadata.Count == 0)
            {
                const string errorMessage = "No covering files for provider bounding box.";
                this._logger.LogWarning(errorMessage);
                throw new Exception(errorMessage);
            }
            else
            {
                // Check if bounding box is fully covered (will result in invalid models without any error being thrown)
                bool covered = this.IsBoundingBoxCovered(bbox, bboxMetadata.Select(m => m.BoundingBox));
                if (!covered)
                {
                    const string errorMessage = "Bounding box is partially covered by DEM dataset. Heightmap in its current state supports only full data tiles.";
                    this._logger.LogWarning(errorMessage);
                    throw new Exception(errorMessage);
                }
                else
                {

                    // get height map for each file at bbox
                    List<HeightMap> tilesHeightMap = new List<HeightMap>(bboxMetadata.Count);
                    foreach (FileMetadata metadata in bboxMetadata)
                    {
                        using (IRasterFile raster = _IRasterService.OpenFile(metadata.Filename, dataSet.FileFormat))
                        {
                            tilesHeightMap.Add(raster.GetHeightMapInBBox(bbox, metadata, NO_DATA_OUT));
                        }
                    }


                    // Merge height maps
                    int totalHeight = tilesHeightMap.GroupBy(h => h.BoundingBox.xMin).Select(g => g.Sum(v => v.Height)).First();
                    int totalWidth = tilesHeightMap.GroupBy(h => h.BoundingBox.yMin).Select(g => g.Sum(v => v.Width)).First();

                    HeightMap heightMap = new HeightMap(totalWidth, totalHeight);
                    heightMap.BoundingBox = new BoundingBox(xmin: tilesHeightMap.Min(h => h.BoundingBox.xMin)
                                                            , xmax: tilesHeightMap.Max(h => h.BoundingBox.xMax)
                                                            , ymin: tilesHeightMap.Min(h => h.BoundingBox.yMin)
                                                            , ymax: tilesHeightMap.Max(h => h.BoundingBox.yMax));
                    heightMap.Coordinates = tilesHeightMap.SelectMany(hmap => hmap.Coordinates).Sort();
                    heightMap.Count = totalWidth * totalHeight;
                    heightMap.Minimum = tilesHeightMap.Min(hmap => hmap.Minimum);
                    heightMap.Maximum = tilesHeightMap.Max(hmap => hmap.Maximum);

                    System.Diagnostics.Debug.Assert(heightMap.Count == tilesHeightMap.Sum(h => h.Count));


                    return heightMap;
                }
            }

        }

        /// <summary>
        /// Check if a bounding box is fully covered by a set of tiles
        /// Detects when a tile is missing and thus involving a "data hole" in the future heightmap
        /// </summary>
        /// <param name="bbox"></param>
        /// <param name="bboxTiles"></param>
        /// <returns></returns>
        public bool IsBoundingBoxCovered(BoundingBox bbox, IEnumerable<BoundingBox> bboxTiles)
        {
            if (bboxTiles == null || !bboxTiles.Any())
                return false;

            var factory = new GeometryFactory(new PrecisionModel(PrecisionModels.FloatingSingle));

            ILinearRing bboxToLinearRing(BoundingBox boundingBox)
            {
                return factory.CreateLinearRing(new Coordinate[] {
                        new Coordinate(boundingBox.xMin, boundingBox.yMax),
                        new Coordinate(boundingBox.xMax, boundingBox.yMax),
                        new Coordinate(boundingBox.xMax, boundingBox.yMin),
                        new Coordinate(boundingBox.xMin, boundingBox.yMin),
                        new Coordinate(boundingBox.xMin, boundingBox.yMax)});
            }
            try
            {
                ILinearRing shell = bboxToLinearRing(bbox);
                ILinearRing[] tiles = bboxTiles.Select(bboxToLinearRing).ToArray();
                var polygon = factory.CreatePolygon(shell, tiles);
                return shell.Difference(NetTopologySuite.Operation.Union.UnaryUnionOp.Union(tiles)).IsEmpty;
            }
            catch(Exception e)
            {
                _logger.LogCritical(e, "error during linear creation");
            }
            return false;
        }

        public HeightMap GetHeightMap(BoundingBox bbox, string rasterFilePath, DEMFileFormat format)
        {
            HeightMap heightMap = null;
            using (IRasterFile raster = _IRasterService.OpenFile(rasterFilePath, format))
            {
                var metaData = raster.ParseMetaData();
                heightMap = raster.GetHeightMapInBBox(bbox, metaData, NO_DATA_OUT);
            }

            return heightMap;
        }
        public HeightMap GetHeightMap(FileMetadata metadata)
        {
            HeightMap map = null;
            using (IRasterFile raster = _IRasterService.OpenFile(metadata.Filename, metadata.fileFormat))
            {
                map = raster.GetHeightMap(metadata);
            }
            return map;
        }


        /// <summary>
        /// Fill altitudes for each GeoPoint provided, opening as few rasters as possible
        /// </summary>
        /// <param name="intersections"></param>
        /// <param name="adjacentRasters"></param>
        /// <param name="segTiles"></param>
        /// <param name="interpolator"></param>
        public IEnumerable<GeoPoint> GetElevationData(IEnumerable<GeoPoint> intersections, RasterFileDictionary adjacentRasters, List<FileMetadata> segTiles, IInterpolator interpolator)
        {
            // Group by raster file for sequential and faster access
            var pointsByTileQuery = from point in intersections
                                    let pointTile = new
                                    {
                                        Point = point,
                                        Tile = segTiles.FirstOrDefault(t => this.IsPointInTile(t, point)),
                                        AdjacentTiles = segTiles.Where(t => this.IsPointInAdjacentTile(t, point)).ToList()
                                    }
                                    group pointTile by pointTile.Tile into pointsByTile
                                    where pointsByTile.Key != null
                                    select pointsByTile;



            float lastElevation = 0;

            // To interpolate well points close to tile edges, we need all adjacent tiles
            //using (RasterFileDictionary adjacentRasters = new RasterFileDictionary())
            //{
            // For each group (key = tile, values = points within this tile)
            // TIP: test use of Parallel (warning : a lot of files may be opened at the same time)
            foreach (var tilePoints in pointsByTileQuery)
            {
                // Get the tile
                FileMetadata mainTile = tilePoints.Key;


                // We open rasters first, then we iterate
                PopulateRasterFileDictionary(adjacentRasters, mainTile, _IRasterService, tilePoints.SelectMany(tp => tp.AdjacentTiles));


                foreach (var pointile in tilePoints)
                {
                    GeoPoint current = pointile.Point;
                    lastElevation = this.GetElevationAtPoint(adjacentRasters, mainTile, current.Latitude, current.Longitude, lastElevation, interpolator);
                    current.Elevation = lastElevation;
                    yield return current;
                }

                //adjacentRasters.Clear();


            }
            //}



        }

        private void PopulateRasterFileDictionary(RasterFileDictionary dictionary, FileMetadata mainTile, IRasterService rasterService, IEnumerable<FileMetadata> fileMetadataList)
        {
            // Add main tile
            if (!dictionary.ContainsKey(mainTile))
            {
                dictionary[mainTile] = rasterService.OpenFile(mainTile.Filename, mainTile.fileFormat);
            }

            foreach (var fileMetadata in fileMetadataList)
            {
                if (!dictionary.ContainsKey(fileMetadata))
                {
                    dictionary[fileMetadata] = rasterService.OpenFile(fileMetadata.Filename, fileMetadata.fileFormat);
                }
            }
        }

        /// <summary>
        /// Finds all intersections between given segment and DEM grid
        /// </summary>
        /// <param name="startLon">Segment start longitude</param>
        /// <param name="startLat">Segment start latitude</param>
        /// <param name="endLon">Segment end longitude</param>
        /// <param name="endLat">Segment end latitude</param>
        /// <param name="segTiles">Metadata files <see cref="IElevationService.GetCoveringFiles"/> to see how to get them relative to segment geometry</param>
        /// <param name="returnStartPoint">If true, the segment starting point will be returned. Useful when processing a line segment by segment.</param>
        /// <param name="returnEndPoind">If true, the segment end point will be returned. Useful when processing a line segment by segment.</param>
        /// <returns></returns>
        private List<GeoPoint> FindSegmentIntersections(double startLon, double startLat, double endLon, double endLat, List<FileMetadata> segTiles, bool returnStartPoint, bool returnEndPoind)
        {
            List<GeoPoint> segmentPointsWithDEMPoints;
            // Find intersections with north/south lines, 
            // starting form segment western point to easternmost point
            GeoPoint westernSegPoint = startLon < endLon ? new GeoPoint(startLat, startLon) : new GeoPoint(endLat, endLon);
            GeoPoint easternSegPoint = startLon > endLon ? new GeoPoint(startLat, startLon) : new GeoPoint(endLat, endLon);
            GeoSegment inputSegment = new GeoSegment(westernSegPoint, easternSegPoint);

            if (segTiles.Any())
            {
                int estimatedCapacity = (segTiles.Select(t => t.OriginLongitude).Distinct().Count() // num horizontal tiles * width
                                        * segTiles.First().Width)
                                        + (segTiles.Select(t => t.OriginLatitude).Distinct().Count() // num vertical tiles * height
                                        * segTiles.First().Height);
                segmentPointsWithDEMPoints = new List<GeoPoint>(estimatedCapacity);
                bool yAxisDown = segTiles.First().pixelSizeY < 0;
                if (yAxisDown == false)
                {
                    throw new NotImplementedException("DEM with y axis upwards not supported.");
                }

                foreach (GeoSegment demSegment in this.GetDEMNorthSouthLines(segTiles, westernSegPoint, easternSegPoint))
                {
                    if (GeometryService.LineLineIntersection(out GeoPoint intersectionPoint, inputSegment, demSegment))
                    {
                        segmentPointsWithDEMPoints.Add(intersectionPoint);
                    }
                }

                // Find intersections with west/east lines, 
                // starting form segment northernmost point to southernmost point
                GeoPoint northernSegPoint = startLat > endLat ? new GeoPoint(startLat, startLon) : new GeoPoint(endLat, endLon);
                GeoPoint southernSegPoint = startLat < endLat ? new GeoPoint(startLat, startLon) : new GeoPoint(endLat, endLon);
                inputSegment = new GeoSegment(northernSegPoint, southernSegPoint);
                foreach (GeoSegment demSegment in this.GetDEMWestEastLines(segTiles, northernSegPoint, southernSegPoint))
                {
                    if (GeometryService.LineLineIntersection(out GeoPoint intersectionPoint, inputSegment, demSegment))
                    {
                        segmentPointsWithDEMPoints.Add(intersectionPoint);
                    }
                }
            }
            else
            {
                // No DEM coverage
                segmentPointsWithDEMPoints = new List<GeoPoint>(2);
            }

            // add start and/or end point
            if (returnStartPoint)
            {
                segmentPointsWithDEMPoints.Add(inputSegment.Start);
            }
            if (returnEndPoind)
            {
                segmentPointsWithDEMPoints.Add(inputSegment.End);
            }

            // sort points in segment order
            //
            segmentPointsWithDEMPoints.Sort(new DistanceFromPointComparer(new GeoPoint(startLat, startLon)));

            return segmentPointsWithDEMPoints;
        }


        private IEnumerable<GeoSegment> GetDEMNorthSouthLines(List<FileMetadata> segTiles, GeoPoint westernSegPoint, GeoPoint easternSegPoint)
        {
            // Get the first north west tile and last south east tile. 
            // The lines are bounded by those tiles

            foreach (var tilesByX in segTiles.GroupBy(t => t.StartLon).OrderBy(g => g.Key))
            {
                List<FileMetadata> NSTilesOrdered = tilesByX.OrderByDescending(t => t.StartLat).ToList();

                FileMetadata top = NSTilesOrdered.First();
                FileMetadata bottom = NSTilesOrdered.Last();

                // TIP: can optimize here starting with min(westernSegPoint, startlon) but careful !
                GeoPoint curPoint = new GeoPoint(top.StartLat, top.StartLon);

                // X Index in tile coords
                int curIndex = (int)Math.Ceiling((curPoint.Longitude - top.StartLon) / top.PixelScaleX);
                while (IsPointInTile(top, curPoint))
                {
                    if (curIndex >= top.Width)
                    {
                        break;
                    }

                    curPoint.Longitude = top.StartLon + (top.pixelSizeX * curIndex);
                    if (curPoint.Longitude > easternSegPoint.Longitude)
                    {
                        break;
                    }
                    GeoSegment line = new GeoSegment(new GeoPoint(top.OriginLatitude, curPoint.Longitude), new GeoPoint(bottom.EndLatitude, curPoint.Longitude));
                    curIndex++;
                    yield return line;
                }
            }
        }

        private IEnumerable<GeoSegment> GetDEMWestEastLines(List<FileMetadata> segTiles, GeoPoint northernSegPoint, GeoPoint southernSegPoint)
        {
            // Get the first north west tile and last south east tile. 
            // The lines are bounded by those tiles

            foreach (var tilesByY in segTiles.GroupBy(t => t.StartLat).OrderByDescending(g => g.Key))
            {
                List<FileMetadata> WETilesOrdered = tilesByY.OrderBy(t => t.StartLon).ToList();

                FileMetadata left = WETilesOrdered.First();
                FileMetadata right = WETilesOrdered.Last();

                GeoPoint curPoint = new GeoPoint(left.StartLat, left.StartLon);

                // Y Index in tile coords
                int curIndex = (int)Math.Ceiling((left.StartLat - curPoint.Latitude) / left.PixelScaleY);
                while (IsPointInTile(left, curPoint))
                {
                    if (curIndex >= left.Height)
                    {
                        break;
                    }

                    curPoint.Latitude = left.StartLat + (left.pixelSizeY * curIndex);
                    if (curPoint.Latitude < southernSegPoint.Latitude)
                    {
                        break;
                    }
                    GeoSegment line = new GeoSegment(new GeoPoint(curPoint.Latitude, left.OriginLongitude), new GeoPoint(curPoint.Latitude, right.EndLongitude));
                    curIndex++;
                    yield return line;
                }
            }

        }



        public BoundingBox GetTilesBoundingBox(List<FileMetadata> tiles)
        {
            double xmin = tiles.Min(t => t.OriginLongitude);
            double xmax = tiles.Max(t => t.EndLongitude);
            double ymin = tiles.Min(t => t.EndLatitude);
            double ymax = tiles.Max(t => t.OriginLatitude);
            return new BoundingBox(xmin, xmax, ymin, ymax);
        }


        public List<FileMetadata> GetCoveringFiles(BoundingBox bbox, DEMDataSet dataSet, List<FileMetadata> subSet = null)
        {
            // Locate which files are needed

            // Load metadata catalog
            List<FileMetadata> metadataCatalog = subSet ?? _IRasterService.LoadManifestMetadata(dataSet, false);

            // Find files matching coords
            List<FileMetadata> bboxMetadata = new List<FileMetadata>(metadataCatalog.Where(m => IsBboxIntersectingTile(m, bbox)));

            if (bboxMetadata.Count == 0)
            {
                _logger?.LogWarning($"No coverage found matching provided bounding box { bbox}.");
                //throw new NoCoverageException(dataSet, bbox, $"No coverage found matching provided bounding box {bbox}.");
            }

            return bboxMetadata;
        }
        public List<FileMetadata> GetCoveringFiles(double lat, double lon, DEMDataSet dataSet, List<FileMetadata> subSet = null)
        {
            // Locate which files are needed

            // Load metadata catalog
            List<FileMetadata> metadataCatalog = subSet ?? _IRasterService.LoadManifestMetadata(dataSet, false);

            var geoPoint = new GeoPoint(lat, lon);
            // Find files matching coords
            List<FileMetadata> bboxMetadata = new List<FileMetadata>(metadataCatalog.Where(m => IsPointInTile(m, geoPoint)));

            if (bboxMetadata.Count == 0)
            {
                _logger?.LogWarning($"No coverage found matching provided point {geoPoint}.");
                //throw new NoCoverageException(dataSet, lat, lon, $"No coverage found matching provided point {geoPoint}.");
            }

            return bboxMetadata;
        }

        public bool IsBboxIntersectingTile(FileMetadata tileMetadata, BoundingBox bbox)
        {
            BoundingBox tileBBox = tileMetadata.BoundingBox;

            return (tileBBox.xMax >= bbox.xMin && tileBBox.xMin <= bbox.xMax) && (tileBBox.yMax >= bbox.yMin && tileBBox.yMin <= bbox.yMax);
        }
        public bool IsPointInTile(FileMetadata tileMetadata, GeoPoint point)
        {
            return IsPointInTile(tileMetadata, point.Latitude, point.Longitude);
        }
        public bool IsPointInTile(FileMetadata tileMetadata, double lat, double lon)
        {
            BoundingBox tileBbox = tileMetadata.BoundingBox;

            bool isInsideX = (tileBbox.xMax >= lon && tileBbox.xMin <= lon);
            bool isInsideY = (tileBbox.yMax >= lat && tileBbox.yMin <= lat);
            return isInsideX && isInsideY;
        }
        // is the tile a tile just next to the tile the point is in ?
        private bool IsPointInAdjacentTile(FileMetadata tile, GeoPoint point)
        {
            double offsetX = tile.PixelScaleX * 2d;
            double offsetY = tile.PixelScaleY * 2d;
            bool inside = IsPointInTile(tile, point.Latitude + offsetY, point.Longitude + offsetX * 2d)
                        || IsPointInTile(tile, point.Latitude + offsetY, point.Longitude - offsetX * 2d)
                        || IsPointInTile(tile, point.Latitude - offsetY, point.Longitude - offsetX * 2d)
                        || IsPointInTile(tile, point.Latitude - offsetY, point.Longitude + offsetX * 2d);
            return inside;

        }

        private float GetElevationAtPoint(RasterFileDictionary adjacentTiles, FileMetadata metadata, double lat, double lon, float lastElevation, IInterpolator interpolator)
        {
            float heightValue = 0;
            try
            {

                IRasterFile mainRaster = adjacentTiles[metadata];

                //const double epsilon = (Double.Epsilon * 100);
                float noData = metadata.NoDataValueFloat;


                // precise position on the grid (with commas)
                double ypos = (lat - metadata.StartLat) / metadata.pixelSizeY;
                double xpos = (lon - metadata.StartLon) / metadata.pixelSizeX;

                // If pure integers, then it's on the grid
                float xInterpolationAmount = (float)xpos % 1;
                float yInterpolationAmount = (float)ypos % 1;

                bool xOnGrid = Math.Abs(xInterpolationAmount) < float.Epsilon;
                bool yOnGrid = Math.Abs(yInterpolationAmount) < float.Epsilon;

                // If xOnGrid and yOnGrid, we are on a grid intersection, and that's all
                if (xOnGrid && yOnGrid)
                {
                    int x = (int)Math.Round(xpos, 0);
                    int y = (int)Math.Round(ypos, 0);
                    var tile = FindTile(metadata, adjacentTiles, x, y, out x, out y);
                    heightValue = mainRaster.GetElevationAtPoint(tile, x, y);
                }
                else
                {
                    int xCeiling = (int)Math.Ceiling(xpos);
                    int xFloor = (int)Math.Floor(xpos);
                    int yCeiling = (int)Math.Ceiling(ypos);
                    int yFloor = (int)Math.Floor(ypos);
                    // Get 4 grid nearest points (DEM grid corners)

                    // If not yOnGrid and not xOnGrid we are on grid horizontal line
                    // We need elevations for top, bottom, left and right grid points (along x axis and y axis)
                    float northWest = GetElevationAtPoint(metadata, adjacentTiles, xFloor, yFloor, NO_DATA_OUT);
                    float northEast = GetElevationAtPoint(metadata, adjacentTiles, xCeiling, yFloor, NO_DATA_OUT);
                    float southWest = GetElevationAtPoint(metadata, adjacentTiles, xFloor, yCeiling, NO_DATA_OUT);
                    float southEast = GetElevationAtPoint(metadata, adjacentTiles, xCeiling, yCeiling, NO_DATA_OUT);

                    float avgHeight = GetAverageExceptForNoDataValue(noData, NO_DATA_OUT, southWest, southEast, northWest, northEast);

                    if (northWest == noData) northWest = avgHeight;
                    if (northEast == noData) northEast = avgHeight;
                    if (southWest == noData) southWest = avgHeight;
                    if (southEast == noData) southEast = avgHeight;

                    heightValue = interpolator.Interpolate(southWest, southEast, northWest, northEast, xInterpolationAmount, yInterpolationAmount);
                }

                if (heightValue == NO_DATA_OUT)
                {
                    heightValue = lastElevation;
                }
            }
            catch (Exception e)
            {
                _logger?.LogError(e, $"Error while getting elevation data : {e.Message}{Environment.NewLine}{e.ToString()}");
            }
            return heightValue;
        }

        private float GetElevationAtPoint(FileMetadata mainTile, RasterFileDictionary tiles, int x, int y, float nullValue)
        {
            FileMetadata goodTile = FindTile(mainTile, tiles, x, y, out int xRemap, out int yRemap);
            if (goodTile == null)
            {
                return nullValue;
            }

            if (tiles.ContainsKey(goodTile))
            {
                return tiles[goodTile].GetElevationAtPoint(goodTile, xRemap, yRemap);

            }
            else
            {
                throw new Exception("Tile not found. Should not happen.");
            }

        }

        private FileMetadata FindTile(FileMetadata mainTile, RasterFileDictionary tiles, int x, int y, out int newX, out int newY)
        {
            int xTileOffset = x < 0 ? -1 : x >= mainTile.Width ? 1 : 0;
            int yTileOffset = y < 0 ? -1 : y >= mainTile.Height ? 1 : 0;
            if (xTileOffset == 0 && yTileOffset == 0)
            {
                newX = x;
                newY = y;
                return mainTile;
            }
            else
            {
                int yScale = Math.Sign(mainTile.pixelSizeY);
                FileMetadata tile = tiles.Keys.FirstOrDefault(
                    t => Math.Abs(t.OriginLatitude - mainTile.OriginLatitude + yScale * yTileOffset) < float.Epsilon
                    && Math.Abs(t.OriginLongitude - mainTile.OriginLongitude + xTileOffset) < float.Epsilon);

                newX = xTileOffset > 0 ? x % mainTile.Width : (mainTile.Width + x) % mainTile.Width;
                newY = yTileOffset < 0 ? (mainTile.Height + y) % mainTile.Height : y % mainTile.Height;
                return tile;
            }
        }




        private float GetAverageExceptForNoDataValue(float noData, float valueIfAllBad, params float[] values)
        {
            var withValues = values.Where(v => Math.Abs(v - noData) > float.Epsilon);
            if (withValues.Any())
            {
                return withValues.Average();
            }
            else
            {
                return valueIfAllBad;
            }
        }




    }
}
