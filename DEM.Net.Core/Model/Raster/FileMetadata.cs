// FileMetadata.cs
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

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core
{
    /// <summary>
    /// Metadata file generated on the fly.
    /// Extracts necessary data in order to index files for querying without actually open them (which is costly)
    /// </summary>
    public class FileMetadata : IEquatable<FileMetadata>
    {
        #region Versioning

        /* History
         * 
         *  2.1 : file name are relative to data directory
         *  2.2 : file format is now mapped as an enum 
         */

        public const string FILEMETADATA_VERSION = "2.2";
        #endregion


        public FileMetadata(string filename, DEMFileFormat fileFormat, string version = FILEMETADATA_VERSION)
        {
            this.Filename = filename;
            this.fileFormat = fileFormat;
            this.Version = version;
        }


        public string Version { get; set; }
        public string Filename { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public double PixelScaleX { get; set; }
        public double PixelScaleY { get; set; }
        public double OriginLatitude { get; set; }
        public double OriginLongitude { get; set; }
        public int BitsPerSample { get; set; }
        public string WorldUnits { get; set; }
        public string SampleFormat { get; set; }
        public string NoDataValue { get; set; }
        public int ScanlineSize { get; set; }
        /// <summary>
        /// Origin longitude of physical image (for cell centered images this can be offset by 1px)
        /// </summary>
        public double StartLon { get; set; }
        ///
        /// Origin latitude of physical image (for cell centered images this can be offset by 1px)
        public double StartLat { get; set; }
        public double pixelSizeX { get; set; }
        public double pixelSizeY { get; set; }
        public DEMFileFormat fileFormat { get; set; }
        public float MinimumAltitude { get; set; }
        public float MaximumAltitude { get; set; }
        public double EndLongitude
        {
            get
            {
                return Width * pixelSizeX + OriginLongitude;
            }
        }
        public double EndLatitude
        {
            get
            {
                return Height * pixelSizeY + OriginLatitude;
            }
        }

        private float _noDataValue;
        private bool _noDataValueSet = false;

        public float NoDataValueFloat
        {
            get
            {
                if (!_noDataValueSet)
                {
                    _noDataValue = float.Parse(NoDataValue);
                    _noDataValueSet = true;
                }
                return _noDataValue;
            }
            set { _noDataValue = value; }
        }


        public override string ToString()
        {
            return $"{System.IO.Path.GetFileName(Filename)}: {OriginLatitude} {OriginLongitude} -> {EndLatitude} {EndLongitude}";
        }

        public override bool Equals(object obj)
        {
            FileMetadata objTyped = obj as FileMetadata;
            if (objTyped == null)
                return false;

            return this.Equals(objTyped);
        }
        public override int GetHashCode()
        {
            return Filename.GetHashCode();
        }

        public bool Equals(FileMetadata other)
        {
            if (this == null || other == null)
                return false;
            return this.GetHashCode().Equals(other.GetHashCode());
        }

        private BoundingBox _boundingBox;
        [JsonIgnore]
        public BoundingBox BoundingBox
        {
            get
            {
                if (_boundingBox == null)
                {
                    double xmin = Math.Min(OriginLongitude, EndLongitude);
                    double xmax = Math.Max(OriginLongitude, EndLongitude);
                    double ymin = Math.Min(EndLatitude, OriginLatitude);
                    double ymax = Math.Max(EndLatitude, OriginLatitude);
                    _boundingBox = new BoundingBox(xmin, xmax, ymin, ymax);
                }
                return _boundingBox;
            }
        }

    }

    internal static class FileMetadataMigrations
    {
        public static FileMetadata Migrate(ILogger logger, FileMetadata oldMetadata, string dataRootDirectory, DEMDataSet dataSet)
        {
            if (oldMetadata != null)
            {
                logger.LogInformation($"Migration metadata file {oldMetadata.Filename} from {oldMetadata.Version} to {FileMetadata.FILEMETADATA_VERSION}");

                switch (oldMetadata.Version)
                {
                    case "2.1":

                        // 2.2 : file format
                       
                        break;
                    case "2.0":

                        // 2.1 : relative path
                        // Find dataset root within path
                        DirectoryInfo dir = new DirectoryInfo(Path.GetDirectoryName(oldMetadata.Filename));
                        while (dir.Name != dataSet.Name)
                        {
                            dir = dir.Parent;
                        }
                        dir = dir.Parent;
                        // replace directory
                        oldMetadata.Filename = oldMetadata.Filename.Replace(dir.FullName, dataRootDirectory);
                        Uri fullPath = new Uri(oldMetadata.Filename, UriKind.Absolute);
                        if (!(dataRootDirectory.Last() == Path.DirectorySeparatorChar))
                            dataRootDirectory += Path.DirectorySeparatorChar;
                        Uri relRoot = new Uri(dataRootDirectory, UriKind.Absolute);

                        oldMetadata.Filename = Uri.UnescapeDataString(relRoot.MakeRelativeUri(fullPath).ToString());

                        break;
                    default:

                        // DEMFileFormat
                        switch (Path.GetExtension(oldMetadata.Filename).ToUpper())
                        {
                            case ".TIF":
                            case ".TIFF":
                                oldMetadata.fileFormat = DEMFileFormat.GEOTIFF;
                                break;
                            default:
                                // not possible since pre V2 files could only be GEOTIFF
                                throw new Exception("Metadata corrupted.");
                        }
                        break;
                }

                // set version and fileFormat
                oldMetadata.Version = FileMetadata.FILEMETADATA_VERSION;


            }
            return oldMetadata;
        }
    }
}
