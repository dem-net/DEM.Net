// IRasterService.cs
//
// Author:
//       Xavier Fischer
//
// Copyright (c) 2019 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the right
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
using System.Threading.Tasks;
using DEM.Net.Core.Model;

namespace DEM.Net.Core
{
    public interface IRasterService : IRasterDownloader
    {
        FileMetadata ParseMetadata(IRasterFile rasterFile, DEMFileDefinition fileFormat, bool makeRelativePath = false);
        FileMetadata ParseMetadata(string fileName, DEMFileDefinition fileFormat, bool makeRelativePath = true);
        List<FileMetadata> LoadManifestMetadata(DEMDataSet dataSet, bool force, bool logTimeSpent = false);

        /// <summary>
        /// Open specified file
        /// </summary>
        /// <param name="filePath">If path is rooted (full file name), the specified file will be openened,
        /// otherwise the file path will be relative to <see cref="LocalDirectory"/></param>
        /// <param name="fileFormat"></param>
        /// <returns></returns>
        IRasterFile OpenFile(string filePath, DEMFileType fileFormat);

        string LocalDirectory { get; }

        /// <summary>
        /// Change directory to user specified directory. Causes local caches to reset.
        /// Directory will be created if not existing
        /// </summary>
        /// <param name="localDirectory"></param>
        void SetLocalDirectory(string localDirectory);
        string GetLocalDEMPath(DEMDataSet dataset);
        string GetLocalDEMFilePath(DEMDataSet dataset, string fileTitle);


        /// <summary>
        /// Generate metadata files for fast in-memory indexing
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="force">If true, force regeneration of all files. If false, only missing files will be generated.</param>
        /// <param name="deleteOnError">If true, files where error are encountered will be deleted</param>
        void GenerateDirectoryMetadata(DEMDataSet dataSet, bool force, bool deleteOnError = false);


        /// <summary>
        /// Compare LST file and local directory and generates dictionary with key : remoteFile and value = true if file is present and false if it is not downloaded
        /// </summary>
        /// <param name="dataSet">DEM dataset information</param>
        /// <param name="bbox">Bbox for filtering</param>
        /// <returns></returns>
        List<DemFileReport> GenerateReport(DEMDataSet dataSet, BoundingBox bbox = null);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="lat"></param>
        /// <param name="lon"></param>
        /// <returns></returns>
        IEnumerable<DemFileReport> GenerateReportForLocation(DEMDataSet dataSet, double lat, double lon);

        /// <summary>
        /// Generates a full report of all datasets to check size and number of downloaded tiles
        /// </summary>
        /// <returns>A string containing the report</returns>
        Task<List<DatasetReport>> GenerateReportAsync();
        string GenerateReportAsString();

        /// <summary>
        /// Generates a <see cref="FileMetadata"/> as JSON file containing raster file information.
        /// This metadata is used for fast indexing, preventing to open every raster file when performing spatial queries
        /// </summary>
        /// <param name="rasterFileName">Local file name</param>
        /// <param name="fileFormat">File format definition, see <see cref="DEMFileDefinition"/></param>
        /// <param name="force">If true, metadata will be replaced, if false the metadata will be generated only if the JSON file does not exists</param>
        void GenerateFileMetadata(string rasterFileName, DEMFileDefinition fileFormat, bool force);

    }
}