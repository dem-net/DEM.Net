using BitMiracle.LibTiff.Classic;
using DEM.Net.Lib.Services.Interpolation;
using Microsoft.SqlServer.Types;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Services
{
    public class ElevationService : IElevationService
    {
        private readonly IGeoTiffService _IGeoTiffService;
        public ElevationService()
        {
            _IGeoTiffService = new GeoTiffService();
        }

        /// <summary>
        /// Extract elevation data along line path
        /// </summary>
        /// <param name="lineWKT"></param>
        /// <param name="geoTiffRepository"></param>
        /// <returns></returns>
        public List<GeoPoint> GetLineGeometryElevation(string lineWKT, string geoTiffRepository, InterpolationMode interpolationMode = InterpolationMode.Bilinear)
        {
            BoundingBox bbox = GeometryService.GetBoundingBox(lineWKT);
            //HeightMap heightMap = GeoTiffService.GetHeightMap(bbox, geoTiffRepository);
            SqlGeometry geom = GeometryService.GetNativeGeometry(lineWKT);
            List<FileMetadata> tiles = this.GetCoveringFiles(bbox, geoTiffRepository);

            // Init interpolator
            IInterpolator interpolator = GetInterpolator(interpolationMode);

            double lengthMeters = GeometryService.GetLength(lineWKT);
            int demResolution = GeoTiffService.GetResolutionMeters(tiles.First());
            int totalCapacity = 2 * (int)(lengthMeters / demResolution);

            List<GeoPoint> geoPoints = new List<GeoPoint>(totalCapacity);

            bool isFirstSegment = true; // used to return first point only for first segments, for all other segments last point will be returned
            foreach (SqlGeometry segment in geom.Segments())
            {
                List<FileMetadata> segTiles = this.GetCoveringFiles(segment.GetBoundingBox(), geoTiffRepository, tiles);

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

        public HeightMap GetHeightMap(BoundingBox bbox, string tiffPath)
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
        /// Fill altitudes for each GeoPoint provided
        /// </summary>
        /// <param name="intersections"></param>
        /// <param name="segTiles"></param>
        public void GetElevationData(ref List<GeoPoint> intersections, List<FileMetadata> segTiles, IInterpolator interpolator)
        {
            // Group by tiff file for sequential and faster access
            var pointsByTileQuery = from point in intersections
                                    let pointTile = new { Point = point, Tile = segTiles.First(t => this.IsPointInTile(t, point)) }
                                    group pointTile by pointTile.Tile into pointsByTile
                                    select pointsByTile;

            foreach (var tilePoints in pointsByTileQuery)
            {
                FileMetadata tile = tilePoints.Key;
                using (GeoTiff tiff = new GeoTiff(tile.Filename))
                {
                    foreach (GeoPoint pt in tilePoints.Select(a => a.Point))
                    {
                        pt.Altitude = this.ParseGeoDataAtPoint(tiff.TiffFile, tile, pt.Latitude, pt.Longitude, interpolator);
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
        public List<GeoPoint> FindSegmentIntersections(double startLon, double startLat, double endLon, double endLat, List<FileMetadata> segTiles,
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
                GeoSegment line = new GeoSegment(new GeoPoint(curTile.OriginLatitude, curPoint.Longitude), new GeoPoint(curTile.EndLatitude, curPoint.Longitude));

                curIndex++;
                yield return line;
            }
        }
        public IEnumerable<GeoSegment> GetDEMWestEastLines(List<FileMetadata> segTiles, GeoPoint northernSegPoint, GeoPoint southernSegPoint)
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


        public BoundingBox GetTilesBoundingBox(List<FileMetadata> tiles)
        {
            double xmin = tiles.Min(t => t.OriginLongitude);
            double xmax = tiles.Max(t => t.EndLongitude);
            double ymin = tiles.Min(t => t.EndLatitude);
            double ymax = tiles.Max(t => t.OriginLatitude);
            return new BoundingBox(xmin, xmax, ymin, ymax);
        }

        public BoundingBox GetSegmentBoundingBox(double xStart, double yStart, double xEnd, double yEnd)
        {
            double xmin = Math.Min(xStart, xEnd);
            double xmax = Math.Max(xStart, xEnd);
            double ymin = Math.Min(yStart, yEnd);
            double ymax = Math.Max(yStart, yEnd);
            return new BoundingBox(xmin, xmax, ymin, ymax);
        }

        public List<FileMetadata> GetCoveringFiles(BoundingBox bbox, string tiffPath, List<FileMetadata> subSet = null)
        {
            // Locate which files are needed

            // Load metadata catalog
            List<FileMetadata> metadataCatalog = subSet ?? _IGeoTiffService.LoadManifestMetadata(tiffPath);

            // Find files matching coords
            List<FileMetadata> bboxMetadata = new List<FileMetadata>(metadataCatalog.Where(m => IsBboxInTile(m.OriginLatitude, m.OriginLongitude, bbox)));

            if (bboxMetadata.Count == 0)
            {
                throw new Exception($"No coverage found matching provided bounding box {bbox}.");
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

        /// <summary>
        /// Extract elevation data for given coordinates
        /// If not exactly on grid DEM, will interpolate elevation from neighbor points
        /// </summary>
        /// <param name="tiff"></param>
        /// <param name="metadata"></param>
        /// <param name="lat"></param>
        /// <param name="lon"></param>
        /// <returns></returns>
        public float ParseGeoDataAtPoint_old(Tiff tiff, FileMetadata metadata, double lat, double lon)
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
                    float top = ParseGeoDataAtPoint(tiff, metadata, clampedX, clampedYCeiling);
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
                    float top = ParseGeoDataAtPoint(tiff, metadata, clampedX, clampedYCeiling);
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
        public float ParseGeoDataAtPoint(Tiff tiff, FileMetadata metadata, double lat, double lon, IInterpolator interpolator)
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

                // Get 4 grid nearest points (DEM grid corners)

                // If not yOnGrid and not xOnGrid we are on grid horizontal line
                // We need elevations for top, bottom, left and right grid points (along x axis and y axis)
                float northWest = ParseGeoDataAtPoint(tiff, metadata, clampedXFloor, clampedYFloor);
                float northEast = ParseGeoDataAtPoint(tiff, metadata, clampedXCeiling, clampedYFloor);
                float southWest = ParseGeoDataAtPoint(tiff, metadata, clampedXFloor, clampedYCeiling);
                float southEast = ParseGeoDataAtPoint(tiff, metadata, clampedXCeiling, clampedYCeiling);

                float avgHeight = GetAverageExceptForNoDataValue(noData, NO_DATA_OUT, southWest, southEast, northWest, northEast);

                if (northWest == noData) northWest = avgHeight;
                if (northEast == noData) northEast = avgHeight;
                if (southWest == noData) southWest = avgHeight;
                if (southEast == noData) southEast = avgHeight;

                heightValue = interpolator.Interpolate(southWest, southEast, northWest, northEast, xInterpolationAmount, yInterpolationAmount);
            }
            return heightValue;
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

        /// <summary>
        /// 
        /// 
        /// The concept of linear interpolation between two points can be extended to bilinear interpolation within 
        /// the grid cell. The function is said to be linear in each variable when the other is held fixed. 
        /// 
        /// For example, to determine the height hi at x, y in Figure 5, the elevations at y on the vertical 
        /// boundaries of the grid cell can be linearly interpolated between h1 and h3 at ha, and h2 and h4 at hb.
        /// Finally, the required elevation at x can be linearly interpolated between ha and hb. 
        /// 
        /// Source : http://www.geocomputation.org/1999/082/gc_082.htm
        /// </summary>
        /// <param name="h1"></param>
        /// <param name="h2"></param>
        /// <param name="h3"></param>
        /// <param name="h4"></param>
        /// <returns></returns>
        public float BilinearInterpolation(float h1, float h2, float h3, float h4, float x, float y)
        {
            // bilinear
            float ha = MathHelper.Lerp(h3, h1, y);
            float hb = MathHelper.Lerp(h4, h2, y);
            float hi_linear = MathHelper.Lerp(ha, hb, x);

            // hyperbolic
            float a00 = h1;
            float a10 = h2 - h1;
            float a01 = h3 - h1;
            float a11 = h1 - h2 - h3 + h4;
            float hi_hyperbolic = a00 + a10 * x + a01 * y + a11 * x * y;


            return hi_linear;
        }


        public float ParseGeoDataAtPoint(Tiff tiff, FileMetadata metadata, int x, int y)
        {
            byte[] scanline = new byte[metadata.ScanlineSize];
            ushort[] scanline16Bit = new ushort[metadata.ScanlineSize / 2];

            tiff.ReadScanline(scanline, y);
            Buffer.BlockCopy(scanline, 0, scanline16Bit, 0, scanline.Length);

            float heightValue = ParseGeoDataAtPoint(metadata, x, scanline16Bit);

            return heightValue;
        }
        public float ParseGeoDataAtPoint(FileMetadata metadata, int x, ushort[] scanline16Bit)
        {
            float heightValue = (float)scanline16Bit[x];
            if (heightValue > 32768)
            {
                heightValue = -10000;
            }

            return heightValue;
        }



    }
}
