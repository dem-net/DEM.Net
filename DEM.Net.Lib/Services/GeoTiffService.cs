using BitMiracle.LibTiff.Classic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Services
{
    public static class GeoTiffService
    {

        private const string MANIFEST_DIR = "manifest";

        public static FileMetadata ParseMetadata(GeoTiff tiff, string tiffPath)
        {
            FileMetadata metadata = new FileMetadata(tiffPath);

            ///
            metadata.Height = tiff.TiffFile.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
            metadata.Width = tiff.TiffFile.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();

            ///
            FieldValue[] modelPixelScaleTag = tiff.TiffFile.GetField(TiffTag.GEOTIFF_MODELPIXELSCALETAG);
            FieldValue[] modelTiepointTag = tiff.TiffFile.GetField(TiffTag.GEOTIFF_MODELTIEPOINTTAG);

            byte[] modelPixelScale = modelPixelScaleTag[1].GetBytes();
            double pixelSizeX = BitConverter.ToDouble(modelPixelScale, 0);
            double pixelSizeY = BitConverter.ToDouble(modelPixelScale, 8) * -1;
            metadata.pixelSizeX = pixelSizeX;
            metadata.pixelSizeY = pixelSizeY;
            metadata.PixelScaleX = BitConverter.ToDouble(modelPixelScale, 0);
            metadata.PixelScaleY = BitConverter.ToDouble(modelPixelScale, 8);

            // Ignores first set of model points (3 bytes) and assumes they are 0's...
            byte[] modelTransformation = modelTiepointTag[1].GetBytes();
            metadata.OriginLongitude = BitConverter.ToDouble(modelTransformation, 24);
            metadata.OriginLatitude = BitConverter.ToDouble(modelTransformation, 32);


            double startLat = metadata.OriginLatitude + (pixelSizeY / 2.0);
            double startLon = metadata.OriginLongitude + (pixelSizeX / 2.0);
            metadata.StartLat = startLat;
            metadata.StartLon = startLon;

            var scanline = new byte[tiff.TiffFile.ScanlineSize()];
            metadata.ScanlineSize = tiff.TiffFile.ScanlineSize();
            //TODO: Check if band is stored in 1 byte or 2 bytes. 
            //If 2, the following code would be required
            var scanline16Bit = new ushort[tiff.TiffFile.ScanlineSize() / 2];
            Buffer.BlockCopy(scanline, 0, scanline16Bit, 0, scanline.Length);


            // Grab some raster metadata
            metadata.BitsPerSample = tiff.TiffFile.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();
            var sampleFormat = tiff.TiffFile.GetField(TiffTag.SAMPLEFORMAT);
            // Add other information about the data
            metadata.SampleFormat = sampleFormat[0].Value.ToString();
            // TODO: Read this from tiff metadata or determine after parsing
            metadata.NoDataValue = "-10000";

            metadata.WorldUnits = "meter";

            //DumpTiffTags(tiff);

            return metadata;
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
                    heightMap = GeoTiffService.ParseGeoDataInBBox(geoTiff, bbox, metadata);
                }
            }

            FileMetadata meta = bboxMetadata.First();
            using (GeoTiff geoTiff = new GeoTiff(meta.Filename))
            {
                heightMap = GeoTiffService.ParseGeoDataInBBox(geoTiff, bbox, meta);
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
                                    let pointTile = new { Point = point, Tile = segTiles.First(t => GeoTiffService.IsPointInTile(t, point)) }
                                    group pointTile by pointTile.Tile into pointsByTile
                                    select pointsByTile;

            foreach (var tilePoints in pointsByTileQuery)
            {
                FileMetadata tile = tilePoints.Key;
                using (GeoTiff tiff = new GeoTiff(tile.Filename))
                {
                    foreach (GeoPoint pt in tilePoints.Select(a => a.Point))
                    {
                        pt.Altitude = GeoTiffService.ParseGeoDataAtPoint(tiff.TiffFile, tile, pt.Latitude, pt.Longitude);
                    }
                }
            }
        }

        public static List<GeoPoint> FindLineIntersections(double startLon, double startLat, double endLon, double endLat, List<FileMetadata> segTiles)
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

            BoundingBox segmentBbox = GetSegmentBoundingBox(startLon, startLat, endLon, endLat);
            BoundingBox tilesBbox = GetTilesBoundingBox(segTiles);

            // Find intersections with north/south lines, 
            // starting form segment western point to easternmost point
            GeoPoint westernSegPoint = startLon < endLon ? new GeoPoint(startLat, startLon) : new GeoPoint(endLat, endLon);
            GeoPoint easternSegPoint = startLon > endLon ? new GeoPoint(startLat, startLon) : new GeoPoint(endLat, endLon);
            GeoSegment inputSegment = new GeoSegment(westernSegPoint, easternSegPoint);
            segmentPointsWithDEMPoints.Add(inputSegment.Start);
            segmentPointsWithDEMPoints.Add(inputSegment.End);
            foreach (GeoSegment demSegment in GeoTiffService.GetDEMNorthSouthLines(segTiles, westernSegPoint, easternSegPoint))
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
            foreach (GeoSegment demSegment in GeoTiffService.GetDEMWestEastLines(segTiles, northernSegPoint, southernSegPoint))
            {
                GeoPoint intersectionPoint = null;
                if (GeometryService.LineLineIntersection(out intersectionPoint, inputSegment, demSegment))
                {
                    segmentPointsWithDEMPoints.Add(intersectionPoint);
                }
            }


            return segmentPointsWithDEMPoints;
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
            List<FileMetadata> metadataCatalog = subSet ?? LoadManifestMetadata(tiffPath);

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

        private static List<FileMetadata> _metadataCatalogCache = null;
        private static List<FileMetadata> LoadManifestMetadata(string tiffPath)
        {
            if (_metadataCatalogCache == null)
            {
                string manifestDir = Path.Combine(tiffPath, MANIFEST_DIR);
                string[] manifestFiles = Directory.GetFiles(manifestDir, "*.json");
                List<FileMetadata> metaList = new List<FileMetadata>(manifestFiles.Length);

                foreach (var file in manifestFiles)
                {
                    string jsonContent = File.ReadAllText(file);
                    metaList.Add(JsonConvert.DeserializeObject<FileMetadata>(jsonContent));
                }

                _metadataCatalogCache = metaList;
            }
            return _metadataCatalogCache;
        }

        public static void DumpTiffTags(Tiff tiff)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var value in Enum.GetValues(typeof(TiffTag)))
            {
                TiffTag tag = (TiffTag)value;
                FieldValue[] values = tiff.GetField(tag);
                if (values != null)
                {
                    sb.AppendLine(value + ": ");
                    foreach (var fieldValue in values)
                    {
                        sb.Append("\t");
                        sb.AppendLine(fieldValue.Value.ToString());
                    }
                }
            }
            Console.WriteLine(sb.ToString());
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

            // precise position on the grid (with commas)
            double ypos = Clamp((lat - metadata.StartLat) / metadata.pixelSizeY, 0, metadata.Height - 1);
            double xpos = Clamp((lon - metadata.StartLon) / metadata.pixelSizeX, 0, metadata.Width - 1);

            // If pure integers, then it's on the grid
            //bool xOnGrid = Math.Abs(xpos % 1) <= epsilon;
            //bool yOnGrid = Math.Abs(ypos % 1) <= epsilon;
            float xInterpolationAmount = (float)xpos % 1;
            float yInterpolationAmount = (float)ypos % 1;

            bool xOnGrid = xInterpolationAmount == 0;
            bool yOnGrid = yInterpolationAmount == 0;

            // clamp all values to avoid OutOfRangeExceptions
            int clampedXCeiling = Clamp((int)Math.Ceiling(xpos), 0, metadata.Width - 1);
            int clampedXFloor = Clamp((int)Math.Floor(xpos), 0, metadata.Width - 1);
            int clampedYCeiling = Clamp((int)Math.Ceiling(ypos), 0, metadata.Height - 1);
            int clampedYFloor = Clamp((int)Math.Floor(ypos), 0, metadata.Height - 1);
            int clampedX = Clamp((int)Math.Round(xpos, 0), 0, metadata.Width - 1);
            int clampedY = Clamp((int)Math.Round(ypos, 0), 0, metadata.Height - 1);


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
                    heightValue = Lerp(bottom, top, yInterpolationAmount);
                }
                else if (yOnGrid)
                {
                    // If yOnGrid and not xOnGrid we are on grid horizontal line
                    // We need elevations for left and right grid points (along x axis)
                    float left = ParseGeoDataAtPoint(tiff, metadata, clampedXFloor, clampedY);
                    float right = ParseGeoDataAtPoint(tiff, metadata, clampedXCeiling, clampedY);
                    heightValue = Lerp(left, right, xInterpolationAmount);
                }
                else
                {
                    // If not yOnGrid and not xOnGrid we are on grid horizontal line
                    // We need elevations for top, bottom, left and right grid points (along x axis and y axis)
                    float bottom = ParseGeoDataAtPoint(tiff, metadata, clampedX, clampedYFloor);
                    float top = ParseGeoDataAtPoint(tiff, metadata, clampedY, clampedYCeiling);
                    float left = ParseGeoDataAtPoint(tiff, metadata, clampedXFloor, clampedY);
                    float right = ParseGeoDataAtPoint(tiff, metadata, clampedXCeiling, clampedY);
                    float heightValueX = Lerp(left, right, xInterpolationAmount);
                    float heightValueY = Lerp(bottom, top, yInterpolationAmount);
                    heightValue = (heightValueX + heightValueY) / 2f;
                }
            }
            return heightValue;
        }

        private static float Lerp(float value1, float value2, float amount)
        { return value1 + (value2 - value1) * amount; }

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

        public static void GenerateDirectoryMetadata(string directoryPath)
        {
            Parallel.ForEach(Directory.GetFiles(directoryPath, "*.tif", SearchOption.TopDirectoryOnly), new ParallelOptions() { MaxDegreeOfParallelism = 2 }, file =>
                        {
                            Trace.TraceInformation($"Generating manifest for file {file}.");
                            HeightMap heightMap = GeoTiffService.GetHeightMap(file);
                            GeoTiffService.WriteManifestFiles(heightMap);
                            GC.Collect();
                            Trace.TraceInformation($"Manifest generated for file {file}.");
                        });
        }

        public static HeightMap GetHeightMap(string fileName)
        {
            fileName = Path.GetFullPath(fileName);
            string fileTitle = Path.GetFileNameWithoutExtension(fileName);

            HeightMap heightMap = null;
            using (GeoTiff tiff = new GeoTiff(fileName))
            {
                FileMetadata metadata = GeoTiffService.ParseMetadata(tiff, fileName);
                heightMap = GeoTiffService.ParseGeoData(tiff, metadata);

            }
            return heightMap;
        }

        public static void WriteManifestFiles(HeightMap heightMap)
        {

            var fileName = heightMap.FileMetadata.Filename;
            var fileTitle = Path.GetFileNameWithoutExtension(fileName);

            string outDirPath = Path.Combine(Path.GetDirectoryName(fileName), MANIFEST_DIR);
            if (!Directory.Exists(outDirPath))
            {
                Directory.CreateDirectory(outDirPath);
            }

            // Save metadata
            var outputJsonPath = Path.Combine(Path.GetDirectoryName(fileName), MANIFEST_DIR, fileTitle + ".json");
            if (File.Exists(outputJsonPath)) File.Delete(outputJsonPath);

            var bitmapPath = Path.Combine(Path.GetDirectoryName(fileName), MANIFEST_DIR, fileTitle + ".bmp");
            if (File.Exists(bitmapPath)) File.Delete(bitmapPath);

            // Json manifest
            File.WriteAllText(outputJsonPath, JsonConvert.SerializeObject(heightMap.FileMetadata, Formatting.Indented));

            // Bitmap
            DiagnosticUtils.OutputDebugBitmap(heightMap, bitmapPath);
        }

        private static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
        {
            return value.CompareTo(min) == -1 ? min
                        : value.CompareTo(max) == 1 ? max
                        : value;

        }

    }
}
