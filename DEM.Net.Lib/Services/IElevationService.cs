using BitMiracle.LibTiff.Classic;
using System.Collections.Generic;

namespace DEM.Net.Lib.Services
{
    public interface IElevationService
    {

        /// <summary>
        /// Extract elevation data along line path
        /// </summary>
        /// <param name="lineWKT"></param>
        /// <param name="geoTiffRepository"></param>
        /// <returns></returns>
        List<GeoPoint> GetLineGeometryElevation(string lineWKT, string geoTiffRepository, InterpolationMode interpolationMode = InterpolationMode.Bilinear);

        IInterpolator GetInterpolator(InterpolationMode interpolationMode);

        string ExportElevationTable(List<GeoPoint> lineElevationData);

        HeightMap GetHeightMap(BoundingBox bbox, string tiffPath);



        /// <summary>
        /// Fill altitudes for each GeoPoint provided
        /// </summary>
        /// <param name="intersections"></param>
        /// <param name="segTiles"></param>
        void GetElevationData(ref List<GeoPoint> intersections, List<FileMetadata> segTiles, IInterpolator interpolator);

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
        List<GeoPoint> FindSegmentIntersections(double startLon, double startLat, double endLon, double endLat, List<FileMetadata> segTiles,
                                                                                                                   bool returnStartPoint, bool returnEndPoind);


        IEnumerable<GeoSegment> GetDEMNorthSouthLines(List<FileMetadata> segTiles, GeoPoint westernSegPoint, GeoPoint easternSegPoint);
        IEnumerable<GeoSegment> GetDEMWestEastLines(List<FileMetadata> segTiles, GeoPoint northernSegPoint, GeoPoint southernSegPoint);


        BoundingBox GetTilesBoundingBox(List<FileMetadata> tiles);

        BoundingBox GetSegmentBoundingBox(double xStart, double yStart, double xEnd, double yEnd);

        List<FileMetadata> GetCoveringFiles(BoundingBox bbox, string tiffPath, List<FileMetadata> subSet = null);

        bool IsBboxInTile(double originLatitude, double originLongitude, BoundingBox bbox);
        bool IsPointInTile(FileMetadata tileMetadata, GeoPoint point);
        bool IsPointInTile(double originLatitude, double originLongitude, GeoPoint point);

       
        HeightMap ParseGeoDataInBBox(GeoTiff tiff, BoundingBox bbox, FileMetadata metadata);

        float ParseGeoDataAtPoint(Tiff tiff, FileMetadata metadata, double lat, double lon, IInterpolator interpolator);

        float GetAverageExceptForNoDataValue(float noData, float valueIfAllBad, params float[] values);

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
        float BilinearInterpolation(float h1, float h2, float h3, float h4, float x, float y);


        float ParseGeoDataAtPoint(Tiff tiff, FileMetadata metadata, int x, int y);
        float ParseGeoDataAtPoint(FileMetadata metadata, int x, ushort[] scanline16Bit);


    }
}