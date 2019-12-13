using System;
using System.Collections.Generic;
using System.Text;

namespace DEM.Net.Core
{
    public class LocalFileSystemIndex : IDEMDataSetIndex
    {
        public void DownloadRasterFile(DemFileReport report, DEMDataSet dataset)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<DEMFileSource> GetCoveredFileSources(DEMDataSet dataset, BoundingBox bbox)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<DEMFileSource> GetFileSources(DEMDataSet dataset)
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public void Setup(DEMDataSet dataset, string dataSetLocalDir)
        {
            throw new NotImplementedException();
        }
    }
}
