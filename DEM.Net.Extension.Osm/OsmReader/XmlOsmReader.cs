using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using DEM.Net.Extension.Osm.Schema;

namespace DEM.Net.Extension.Osm
{
    public class XmlOsmReader : IOsmReader
    {
        public IEnumerable<Relation> ReadRelations(string fileName, AttributeRegistry attributeRegistry)
        {
            using (var file = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var reader = XmlReader.Create(file))
                {
                    reader.MoveToContent();
                    reader.ReadStartElement("osm");

                    var foundFirstElement = false;
                    while (reader.IsStartElement())
                    {
                        if (reader.Name != "relation")
                        {
                            reader.Skip();
                            if (foundFirstElement)
                                yield break;
                            continue;
                        }

                        foundFirstElement = true;
                        var relationId = long.Parse(reader.GetAttribute("id"));
                        var relation = new Relation
                        {
                            RelationId = relationId
                        };
                        reader.ReadStartElement(); // node
                        reader.Skip();

                        while (reader.Name == "member")
                        {
                            relation.Members.Add(
                                new Relation.Member
                                {
                                    Type =
                                        attributeRegistry.GetAttributeValueId(OsmAttribute.MemberType,
                                            reader.GetAttribute("type")),
                                    Ref = long.Parse(reader.GetAttribute("ref")),
                                    Role =
                                        attributeRegistry.GetAttributeValueId(OsmAttribute.MemberRole,
                                            reader.GetAttribute("role"))
                                });
                            reader.ReadStartElement();
                            reader.Skip();
                        }

                        var usedTagTypes = new HashSet<int>();
                        while (reader.Name == "tag")
                        {
                            var tagType = attributeRegistry.GetAttributeValueId(OsmAttribute.TagType,
                                reader.GetAttribute("k"));
                            if (!usedTagTypes.Contains(tagType))
                            {
                                relation.Tags.Add(new Tag
                                {
                                    Value = reader.GetAttribute("v"),
                                    Typ = tagType
                                });
                                usedTagTypes.Add(tagType);
                            }
                            reader.ReadStartElement();
                            reader.Skip();
                        }

                        if (reader.NodeType == XmlNodeType.EndElement)
                            reader.ReadEndElement();

                        yield return relation;
                    }
                }
            }
        }

        public IEnumerable<Way> ReadWays(string fileName, AttributeRegistry attributeRegistry)
        {
            using (var file = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var reader = XmlReader.Create(file))
                {
                    reader.MoveToContent();
                    reader.ReadStartElement("osm");

                    var foundFirstElement = false;
                    while (reader.IsStartElement())
                    {
                        if (reader.Name != "way")
                        {
                            reader.Skip();
                            if (foundFirstElement)
                                yield break;
                            continue;
                        }

                        foundFirstElement = true;
                        var wayId = long.Parse(reader.GetAttribute("id"));
                        var way = new Way
                        {
                            WayId = wayId
                        };
                        reader.ReadStartElement(); // node
                        reader.Skip();

                        while (reader.Name == "nd")
                        {
                            way.NodeRefs.Add(long.Parse(reader.GetAttribute("ref")));
                            reader.ReadStartElement();
                            reader.Skip();
                        }

                        var usedTagTypes = new HashSet<int>();
                        while (reader.Name == "tag")
                        {
                            var tagType = attributeRegistry.GetAttributeValueId(OsmAttribute.TagType,
                                reader.GetAttribute("k"));

                            if (!usedTagTypes.Contains(tagType))
                            {
                                way.Tags.Add(new Tag
                                {
                                    Value = reader.GetAttribute("v"),
                                    Typ = tagType
                                });
                                usedTagTypes.Add(tagType);
                            }

                            reader.ReadStartElement();
                            reader.Skip();
                        }

                        if (reader.NodeType == XmlNodeType.EndElement)
                            reader.ReadEndElement();
                        yield return way;
                    }
                }
            }
        }

        public IEnumerable<Node> ReadNodes(string fileName, AttributeRegistry attributeRegistry)
        {
            using (var file = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var reader = XmlReader.Create(file))
                {
                    reader.MoveToContent();
                    reader.ReadStartElement("osm");

                    var foundFirstElement = false;
                    while (reader.IsStartElement())
                    {
                        if (reader.Name != "node")
                        {
                            reader.Skip();
                            if (foundFirstElement)
                                yield break;
                            continue;
                        }
                        foundFirstElement = true;
                        var node = new Node
                        {
                            NodeId = long.Parse(reader.GetAttribute("id")),
                            Latitude = double.Parse(reader.GetAttribute("lat"), CultureInfo.InvariantCulture),
                            Longitude = double.Parse(reader.GetAttribute("lon"), CultureInfo.InvariantCulture)
                        };

                        reader.ReadStartElement();
                        reader.Skip();

                        var usedTagTypes = new HashSet<int>();
                        while (reader.Name == "tag")
                        {
                            var key = reader.GetAttribute("k");
                            var tagType = attributeRegistry.GetAttributeValueId(OsmAttribute.TagType, key);
                            if (!usedTagTypes.Contains(tagType))
                            {
                                node.Tags.Add(new Tag
                                {
                                    Value = reader.GetAttribute("v"),
                                    Typ = tagType
                                });
                                usedTagTypes.Add(tagType);
                            }

                            reader.ReadStartElement();
                            reader.Skip();
                        }

                        if (reader.NodeType == XmlNodeType.EndElement)
                            reader.ReadEndElement();

                        yield return node;
                    }
                }
            }
        }
    }
}