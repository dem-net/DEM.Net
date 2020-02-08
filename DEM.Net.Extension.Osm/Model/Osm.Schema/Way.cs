using System.Collections.Generic;

namespace DEM.Net.Extension.Osm.Schema
{
    public class Way
    {
        public long WayId;

        public Way()
        {
            Tags = new List<Tag>();
            NodeRefs = new List<long>();
        }

        public List<Tag> Tags { get; set; }
        public List<long> NodeRefs { get; set; }
    }
}