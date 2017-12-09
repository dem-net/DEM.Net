using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib
{
    public class GDALSource
    {

        public bool RelativeToVRT { get; set; }
        public string SourceFileName { get; set; }
        public string Type { get; internal set; }
        public Dictionary<string,string> Properties { get; set; }
        public int DstxOff { get; internal set; }
        public int DstyOff { get; internal set; }
        public int DstxSize { get; internal set; }
        public int DstySize { get; internal set; }
        public double NoData { get; internal set; }

        public GDALSource()
        {
            Properties = new Dictionary<string, string>();
        }
    }
}
