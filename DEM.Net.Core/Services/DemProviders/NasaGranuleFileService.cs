using System;
using System.Collections.Generic;
using System.Text;

namespace DEM.Net.Core.EarthData
{
    public class NasaGranuleFileService : IDEMDataSetIndex
    {
        private DEMDataSet dataset;
        private string localDir;

        public IEnumerable<DEMFileSource> GetCoveredFileSources(DEMDataSet dataset, BoundingBox bbox)
        {
            throw new NotImplementedException(nameof(NasaGranuleFileService));
        }

        public IEnumerable<DEMFileSource> GetFileSources(DEMDataSet dataset)
        {
            throw new NotImplementedException(nameof(NasaGranuleFileService));
            // S83 to N82 => -83 < lat < 83
            // W180 to E179 => -180 < lon < 180
            // 
            // example with N00E006
            // 6 < lon < 7
            // 0 < lat < 1


        }

        public void Reset()
        {
        }

        public void Setup(DEMDataSet dataset, string dataSetLocalDir)
        {
            this.dataset = dataset;
            this.localDir = dataSetLocalDir;
        }
    }
}
