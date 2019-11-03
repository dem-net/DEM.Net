using System.Collections.Generic;

namespace DEM.Net.Core
{
    public interface IDEMDataSetIndex : IRasterDownloader
    {
        void Setup(DEMDataSet dataset, string dataSetLocalDir);

        IEnumerable<DEMFileSource> GetFileSources(DEMDataSet dataset);

        IEnumerable<DEMFileSource> GetCoveredFileSources(DEMDataSet dataset, BoundingBox bbox);

        void Reset();

    }
}