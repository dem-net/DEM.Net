using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core
{
    public interface IRasterDownloader
    {
        Task DownloadRasterFileAsync(DemFileReport report, DEMDataSet dataset);
    }
}
