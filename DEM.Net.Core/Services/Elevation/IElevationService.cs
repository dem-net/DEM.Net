// IElevationService.cs
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

using System.Collections.Generic;
using GeoAPI.Geometries;

namespace DEM.Net.Core
{
    public interface IElevationService
    {
        /// <summary>
        /// Given a bounding box and a dataset, downloads all covered tiles
        /// using VRT file specified in dataset
        /// </summary>
        /// <param name="dataSet">DEMDataSet used</param>
        /// <param name="bbox">Bounding box, <see cref="GeometryService.GetBoundingBox(string)"/></param>
        /// <remarks>VRT file is downloaded once. It will be cached in local for 30 days.
        /// </remarks>
        void DownloadMissingFiles(DEMDataSet dataSet, BoundingBox bbox = null);

        /// <summary>
        /// Given a location and a dataset, downloads all covered tiles
        /// using VRT file specified in dataset
        /// </summary>
        /// <param name="dataSet">DEMDataSet used</param>
        /// <param name="lat">Latitude of location</param>
        /// <param name="lon">Longitude of location</param>
        /// <remarks>VRT file is downloaded once. It will be cached in local for 30 days.
        /// </remarks>
        void DownloadMissingFiles(DEMDataSet dataSet, double lat, double lon);

        /// <summary>
        /// Given a location and a dataset, downloads all covered tiles
        /// using VRT file specified in dataset
        /// </summary>
        /// <param name="dataSet">DEMDataSet used</param>
        /// <param name="geoPoint">GeoPoint</param>
        /// <remarks>VRT file is downloaded once. It will be cached in local for 30 days.
        /// </remarks>
        void DownloadMissingFiles(DEMDataSet dataSet, GeoPoint geoPoint);

        /// <summary>
        /// High level method that retrieves all dataset elevations along given line
        /// </summary>
        /// <param name="lineGeoPoints">List of points that, when joined, makes the input line</param>
        /// <param name="dataSet">DEM dataset to use</param>
        /// <param name="interpolationMode">Interpolation mode</param>
        /// <remarks>Output can be BIG, as all elevations will be returned.</remarks>
        /// <returns></returns>
        List<GeoPoint> GetLineGeometryElevation(IEnumerable<GeoPoint> lineGeoPoints, DEMDataSet dataSet, InterpolationMode interpolationMode = InterpolationMode.Bilinear);
        /// <summary>
        /// High level method that retrieves all dataset elevations along given line
        /// </summary>
        /// <param name="lineStringGeometry">Line geometry</param>
        /// <param name="dataSet">DEM dataset to use</param>
        /// <param name="interpolationMode">Interpolation mode</param>
        /// <remarks>Output can be BIG, as all elevations will be returned.</remarks>
        /// <returns></returns>
        List<GeoPoint> GetLineGeometryElevation(IGeometry lineStringGeometry, DEMDataSet dataSet, InterpolationMode interpolationMode = InterpolationMode.Bilinear);
        /// <summary>
        /// High level method that retrieves all dataset elevations along given line
        /// </summary>
        /// <param name="lineWKT">Line geometry in WKT</param>
        /// <param name="dataSet">DEM dataset to use</param>
        /// <param name="interpolationMode">Interpolation mode</param>
        /// <remarks>Output can be BIG, as all elevations will be returned.</remarks>
        /// <returns></returns>
        List<GeoPoint> GetLineGeometryElevation(string lineWKT, DEMDataSet dataSet, InterpolationMode interpolationMode = InterpolationMode.Bilinear);
        /// <summary>
        /// High level method that retrieves elevation for given point
        /// </summary>
        /// <param name="lat">Point latitude</param>
        /// <param name="lon">Point longitude</param>
        /// <param name="dataSet">DEM dataset to use</param>
        /// <param name="interpolationMode">Interpolation mode</param>
        /// <returns></returns>
        GeoPoint GetPointElevation(double lat, double lon, DEMDataSet dataSet, InterpolationMode interpolationMode = InterpolationMode.Bilinear);
        /// <summary>
        /// Get elevation for any raster at specified point (in raster coordinate system)
        /// </summary>
        /// <param name="metadata">File metadata, <see cref="IRasterFile.ParseMetaData"/> and <see cref="IRasterService.OpenFile(string, DEMFileFormat)"/></param>
        /// <param name="lat"></param>
        /// <param name="lon"></param>
        /// <param name="interpolator">If null, then Bilinear interpolation will be used</param>
        /// <returns></returns>
        float GetPointElevation(FileMetadata metadata, double lat, double lon, IInterpolator interpolator = null);
        /// <summary>
        /// Get elevation for any raster at specified point (in raster coordinate system)
        /// </summary>
        /// <param name="raster">Raster file, <see cref="IRasterService.OpenFile(string, DEMFileFormat)"/></param>
        /// <param name="metadata">File metadata, <see cref="IRasterFile.ParseMetaData"/></param>
        /// <param name="lat"></param>
        /// <param name="lon"></param>
        /// <param name="interpolator">If null, then Bilinear interpolation will be used</param>
        /// <returns></returns>
        float GetPointElevation(IRasterFile raster, FileMetadata metadata, double lat, double lon, IInterpolator interpolator = null);
        /// <summary>
        /// High level method that retrieves elevation for each given point
        /// </summary>
        /// <param name="points">List of points</param>
        /// <param name="dataSet">DEM dataset to use</param>
        /// <param name="interpolationMode">Interpolation mode</param>
        /// <returns></returns>
        IEnumerable<GeoPoint> GetPointsElevation(IEnumerable<GeoPoint> points, DEMDataSet dataSet, InterpolationMode interpolationMode = InterpolationMode.Bilinear);
        float GetPointsElevation(IRasterFile raster, FileMetadata metadata, IEnumerable<GeoPoint> points, IInterpolator interpolator = null);

        /// <summary>
        /// Returns all elevations in given bbox
        /// </summary>
        /// <param name="bbox"></param>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        HeightMap GetHeightMap(BoundingBox bbox, DEMDataSet dataSet);

        /// <summary>
        /// Get all elevation for a given raster file
        /// </summary>
        /// <param name="metadata">Raster file metadata. <see cref="GetCoveringFiles(BoundingBox, DEMDataSet, List{FileMetadata})"></see></param>
        /// <returns></returns>
        HeightMap GetHeightMap(FileMetadata metadata);
        HeightMap GetHeightMap(BoundingBox bbox, string rasterFilePath, DEMFileFormat format);

        /// <summary>
        /// Retrieves bounding box for the uning of all raster file list
        /// </summary>
        /// <param name="tiles"></param>
        /// <returns></returns>
        BoundingBox GetTilesBoundingBox(List<FileMetadata> tiles);
        /// <summary>
        /// Performs point / bbox intersection
        /// </summary>
        /// <param name="originLatitude"></param>
        /// <param name="originLongitude"></param>
        /// <param name="bbox"></param>
        /// <returns></returns>
        bool IsBboxIntersectingTile(FileMetadata tileMetadata, BoundingBox bbox);
        bool IsPointInTile(FileMetadata tileMetadata, GeoPoint point);
        List<FileMetadata> GetCoveringFiles(BoundingBox bbox, DEMDataSet dataSet, List<FileMetadata> subSet = null);
        List<FileMetadata> GetCoveringFiles(double lat, double lon, DEMDataSet dataSet, List<FileMetadata> subSet = null);
        string GetDEMLocalPath(DEMDataSet dataSet);



        /// <summary>
        /// Generate a tab separated list of points and elevations
        /// </summary>
        /// <param name="lineElevationData"></param>
        /// <returns></returns>
        string ExportElevationTable(List<GeoPoint> lineElevationData);
    }
}