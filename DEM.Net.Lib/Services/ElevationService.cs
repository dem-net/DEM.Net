using BitMiracle.LibTiff.Classic;
using Microsoft.SqlServer.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Services
{
    public static class ElevationService
    {
        /// <summary>
        /// Extract elevation data along line path
        /// </summary>
        /// <param name="lineWKT"></param>
        /// <param name="geoTiffRepository"></param>
        /// <returns></returns>
        public static List<GeoPoint> GetLineGeometryElevation(string lineWKT, string geoTiffRepository)
        {
            BoundingBox bbox = GeometryService.GetBoundingBox(lineWKT);
            //HeightMap heightMap = GeoTiffService.GetHeightMap(bbox, geoTiffRepository);
            SqlGeometry geom = GeometryService.GetNativeGeometry(lineWKT);
            List<FileMetadata> tiles = ElevationService.GetCoveringFiles(bbox, geoTiffRepository);

            double lengthMeters = GeometryService.GetLength(lineWKT);
            int demResolution = GeoTiffService.GetResolutionMeters(tiles.First());
            int totalCapacity = 2 * (int)(lengthMeters / demResolution);

            List<GeoPoint> geoPoints = new List<GeoPoint>(totalCapacity);

            bool isFirstSegment = true; // used to return first point only for first segments, for all other segments last point will be returned
            foreach (SqlGeometry segment in geom.Segments())
            {
                List<FileMetadata> segTiles = ElevationService.GetCoveringFiles(segment.GetBoundingBox(), geoTiffRepository, tiles);

                // Find all intersection with segment and DEM grid
                List<GeoPoint> intersections = ElevationService.FindSegmentIntersections(segment.STStartPoint().STX.Value, segment.STStartPoint().STY.Value,
                                                                                        segment.STEndPoint().STX.Value, segment.STEndPoint().STY.Value,
                                                                                        segTiles, isFirstSegment, true);

                // Get elevation for each point
                ElevationService.GetElevationData(ref intersections, segTiles);

                // Add to output list
                geoPoints.AddRange(intersections);

                isFirstSegment = false;
            }

            return geoPoints;
        }

        public static string ExportElevationTable(List<GeoPoint> lineElevationData)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("X;Z");
            GeoPoint refPoint = lineElevationData.First();
            foreach (GeoPoint pt in lineElevationData)
            {
                sb.AppendLine($"{pt.DistanceSquaredTo(refPoint).ToString("F7")};{pt.Altitude}");
            }
            return sb.ToString();
        }

        public static HeightMap GetHeightMap(BoundingBox bbox, string tiffPath)
        {
            // Locate which files are needed
            // Find files matching coords
            List<FileMetadata> bboxMetadata = GetCoveringFiles(bbox, tiffPath);

            HeightMap heightMap = null;
            // get height map for each file at bbox
            foreach (FileMetadata metadata in bboxMetadata)
            {
                using (GeoTiff geoTiff = new GeoTiff(metadata.Filename))
                {
                    heightMap = ElevationService.ParseGeoDataInBBox(geoTiff, bbox, metadata);
                }
            }

            FileMetadata meta = bboxMetadata.First();
            using (GeoTiff geoTiff = new GeoTiff(meta.Filename))
            {
                heightMap = ElevationService.ParseGeoDataInBBox(geoTiff, bbox, meta);
            }
            return heightMap;
        }

        /// <summary>
        /// Fill altitudes for each GeoPoint provided
        /// </summary>
        /// <param name="intersections"></param>
        /// <param name="segTiles"></param>
        public static void GetElevationData(ref List<GeoPoint> intersections, List<FileMetadata> segTiles)
        {
            // Group by tiff file for sequential and faster access
            var pointsByTileQuery = from point in intersections
                                    let pointTile = new { Point = point, Tile = segTiles.First(t => ElevationService.IsPointInTile(t, point)) }
                                    group pointTile by pointTile.Tile into pointsByTile
                                    select pointsByTile;

            foreach (var tilePoints in pointsByTileQuery)
            {
                FileMetadata tile = tilePoints.Key;
                using (GeoTiff tiff = new GeoTiff(tile.Filename))
                {
                    foreach (GeoPoint pt in tilePoints.Select(a => a.Point))
                    {
                        pt.Altitude = ElevationService.ParseGeoDataAtPoint(tiff.TiffFile, tile, pt.Latitude, pt.Longitude);
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
        /// <param name="segTiles">Metadata files <see cref="GeoTiffService.GetCoveringFiles"/> to see how to get them relative to segment geometry</param>
        /// <param name="returnStartPoint">If true, the segment starting point will be returned. Useful when processing a line segment by segment.</param>
        /// <param name="returnEndPoind">If true, the segment end point will be returned. Useful when processing a line segment by segment.</param>
        /// <returns></returns>
        public static List<GeoPoint> FindSegmentIntersections(double startLon, double startLat, double endLon, double endLat, List<FileMetadata> segTiles,
                                                                                                                    bool returnStartPoint, bool returnEndPoind)
        {
            int estimatedCapacity = (segTiles.Select(t => t.OriginLongitude).Distinct().Count() // num horizontal tiles * width
                                                                                                            * segTiles.First().Width)
                                                                                                            + (segTiles.Select(t => t.OriginLatitude).Distinct().Count() // num vertical tiles * height
                                                                                                            * segTiles.First().Height);
            List<GeoPoint> segmentPointsWithDEMPoints = new List<GeoPoint>(estimatedCapacity);
            bool yAxisDown = segTiles.First().pixelSizeY < 0;
            if (yAxisDown == false)
            {
                throw new NotImplementedException("DEM with y axis upwards not supported.");
            }

            // Find intersections with north/south lines, 
            // starting form segment western point to easternmost point
            GeoPoint westernSegPoint = startLon < endLon ? new GeoPoint(startLat, startLon) : new GeoPoint(endLat, endLon);
            GeoPoint easternSegPoint = startLon > endLon ? new GeoPoint(startLat, startLon) : new GeoPoint(endLat, endLon);
            GeoSegment inputSegment = new GeoSegment(westernSegPoint, easternSegPoint);

            foreach (GeoSegment demSegment in ElevationService.GetDEMNorthSouthLines(segTiles, westernSegPoint, easternSegPoint))
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
            foreach (GeoSegment demSegment in ElevationService.GetDEMWestEastLines(segTiles, northernSegPoint, southernSegPoint))
            {
                GeoPoint intersectionPoint = null;
                if (GeometryService.LineLineIntersection(out intersectionPoint, inputSegment, demSegment))
                {
                    segmentPointsWithDEMPoints.Add(intersectionPoint);
                }
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

        /// <summary>
        /// Points will be sorted from closest to referencePoint to farthest from referencePoint
        /// </summary>
        /// <param name="points"></param>
        /// <param name="referencePoint"></param>
        private static void SortPointByDistance(ref List<GeoPoint> points, GeoPoint referencePoint)
        {

        }

        private static IEnumerable<GeoSegment> GetDEMNorthSouthLines(List<FileMetadata> segTiles, GeoPoint westernSegPoint, GeoPoint easternSegPoint)
        {
            BoundingBox tilesBbox = GetTilesBoundingBox(segTiles);

            FileMetadata curTile = segTiles.First(tile => IsPointInTile(tile, westernSegPoint));

            GeoPoint curPoint = westernSegPoint.Clone();
            // X Index in tile coords
            int curIndex = (int)Math.Ceiling((curPoint.Longitude - curTile.StartLon) / curTile.PixelScaleX);
            while (IsPointInTile(curTile, curPoint))
            {
                if (curIndex >= curTile.Width)
                {
                    double longitude = curTile.StartLon + (curTile.pixelSizeX * curIndex);
                    if (longitude > easternSegPoint.Longitude)
                    {
                        break;
                    }
                    curPoint.Longitude = longitude;
                    curTile = segTiles.FirstOrDefault(tile => IsPointInTile(tile, curPoint));
                    if (curTile == null)
                    {
                        throw new Exception("Should not happen, as we check bounds with easternSegPoint.lon");
                    }
                    curIndex = 0;
                }

                curPoint.Longitude = curTile.StartLon + (curTile.pixelSizeX * curIndex);
                if (curPoint.Longitude > easternSegPoint.Longitude)
                {
                    break;
                }
                GeoSegment line = new GeoSegment(new GeoPoint(curTile.OriginLatitude, curPoint.Longitude)
                                                                                                                                                , new GeoPoint(curTile.EndLatitude, curPoint.Longitude));

                curIndex++;
                yield return line;
            }
        }
        private static IEnumerable<GeoSegment> GetDEMWestEastLines(List<FileMetadata> segTiles, GeoPoint northernSegPoint, GeoPoint southernSegPoint)
        {
            BoundingBox tilesBbox = GetTilesBoundingBox(segTiles);

            FileMetadata curTile = segTiles.First(tile => IsPointInTile(tile, northernSegPoint));

            GeoPoint curPoint = northernSegPoint.Clone();
            // Y Index in tile coords
            int curIndex = (int)Math.Ceiling((curTile.StartLat - curPoint.Latitude) / curTile.PixelScaleY);
            while (IsPointInTile(curTile, curPoint))
            {
                if (curIndex >= curTile.Width)
                {
                    double latitude = curTile.StartLat + (curTile.pixelSizeY * curIndex);
                    if (latitude < southernSegPoint.Latitude)
                    {
                        break;
                    }
                    curPoint.Latitude = latitude;
                    curTile = segTiles.FirstOrDefault(tile => IsPointInTile(tile, curPoint));
                    if (curTile == null)
                    {
                        throw new Exception("Should not happen, as we check bounds with southernSegPoint.lat");
                    }
                    curIndex = 0;
                }

                curPoint.Latitude = curTile.StartLat + (curTile.pixelSizeY * curIndex);
                if (curPoint.Latitude < southernSegPoint.Latitude)
                {
                    break;
                }
                GeoSegment line = new GeoSegment(new GeoPoint(curPoint.Latitude, curTile.OriginLongitude)
                                                                                                                                                , new GeoPoint(curPoint.Latitude, curTile.EndLongitude));

                curIndex++;
                yield return line;
            }
        }


        public static BoundingBox GetTilesBoundingBox(List<FileMetadata> tiles)
        {
            double xmin = tiles.Min(t => t.OriginLongitude);
            double xmax = tiles.Max(t => t.EndLongitude);
            double ymin = tiles.Min(t => t.EndLatitude);
            double ymax = tiles.Max(t => t.OriginLatitude);
            return new BoundingBox(xmin, xmax, ymin, ymax);
        }

        public static BoundingBox GetSegmentBoundingBox(double xStart, double yStart, double xEnd, double yEnd)
        {
            double xmin = Math.Min(xStart, xEnd);
            double xmax = Math.Max(xStart, xEnd);
            double ymin = Math.Min(yStart, yEnd);
            double ymax = Math.Max(yStart, yEnd);
            return new BoundingBox(xmin, xmax, ymin, ymax);
        }

        public static List<FileMetadata> GetCoveringFiles(BoundingBox bbox, string tiffPath, List<FileMetadata> subSet = null)
        {
            // Locate which files are needed

            // Load metadata catalog
            List<FileMetadata> metadataCatalog = subSet ?? GeoTiffService.LoadManifestMetadata(tiffPath);

            // Find files matching coords
            List<FileMetadata> bboxMetadata = new List<FileMetadata>(metadataCatalog.Where(m => IsBboxInTile(m.OriginLatitude, m.OriginLongitude, bbox)));

            if (bboxMetadata.Count == 0)
            {
                throw new Exception($"No coverage found matching provided bounding box {bbox}.");
            }

            return bboxMetadata;
        }

        private static bool IsBboxInTile(double originLatitude, double originLongitude, BoundingBox bbox)
        {
            bool isInsideY = originLatitude >= bbox.yMin && (originLatitude - 1) <= bbox.yMax;
            bool isInsideX = (originLongitude + 1) >= bbox.xMin && originLongitude <= bbox.xMax;
            bool isInside = isInsideX && isInsideY;
            return isInside;
        }
        private static bool IsPointInTile(FileMetadata tileMetadata, GeoPoint point)
        {
            return IsPointInTile(tileMetadata.OriginLatitude, tileMetadata.OriginLongitude, point);
        }
        private static bool IsPointInTile(double originLatitude, double originLongitude, GeoPoint point)
        {
            bool isInsideY = originLatitude >= point.Latitude && (originLatitude - 1) <= point.Latitude;
            bool isInsideX = (originLongitude + 1) >= point.Longitude && originLongitude <= point.Longitude;
            bool isInside = isInsideX && isInsideY;
            return isInside;
        }


        public static HeightMap ParseGeoData(GeoTiff tiff, FileMetadata metadata)
        {
            HeightMap heightMap = new HeightMap(metadata.Width, metadata.Height);
            heightMap.FileMetadata = metadata;

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

        public static HeightMap ParseGeoDataInBBox(GeoTiff tiff, BoundingBox bbox, FileMetadata metadata)
        {
            HeightMap heightMap = new HeightMap(metadata.Width, metadata.Height);
            heightMap.FileMetadata = metadata;

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

        /// <summary>
        /// Extract elevation data for given coordinates
        /// If not exactly on grid DEM, will interpolate elevation from neighbor points
        /// </summary>
        /// <param name="tiff"></param>
        /// <param name="metadata"></param>
        /// <param name="lat"></param>
        /// <param name="lon"></param>
        /// <returns></returns>
        public static float ParseGeoDataAtPoint(Tiff tiff, FileMetadata metadata, double lat, double lon)
        {
            //const double epsilon = (Double.Epsilon * 100);
            byte[] scanline = new byte[metadata.ScanlineSize];
            ushort[] scanline16Bit = new ushort[metadata.ScanlineSize / 2];
            float noData = float.Parse(metadata.NoDataValue);
            const float NO_DATA_OUT = -100;

            // precise position on the grid (with commas)
            double ypos = MathHelper.Clamp((lat - metadata.StartLat) / metadata.pixelSizeY, 0, metadata.Height - 1);
            double xpos = MathHelper.Clamp((lon - metadata.StartLon) / metadata.pixelSizeX, 0, metadata.Width - 1);

            // If pure integers, then it's on the grid
            //bool xOnGrid = Math.Abs(xpos % 1) <= epsilon;
            //bool yOnGrid = Math.Abs(ypos % 1) <= epsilon;
            float xInterpolationAmount = (float)xpos % 1;
            float yInterpolationAmount = (float)ypos % 1;

            bool xOnGrid = xInterpolationAmount == 0;
            bool yOnGrid = yInterpolationAmount == 0;

            // clamp all values to avoid OutOfRangeExceptions
            int clampedXCeiling = MathHelper.Clamp((int)Math.Ceiling(xpos), 0, metadata.Width - 1);
            int clampedXFloor = MathHelper.Clamp((int)Math.Floor(xpos), 0, metadata.Width - 1);
            int clampedYCeiling = MathHelper.Clamp((int)Math.Ceiling(ypos), 0, metadata.Height - 1);
            int clampedYFloor = MathHelper.Clamp((int)Math.Floor(ypos), 0, metadata.Height - 1);
            int clampedX = MathHelper.Clamp((int)Math.Round(xpos, 0), 0, metadata.Width - 1);
            int clampedY = MathHelper.Clamp((int)Math.Round(ypos, 0), 0, metadata.Height - 1);


            float heightValue = 0;
            // If xOnGrid and yOnGrid, we are on a grid intersection, and that's all
            if (xOnGrid && yOnGrid)
            {
                heightValue = ParseGeoDataAtPoint(tiff, metadata, (int)Math.Round(xpos, 0), (int)Math.Round(ypos, 0));
            }
            else
            {

                if (xOnGrid)
                {
                    // If xOnGrid and not yOnGrid we are on grid vertical line
                    // We need elevations for upper and lower grid points (along y axis)
                    float bottom = ParseGeoDataAtPoint(tiff, metadata, clampedX, clampedYFloor);
                    float top = ParseGeoDataAtPoint(tiff, metadata, clampedY, clampedYCeiling);
                    if (bottom == noData) bottom = top;
                    if (top == noData) top = bottom;
                    heightValue = MathHelper.Lerp(bottom, top, yInterpolationAmount);
                    heightValue = MathHelper.Clamp(heightValue, NO_DATA_OUT, float.MaxValue);
                }
                else if (yOnGrid)
                {
                    // If yOnGrid and not xOnGrid we are on grid horizontal line
                    // We need elevations for left and right grid points (along x axis)
                    float left = ParseGeoDataAtPoint(tiff, metadata, clampedXFloor, clampedY);
                    float right = ParseGeoDataAtPoint(tiff, metadata, clampedXCeiling, clampedY);
                    if (left == noData) left = right;
                    if (right == noData) right = left;
                    heightValue = MathHelper.Lerp(left, right, xInterpolationAmount);
                    heightValue = MathHelper.Clamp(heightValue, NO_DATA_OUT, float.MaxValue);
                }
                else
                {
                    // If not yOnGrid and not xOnGrid we are on grid horizontal line
                    // We need elevations for top, bottom, left and right grid points (along x axis and y axis)
                    float bottom = ParseGeoDataAtPoint(tiff, metadata, clampedX, clampedYFloor);
                    float top = ParseGeoDataAtPoint(tiff, metadata, clampedY, clampedYCeiling);
                    float left = ParseGeoDataAtPoint(tiff, metadata, clampedXFloor, clampedY);
                    float right = ParseGeoDataAtPoint(tiff, metadata, clampedXCeiling, clampedY);
                    if (bottom == noData) bottom = top;
                    if (top == noData) top = bottom;
                    if (left == noData) left = right;
                    if (right == noData) right = left;
                    float heightValueX = MathHelper.Lerp(left, right, xInterpolationAmount);
                    float heightValueY = MathHelper.Lerp(bottom, top, yInterpolationAmount);
                    if (heightValueX == noData) heightValueX = heightValueY;
                    if (heightValueY == noData) heightValueY = heightValueX;
                    heightValue = (heightValueX + heightValueY) / 2f;
                }
            }
            return heightValue;
        }


        private static float ParseGeoDataAtPoint(Tiff tiff, FileMetadata metadata, int x, int y)
        {
            byte[] scanline = new byte[metadata.ScanlineSize];
            ushort[] scanline16Bit = new ushort[metadata.ScanlineSize / 2];

            tiff.ReadScanline(scanline, y);
            Buffer.BlockCopy(scanline, 0, scanline16Bit, 0, scanline.Length);

            float heightValue = ParseGeoDataAtPoint(metadata, x, scanline16Bit);

            return heightValue;
        }
        private static float ParseGeoDataAtPoint(FileMetadata metadata, int x, ushort[] scanline16Bit)
        {
            float heightValue = (float)scanline16Bit[x];
            if (heightValue > 32768)
            {
                heightValue = -10000;
            }

            return heightValue;
        }

        public static HeightMap GetHeightMap(string fileName)
        {
            fileName = Path.GetFullPath(fileName);
            string fileTitle = Path.GetFileNameWithoutExtension(fileName);

            HeightMap heightMap = null;
            using (GeoTiff tiff = new GeoTiff(fileName))
            {
                FileMetadata metadata = GeoTiffService.ParseMetadata(tiff, fileName);
                heightMap = ElevationService.ParseGeoData(tiff, metadata);

            }
            return heightMap;
        }
    }
}
