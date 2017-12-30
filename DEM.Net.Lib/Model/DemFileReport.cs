using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib
{
    public class DemFileReport
    {
        bool isZipped { get; set; }
        public string URL { get; set; }
        public string LocalName { get; set; }
        public string LocalZipName { get; set; }

        public bool IsExistingLocally { get; set; }
        public GDALSource Source { get; internal set; }
		public bool IsMetadataGenerated { get; internal set; }
	}
}
