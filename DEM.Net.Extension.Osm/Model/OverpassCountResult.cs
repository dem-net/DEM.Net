using System;
using System.Collections.Generic;
using System.Text;

namespace DEM.Net.Extension.Osm.Model
{
    public class OverpassCountResult
    {
        /*
        "type": "count",
  "id": 0,
  "tags": {
    "nodes": "210",
    "ways": "19",
    "relations": "0",
    "total": "229"
  }
  */

        public string Type { get; set; }
        public int Id { get; set; }
        public Tags Tags { get; set; }
    }

    public class Tags
    {
        public int Nodes { get; set; }
        public int Ways { get; set; }
        public int Relations { get; set; }
        public int Total { get; set; }
    }
}
