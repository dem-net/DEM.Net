using System.Threading;
using System.Threading.Tasks;

namespace DEM.Net.Core
{
    public interface IRasterDownloader
    {
        /// <summary>Downloads the raster file of the report to <see cref="DemFileReport.LocalName"/>.</summary>
        Task DownloadRasterFileAsync(DemFileReport report, DEMDataSet dataset, CancellationToken cancellationToken = default);
    }
}
