using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib
{
    public sealed class DEMFileFormat
    {

        private readonly string name;
        private readonly string _fileExtension;

        public string FileExtension
        {
            get { return _fileExtension; }
        }

        public static readonly DEMFileFormat SRTM_HGT = new DEMFileFormat("Shuttle Radar Topography Mission (SRTM) Data file.", ".hgt");
        public static readonly DEMFileFormat GEOTIFF = new DEMFileFormat("GeoTIFF file", ".tif");

        private DEMFileFormat(string name, string fileExtension)
        {
            this.name = name;
            this._fileExtension = fileExtension;
        }

        public override string ToString()
        {
            return name;
        }

    }
}
