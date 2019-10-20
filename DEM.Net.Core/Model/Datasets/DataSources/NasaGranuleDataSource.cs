using System;
using System.Collections.Generic;
using System.Text;

namespace DEM.Net.Core.Datasets
{
    // https://e4ftl01.cr.usgs.gov/ASTT/ASTGTM.003/2000.03.01/
    public class NasaGranuleDataSource : IDEMDataSource
    {
        public string IndexFilePath { get; }

        public bool IsGlobalFile { get; } = false;

        public DEMDataSourceType DataSourceType => DEMDataSourceType.NASA;

        public NasaGranuleDataSource(string vrtFileUrl)
        {
            this.IndexFilePath = vrtFileUrl;
        }

    }
}
