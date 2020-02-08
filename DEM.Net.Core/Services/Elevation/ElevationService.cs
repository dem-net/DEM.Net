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

namespace DEM.Net.Core
{
    public class ElevationService : IElevationService
    {
        public const float NO_DATA_OUT = 0;
        private const double EPSILON = 1E-10;
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

            if (report == null || !report.Any())
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

            DownloadMissingFiles_FromReport(report, dataSet);

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
                    Parallel.ForEach(filesToDownload, file =>
                       {
                           _IRasterService.DownloadRasterFile(file, dataSet);
                       }
                    );

                    _IRasterService.GenerateDirectoryMetadata(dataSet, false, false);
                    _IRasterService.LoadManifestMetadata(dataSet, true);

                }
                catch (AggregateException ex)
                {
                    _logger?.LogError(ex, $"Error downloading missing files. Check internet connection or retry later. {ex.GetInnerMostException().Message}");
                    throw;
                }


            }
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
            double registrationOffset = dataSet.FileFormat.Registration == DEMFileRegistrationMode.Cell ? 0 : 0.5;

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
                        , registrationOffset
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
                using (IRasterFile raster = _IRasterService.OpenFile(metadata.Filename, metadata.FileFormat.Type))
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
            // Do not dispose Dictionary, as IRasterFile disposal is the caller's responsability
            RasterFileDictionary rasters = new RasterFileDictionary();
            rasters.Add(metadata, raster);
            return GetElevationAtPoint(raster, rasters, metadata, lat, lon, 0, interpolator);

        }
        public GeoPoint GetPointElevation(GeoPoint location, DEMDataSet dataSet, InterpolationMode interpolationMode = InterpolationMode.Bilinear)
        {
            return GetPointElevation(location.Latitude, location.Longitude, dataSet, interpolationMode);
        }
        public GeoPoint GetPointElevation(double lat, double lon, DEMDataSet dataSet, InterpolationMode interpolationMode = InterpolationMode.Bilinear)
        {
            GeoPoint geoPoint = new GeoPoint(lat, lon);
            FileMetadata tile = this.GetCoveringFile(lat, lon, dataSet);

            if (tile == null)
            {
                _logger?.LogWarning($"No coverage found matching provided point {geoPoint} for dataset {dataSet.Name}");
                return null;
            }
            else
            {
                IEnumerable<FileMetadata> adjacentRasters = null;
                // for cell registered rasters, we need adjacent tiles for edge locations
                if (dataSet.FileFormat.Registration == DEMFileRegistrationMode.Cell)
                {
                    // construct a bbox around the location
                    adjacentRasters = this.GetCoveringFiles(BoundingBox.AroundPoint(lat, lon, Math.Abs(tile.PixelScaleX)), dataSet);
                }
                // Init interpolator
                IInterpolator interpolator = GetInterpolator(interpolationMode);

                // TODO : grab adjacent tiles for cell registrered datasets to allow correct interpolation when close to edges
                using (RasterFileDictionary tileCache = new RasterFileDictionary())
                {
                    PopulateRasterFileDictionary(tileCache, tile, _IRasterService, adjacentRasters);

                    geoPoint.Elevation = GetElevationAtPoint(tileCache, tile, lat, lon, 0, interpolator);


                    //Debug.WriteLine(adjacentRasters.Count);
                }  // Ensures all geotifs are properly closed
            }

            return geoPoint;
        }
        public IEnumerable<GeoPoint> GetPointsElevation(IEnumerable<GeoPoint> points, DEMDataSet dataSet, InterpolationMode interpolationMode = InterpolationMode.Bilinear, bool downloadMissingFiles = true)
        {
            if (points == null)
                return null;
            IEnumerable<GeoPoint> pointsWithElevation;
            BoundingBox bbox = points.GetBoundingBox();
            if (downloadMissingFiles)
            {
                DownloadMissingFiles(dataSet, bbox);
            }
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

        public HeightMap GetHeightMap(ref BoundingBox bbox, DEMDataSet dataSet)
        {
            DownloadMissingFiles(dataSet, bbox);

            // Locate which files are needed
            // Find files matching coords
            List<FileMetadata> bboxMetadata = GetCoveringFiles(bbox, dataSet);
            if (bboxMetadata.Count == 0)
            {
                string errorMessage = $"Dataset {dataSet.Name} has no coverage for provided bounding box.";
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
                        using (IRasterFile raster = _IRasterService.OpenFile(metadata.Filename, dataSet.FileFormat.Type))
                        {
                            tilesHeightMap.Add(raster.GetHeightMapInBBox(bbox, metadata, NO_DATA_OUT));
                        }
                    }

                    HeightMap heightMap;
                    if (tilesHeightMap.Count == 1)
                    {
                        heightMap = tilesHeightMap.First();
                        bbox = heightMap.BoundingBox;
                    }
                    else
                    {
                        // Merge height maps
                        int totalHeight = tilesHeightMap.GroupBy(h => h.BoundingBox.xMin).Select(g => g.Sum(v => v.Height)).First();
                        int totalWidth = tilesHeightMap.GroupBy(h => h.BoundingBox.yMin).Select(g => g.Sum(v => v.Width)).First();

                        heightMap = new HeightMap(totalWidth, totalHeight);
                        heightMap.BoundingBox = new BoundingBox(xmin: tilesHeightMap.Min(h => h.BoundingBox.xMin)
                                                                , xmax: tilesHeightMap.Max(h => h.BoundingBox.xMax)
                                                                , ymin: tilesHeightMap.Min(h => h.BoundingBox.yMin)
                                                                , ymax: tilesHeightMap.Max(h => h.BoundingBox.yMax));
                        bbox = heightMap.BoundingBox;
                        heightMap.Coordinates = tilesHeightMap.SelectMany(hmap => hmap.Coordinates).Sort();
                        heightMap.Count = totalWidth * totalHeight;
                        heightMap.Minimum = tilesHeightMap.Min(hmap => hmap.Minimum);
                        heightMap.Maximum = tilesHeightMap.Max(hmap => hmap.Maximum);
                    }
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
            return GeometryService.IsCovered(bbox, bboxTiles);
        }

        public HeightMap GetHeightMap(BoundingBox bbox, string rasterFilePath, DEMFileDefinition format)
        {
            HeightMap heightMap = null;
            using (IRasterFile raster = _IRasterService.OpenFile(rasterFilePath, format.Type))
            {
                var metaData = raster.ParseMetaData(format);
                heightMap = raster.GetHeightMapInBBox(bbox, metaData, NO_DATA_OUT);
            }

            return heightMap;
        }
        public HeightMap GetHeightMap(FileMetadata metadata)
        {
            HeightMap map = null;
            using (IRasterFile raster = _IRasterService.OpenFile(metadata.Filename, metadata.FileFormat.Type))
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
            }
        }

        private void PopulateRasterFileDictionary(RasterFileDictionary dictionary, FileMetadata mainTile, IRasterService rasterService, IEnumerable<FileMetadata> fileMetadataList)
        {
            // Add main tile
            if (!dictionary.ContainsKey(mainTile))
            {
                dictionary[mainTile] = rasterService.OpenFile(mainTile.Filename, mainTile.FileFormat.Type);
            }

            if (fileMetadataList != null)
            {
                foreach (var fileMetadata in fileMetadataList)
                {
                    if (!dictionary.ContainsKey(fileMetadata))
                    {
                        dictionary[fileMetadata] = rasterService.OpenFile(fileMetadata.Filename, fileMetadata.FileFormat.Type);
                    }
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
        private List<GeoPoint> FindSegmentIntersections(double startLon, double startLat, double endLon, double endLat, List<FileMetadata> segTiles, bool returnStartPoint, double registrationOffsetPx, bool returnEndPoind)
        {
            List<GeoPoint> segmentPointsWithDEMPoints;
            // Find intersections with north/south lines, 
            // starting form segment western point to easternmost point
            GeoPoint westernSegPoint = startLon < endLon ? new GeoPoint(startLat, startLon) : new GeoPoint(endLat, endLon);
            GeoPoint easternSegPoint = startLon > endLon ? new GeoPoint(startLat, startLon) : new GeoPoint(endLat, endLon);
            GeoSegment inputSegment = new GeoSegment(westernSegPoint, easternSegPoint);

            if (segTiles.Any())
            {
                int estimatedCapacity = (segTiles.Select(t => t.DataStartLon).Distinct().Count() // num horizontal tiles * width
                                        * segTiles.First().Width)
                                        + (segTiles.Select(t => t.DataStartLat).Distinct().Count() // num vertical tiles * height
                                        * segTiles.First().Height);
                segmentPointsWithDEMPoints = new List<GeoPoint>(estimatedCapacity);
                bool yAxisDown = segTiles.First().pixelSizeY < 0;
                if (yAxisDown == false)
                {
                    throw new NotImplementedException("DEM with y axis upwards not supported.");
                }

                foreach (GeoSegment demSegment in this.GetDEMNorthSouthLines(segTiles, westernSegPoint, easternSegPoint, registrationOffsetPx))
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
                foreach (GeoSegment demSegment in this.GetDEMWestEastLines(segTiles, northernSegPoint, southernSegPoint, registrationOffsetPx))
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


        private IEnumerable<GeoSegment> GetDEMNorthSouthLines(List<FileMetadata> segTiles, GeoPoint westernSegPoint, GeoPoint easternSegPoint, double registrationOffsetPx)
        {
            // Get the first north west tile and last south east tile. 
            // The lines are bounded by those tiles

            foreach (var tilesByX in segTiles.GroupBy(t => t.DataStartLon).OrderBy(g => g.Key))
            {
                List<FileMetadata> NSTilesOrdered = tilesByX.OrderByDescending(t => t.DataStartLat).ToList();

                FileMetadata top = NSTilesOrdered.First();
                FileMetadata bottom = NSTilesOrdered.Last();

                // TIP: can optimize here starting with min(westernSegPoint, startlon) but careful !
                GeoPoint curPoint = new GeoPoint(top.DataStartLat, top.DataStartLon);
                // X Index in tile coords
                int curIndex = (int)Math.Ceiling((curPoint.Longitude - top.PhysicalStartLon) / top.PixelScaleX - registrationOffsetPx);

                // For cell registered datasets, DataStart is not matching the start data point. Start data point is at cell center (0.5 pixel off)
                double startLon = top.FileFormat.Registration == DEMFileRegistrationMode.Cell
                    ? top.DataStartLon + top.PixelScaleX / 2d
                    : top.DataStartLon;


                while (IsPointInTile(top, curPoint))
                {
                    if (curIndex >= top.Width)
                    {
                        break;
                    }

                    curPoint.Longitude = startLon + (top.pixelSizeX * curIndex);
                    if (curPoint.Longitude > easternSegPoint.Longitude)
                    {
                        break;
                    }
                    GeoSegment line = new GeoSegment(new GeoPoint(top.DataEndLat, curPoint.Longitude), new GeoPoint(bottom.DataStartLat, curPoint.Longitude));
                    curIndex++;
                    yield return line;
                }
            }
        }

        private IEnumerable<GeoSegment> GetDEMWestEastLines(List<FileMetadata> segTiles, GeoPoint northernSegPoint, GeoPoint southernSegPoint, double registrationOffsetPx)
        {
            // Get the first north west tile and last south east tile. 
            // The lines are bounded by those tiles

            foreach (var tilesByY in segTiles.GroupBy(t => t.DataStartLat).OrderByDescending(g => g.Key))
            {
                List<FileMetadata> WETilesOrdered = tilesByY.OrderBy(t => t.DataStartLon).ToList();

                FileMetadata left = WETilesOrdered.First();
                FileMetadata right = WETilesOrdered.Last();

                GeoPoint curPoint = new GeoPoint(left.DataEndLat, left.DataStartLon);

                // For cell registered datasets, DataStart is not matching the start data point. Start data point is at cell center (0.5 pixel off)
                double endLat = left.FileFormat.Registration == DEMFileRegistrationMode.Cell
                                ? left.DataEndLat + left.PixelScaleY / 2d
                                : left.DataEndLat;

                // Y Index in tile coords
                int curIndex = (int)Math.Floor((left.PhysicalEndLat - curPoint.Latitude) / left.PixelScaleY - -registrationOffsetPx);
                while (IsPointInTile(left, curPoint))
                {
                    if (curIndex >= left.Height)
                    {
                        break;
                    }

                    curPoint.Latitude = endLat + (left.pixelSizeY * curIndex);
                    if (curPoint.Latitude < southernSegPoint.Latitude)
                    {
                        break;
                    }
                    GeoSegment line = new GeoSegment(new GeoPoint(curPoint.Latitude, left.DataStartLon), new GeoPoint(curPoint.Latitude, right.DataEndLon));
                    curIndex++;
                    yield return line;
                }
            }

        }



        public BoundingBox GetTilesBoundingBox(List<FileMetadata> tiles)
        {
            double xmin = tiles.Min(t => t.DataStartLon);
            double xmax = tiles.Max(t => t.PhysicalEndLon);
            double ymin = tiles.Min(t => t.PhysicalEndLat);
            double ymax = tiles.Max(t => t.DataStartLat);
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
        public FileMetadata GetCoveringFile(double lat, double lon, DEMDataSet dataSet, List<FileMetadata> subSet = null)
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
            else if (bboxMetadata.Count > 1)
            {
                _logger?.LogInformation($"One tile expected for a point. Got {bboxMetadata.Count} tiles for {geoPoint}.");
            }

            return bboxMetadata.FirstOrDefault();
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
            return GetElevationAtPoint(adjacentTiles[metadata], adjacentTiles, metadata, lat, lon, lastElevation, interpolator ?? GetInterpolator(InterpolationMode.Bilinear));

        }
        private float GetElevationAtPoint(IRasterFile mainRaster, RasterFileDictionary adjacentTiles, FileMetadata metadata, double lat, double lon, float lastElevation, IInterpolator interpolator)
        {
            float heightValue = 0;
            try
            {
                //const double epsilon = (Double.Epsilon * 100);
                float noData = metadata.NoDataValueFloat;

                double yPixel, xPixel, xInterpolationAmount, yInterpolationAmount;

                // pixel coordinates interpolated
                if (metadata.FileFormat.Registration == DEMFileRegistrationMode.Grid)
                {
                    yPixel = Math.Sign(metadata.pixelSizeY) == 1 ?
                        (metadata.DataEndLat - lat) / metadata.pixelSizeY
                        : (lat - metadata.DataEndLat) / metadata.pixelSizeY;
                    xPixel = (lon - metadata.DataStartLon) / metadata.pixelSizeX;
                    // If at pixel center (ending by .5, .5), we are on the data point, so no need for adjacent raster checks
                    xInterpolationAmount = (double)(xPixel) % 1d;
                    yInterpolationAmount = (double)(yPixel) % 1d;
                }
                else
                {
                    // In cell registration mode, the actual data point is at pixel center
                    // If at pixel center (ending by .5, .5), we are on the data point, so no need for adjacent raster checks
                    yPixel = Math.Sign(metadata.pixelSizeY) == 1 ?
                         ((metadata.PhysicalEndLat + metadata.pixelSizeY / 2) - lat) / metadata.pixelSizeY
                        : (lat - (metadata.PhysicalEndLat + metadata.pixelSizeY / 2)) / metadata.pixelSizeY;
                    xPixel = (lon - (metadata.PhysicalStartLon + metadata.pixelSizeX / 2)) / metadata.pixelSizeX;

                    xInterpolationAmount = Math.Abs((double)(xPixel) % 1d);
                    yInterpolationAmount = Math.Abs((double)(yPixel) % 1d);

                }


                bool xOnDataPoint = Math.Abs(xInterpolationAmount) < EPSILON;
                bool yOnDataPoint = Math.Abs(yInterpolationAmount) < EPSILON;

                // If xOnGrid and yOnGrid, we are on a grid intersection, and that's all
                // TODO fix that
                // When cell registered, this true when interpolation is 0.5 / 0.5
                // When grid registered, this is true 
                if (xOnDataPoint && yOnDataPoint)
                {
                    int x = (int)Math.Round(xPixel, 0);
                    int y = (int)Math.Round(yPixel, 0);
                    var tile = FindTile(metadata, adjacentTiles, x, y, out x, out y);
                    heightValue = mainRaster.GetElevationAtPoint(tile, x, y);
                }
                else
                {
                    int xCeiling = (int)Math.Ceiling(xPixel);
                    int xFloor = (int)Math.Floor(xPixel);
                    int yCeiling = (int)Math.Ceiling(yPixel);
                    int yFloor = (int)Math.Floor(yPixel);
                    // Get 4 grid nearest points (DEM grid corners)

                    // If not yOnGrid and not xOnGrid we are on grid horizontal line
                    // We need elevations for top, bottom, left and right grid points (along x axis and y axis)
                    float northWest = GetElevationAtPoint(metadata, adjacentTiles, xFloor, yFloor, noData);
                    float northEast = GetElevationAtPoint(metadata, adjacentTiles, xCeiling, yFloor, noData);
                    float southWest = GetElevationAtPoint(metadata, adjacentTiles, xFloor, yCeiling, noData);
                    float southEast = GetElevationAtPoint(metadata, adjacentTiles, xCeiling, yCeiling, noData);

                    float avgHeight = GetAverageExceptForNoDataValue(noData, NO_DATA_OUT, southWest, southEast, northWest, northEast);

                    if (northWest == noData) northWest = avgHeight;
                    if (northEast == noData) northEast = avgHeight;
                    if (southWest == noData) southWest = avgHeight;
                    if (southEast == noData) southEast = avgHeight;

                    heightValue = (float)interpolator.Interpolate(southWest, southEast, northWest, northEast, xInterpolationAmount, yInterpolationAmount);
                }

                if (heightValue == NO_DATA_OUT)
                {
                    heightValue = lastElevation;
                }
            }
            catch (Exception e)
            {
                _logger?.LogError(e, $"Error while getting elevation data : {e.Message}{Environment.NewLine}{e.ToString()}");
                throw;
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
                FileMetadata tile = null;
                foreach (var t in tiles.Keys)
                {
                    var xOffset = Math.Abs(t.DataStartLon - mainTile.DataStartLon - xTileOffset);
                    var yOffset = Math.Abs(t.DataStartLat - mainTile.DataStartLat - yScale * yTileOffset);
                    if (xOffset < float.Epsilon && yOffset < float.Epsilon)
                    {
                        tile = t;
                        break;
                    }
                }
                //FileMetadata tile = tiles.Keys.FirstOrDefault(
                //    t => Math.Abs(t.DataStartLat - mainTile.DataStartLat + yScale * yTileOffset) < float.Epsilon
                //    &&  < float.Epsilon);

                if (tile == null)
                {
                    // No adjacent tile found (adjacent tiles may not have been set)
                    if (x == mainTile.Width
                        || y == mainTile.Height)
                    {
                        _logger.LogWarning($"No adjacent tile found (adjacent tiles may not have been set). Returning main tile. (x,y, tile) = ({x},{y},{mainTile})");
                        newX = x == mainTile.Width ? mainTile.Width - 1 : x;
                        newY = y == mainTile.Height ? mainTile.Height - 1 : y;
                        return mainTile;
                    }
                    else
                    {
                        throw new Exception($"No adjacent tile found (adjacent tiles may not have been set). (x,y, tile) = ({x},{y},{mainTile})");
                    }
                }
                else
                {
                    newX = xTileOffset > 0 ? x % mainTile.Width : (mainTile.Width + x) % mainTile.Width;
                    newY = yTileOffset < 0 ? (mainTile.Height + y) % mainTile.Height : y % mainTile.Height;
                    return tile;
                }
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

        public IntervisibilityReport GetIntervisibilityReport(GeoPoint source, GeoPoint target, DEMDataSet dataSet
            , bool downloadMissingFiles = true
            , double sourceVerticalOffset = 0d
            , InterpolationMode interpolationMode = InterpolationMode.Bilinear)
        {
            try
            {
                var elevationLine = GeometryService.ParseGeoPointAsGeometryLine(source, target);

                if (downloadMissingFiles)
                    this.DownloadMissingFiles(dataSet, elevationLine.GetBoundingBox());

                var geoPoints = this.GetLineGeometryElevation(elevationLine, dataSet);

                var metrics = geoPoints.ComputeVisibilityMetrics(sourceVerticalOffset);

                return new IntervisibilityReport(geoPoints, metrics, originVerticalOffset: sourceVerticalOffset);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(GetIntervisibilityReport)} error: {ex.Message}");
                throw;
            }

        }

        public IntervisibilityReport GetIntervisibilityReport(List<GeoPoint> linePoints
            , double sourceVerticalOffset = 0d)
        {
            try
            {
                var metrics = linePoints.ComputeVisibilityMetrics(sourceVerticalOffset);

                return new IntervisibilityReport(linePoints, metrics, originVerticalOffset: sourceVerticalOffset);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(GetIntervisibilityReport)} error: {ex.Message}");
                throw;
            }
        }


    }
}
