using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib
{
    public class FileMetadata : IEquatable<FileMetadata>
    {
        public const string FILEMETADATA_VERSION = "2.0";

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
        public double StartLon { get; set; }
        public double StartLat { get; set; }
        public double pixelSizeX { get; set; }
        public double pixelSizeY { get; set; }
        public DEMFileFormat fileFormat { get; set; }

        public float MininumAltitude { get; set; }
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

    public static class FileMetadataMigrations
    {
        public static FileMetadata Migrate(FileMetadata oldMetadata, string dataRootDirectory)
        {
            if (oldMetadata != null)
            {
                Logger.Info($"Migration metadata file from {oldMetadata.Version} to {FileMetadata.FILEMETADATA_VERSION}");

                switch (oldMetadata.Version)
                {
                    case "2.0":

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
