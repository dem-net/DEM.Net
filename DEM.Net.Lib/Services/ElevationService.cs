using DEM.Net.Lib.Services.Interpolation;
using Microsoft.SqlServer.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Services
{
    public class ElevationService
    {
        private readonly IGeoTiffService _IGeoTiffService;
        public ElevationService(IGeoTiffService geoTiffService)
        {
            _IGeoTiffService = geoTiffService;
        }


        public string GetDEMLocalPath(DEMDataSet dataSet)
        {
            return _IGeoTiffService.GetLocalDEMPath(dataSet);
        }

        public void DownloadMissingFiles(DEMDataSet dataSet, BoundingBox bbox = null)
        {
            var report = _IGeoTiffService.GenerateReport(dataSet, bbox);
            List<DemFileReport> v_files = new List<DemFileReport>(report.Where(kvp => kvp.Value.IsExistingLocally == false).Select(kvp => kvp.Value));

            if (v_files.Count == 0)
            {
                Trace.TraceInformation("No missing file(s).");
            }
            else
            {
                Trace.TraceInformation($"Downloading {v_files.Count} missing file(s).");
            }
            Parallel.ForEach(v_files, new ParallelOptions { MaxDegreeOfParallelism = 2 }, file =>
            //Parallel.ForEach(v_files, file =>
            {
                using (WebClient wc = new WebClient())
                {
                    // Create directories if not existing
                    new FileInfo(file.LocalName).Directory.Create();

                    Trace.TraceInformation($"Downloading file {file.URL}...");
                    wc.DownloadFile(file.URL, _IGeoTiffService.GetLocalDEMFilePath(dataSet, file.LocalName));
                }
            });

            if (v_files.Any())
            {
                _IGeoTiffService.GenerateDirectoryMetadata(dataSet, false, false);
                _IGeoTiffService.LoadManifestMetadata(dataSet, true);
            }
        }

        /// <summary>
        /// Extract elevation data along line path
        /// </summary>
        /// <param name="lineWKT"></param>
        /// <param name="geoTiffRepository"></param>
        /// <returns></returns>
        public List<GeoPoint> GetLineGeometryElevation(string lineWKT, DEMDataSet dataSet, InterpolationMode interpolationMode = InterpolationMode.Bilinear)
        {
            BoundingBox bbox = GeometryService.GetBoundingBox(lineWKT);
            SqlGeometry geom = GeometryService.ParseWKTAsGeometry(lineWKT);
            List<FileMetadata> tiles = this.GetCoveringFiles(bbox, dataSet);

            // Init interpolator
            IInterpolator interpolator = GetInterpolator(interpolationMode);

            double lengthMeters = GeometryService.GetLength(lineWKT);
            int demResolution = dataSet.ResolutionMeters;
            int totalCapacity = 2 * (int)(lengthMeters / demResolution);

            List<GeoPoint> geoPoints = new List<GeoPoint>(totalCapacity);

            bool isFirstSegment = true; // used to return first point only for first segments, for all other segments last point will be returned
            foreach (SqlGeometry segment in geom.Segments())
            {
                List<FileMetadata> segTiles = this.GetCoveringFiles(segment.GetBoundingBox(), dataSet, tiles);

                // Find all intersection with segment and DEM grid
                List<GeoPoint> intersections = this.FindSegmentIntersections(segment.STStartPoint().STX.Value, segment.STStartPoint().STY.Value,
                                                                                                                                                                segment.STEndPoint().STX.Value, segment.STEndPoint().STY.Value,
                                                                                                                                                                segTiles, isFirstSegment, true);

                // Get elevation for each point
                this.GetElevationData(ref intersections, segTiles, interpolator);

                // Add to output list
                geoPoints.AddRange(intersections);

                isFirstSegment = false;
            }

            return geoPoints;
        }

        //GetLineGeometryElevation_Profiled
        public List<GeoPoint> GetLineGeometryElevation_Profiled(string lineWKT, DEMDataSet dataSet, InterpolationMode interpolationMode = InterpolationMode.Bilinear)
        {
            BoundingBox bbox = GeometryService.GetBoundingBox(lineWKT);
            //HeightMap heightMap = GeoTiffService.GetHeightMap(bbox, geoTiffRepository);
            SqlGeometry geom = GeometryService.ParseWKTAsGeometry(lineWKT);
            List<FileMetadata> tiles = this.GetCoveringFiles(bbox, dataSet);

            // Init interpolator
            IInterpolator interpolator = GetInterpolator(interpolationMode);

            double lengthMeters = GeometryService.GetLength(lineWKT);
            int demResolution = GeoTiffService.GetResolutionMeters(tiles.First());
            int totalCapacity = 2 * (int)(lengthMeters / demResolution);

            List<GeoPoint> geoPoints = new List<GeoPoint>(totalCapacity);

            Stopwatch swEnumSegments = new Stopwatch();
            Stopwatch swCovering = new Stopwatch();
            Stopwatch swInter = new Stopwatch();
            Stopwatch swElevation = new Stopwatch();
            Stopwatch swAdd = new Stopwatch();


            bool isFirstSegment = true; // used to return first point only for first segments, for all other segments last point will be returned
            swEnumSegments.Start();
            foreach (SqlGeometry segment in geom.Segments())
            {
                swEnumSegments.Stop();
                swCovering.Start();
                List<FileMetadata> segTiles = this.GetCoveringFiles(segment.GetBoundingBox(), dataSet, tiles);
                swCovering.Stop();
                swInter.Start();
                // Find all intersection with segment and DEM grid
                List<GeoPoint> intersections = this.FindSegmentIntersections(segment.STStartPoint().STX.Value, segment.STStartPoint().STY.Value,
                                                                                                                                                                segment.STEndPoint().STX.Value, segment.STEndPoint().STY.Value,
                                                                                                                                                                segTiles, isFirstSegment, true);
                swInter.Stop();
                swElevation.Start();
                // Get elevation for each point
                this.GetElevationData(ref intersections, segTiles, interpolator);
                swElevation.Stop();
                swAdd.Start();
                // Add to output list
                geoPoints.AddRange(intersections);
                swAdd.Stop();
                swEnumSegments.Start();
                isFirstSegment = false;
            }
            swEnumSegments.Stop();
            swCovering.Stop();
            swInter.Stop();
            swElevation.Stop();
            swAdd.Stop();
            return geoPoints;
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
            GeoPoint refPoint = lineElevationData.First();
            foreach (GeoPoint pt in lineElevationData)
            {
                sb.AppendLine($"{pt.Longitude.ToString(CultureInfo.InvariantCulture)}\t{pt.Latitude.ToString(CultureInfo.InvariantCulture)}\t{pt.DistanceFromOriginMeters.ToString("F2")}\t{pt.Altitude}");
            }
            return sb.ToString();
        }

        public HeightMap GetHeightMap(BoundingBox bbox, DEMDataSet dataSet)
        {
            // Locate which files are needed
            // Find files matching coords
            List<FileMetadata> bboxMetadata = GetCoveringFiles(bbox, dataSet);

            HeightMap heightMap = null;
            // get height map for each file at bbox
            foreach (FileMetadata metadata in bboxMetadata)
            {
                using (GeoTiff geoTiff = new GeoTiff(metadata.Filename))
                {
                    heightMap = this.ParseGeoDataInBBox(geoTiff, bbox, metadata);
                }
            }

            FileMetadata meta = bboxMetadata.First();
            using (GeoTiff geoTiff = new GeoTiff(meta.Filename))
            {
                heightMap = this.ParseGeoDataInBBox(geoTiff, bbox, meta);
            }
            return heightMap;
        }
        public static HeightMap GetHeightMap(string fileName, FileMetadata metadata)
        {
            fileName = Path.GetFullPath(fileName);
            string fileTitle = Path.GetFileNameWithoutExtension(fileName);

            HeightMap heightMap = null;
            using (GeoTiff tiff = new GeoTiff(fileName))
            {
                heightMap = ParseGeoData(tiff, metadata);
            }
            return heightMap;
        }
        private static HeightMap ParseGeoData(GeoTiff tiff, FileMetadata metadata)
        {
            HeightMap heightMap = new HeightMap(metadata.Width, metadata.Height);

            byte[] scanline = new byte[metadata.ScanlineSize];
            ushort[] scanline16Bit = new ushort[metadata.ScanlineSize / 2];
            Buffer.BlockCopy(scanline, 0, scanline16Bit, 0, scanline.Length);

            for (int y = 0; y < metadata.Height; y++)
            {
                tiff.TiffFile.ReadScanline(scanline, y);
                Buffer.BlockCopy(scanline, 0, scanline16Bit, 0, scanline.Length);

                double latitude = metadata.StartLat + (metadata.pixelSizeY * y);
                for (int x = 0; x < scanline16Bit.Length; x++)
                {
                    double longitude = metadata.StartLon + (metadata.pixelSizeX * x);

                    float heightValue = (float)scanline16Bit[x];
                    if (heightValue < 32768)
                    {
                        heightMap.Mininum = Math.Min(metadata.MininumAltitude, heightValue);
                        heightMap.Maximum = Math.Max(metadata.MaximumAltitude, heightValue);
                    }
                    else
                    {
                        heightValue = -10000;
                    }
                    heightMap.Coordinates.Add(new GeoPoint(latitude, longitude, heightValue, x, y));

                }
            }

            return heightMap;
        }


        /// <summary>
        /// Fill altitudes for each GeoPoint provided, opening as few GeoTiffs as possible
        /// </summary>
        /// <param name="intersections"></param>
        /// <param name="segTiles"></param>
        public void GetElevationData(ref List<GeoPoint> intersections, List<FileMetadata> segTiles, IInterpolator interpolator)
        {
            // Group by tiff file for sequential and faster access
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


            try
            {
                // To interpolate well points close to tile edges, we need all adjacent tiles
                using (GeoTiffDictionary adjacentGeoTiffs = new GeoTiffDictionary())
                {
                    // For each group (key = tile, values = points within this tile)
                    // TIP: test use of Parallel (warning : a lot of files may be opened at the same time)
                    foreach (var tilePoints in pointsByTileQuery)
                    {
                        // Get the tile
                        FileMetadata mainTile = tilePoints.Key;


                        // We open geotiffs first, then we iterate
                        PopulateGeoTiffDictionary(adjacentGeoTiffs, mainTile, _IGeoTiffService, tilePoints.SelectMany(tp => tp.AdjacentTiles));

                        foreach (var pointile in tilePoints)
                        {
                            GeoPoint current = pointile.Point;
                            current.Altitude = this.ParseGeoDataAtPoint(adjacentGeoTiffs, mainTile, current.Latitude, current.Longitude, interpolator);
                        }

                        adjacentGeoTiffs.Clear();

                    } // Ensures all geotifs are properly closed
                }
            }

            catch (Exception e)
            {
                Trace.TraceError($"Error while getting elevation data : {e.Message}{Environment.NewLine}{e.ToString()}");
            }

        }

        private void PopulateGeoTiffDictionary(GeoTiffDictionary dictionary, FileMetadata mainTile, IGeoTiffService geoTiffService, IEnumerable<FileMetadata> fileMetadataList)
        {
            // Add main tile
            dictionary[mainTile] = geoTiffService.OpenFile(mainTile.Filename);

            foreach (var fileMetadata in fileMetadataList)
            {
                if (!dictionary.ContainsKey(fileMetadata))
                {
                    dictionary[fileMetadata] = geoTiffService.OpenFile(fileMetadata.Filename);
                }
            }
        }

        private bool IsPointInAdjacentTile(FileMetadata tile, GeoPoint point)
        {
            bool isInsideY = tile.OriginLatitude + 1 >= point.Latitude && (tile.OriginLatitude - 2) <= point.Latitude;
            bool isInsideX = (tile.OriginLongitude + 2) >= point.Longitude && (tile.OriginLongitude - 2) <= point.Longitude;
            bool isInside = isInsideX && isInsideY;
            return isInside;
        }

        /// <summary>
        /// Finds all intersections between given segment and DEM grid
        /// </summary>
        /// <param name="startLon">Segment start longitude</param>
        /// <param name="startLat">Segment start latitude</param>
        /// <param name="endLon">Segment end longitude</param>
        /// <param name="endLat">Segment end latitude</param>
        /// <param name="segTiles">Metadata files <see cref="GeoTiffService.GetCoveringFiles"/> to see how to get them relative to segment geometry</param>
        /// <param name="returnStartPoint">If true, the segment starting point will be returned. Useful when processing a line segment by segment.</param>
        /// <param name="returnEndPoind">If true, the segment end point will be returned. Useful when processing a line segment by segment.</param>
        /// <returns></returns>
        public List<GeoPoint> FindSegmentIntersections(double startLon, double startLat, double endLon, double endLat, List<FileMetadata> segTiles, bool returnStartPoint, bool returnEndPoind)
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
                    GeoPoint intersectionPoint = null;
                    if (GeometryService.LineLineIntersection(out intersectionPoint, inputSegment, demSegment))
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
                    GeoPoint intersectionPoint = null;
                    if (GeometryService.LineLineIntersection(out intersectionPoint, inputSegment, demSegment))
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


        public IEnumerable<GeoSegment> GetDEMNorthSouthLines(List<FileMetadata> segTiles, GeoPoint westernSegPoint, GeoPoint easternSegPoint)
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

        public IEnumerable<GeoSegment> GetDEMWestEastLines(List<FileMetadata> segTiles, GeoPoint northernSegPoint, GeoPoint southernSegPoint)
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
        public BoundingBox GetTileBoundingBox(FileMetadata tile)
        {
            double xmin = tile.OriginLongitude;
            double xmax = tile.EndLongitude;
            double ymin = tile.EndLatitude;
            double ymax = tile.OriginLatitude;
            return new BoundingBox(xmin, xmax, ymin, ymax);
        }

        public List<FileMetadata> GetCoveringFiles(BoundingBox bbox, DEMDataSet dataSet, List<FileMetadata> subSet = null)
        {
            // Locate which files are needed

            // Load metadata catalog
            List<FileMetadata> metadataCatalog = subSet ?? _IGeoTiffService.LoadManifestMetadata(dataSet, false);

            // Find files matching coords
            List<FileMetadata> bboxMetadata = new List<FileMetadata>(metadataCatalog.Where(m => IsBboxInTile(m.OriginLatitude, m.OriginLongitude, bbox)));

            if (bboxMetadata.Count == 0)
            {
                Trace.TraceWarning($"No coverage found matching provided bounding box { bbox}.");
                //throw new Exception($"No coverage found matching provided bounding box {bbox}.");
            }

            return bboxMetadata;
        }

        public bool IsBboxInTile(double originLatitude, double originLongitude, BoundingBox bbox)
        {
            bool isInsideY = originLatitude >= bbox.yMin && (originLatitude - 1) <= bbox.yMax;
            bool isInsideX = (originLongitude + 1) >= bbox.xMin && originLongitude <= bbox.xMax;
            bool isInside = isInsideX && isInsideY;
            return isInside;
        }
        public bool IsPointInTile(FileMetadata tileMetadata, GeoPoint point)
        {
            return IsPointInTile(tileMetadata.OriginLatitude, tileMetadata.OriginLongitude, point);
        }
        public bool IsPointInTile(double originLatitude, double originLongitude, GeoPoint point)
        {
            bool isInsideY = originLatitude >= point.Latitude && (originLatitude - 1) <= point.Latitude;
            bool isInsideX = (originLongitude + 1) >= point.Longitude && originLongitude <= point.Longitude;
            bool isInside = isInsideX && isInsideY;
            return isInside;
        }

        public HeightMap ParseGeoDataInBBox(GeoTiff tiff, BoundingBox bbox, FileMetadata metadata)
        {
            HeightMap heightMap = new HeightMap(metadata.Width, metadata.Height);

            byte[] scanline = new byte[metadata.ScanlineSize];
            ushort[] scanline16Bit = new ushort[metadata.ScanlineSize / 2];
            Buffer.BlockCopy(scanline, 0, scanline16Bit, 0, scanline.Length);


            int yStart = (int)Math.Floor((bbox.yMax - metadata.StartLat) / metadata.pixelSizeY);
            int yEnd = (int)Math.Ceiling((bbox.yMin - metadata.StartLat) / metadata.pixelSizeY);
            int xStart = (int)Math.Floor((bbox.xMin - metadata.StartLon) / metadata.pixelSizeX);
            int xEnd = (int)Math.Ceiling((bbox.xMax - metadata.StartLon) / metadata.pixelSizeX);

            xStart = Math.Max(0, xStart);
            xEnd = Math.Min(scanline16Bit.Length - 1, xEnd);
            yStart = Math.Max(0, yStart);
            yEnd = Math.Min(metadata.Height - 1, yEnd);

            for (int y = yStart; y <= yEnd; y++)
            {
                tiff.TiffFile.ReadScanline(scanline, y);
                Buffer.BlockCopy(scanline, 0, scanline16Bit, 0, scanline.Length);

                double latitude = metadata.StartLat + (metadata.pixelSizeY * y);
                for (int x = xStart; x <= xEnd; x++)
                {
                    double longitude = metadata.StartLon + (metadata.pixelSizeX * x);

                    float heightValue = (float)scanline16Bit[x];
                    if (heightValue < 32768)
                    {
                        heightMap.Mininum = Math.Min(heightMap.Mininum, heightValue);
                        heightMap.Maximum = Math.Max(heightMap.Maximum, heightValue);
                    }
                    else
                    {
                        heightValue = -10000;
                    }
                    heightMap.Coordinates.Add(new GeoPoint(latitude, longitude, heightValue, x, y));

                }
            }

            return heightMap;
        }

        public float ParseGeoDataAtPoint(GeoTiffDictionary adjacentTiles, FileMetadata metadata, double lat, double lon, IInterpolator interpolator)
        {
            float heightValue = 0;
            try
            {

                IGeoTiff mainTiff = adjacentTiles[metadata];

                //const double epsilon = (Double.Epsilon * 100);
                float noData = float.Parse(metadata.NoDataValue);
                const float NO_DATA_OUT = -100;

                // precise position on the grid (with commas)
                double ypos = (lat - metadata.StartLat) / metadata.pixelSizeY;
                double xpos = (lon - metadata.StartLon) / metadata.pixelSizeX;

                // If pure integers, then it's on the grid
                float xInterpolationAmount = (float)xpos % 1;
                float yInterpolationAmount = (float)ypos % 1;

                bool xOnGrid = xInterpolationAmount == 0;
                bool yOnGrid = yInterpolationAmount == 0;

                // If xOnGrid and yOnGrid, we are on a grid intersection, and that's all
                if (xOnGrid && yOnGrid)
                {
                    int x = (int)Math.Round(xpos, 0);
                    int y = (int)Math.Round(ypos, 0);
                    var tile = FindTile(metadata, adjacentTiles, x, y, out x, out y);
                    heightValue = mainTiff.ParseGeoDataAtPoint(tile, x, y);
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
                    float northWest  = GetElevationAtPoint(metadata, adjacentTiles, xFloor, yFloor, NO_DATA_OUT);
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
            }
            catch (Exception e)
            {
                Trace.TraceError($"Error while getting elevation data : {e.Message}{Environment.NewLine}{e.ToString()}");
            }
            return heightValue;
        }

        private float GetElevationAtPoint(FileMetadata mainTile, GeoTiffDictionary tiles, int x, int y, float nullValue)
        {
            int xRemap, yRemap;
            FileMetadata goodTile = FindTile(mainTile, tiles, x, y, out xRemap, out yRemap);
            if (goodTile == null )
            {
                return nullValue;
            }

            if (tiles.ContainsKey(goodTile))
            {
                return tiles[goodTile].ParseGeoDataAtPoint(goodTile, xRemap, yRemap);

            }
            else
            {
                throw new Exception("Tile not found. Should not happen.");
            }

        }

        private FileMetadata FindTile(FileMetadata mainTile, GeoTiffDictionary tiles, int x, int y, out int newX, out int newY)
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
                    t => t.OriginLatitude == mainTile.OriginLatitude + yScale * yTileOffset
                    && t.OriginLongitude == mainTile.OriginLongitude + xTileOffset);
                newX = xTileOffset > 0 ? x % mainTile.Width : (mainTile.Width + x) % mainTile.Width;
                newY = yTileOffset < 0 ? (mainTile.Height + y) % mainTile.Height : y % mainTile.Height;
                return tile;
            }
        }




        public float GetAverageExceptForNoDataValue(float noData, float valueIfAllBad, params float[] values)
        {
            var withValues = values.Where(v => v != noData);
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
