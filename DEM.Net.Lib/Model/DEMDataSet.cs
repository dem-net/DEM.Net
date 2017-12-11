using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib
{
    public class DEMDataSet
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string PublicUrl { get; set; }

        public DEMFileFormat FileFormat { get; set; }

        /// <summary>
        /// GDAL Virtual 
        /// </summary>
        public string VRTFileUrl { get; set; }

        public DEMDataSet(string name, string description, string publicUrl, string vrtFileUrl, DEMFileFormat fileFormat)
        {
            Name = name;
            Description = description;
            PublicUrl = publicUrl;
            VRTFileUrl = vrtFileUrl;
            FileFormat = fileFormat;
        }

        // Examples datasets

        public static DEMDataSet SRTM_GL3_srtm
        {
            get {
                return new DEMDataSet("SRTM_GL3",
                    "Shuttle Radar Topography Mission (SRTM GL3) Global 90m",
                    "http://opentopo.sdsc.edu/raster?opentopoID=OTSRTM.042013.4326.1", 
                    "https://cloud.sdsc.edu/v1/AUTH_opentopography/Raster/SRTM_GL3/SRTM_GL3_srtm.vrt",
                    DEMFileFormat.SRTM_HGT);
            }
        }

        public static DEMDataSet AW3D30
        {
            get
            {
                return new DEMDataSet("AW3D30",
                    "ALOS World 3D - 30m",
                    "http://opentopo.sdsc.edu/raster?opentopoID=OTALOS.112016.4326.2",
                    "https://cloud.sdsc.edu/v1/AUTH_opentopography/Raster/AW3D30/AW3D30_alos.vrt",
                    DEMFileFormat.GEOTIFF);
            }
        }
    }
}
