using System.Collections.Generic;

namespace DEM.Net.Core
{
    public interface IGDALVRTFileService
    {
        DEMDataSet Dataset { get; }
        void Setup(bool useMemoryCache);
        IEnumerable<GDALSource> Sources();
    }
}