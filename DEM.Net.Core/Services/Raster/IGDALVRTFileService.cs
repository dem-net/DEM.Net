using System.Collections.Generic;

namespace DEM.Net.Core
{
    public interface IGDALVRTFileService
    {
        void Setup(DEMDataSet dataset, string dataSetLocalDir);

        IEnumerable<GDALSource> Sources(DEMDataSet dataset);
    }
}