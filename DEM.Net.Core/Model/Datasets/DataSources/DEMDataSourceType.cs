using System;
using System.Collections.Generic;
using System.Text;

namespace DEM.Net.Core.Datasets
{
    public enum DEMDataSourceType
    {
        /// <summary>
        /// Data source points to a VRT file (generated gy GDAL)
        /// </summary>
        GDALVrt = 1,
        /// <summary>
        /// Data source is a single or multiple files, downloaded manually and stored at a specific location
        /// </summary>
        LocalFileSystem = 2,
        /// <summary>
        /// Data comes from NASA EarthData Common Metadata Repository (CMR) API at https://earthdata.nasa.gov/
        /// </summary>
        NasaEarthData = 3
    }
}
