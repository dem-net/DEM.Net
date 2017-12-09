using BitMiracle.LibTiff.Classic;
using System.Collections.Generic;

namespace DEM.Net.Lib.Services
{
    public interface IGeoTiffService
    {
        FileMetadata ParseMetadata(GeoTiff tiff);
        FileMetadata ParseMetadata(string fileName);
        List<FileMetadata> LoadManifestMetadata(string tiffPath);

        string LocalDirectory { get; }
        string GetLocalDEMPath(DEMDataSet dataset);
        string GetLocalDEMFilePath(DEMDataSet dataset, string fileTitle);

        void DumpTiffTags(Tiff tiff);

        /// <summary>
        /// Generate metadata files for fast in-memory indexing
        /// </summary>
        /// <param name="directoryPath">GeoTIFF files directory</param>
        /// <param name="generateBitmaps">If true, bitmaps with height map will be generated (heavy memory usage and waaaay slower)</param>
        /// <param name="force">If true, force regeneration of all files. If false, only missing files will be generated.</param>
        void GenerateDirectoryMetadata(string directoryPath, bool generateBitmaps, bool force);


        /// <summary>
        /// Compare LST file and local directory and generates dictionary with key : remoteFile and value = true if file is present and false if it is not downloaded
        /// </summary>
        /// <param name="dataSet">DEM dataset information</param>
        /// <param name="bbox">Bbox for filtering</param>
        /// <returns></returns>
        Dictionary<string, DemFileReport> GenerateReport(DEMDataSet dataSet, BoundingBox bbox = null);
        string GenerateReportAsString(DEMDataSet dataSet, BoundingBox bbox = null);


        void GenerateFileMetadata(string geoTiffFileName, bool generateBitmap, bool force);
    }
}