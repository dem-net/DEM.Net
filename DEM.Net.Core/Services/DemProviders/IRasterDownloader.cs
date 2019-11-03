using System;
using System.Collections.Generic;
using System.Text;

namespace DEM.Net.Core
{
    public interface IRasterDownloader
    {
        void DownloadRasterFile(DemFileReport report, DEMDataSet dataset);
    }
}
