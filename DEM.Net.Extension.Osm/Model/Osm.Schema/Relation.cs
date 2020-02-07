using System.Collections.Generic;

namespace DEM.Net.Extension.Osm.Schema
{
    public class Relation
    {
        public long RelationId;

        public Relation()
        {
            Tags = new List<Tag>();
            Members = new List<Member>();
        }

        public List<Tag> Tags { get; set; }
        public List<Member> Members { get; set; }

        public class Member
        {
            public long Ref;
            public int Role;
            public int Type;
        }
    }
}