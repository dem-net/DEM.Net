using System.Collections.Generic;
using DEM.Net.Extension.Osm.Schema;

namespace DEM.Net.Extension.Osm
{
    public interface IOsmReader
    {
        IEnumerable<Node> ReadNodes(string fileName, AttributeRegistry attributeRegistry);
        IEnumerable<Relation> ReadRelations(string fileName, AttributeRegistry attributeRegistry);
        IEnumerable<Way> ReadWays(string fileName, AttributeRegistry attributeRegistry);
    }
}