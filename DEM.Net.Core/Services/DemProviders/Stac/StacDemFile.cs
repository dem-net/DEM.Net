using DEM.Net.Core.EarthData;
using System;
using System.Collections.Generic;
using System.Text;

namespace DEM.Net.Core.Stac
{
    /// <summary>
    /// Extract of relevant granule information for DEM Net
    /// (whole granule is too big)
    /// </summary>
    public class StacDemFile
    {
        public string FileId { get; }
        public List<float> Box { get; set; }

        public string Href { get; set; }
                
        public StacDemFile(string fileId, List<float> box, string href)
        {
            this.FileId = fileId;
            this.Box = box;
            this.Href = href;
        }
    }
}
