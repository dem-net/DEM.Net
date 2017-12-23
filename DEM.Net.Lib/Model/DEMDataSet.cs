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
        public int ResolutionMeters { get; set; }

        public DEMFileFormat FileFormat { get; set; }

        /// <summary>
        /// GDAL Virtual 
        /// </summary>
        public string VRTFileUrl { get; set; }

        private DEMDataSet()
        {
        }

        // Examples datasets

        public static DEMDataSet SRTM_GL3_srtm
        {
            get
            {
                return new DEMDataSet()
                {
                    Name = "SRTM_GL3",
                    Description = "Shuttle Radar Topography Mission (SRTM GL3) Global 90m",
                    PublicUrl = "http://opentopo.sdsc.edu/raster?opentopoID=OTSRTM.042013.4326.1",
                    VRTFileUrl = "https://cloud.sdsc.edu/v1/AUTH_opentopography/Raster/SRTM_GL3/SRTM_GL3_srtm.vrt",
                    FileFormat = DEMFileFormat.SRTM_HGT,
                    ResolutionMeters = 90
                };
            }
        }

        public static DEMDataSet AW3D30
        {
            get
            {
                return new DEMDataSet()
                {
                    Name = "AW3D30",
                    Description = "ALOS World 3D - 30m",
                    PublicUrl = "http://opentopo.sdsc.edu/raster?opentopoID=OTALOS.112016.4326.2",
                    VRTFileUrl = "https://cloud.sdsc.edu/v1/AUTH_opentopography/Raster/AW3D30/AW3D30_alos.vrt",
                    FileFormat = DEMFileFormat.GEOTIFF,
                    ResolutionMeters = 30
                };
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
