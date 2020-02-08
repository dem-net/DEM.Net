using System.Collections.Generic;

namespace DEM.Net.Extension.Osm.Schema
{
    public class Node
    {
        public Node()
        {
            Tags = new List<Tag>();
        }

        public long NodeId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public List<Tag> Tags { get; set; }
    }
}