using System;
using System.Collections.Generic;
using System.Text;

namespace DEM.Net.Extension.Osm
{
    public class CommonModel
    {
        public string Id { get; internal set; }
        public IDictionary<string, object> Tags { get; internal set; }
    }
}
