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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DEM.Net.Core.Interpolation;
using LazyCache;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;

namespace DEM.Net.Core
{
    public class ElevationService
    {
        public const float NO_DATA_OUT = 0;
        private const double EPSILON = 1E-10;
        private readonly RasterService _RasterService;
        private readonly ILogger<ElevationService> _logger;
        private readonly IAppCache _memCache;

        public ElevationService(RasterService rasterService, IAppCache memCache, ILogger<ElevationService> logger = null)
        {
            _RasterService = rasterService;
            _logger = logger;
            _memCache = memCache;
        }


        public string GetDEMLocalPath(DEMDataSet dataSet)
        {
            return _RasterService.GetLocalDEMPath(dataSet);
        }

        /// <summary>
        /// Given a bounding box and a dataset, downloads all covered tiles
        /// using VRT file specified in dataset
        /// </summary>
        /// <param name="dataSet">DEMDataSet used</param>
        /// <param name="bbox">Bounding box, <see cref="GeometryService.GetBoundingBox(string)"/></param>
        /// <remarks>VRT file is downloaded once. It will be cached in local for 30 days.
        /// </remarks>
        public async Task DownloadMissingFilesAsync(DEMDataSet dataSet, BoundingBox bbox = null, CancellationToken cancellationToken = default)
        {
            var report = await _RasterService.GenerateReportAsync(dataSet, bbox, cancellationToken).ConfigureAwait(false);

            if (report == null || report.Count == 0)
            {
                _logger?.LogWarning($"No coverage for bbox {bbox} in {dataSet.Name} dataset.");
                return;
            }

            await DownloadMissingFilesFromReportAsync(report, dataSet, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Given a location and a dataset, downloads all covered tiles
        /// using VRT file specified in dataset
        /// </summary>
        /// <param name="dataSet">DEMDataSet used</param>
        /// <param name="lat">Latitude of location</param>
        /// <param name="lon">Longitude of location</param>
        /// <remarks>VRT file is downloaded once. It will be cached in local for 30 days.
        /// </remarks>
        public async Task DownloadMissingFilesAsync(DEMDataSet dataSet, double lat, double lon, CancellationToken cancellationToken = default)
        {
            var report = await _RasterService.GenerateReportForLocationAsync(dataSet, lat, lon, cancellationToken).ConfigureAwait(false);

            if (report == null)
            {
                _logger?.LogWarning($"No coverage for lat/lon {lat}/{lon} in {dataSet.Name} dataset.");
                return;
            }

            await DownloadMissingFilesFromReportAsync(report, dataSet, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Given a location and a dataset, downloads all covered tiles
        /// using VRT file specified in dataset
        /// </summary>
        /// <param name="dataSet">DEMDataSet used</param>
        /// <param name="geoPoint">GeoPoint</param>
        /// <remarks>VRT file is downloaded once. It will be cached in local for 30 days.
        /// </remarks>
        public Task DownloadMissingFilesAsync(DEMDataSet dataSet, GeoPoint geoPoint, CancellationToken cancellationToken = default)
        {
            if (geoPoint == null)
                return Task.CompletedTask;

            return DownloadMissingFilesAsync(dataSet, geoPoint.Latitude, geoPoint.Longitude, cancellationToken);
        }

        private async Task DownloadMissingFilesFromReportAsync(List<DemFileReport> report, DEMDataSet dataSet, CancellationToken cancellationToken)
        {
            // Generate metadata files if missing (local files: CPU and disk bound)
            var missingMedata = report.Where(r => r.IsMetadataGenerated == false && r.IsExistingLocally == true).ToList();
            int fileCount = missingMedata.Count;
            int doneFileCount = 0;
            Parallel.ForEach(missingMedata, new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = cancellationToken }, file =>
            {
                int done = Interlocked.Increment(ref doneFileCount);
                _RasterService.GenerateFileMetadata(file.LocalName, dataSet.FileFormat, false);

                if (done % 10 == 0)
                    _logger?.LogInformation($"Generating medata {done:N0}/{fileCount:N0} ({(float)done / fileCount:P0})");
            });
            List<DemFileReport> filesToDownload = report.Where(kvp => kvp.IsExistingLocally == false).ToList();

            if (filesToDownload.Count == 0)
            {
                _logger?.LogTrace("No missing file(s).");

                // New metadata files must reach the in-memory / protobuf catalog, otherwise these tiles stay invisible
                // (e.g. adjacent tiles needed for interpolation) until some other download refreshes it.
                if (missingMedata.Count > 0)
                    _RasterService.LoadManifestMetadata(dataSet, true);
                else if (dataSet.DataSource.DataSourceType == Datasets.DEMDataSourceType.XyzTiles && report.Count > 0)
                {
                    // On demand tile caches grow all the time: after an interrupted run the protobuf catalog can miss
                    // tiles that are on disk with their metadata. Missing tiles would silently read as no data.
                    var catalog = new HashSet<string>(_RasterService.LoadManifestMetadata(dataSet, false).Select(m => Path.GetFileName(m.Filename)));
                    if (report.Any(r => !catalog.Contains(Path.GetFileName(r.LocalName))))
                        _RasterService.LoadManifestMetadata(dataSet, true);
                }
                return;
            }

            _logger?.LogInformation($"Downloading {filesToDownload.Count} missing file(s).");
            try
            {
                int numFiles = 0;
                await Parallel.ForEachAsync(filesToDownload, new ParallelOptions { MaxDegreeOfParallelism = 2, CancellationToken = cancellationToken }, async (file, ct) =>
                {
                    await _RasterService.DownloadRasterFileAsync(file, dataSet, ct).ConfigureAwait(false);
                    int done = Interlocked.Increment(ref numFiles);
                    _logger?.LogInformation($"Download progress: {done:N0}/{filesToDownload.Count:N0} ({(float)done / filesToDownload.Count:P0})");
                }).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger?.LogError(ex, $"Error downloading missing files. Check internet connection or retry later. {ex.Message}");
                throw;
            }

            _RasterService.GenerateDirectoryMetadata(dataSet, false, false);
            _RasterService.LoadManifestMetadata(dataSet, true);
        }


        /// <summary>
        /// High level method that retrieves all dataset elevations along given line
        /// </summary>
        /// <param name="lineWKT">Line geometry in WKT</param>
        /// <param name="dataSet">DEM dataset to use</param>
        /// <param name="interpolationMode">Interpolation mode</param>
        /// <param name="behavior">Action to use when no data is found in dataset</param>
        /// <remarks>Output can be BIG, as all elevations will be returned.</remarks>
        /// <returns></returns>
        public List<GeoPoint> GetLineGeometryElevation(string lineWKT, DEMDataSet dataSet, InterpolationMode interpolationMode = InterpolationMode.Bilinear, NoDataBehavior behavior = NoDataBehavior.SetToZero)
        {
            Geometry geom = GeometryService.ParseWKTAsGeometry(lineWKT);

            if (geom.OgcGeometryType == OgcGeometryType.MultiLineString)
            {
                _logger?.LogWarning("Geometry is a multi line string. Only the longest segment will be processed.");
                geom = geom.Geometries().OrderByDescending(g => g.NumPoints).First();
            }
            return GetLineGeometryElevation(geom, dataSet, interpolationMode);
        }

        /// <summary>
        /// High level method that retrieves all dataset elevations along given line
        /// </summary>
        /// <param name="lineStringGeometry">Line geometry</param>
        /// <param name="dataSet">DEM dataset to use</param>
        /// <param name="interpolationMode">Interpolation mode</param>
        /// <param name="behavior">Action to use when no data is found in dataset</param>
        /// <remarks>Output can be BIG, as all elevations will be returned.</remarks>
        /// <returns></returns>
        public List<GeoPoint> GetLineGeometryElevation(Geometry lineStringGeometry, DEMDataSet dataSet, List<FileMetadata> segTiles, List<GeoSegment> nsLines, List<GeoSegment> weLines, InterpolationMode interpolationMode = InterpolationMode.Bilinear, NoDataBehavior behavior = NoDataBehavior.SetToZero)
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

            // Init interpolator
            IInterpolator interpolator = GetInterpolator(interpolationMode);

            var ptStart = lineStringGeometry.Coordinates[0];
            var ptEnd = lineStringGeometry.Coordinates.Last();
            GeoPoint start = new GeoPoint(ptStart.Y, ptStart.X);
            GeoPoint end = new GeoPoint(ptEnd.Y, ptEnd.X);
            double lengthMeters = start.DistanceTo(end);
            float demResolution = dataSet.ResolutionMeters;
            int totalCapacity = 2 * (int)(lengthMeters / demResolution);
            double registrationOffset = dataSet.FileFormat.Registration == DEMFileRegistrationMode.Cell ? 0 : 0.5;

            List<GeoPoint> geoPoints = new List<GeoPoint>(totalCapacity);

            using (RasterFileDictionary adjacentRasters = new RasterFileDictionary())
            {
                bool isFirstSegment = true; // used to return first point only for first segments, for all other segments last point will be returned
                foreach (GeoSegment segment in lineStringGeometry.Segments())
                {
                    // Find all intersection with segment and DEM grid
                    IEnumerable<GeoPoint> intersections = this.FindSegmentIntersections(segment.Start.Longitude
                        , segment.Start.Latitude
                        , segment.End.Longitude
                        , segment.End.Latitude
                        , segTiles
                        , nsLines
                        , weLines
                        , isFirstSegment
                        , registrationOffset
                        , true);

                    // Get elevation for each point
                    intersections = this.GetElevationData(intersections, adjacentRasters, segTiles, interpolator, behavior);

                    // Add to output list
                    geoPoints.AddRange(intersections);

                    isFirstSegment = false;
                }
                //Debug.WriteLine(adjacentRasters.Count);
            }  // Ensures all rasters are properly closed

            return geoPoints;
        }

        /// <summary>
        /// High level method that retrieves all dataset elevations along given line
        /// </summary>
        /// <param name="lineStringGeometry">Line geometry</param>
        /// <param name="dataSet">DEM dataset to use</param>
        /// <param name="interpolationMode">Interpolation mode</param>
        /// <param name="behavior">Action to use when no data is found in dataset</param>
        /// <remarks>Output can be BIG, as all elevations will be returned.</remarks>
        /// <returns></returns>
        public List<GeoPoint> GetLineGeometryElevation(Geometry lineStringGeometry, DEMDataSet dataSet, InterpolationMode interpolationMode = InterpolationMode.Bilinear, NoDataBehavior behavior = NoDataBehavior.SetToZero)
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
            if (dataSet.SRID != Reprojection.SRID_GEODETIC)
            {
                // Grid intersections below are computed in degrees: projected datasets are sampled instead
                return GetProjectedLineElevation(lineStringGeometry, dataSet, interpolationMode, behavior);
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
            float demResolution = dataSet.ResolutionMeters;
            int totalCapacity = 2 * (int)(lengthMeters / demResolution);
            double registrationOffset = dataSet.FileFormat.Registration == DEMFileRegistrationMode.Cell ? 0 : 0.5;

            List<GeoPoint> geoPoints = new List<GeoPoint>(totalCapacity);

            using (RasterFileDictionary adjacentRasters = new RasterFileDictionary())
            {
                bool isFirstSegment = true; // used to return first point only for first segments, for all other segments last point will be returned
                foreach (GeoSegment segment in lineStringGeometry.Segments())
                {
                    // Find all intersection with segment and DEM grid
                    IEnumerable<GeoPoint> intersections = this.FindSegmentIntersections(segment.Start.Longitude
                        , segment.Start.Latitude
                        , segment.End.Longitude
                        , segment.End.Latitude
                        , tiles
                        , isFirstSegment
                        , registrationOffset
                        , true);

                    // Get elevation for each point
                    intersections = this.GetElevationData(intersections, adjacentRasters, tiles, interpolator, behavior);

                    // Add to output list
                    geoPoints.AddRange(intersections);

                    isFirstSegment = false;
                }
                //Debug.WriteLine(adjacentRasters.Count);
            }  // Ensures all rasters are properly closed

            return geoPoints;
        }

        /// <summary>
        /// High level method that retrieves all dataset elevations along given line
        /// </summary>
        /// <param name="lineGeoPoints">List of points that, when joined, makes the input line</param>
        /// <param name="dataSet">DEM dataset to use</param>
        /// <param name="interpolationMode">Interpolation mode</param>
        /// <param name="behavior">Action to use when no data is found in dataset</param>
        /// <remarks>Output can be BIG, as all elevations will be returned.</remarks>
        /// <returns></returns>
        private const int MaxProjectedLineSamples = 100_000;

        /// <summary>
        /// Line elevation for datasets not in WGS84 (e.g. Web Mercator tiles): the WGS84 line is sampled at the dataset
        /// ground resolution and each sample is interpolated. Returned points are in WGS84.
        /// </summary>
        private List<GeoPoint> GetProjectedLineElevation(Geometry lineStringGeometry, DEMDataSet dataSet, InterpolationMode interpolationMode, NoDataBehavior behavior)
        {
            var segments = lineStringGeometry.Segments().ToList();
            double totalMeters = segments.Sum(s => s.Start.DistanceTo(s.End));

            double centerLat = lineStringGeometry.GetBoundingBox().Center[1];
            double step = dataSet.SRID == Reprojection.SRID_PROJECTED_MERCATOR
                ? dataSet.ResolutionMeters * Math.Cos(centerLat * Math.PI / 180d) // Web Mercator pixels shrink with latitude
                : dataSet.ResolutionMeters;
            step = Math.Max(step, totalMeters / MaxProjectedLineSamples);

            var samples = new List<GeoPoint>();
            int id = 0;
            foreach (var segment in segments)
            {
                double length = segment.Start.DistanceTo(segment.End);
                int n = Math.Max(1, (int)Math.Ceiling(length / step));
                for (int i = samples.Count == 0 ? 0 : 1; i <= n; i++)
                {
                    double t = i / (double)n;
                    samples.Add(new GeoPoint(id++,
                        segment.Start.Latitude + (segment.End.Latitude - segment.Start.Latitude) * t,
                        segment.Start.Longitude + (segment.End.Longitude - segment.Start.Longitude) * t));
                }
            }

            var elevated = GetPointsElevation(samples, dataSet, interpolationMode, behavior);
            if (elevated == null)
                return new List<GeoPoint>();

            // back to WGS84, in line order
            var byId = elevated.ToDictionary(p => p.Id);
            foreach (var sample in samples)
            {
                if (byId.TryGetValue(sample.Id, out var p))
                    sample.Elevation = p.Elevation;
            }
            return samples;
        }

        public List<GeoPoint> GetLineGeometryElevation(IEnumerable<GeoPoint> lineGeoPoints, DEMDataSet dataSet, InterpolationMode interpolationMode = InterpolationMode.Bilinear, NoDataBehavior behavior = NoDataBehavior.SetToZero)
        {
            if (lineGeoPoints == null)
                throw new ArgumentNullException(nameof(lineGeoPoints), "Point list is null");

            Geometry geometry = GeometryService.ParseGeoPointAsGeometryLine(lineGeoPoints);

            return GetLineGeometryElevation(geometry, dataSet, interpolationMode, behavior);
        }

        /// <summary>
        /// High level method that retrieves all dataset elevations along given lines
        /// </summary>
        /// <param name="lineGeoPoints">List of points that, when joined, makes the input line</param>
        /// <param name="dataSet">DEM dataset to use</param>
        /// <param name="interpolationMode">Interpolation mode</param>
        /// <param name="behavior">Action to use when no data is found in dataset</param>
        /// <remarks>Output can be BIG, as all elevations will be returned.</remarks>
        /// <returns></returns>
        public Dictionary<TKey, List<GeoPoint>> GetLinesGeometryElevation<TKey>(Dictionary<TKey, List<GeoPoint>> lineGeoPoints, DEMDataSet dataSet, InterpolationMode interpolationMode = InterpolationMode.Bilinear, NoDataBehavior behavior = NoDataBehavior.SetToZero)
        {
            List<Geometry> geoLines = lineGeoPoints.Select(l => GeometryService.ParseGeoPointAsGeometryLine(l.Value)).ToList();
            var bbox = GeometryService.GetBoundingBox(geoLines.First());
            foreach (var linePts in geoLines.Skip(1))
            {
                bbox.UnionWith(GeometryService.GetBoundingBox(linePts));
            }


            List<FileMetadata> tiles = this.GetCoveringFiles(bbox, dataSet);
            double registrationOffset = dataSet.FileFormat.Registration == DEMFileRegistrationMode.Cell ? 0 : 0.5;
            var weLines = GetDEMWestEastLines(tiles, registrationOffset);
            var nsLines = GetDEMNorthSouthLines(tiles, registrationOffset);

            //Dictionary<TKey, List<GeoPoint>> outLines = new Dictionary<TKey, List<GeoPoint>>(lineGeoPoints.Count);
            //foreach (var linePts in lineGeoPoints)
            //{
            //    Geometry geometry = GeometryService.ParseGeoPointAsGeometryLine(linePts.Value);

            //    outLines.Add(linePts.Key, GetLineGeometryElevation(geometry, dataSet, tiles, nsLines, weLines, interpolationMode, behavior));
            //}
            ConcurrentDictionary<TKey, List<GeoPoint>> outLines = new ConcurrentDictionary<TKey, List<GeoPoint>>();
            Parallel.ForEach(lineGeoPoints, linePts =>
            {
                Geometry geometry = GeometryService.ParseGeoPointAsGeometryLine(linePts.Value);

                if (!outLines.TryAdd(linePts.Key, GetLineGeometryElevation(geometry, dataSet, tiles, nsLines, weLines, interpolationMode, behavior)))
                {
                    _logger.LogWarning("Could not add line");
                }
            });
            return outLines.ToDictionary(k => k.Key, v => v.Value);
        }

        /// <summary>
        /// Get elevation for any raster at specified point (in raster coordinate system)
        /// </summary>
        /// <param name="metadata">File metadata, <see cref="IRasterFile.ParseMetaData"/> and <see cref="RasterService.OpenFile(string, DEMFileType)"/></param>
        /// <param name="lat"></param>
        /// <param name="lon"></param>
        /// <param name="interpolator">If null, then Bilinear interpolation will be used</param>
        /// <returns></returns>
        public float GetPointElevation(FileMetadata metadata, double lat, double lon, IInterpolator interpolator = null)
        {
            float heightValue = 0;
            try
            {
                using (IRasterFile raster = _RasterService.OpenFile(metadata.Filename, metadata.FileFormat.Type))
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

        /// <summary>
        /// Get elevation for any raster at specified point (in raster coordinate system)
        /// </summary>
        /// <param name="raster">Raster file, <see cref="RasterService.OpenFile(string, DEMFileType)"/></param>
        /// <param name="metadata">File metadata, <see cref="IRasterFile.ParseMetaData"/></param>
        /// <param name="lat"></param>
        /// <param name="lon"></param>
        /// <param name="interpolator">If null, then Bilinear interpolation will be used</param>
        /// <returns></returns>
        public float GetPointElevation(IRasterFile raster, FileMetadata metadata, double lat, double lon, IInterpolator interpolator = null)
        {
            // Do not dispose Dictionary, as IRasterFile disposal is the caller's responsability
            RasterFileDictionary rasters = new RasterFileDictionary();
            rasters.Add(metadata, raster);
            return GetElevationAtPoint(raster, rasters, metadata, lat, lon, 0, interpolator, NoDataBehavior.UseNoDataDefinedInDem);

        }

        /// <summary>
        /// High level method that retrieves elevation for given point
        /// </summary>
        /// <param name="location">GeoPoint with latitude/longitude</param>
        /// <param name="dataSet">DEM dataset to use</param>
        /// <param name="interpolationMode">Interpolation mode</param>
        /// <returns></returns>
        public GeoPoint GetPointElevation(GeoPoint location, DEMDataSet dataSet, InterpolationMode interpolationMode = InterpolationMode.Bilinear)
        {
            return GetPointElevation(location.Latitude, location.Longitude, dataSet, interpolationMode);
        }

        /// <summary>
        /// High level method that retrieves elevation for given point
        /// </summary>
        /// <param name="lat">Point latitude</param>
        /// <param name="lon">Point longitude</param>
        /// <param name="dataSet">DEM dataset to use</param>
        /// <param name="interpolationMode">Interpolation mode</param>
        /// <returns></returns>
        public GeoPoint GetPointElevation(double lat, double lon, DEMDataSet dataSet, InterpolationMode interpolationMode = InterpolationMode.Bilinear)
        {
            GeoPoint geoPoint = new GeoPoint(lat, lon).ReprojectTo(Reprojection.SRID_GEODETIC, dataSet.SRID);
            FileMetadata tile = this.GetCoveringFile(geoPoint.Latitude, geoPoint.Longitude, dataSet);

            if (tile == null)
            {
                _logger?.LogDebug($"No coverage found matching provided point {geoPoint} for dataset {dataSet.Name}");
                return null;
            }
            else
            {
                IEnumerable<FileMetadata> adjacentRasters = null;
                // for cell registered rasters, we need adjacent tiles for edge locations
                if (dataSet.FileFormat.Registration == DEMFileRegistrationMode.Cell)
                {
                    // construct a bbox around the location
                    adjacentRasters = this.GetCoveringFiles(BoundingBox.AroundPoint(geoPoint.Latitude, geoPoint.Longitude, Math.Abs(tile.PixelScaleX)), dataSet);
                }
                // Init interpolator
                IInterpolator interpolator = GetInterpolator(interpolationMode);

                // TODO : grab adjacent tiles for cell registrered datasets to allow correct interpolation when close to edges
                using (RasterFileDictionary tileCache = new RasterFileDictionary())
                {
                    PopulateRasterFileDictionary(tileCache, tile, _RasterService, adjacentRasters);

                    geoPoint.Elevation = GetElevationAtPoint(tileCache, tile, geoPoint.Latitude, geoPoint.Longitude, 0, interpolator, NoDataBehavior.SetToZero);


                    //Debug.WriteLine(adjacentRasters.Count);
                }  // Ensures all geotifs are properly closed
            }

            // Location is returned as requested (WGS84), not in the dataset projection
            geoPoint.Latitude = lat;
            geoPoint.Longitude = lon;
            return geoPoint;
        }

        /// <summary>
        /// High level method that retrieves elevation for each given point, downloading the missing files first
        /// </summary>
        /// <param name="points">List of points</param>
        /// <param name="dataSet">DEM dataset to use</param>
        /// <param name="interpolationMode">Interpolation mode</param>
        /// <returns>The points with their elevation (the input points are updated), null when the dataset does not cover them</returns>
        public async Task<List<GeoPoint>> GetPointsElevationAsync(IEnumerable<GeoPoint> points, DEMDataSet dataSet, InterpolationMode interpolationMode = InterpolationMode.Bilinear, NoDataBehavior behavior = NoDataBehavior.SetToZero, bool downloadMissingFiles = true, CancellationToken cancellationToken = default)
        {
            if (points == null)
                return null;
            var pointList = points as IReadOnlyCollection<GeoPoint> ?? points.ToList(); // enumerated twice (bbox, then elevations)
            if (downloadMissingFiles)
            {
                BoundingBox bbox = pointList.GetBoundingBox();
                if (bbox.xMin <= bbox.xMax && bbox.yMin <= bbox.yMax) // no points: an empty bbox spans the world (too many tiles to download)
                    await DownloadMissingFilesAsync(dataSet, bbox, cancellationToken).ConfigureAwait(false);
            }
            return GetPointsElevation(pointList, dataSet, interpolationMode, behavior);
        }

        /// <summary>
        /// High level method that retrieves elevation for each given point, from the files already downloaded
        /// (see <see cref="GetPointsElevationAsync"/> to download the missing files)
        /// </summary>
        /// <param name="points">List of points</param>
        /// <param name="dataSet">DEM dataset to use</param>
        /// <param name="interpolationMode">Interpolation mode</param>
        /// <returns>The points with their elevation (the input points are updated), null when the dataset does not cover them</returns>
        public List<GeoPoint> GetPointsElevation(IEnumerable<GeoPoint> points, DEMDataSet dataSet, InterpolationMode interpolationMode = InterpolationMode.Bilinear, NoDataBehavior behavior = NoDataBehavior.SetToZero)
        {
            if (points == null)
                return null;
            var pointList = points as IReadOnlyCollection<GeoPoint> ?? points.ToList(); // enumerated twice (bbox, then elevations)
            BoundingBox bbox = pointList.GetBoundingBox();
            if (!(bbox.xMin <= bbox.xMax && bbox.yMin <= bbox.yMax))
                return new List<GeoPoint>(); // no points
            List<FileMetadata> tiles = this.GetCoveringFiles(bbox.ReprojectTo(4326, dataSet.SRID), dataSet);

            if (tiles.Count == 0)
            {
                _logger?.LogWarning("No coverage for points in dataset. Check extent or spatial reference id (projection)");
                return null;
            }

            // Init interpolator
            IInterpolator interpolator = GetInterpolator(interpolationMode);

            using (RasterFileDictionary adjacentRasters = new RasterFileDictionary())
            {
                // Materialized before the rasters are closed: GetElevationData reads them lazily
                return this.GetElevationData(pointList.ReprojectTo(4326, dataSet.SRID), adjacentRasters, tiles, interpolator, behavior).ToList();
            }
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

        /// <summary>
        /// Generate a tab separated list of points and elevations
        /// </summary>
        /// <param name="lineElevationData"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Returns all elevations in given bbox, downloading the missing files first
        /// </summary>
        /// <param name="bbox">Bounding box (not modified)</param>
        /// <param name="dataSet"></param>
        /// <returns>The height map. Its <see cref="HeightMap.BoundingBox"/> is the bounding box of the data source points.</returns>
        public async Task<HeightMap> GetHeightMapAsync(BoundingBox bbox, DEMDataSet dataSet, bool downloadMissingFiles = true, bool generateMissingData = true, CancellationToken cancellationToken = default)
        {
            if (downloadMissingFiles)
            {
                await DownloadMissingFilesAsync(dataSet, bbox, cancellationToken).ConfigureAwait(false);
            }
            return GetHeightMap(bbox, dataSet, generateMissingData);
        }

        /// <summary>
        /// Returns all elevations in given bbox, from the files already downloaded
        /// (see <see cref="GetHeightMapAsync"/> to download the missing files)
        /// </summary>
        /// <param name="bbox">Bounding box (not modified)</param>
        /// <param name="dataSet"></param>
        /// <returns>The height map. Its <see cref="HeightMap.BoundingBox"/> is the bounding box of the data source points.</returns>
        public HeightMap GetHeightMap(BoundingBox bbox, DEMDataSet dataSet, bool generateMissingData = true)
        {
            bbox = bbox.Clone(); // may be shrunk to the dataset coverage below

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
                bool covered = this.IsBoundingBoxCovered(bbox, bboxMetadata);
                if (!covered && generateMissingData)
                {
                    if (!dataSet.AllowMissingDataGeneration)
                    {
                        bbox.IntersectWith(bboxMetadata.BoundingBox());
                    }
                    else
                    {
                        this._logger.LogWarning("Bounding box is partially covered by DEM dataset. Generating missing tiles as virtual with no_data.");
                        // create missing metadata. Will be marked as "VirtalMetadata"
                        var missingMetadata = CreateMissingTilesMetadata(bbox, dataSet, bboxMetadata);

                        Debug.Assert(missingMetadata.Select(t => t.Filename).Distinct().Count() == missingMetadata.Count, "Non unique tiles. This is BAD");
                        bboxMetadata.AddRange(missingMetadata);
                        //Debug.Assert(this.IsBoundingBoxCovered(bbox, bboxMetadata.Select(m => m.BoundingBox)), "Still uncovered. Missing tiles generation failed");
                    }
                }

                // get height map for each file at bbox
                List<HeightMap> tilesHeightMap = new List<HeightMap>(bboxMetadata.Count);
                foreach (FileMetadata metadata in bboxMetadata)
                {
                    if (metadata.VirtualMetadata)
                    {
                        var hmap = RasterService.GetVirtualHeightMapInBBox(bbox, metadata, dataSet.NoDataValue);
                        hmap.BoundingBox.SRID = bbox.SRID;
                        if (hmap.Count > 0)
                        {
                            tilesHeightMap.Add(hmap);
                        }
                    }
                    else
                    {
                        if (dataSet.FileFormat.Type == DEMFileType.ASCIIGridGzip)
                        {
                            IRasterFile raster = _memCache.GetOrAdd<IRasterFile>(metadata.Filename, () =>
                            {
                                var r = _RasterService.OpenFile(metadata.Filename, dataSet.FileFormat.Type);
                                if (r is ASCIIGridFile asc)
                                    asc.ReadAllFile(metadata);

                                return r;
                            }, DateTimeOffset.Now + TimeSpan.FromSeconds(180), ExpirationMode.ImmediateEviction);
                            var hmap = raster.GetHeightMapInBBox(bbox, metadata, dataSet.NoDataValue);
                            hmap.BoundingBox.SRID = bbox.SRID;
                            if (hmap.Count > 0)
                            {
                                tilesHeightMap.Add(hmap);
                            }
                        }
                        else
                        {
                            using (IRasterFile raster = _RasterService.OpenFile(metadata.Filename, dataSet.FileFormat.Type))
                            {
                                var hmap = raster.GetHeightMapInBBox(bbox, metadata, dataSet.NoDataValue);
                                hmap.BoundingBox.SRID = bbox.SRID;
                                if (hmap.Count > 0)
                                {
                                    tilesHeightMap.Add(hmap);
                                }
                            }
                        }


                    }
                }

                // Stitch tiles (also removes the overlapping seams of grid-registered tiles, e.g. TINItaly)
                HeightMap heightMap = tilesHeightMap.MergeTiles();
                heightMap.BoundingBox.SRID = bbox.SRID;

                return heightMap;

            }

        }

        private List<FileMetadata> CreateMissingTilesMetadata(BoundingBox bbox, DEMDataSet dataSet, List<FileMetadata> bboxMetadata)
        {
            // Take the top left bbox point
            // traverse bbox from left to right and top to bottom with metadata size increments
            // check at each increment if we intersect a tile
            // if no => construct missing tile metadata

            // debug
            //var metadata = bboxMetadata.First();
            //using (IRasterFile raster = _RasterService.OpenFile(metadata.Filename, dataSet.FileFormat.Type))
            //{
            //    var test = raster.ParseMetaData(dataSet.FileFormat);
            //}

            // We suppose all tiles have equal dimensions, let's take the x/y min one to allow easier creation later on
            var tilesBbox = bboxMetadata.BoundingBox();
            FileMetadata templateTile = bboxMetadata.First();

            int registrationOffset = dataSet.FileFormat.Registration == DEMFileRegistrationMode.Grid ? 1 : 0;

            //xWest = Math.Max(0, xWest);
            //xEast = Math.Min(missingTile.Width - 1 - registrationOffset, xEast);
            //yNorth = Math.Max(0, yNorth);
            //ySouth = Math.Min(missingTile.Height - 1 - registrationOffset, ySouth);

            // get x/y increments
            double tileSizeX = ((double)templateTile.Width - registrationOffset) * templateTile.PixelScaleX;
            double tileSizeY = ((double)templateTile.Height - registrationOffset) * templateTile.PixelScaleY;

            // template position in the tile index grid (relative to the covering tiles bbox)
            int templateIndexX = (int)Math.Round((templateTile.BoundingBox.xMin - tilesBbox.xMin) / tileSizeX);
            int templateIndexY = (int)Math.Round((templateTile.BoundingBox.yMin - tilesBbox.yMin) / tileSizeY);

            // output list
            HashSet<FileMetadata> missingTiles = new HashSet<FileMetadata>();

            double x = bbox.xMin, y = bbox.yMin;
            double currentX = x;
            int i = 0, j = 0;
            do
            {
                j = 0;
                currentX = bbox.xMin + i * tileSizeX;
                x = Math.Min(bbox.xMax, currentX);
                y = bbox.yMin;
                double currentY = y;
                do
                {
                    currentY = bbox.yMin + j * tileSizeY;
                    y = Math.Min(bbox.yMax, currentY);

                    if (!bboxMetadata.Any(tile => IsPointInTile(tile, y, x)))
                    {

                        // filename must be unique so we use a Guid
                        FileMetadata missingTile = templateTile.Clone();
                        var xIndex = (int)Math.Floor((x - tilesBbox.xMin) / tileSizeX);
                        var yIndex = (int)Math.Floor((y - tilesBbox.yMin) / tileSizeY);
                        missingTile.Filename = $"[{xIndex}, {yIndex}]";

                        // missing tile
                        _logger.LogInformation($"Generating missing tile at x/y = {x:F5} {y:F5}, index = [{xIndex}, {yIndex}]");



                        // The missing tile is the template tile translated by whole tiles: same internal layout
                        // (data bounds span Width-1 pixels for cell registration, physical bounds...) so that its
                        // samples fall on the same grid as the real tiles and can be stitched to them.
                        double offsetX = tileSizeX * (xIndex - templateIndexX);
                        double offsetY = tileSizeY * (yIndex - templateIndexY);
                        missingTile.DataStartLon = templateTile.DataStartLon + offsetX;
                        missingTile.DataEndLon = templateTile.DataEndLon + offsetX;
                        missingTile.DataStartLat = templateTile.DataStartLat + offsetY;
                        missingTile.DataEndLat = templateTile.DataEndLat + offsetY;
                        missingTile.PhysicalStartLon = templateTile.PhysicalStartLon + offsetX;
                        missingTile.PhysicalEndLon = templateTile.PhysicalEndLon + offsetX;
                        missingTile.PhysicalStartLat = templateTile.PhysicalStartLat + offsetY;
                        missingTile.PhysicalEndLat = templateTile.PhysicalEndLat + offsetY;

                        missingTile.VirtualMetadata = true;

                        if (missingTiles.Contains(missingTile))
                            _logger.LogWarning("Missing tiles already contains tile. Algo must be optimized...");
                        else
                            missingTiles.Add(missingTile);
                    }
                    j++;

                }
                while (currentY < bbox.yMax);

                i++;

            } while (currentX < bbox.xMax);

            _logger.LogInformation($"{missingTiles.Count} missing tiles generated.");
            return missingTiles.ToList();

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
        public bool IsBoundingBoxCovered(BoundingBox bbox, IEnumerable<FileMetadata> bboxMetadata)
        {
            return this.IsBoundingBoxCovered(bbox, bboxMetadata.Select(m => m.BoundingBox));
        }

        public HeightMap GetHeightMap(BoundingBox bbox, string rasterFilePath, DEMFileDefinition format)
        {
            HeightMap heightMap = null;
            using (IRasterFile raster = _RasterService.OpenFile(rasterFilePath, format.Type))
            {
                var metaData = raster.ParseMetaData(format);
                heightMap = raster.GetHeightMapInBBox(bbox, metaData, NO_DATA_OUT);
            }

            return heightMap;
        }

        /// <summary>
        /// Get all elevation for a given raster file
        /// </summary>
        /// <param name="metadata">Raster file metadata. <see cref="GetCoveringFiles(BoundingBox, DEMDataSet, List{FileMetadata})"></see></param>
        /// <returns></returns>
        public HeightMap GetHeightMap(FileMetadata metadata)
        {
            HeightMap map = null;
            using (IRasterFile raster = _RasterService.OpenFile(metadata.Filename, metadata.FileFormat.Type))
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
        public IEnumerable<GeoPoint> GetElevationData(IEnumerable<GeoPoint> intersections, RasterFileDictionary adjacentRasters, List<FileMetadata> segTiles, IInterpolator interpolator, NoDataBehavior behavior)
        {
            // Group by raster file for sequential and faster access
            // Adjacent tiles are only needed near the tile edges: the search (one list per point) is skipped elsewhere
            var noAdjacentTiles = new List<FileMetadata>(0);
            FileMetadata lastTile = null;
            FileMetadata FindTile(GeoPoint point)
            {
                if (lastTile != null && this.IsPointInTile(lastTile, point))
                    return lastTile;
                var tile = segTiles.FirstOrDefault(t => this.IsPointInTile(t, point));
                if (tile != null) lastTile = tile;
                return tile;
            }
            List<FileMetadata> FindAdjacentTiles(FileMetadata tile, GeoPoint point)
            {
                if (segTiles.Count == 1 || (tile != null && this.IsPointFarFromTileEdges(tile, point)))
                    return noAdjacentTiles;
                return segTiles.Where(t => this.IsPointInAdjacentTile(t, point)).ToList();
            }
            var pointsByTileQuery = from point in intersections
                                    let tile = FindTile(point)
                                    let pointTile = new
                                    {
                                        Point = point,
                                        Tile = tile,
                                        AdjacentTiles = FindAdjacentTiles(tile, point)
                                    }
                                    group pointTile by pointTile.Tile into pointsByTile
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
                if (mainTile == null)
                {
                    foreach (var pointile in tilePoints)
                    {
                        GeoPoint current = pointile.Point;
                        current.Elevation = 0;
                        yield return current;
                    }
                }
                else
                {
                    // We open rasters first, then we iterate
                    PopulateRasterFileDictionary(adjacentRasters, mainTile, _RasterService, tilePoints.SelectMany(tp => tp.AdjacentTiles));


                    foreach (var pointile in tilePoints)
                    {
                        GeoPoint current = pointile.Point;
                        lastElevation = this.GetElevationAtPoint(adjacentRasters, mainTile, current.Latitude, current.Longitude, lastElevation, interpolator, behavior);
                        current.Elevation = lastElevation;
                        yield return current;
                    }
                }
            }
        }

        private void PopulateRasterFileDictionary(RasterFileDictionary dictionary, FileMetadata mainTile, RasterService rasterService, IEnumerable<FileMetadata> fileMetadataList)
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
        /// <param name="segTiles">Metadata files <see cref="ElevationService.GetCoveringFiles"/> to see how to get them relative to segment geometry</param>
        /// <param name="returnStartPoint">If true, the segment starting point will be returned. Useful when processing a line segment by segment.</param>
        /// <param name="returnEndPoind">If true, the segment end point will be returned. Useful when processing a line segment by segment.</param>
        /// <returns></returns>
        private List<GeoPoint> FindSegmentIntersections(double startLon, double startLat, double endLon, double endLat, List<FileMetadata> segTiles, List<GeoSegment> nsLines, List<GeoSegment> weLines, bool returnStartPoint, double registrationOffsetPx, bool returnEndPoind)
        {
            List<GeoPoint> segmentPointsWithDEMPoints;
            // Find intersections with north/south lines, 
            // starting form segment western point to easternmost point
            GeoPoint westernSegPoint = startLon < endLon ? new GeoPoint(startLat, startLon) : new GeoPoint(endLat, endLon);
            GeoPoint easternSegPoint = startLon > endLon ? new GeoPoint(startLat, startLon) : new GeoPoint(endLat, endLon);
            GeoSegment inputSegment = new GeoSegment(westernSegPoint, easternSegPoint);

            if (segTiles.Any())
            {
                // grid lines crossed by the segment (the former estimate was a whole tile width + height per segment)
                var firstTile = segTiles[0];
                int estimatedCapacity = (int)Math.Min(int.MaxValue / 2,
                    Math.Abs(endLon - startLon) / Math.Abs(firstTile.pixelSizeX) + Math.Abs(endLat - startLat) / Math.Abs(firstTile.pixelSizeY)) + 4;
                segmentPointsWithDEMPoints = new List<GeoPoint>(estimatedCapacity);
                bool yAxisDown = segTiles.First().pixelSizeY < 0;
                if (yAxisDown == false)
                {
                    throw new NotImplementedException("DEM with y axis upwards not supported.");
                }

                foreach (GeoSegment demSegment in nsLines.Where(l => l.Start.Longitude > startLon && l.Start.Longitude < endLon))
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
                foreach (GeoSegment demSegment in weLines.Where(l => l.Start.Latitude > endLat && l.Start.Latitude < startLat))
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
                segmentPointsWithDEMPoints.Add(new GeoPoint(startLat, startLon));
                //segmentPointsWithDEMPoints.Add(inputSegment.Start);
            }
            if (returnEndPoind)
            {
                segmentPointsWithDEMPoints.Add(new GeoPoint(endLat, endLon));
                //segmentPointsWithDEMPoints.Add(inputSegment.End);
            }

            // sort points in segment order
            //
            segmentPointsWithDEMPoints.Sort(new DistanceFromPointComparer(new GeoPoint(startLat, startLon)));

            return segmentPointsWithDEMPoints;
        }

        /// <summary>
        /// Finds all intersections between given segment and DEM grid
        /// </summary>
        /// <param name="startLon">Segment start longitude</param>
        /// <param name="startLat">Segment start latitude</param>
        /// <param name="endLon">Segment end longitude</param>
        /// <param name="endLat">Segment end latitude</param>
        /// <param name="segTiles">Metadata files <see cref="ElevationService.GetCoveringFiles"/> to see how to get them relative to segment geometry</param>
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
                // grid lines crossed by the segment (the former estimate was a whole tile width + height per segment)
                var firstTile = segTiles[0];
                int estimatedCapacity = (int)Math.Min(int.MaxValue / 2,
                    Math.Abs(endLon - startLon) / Math.Abs(firstTile.pixelSizeX) + Math.Abs(endLat - startLat) / Math.Abs(firstTile.pixelSizeY)) + 4;
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
                segmentPointsWithDEMPoints.Add(new GeoPoint(startLat, startLon));
                //segmentPointsWithDEMPoints.Add(inputSegment.Start);
            }
            if (returnEndPoind)
            {
                segmentPointsWithDEMPoints.Add(new GeoPoint(endLat, endLon));
                //segmentPointsWithDEMPoints.Add(inputSegment.End);
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


                // Lines west of the segment cannot intersect it: start just before its western point instead of the
                // tile edge (a segment used to walk up to a whole tile width of lines). The loop checks the previous
                // line, so it is restored too: the lines yielded are the same as when walking from the edge.
                int firstUsefulIndex = (int)Math.Floor((westernSegPoint.Longitude - startLon) / top.pixelSizeX) - 1;
                if (firstUsefulIndex > curIndex)
                {
                    curIndex = firstUsefulIndex;
                    curPoint.Longitude = startLon + (top.pixelSizeX * (curIndex - 1));
                }

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
                // (pixelSizeY is negative: the first row center is half a pixel south of the north edge)
                double endLat = left.FileFormat.Registration == DEMFileRegistrationMode.Cell
                                ? left.DataEndLat + left.pixelSizeY / 2d
                                : left.DataEndLat;

                // Y Index in tile coords
                int curIndex = (int)Math.Floor((left.PhysicalEndLat - curPoint.Latitude) / left.PixelScaleY - -registrationOffsetPx);
                // Lines north of the segment cannot intersect it: same shortcut as GetDEMNorthSouthLines
                int firstUsefulIndex = (int)Math.Floor((northernSegPoint.Latitude - endLat) / left.pixelSizeY) - 1;
                if (firstUsefulIndex > curIndex)
                {
                    curIndex = firstUsefulIndex;
                    curPoint.Latitude = endLat + (left.pixelSizeY * (curIndex - 1));
                }

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


        private List<GeoSegment> GetDEMNorthSouthLines(List<FileMetadata> segTiles, double registrationOffsetPx)
        {
            // Get the first north west tile and last south east tile. 
            // The lines are bounded by those tiles

            List<GeoSegment> segments = new List<GeoSegment>();

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

                    segments.Add(new GeoSegment(new GeoPoint(top.DataEndLat, curPoint.Longitude), new GeoPoint(bottom.DataStartLat, curPoint.Longitude)));
                    curIndex++;
                }
            }

            return segments;
        }

        private List<GeoSegment> GetDEMWestEastLines(List<FileMetadata> segTiles, double registrationOffsetPx)
        {
            // Get the first north west tile and last south east tile. 
            // The lines are bounded by those tiles
            List<GeoSegment> segments = new List<GeoSegment>();
            foreach (var tilesByY in segTiles.GroupBy(t => t.DataStartLat).OrderByDescending(g => g.Key))
            {
                List<FileMetadata> WETilesOrdered = tilesByY.OrderBy(t => t.DataStartLon).ToList();

                FileMetadata left = WETilesOrdered.First();
                FileMetadata right = WETilesOrdered.Last();

                GeoPoint curPoint = new GeoPoint(left.DataEndLat, left.DataStartLon);

                // For cell registered datasets, DataStart is not matching the start data point. Start data point is at cell center (0.5 pixel off)
                // (pixelSizeY is negative: the first row center is half a pixel south of the north edge)
                double endLat = left.FileFormat.Registration == DEMFileRegistrationMode.Cell
                                ? left.DataEndLat + left.pixelSizeY / 2d
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

                    segments.Add(new GeoSegment(new GeoPoint(curPoint.Latitude, left.DataStartLon), new GeoPoint(curPoint.Latitude, right.DataEndLon)));
                    curIndex++;
                }
            }

            return segments;

        }


        /// <summary>
        /// Retrieves bounding box for the uning of all raster file list
        /// </summary>
        /// <param name="tiles"></param>
        /// <returns></returns>
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
            List<FileMetadata> metadataCatalog = subSet ?? _RasterService.LoadManifestMetadata(dataSet, false);

            // Find files matching coords
            List<FileMetadata> bboxMetadata = new List<FileMetadata>(metadataCatalog.Where(m => IsBboxIntersectingTile(m, bbox)).Distinct());

            if (bboxMetadata.Count == 0)
            {
                _logger?.LogWarning($"No coverage found matching provided bounding box {bbox}.");
                //throw new NoCoverageException(dataSet, bbox, $"No coverage found matching provided bounding box {bbox}.");
            }

            return bboxMetadata;
        }
        public FileMetadata GetCoveringFile(double lat, double lon, DEMDataSet dataSet, List<FileMetadata> subSet = null)
        {
            // Locate which files are needed

            // Load metadata catalog
            List<FileMetadata> metadataCatalog = subSet ?? _RasterService.LoadManifestMetadata(dataSet, false);

            var geoPoint = new GeoPoint(lat, lon);
            // Find files matching coords
            List<FileMetadata> bboxMetadata = new List<FileMetadata>(metadataCatalog.Where(m => IsPointInTile(m, geoPoint)));

            if (bboxMetadata.Count == 0)
            {
                _logger?.LogDebug($"No coverage found matching provided point {geoPoint}.");
                //throw new NoCoverageException(dataSet, lat, lon, $"No coverage found matching provided point {geoPoint}.");
            }
            else if (bboxMetadata.Count > 1)
            {
                _logger?.LogInformation($"One tile expected for a point. Got {bboxMetadata.Count} tiles for {geoPoint}.");
            }

            return bboxMetadata.FirstOrDefault();
        }

        /// <summary>
        /// Performs point / bbox intersection
        /// </summary>
        /// <param name="originLatitude"></param>
        /// <param name="originLongitude"></param>
        /// <param name="bbox"></param>
        /// <returns></returns>
        public bool IsBboxIntersectingTile(FileMetadata tileMetadata, BoundingBox bbox)
        {
            BoundingBox tileBBox = tileMetadata.BoundingBox;
            double tx = EdgeToleranceX(tileMetadata), ty = EdgeToleranceY(tileMetadata);

            return (tileBBox.xMax + tx >= bbox.xMin && tileBBox.xMin - tx <= bbox.xMax) && (tileBBox.yMax + ty >= bbox.yMin && tileBBox.yMin - ty <= bbox.yMax);
        }

        // Adjacent tile bounds are computed independently and can leave a gap of a few ulps between them (Web Mercator
        // tiles: 2e-9 m). A point on the shared edge, such as an OSM vertex cut at a tile edge, then belonged to no tile
        // and got a 0 elevation. A millionth of a pixel closes the gap.
        private static double EdgeToleranceX(FileMetadata tile) => Math.Abs(tile.PixelScaleX) * 1e-6;
        private static double EdgeToleranceY(FileMetadata tile) => Math.Abs(tile.PixelScaleY) * 1e-6;
        public bool IsPointInTile(FileMetadata tileMetadata, GeoPoint point)
        {
            return IsPointInTile(tileMetadata, point.Latitude, point.Longitude);
        }
        public bool IsPointInTile(FileMetadata tileMetadata, double lat, double lon)
        {
            BoundingBox tileBbox = tileMetadata.BoundingBox;
            double tx = EdgeToleranceX(tileMetadata), ty = EdgeToleranceY(tileMetadata);

            return
                (tileBbox.xMax + tx >= lon && tileBbox.xMin - tx <= lon) // isInsideX
              && (tileBbox.yMax + ty >= lat && tileBbox.yMin - ty <= lat); // isInsideY

        }
        // Far enough from the edges for IsPointInAdjacentTile to only match the tile itself (twice its margin)
        private bool IsPointFarFromTileEdges(FileMetadata tile, GeoPoint point)
        {
            double marginX = Math.Abs(tile.PixelScaleX) * 8d;
            double marginY = Math.Abs(tile.PixelScaleY) * 4d;
            BoundingBox tileBbox = tile.BoundingBox;
            return point.Longitude - marginX > tileBbox.xMin && point.Longitude + marginX < tileBbox.xMax
                && point.Latitude - marginY > tileBbox.yMin && point.Latitude + marginY < tileBbox.yMax;
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

        private float GetElevationAtPoint(RasterFileDictionary adjacentTiles, FileMetadata metadata, double lat, double lon, float noDataElevation, IInterpolator interpolator, NoDataBehavior behavior)
        {
            return GetElevationAtPoint(adjacentTiles[metadata], adjacentTiles, metadata, lat, lon, noDataElevation, interpolator ?? GetInterpolator(InterpolationMode.Bilinear), behavior);

        }
        public float GetElevationAtPoint(IRasterFile mainRaster, RasterFileDictionary adjacentTiles, FileMetadata metadata, double lat, double lon, float noDataElevation, IInterpolator interpolator, NoDataBehavior behavior)
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
                }
                else
                {
                    // In cell registration mode, the actual data point is at pixel center
                    // If at pixel center (ending by .5, .5), we are on the data point, so no need for adjacent raster checks
                    yPixel = Math.Sign(metadata.pixelSizeY) == 1 ?
                         ((metadata.PhysicalEndLat + metadata.pixelSizeY / 2) - lat) / metadata.pixelSizeY
                        : (lat - (metadata.PhysicalEndLat + metadata.pixelSizeY / 2)) / metadata.pixelSizeY;
                    xPixel = (lon - (metadata.PhysicalStartLon + metadata.pixelSizeX / 2)) / metadata.pixelSizeX;
                }

                // Weights relative to the north west data point (floor). Pixel coordinates are negative within
                // half a pixel of the west / north edges of cell registered tiles: a modulo would mirror the weight.
                xInterpolationAmount = xPixel - Math.Floor(xPixel);
                yInterpolationAmount = yPixel - Math.Floor(yPixel);


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
                    switch (behavior)
                    {
                        case NoDataBehavior.LastElevation: heightValue = noDataElevation; break;
                        case NoDataBehavior.SetToZero: heightValue = 0; break;
                        case NoDataBehavior.UseNoDataDefinedInDem: heightValue = NO_DATA_OUT; break;
                    }
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

            if (tiles.TryGetValue(goodTile, out IRasterFile raster))
            {
                return raster.GetElevationAtPoint(goodTile, xRemap, yRemap);
            }
            throw new InvalidOperationException("Tile not found. Should not happen.");
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
                // Locate the requested pixel center geographically, then find the adjacent tile holding that sample.
                // (Comparing tile origins assumed 1° tiles and exact tie points, which fails on noisy tie points like
                // AW3D30's 4.9999999999999964, and silently read pixels from the opposite side of the main tile.)
                double lon = PixelCenterLon(mainTile, x);
                double lat = PixelCenterLat(mainTile, y);
                FileMetadata tile = null;
                newX = newY = 0;
                foreach (var t in tiles.Keys)
                {
                    if (t == mainTile) continue;
                    int tx = (int)Math.Round((lon - PixelCenterLon(t, 0)) / t.pixelSizeX);
                    int ty = (int)Math.Round((lat - PixelCenterLat(t, 0)) / t.pixelSizeY);
                    if (tx >= 0 && tx < t.Width && ty >= 0 && ty < t.Height)
                    {
                        tile = t;
                        newX = tx;
                        newY = ty;
                        return tile;
                    }
                }

                // No adjacent tile loaded (dataset edge or missing tile): use the nearest pixel of the main tile.
                // Wrapping the index around would read the opposite side of the tile.
                _logger.LogDebug($"No adjacent tile found for pixel ({x},{y}) of {mainTile}, using nearest edge pixel.");
                newX = Math.Clamp(x, 0, mainTile.Width - 1);
                newY = Math.Clamp(y, 0, mainTile.Height - 1);
                return mainTile;
            }
        }

        // Pixel center coordinates, consistent with the pixel coordinates used for interpolation in GetElevationAtPoint
        private static double PixelCenterLon(FileMetadata tile, int x)
            => tile.FileFormat.Registration == DEMFileRegistrationMode.Cell
                ? tile.PhysicalStartLon + (x + 0.5) * tile.pixelSizeX
                : tile.DataStartLon + x * tile.pixelSizeX;

        private static double PixelCenterLat(FileMetadata tile, int y)
            => tile.FileFormat.Registration == DEMFileRegistrationMode.Cell
                ? tile.PhysicalEndLat + (y + 0.5) * tile.pixelSizeY
                : tile.DataEndLat + y * tile.pixelSizeY;




        /// <summary>Average of the 4 corner values that are not no data (per interpolated point: no array, no LINQ).</summary>
        private static float GetAverageExceptForNoDataValue(float noData, float valueIfAllBad, float a, float b, float c, float d)
        {
            double sum = 0; // same accumulation as Enumerable.Average(IEnumerable<float>)
            int count = 0;
            if (Math.Abs(a - noData) > float.Epsilon) { sum += a; count++; }
            if (Math.Abs(b - noData) > float.Epsilon) { sum += b; count++; }
            if (Math.Abs(c - noData) > float.Epsilon) { sum += c; count++; }
            if (Math.Abs(d - noData) > float.Epsilon) { sum += d; count++; }
            return count == 0 ? valueIfAllBad : (float)(sum / count);
        }

        /// <summary>
        /// High level method reporting ray casting from an origin to a target point,
        /// thus giving information about wether source and target are intervisible or not
        /// </summary>
        /// <param name="source">Source point</param>
        /// <param name="target">Target point</param>
        /// <param name="sourceVerticalOffset">Vertical elevation offset at source point. The line of sight will be calculated from this point (set to 1.8 for simulate a human eye height)</param>
        /// <param name="dataSet">DEM dataset to use</param>
        /// <param name="interpolationMode">Interpolation mode</param>
        /// <remarks>Source and Target are interchangeable. Output can be BIG, as all elevations will be returned.</remarks>
        /// <returns>A report with all obstacles</returns>
        public async Task<IntervisibilityReport> GetIntervisibilityReportAsync(GeoPoint source, GeoPoint target, DEMDataSet dataSet
            , bool downloadMissingFiles = true
            , double sourceVerticalOffset = 0d
            , double targetVerticalOffset = 0d
            , InterpolationMode interpolationMode = InterpolationMode.Bilinear
            , CancellationToken cancellationToken = default)
        {
            try
            {
                var elevationLine = GeometryService.ParseGeoPointAsGeometryLine(source, target);

                if (downloadMissingFiles)
                    await this.DownloadMissingFilesAsync(dataSet, elevationLine.GetBoundingBox(), cancellationToken).ConfigureAwait(false);

                // WGS84 points, whatever the dataset projection
                var geoPoints = this.GetLineGeometryElevation(elevationLine, dataSet, interpolationMode);

                var metrics = geoPoints.ComputeVisibilityMetrics(sourceVerticalOffset, targetVerticalOffset, dataSet.NoDataValue);

                return new IntervisibilityReport(geoPoints, metrics, originVerticalOffset: sourceVerticalOffset, targetVerticalOffset: targetVerticalOffset);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger?.LogError($"{nameof(GetIntervisibilityReportAsync)} error: {ex.Message}");
                throw;
            }

        }

        /// <summary>
        /// Reports ray casting from an origin to a target point,
        /// thus giving information about wether source and target are intervisible or not. The input is a line where all points have elevations computed
        /// </summary>
        /// <param name="linePoints">Line of sight points (returned from <see cref="GetLineGeometryElevation(IEnumerable{GeoPoint}, DEMDataSet, InterpolationMode)"/>).
        /// Points must be all aligned. Non aligned point will return unexpected results.</param>
        /// <param name="sourceVerticalOffset">Vertical elevation offset at source point. The line of sight will be calculated from this point (set to 1.8 for simulate a human eye height)</param>
        /// <param name="targetVerticalOffset">Vertical elevation offset at target point. The line of sight will be calculated from this point (set to 1.8 for simulate a human eye height)</param>
        /// <param name="noDataValue">Value to expect when point has no elevation data available. See <see cref="ElevationMetrics.HasVoids"/></param>
        /// <remarks>Source and Target are interchangeable. Output can be BIG, as all elevations will be returned.</remarks>
        /// <returns>A report with all obstacles</returns>
        public IntervisibilityReport GetIntervisibilityReport(List<GeoPoint> linePoints
            , double sourceVerticalOffset = 0d, double targetVerticalOffset = 0d, double? noDataValue = null)
        {
            try
            {
                var metrics = linePoints.ComputeVisibilityMetrics(sourceVerticalOffset, targetVerticalOffset, noDataValue: noDataValue);

                return new IntervisibilityReport(linePoints, metrics, originVerticalOffset: sourceVerticalOffset, targetVerticalOffset: targetVerticalOffset);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(GetIntervisibilityReport)} error: {ex.Message}");
                throw;
            }
        }


    }
}
