using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DEM.Net.Core
{
    public interface IDEMDataSetIndex : IRasterDownloader
    {
        /// <summary>
        /// Prepares the index of the dataset (downloads and caches the remote index once). Safe to call concurrently
        /// and repeatedly: the work is done once per dataset.
        /// </summary>
        Task SetupAsync(DEMDataSet dataset, string dataSetLocalDir, CancellationToken cancellationToken = default);

        /// <summary>All file sources of the dataset. <see cref="SetupAsync"/> must have been called first.</summary>
        IEnumerable<DEMFileSource> GetFileSources(DEMDataSet dataset);

        /// <summary>File sources intersecting the bbox. <see cref="SetupAsync"/> must have been called first.</summary>
        IEnumerable<DEMFileSource> GetCoveredFileSources(DEMDataSet dataset, BoundingBox bbox);

        void Reset();

    }
}
