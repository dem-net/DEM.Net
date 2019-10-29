using DEM.Net.Core.EarthData;
using System;
using System.Collections.Generic;
using System.Text;

namespace DEM.Net.Core.EarthData
{
    /// <summary>
    /// Extract of relevant granule information for DEM Net
    /// (whole granule is too big)
    /// </summary>
    public class NasaDemFile
    {
        public string Box { get; set; }

        public string ZipFileLink { get; set; }

        public string GranuleId { get; set; }

        public NasaDemFile(string producerGranuleId, string box, string zipFileLink)
        {
            this.Box = box;
            this.ZipFileLink = zipFileLink;
            this.GranuleId = producerGranuleId;
        }
    }
}
