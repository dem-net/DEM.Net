using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using DEM.Net.Extension.Osm.Schema;
using DEM.Net.Extension.Osm.Protobuf;
using ProtoBuf;

namespace DEM.Net.Extension.Osm
{
    public class PbfOsmReader : IOsmReader
    {
        public const int MaxDataBlockSize = 32*1024*1024;
        public const int MaxHeaderBlockSize = 64*1024;

        public IEnumerable<Node> ReadNodes(string fileName, AttributeRegistry attributeRegistry)
        {
            using (var file = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                BlobHeader blobHeader = null;

                while ((blobHeader = ReadBlobHeader(file)) != null)
                {
                    var block = ReadBlob(file, blobHeader) as PrimitiveBlock;
                    if (block != null)
                    {
                        foreach (var group in block.PrimitiveGroup)
                        {
                            foreach (var item in ProcessNodes(block, group, attributeRegistry))
                                yield return item;

                            foreach (var item in ProcessDenseNodes(block, group, attributeRegistry))
                                yield return item;
                        }
                    }
                }
            }
        }

        public IEnumerable<Relation> ReadRelations(string fileName, AttributeRegistry attributeRegistry)
        {
            using (var file = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                BlobHeader blobHeader = null;

                while ((blobHeader = ReadBlobHeader(file)) != null)
                {
                    var block = ReadBlob(file, blobHeader) as PrimitiveBlock;
                    if (block != null)
                    {
                        foreach (var group in block.PrimitiveGroup)
                        {
                            if (group.Relations == null)
                                continue;

                            foreach (var relation in group.Relations)
                            {
                                var rel = new Relation
                                {
                                    RelationId = relation.ID
                                };
                                long memberRefStore = 0;

                                for (var i = 0; i < relation.MemberIds.Count; i++)
                                {
                                    memberRefStore += relation.MemberIds[i];
                                    var role = block.StringTable[relation.RolesIndexes[i]];

                                    rel.Members.Add(
                                        new Relation.Member
                                        {
                                            Type =
                                                attributeRegistry.GetAttributeValueId(OsmAttribute.MemberType,
                                                    relation.Types[i].ToString()),
                                            Ref = memberRefStore,
                                            Role = attributeRegistry.GetAttributeValueId(OsmAttribute.MemberRole, role)
                                        });
                                }


                                var usedTagTypes = new HashSet<int>();
                                if (relation.Keys != null)
                                {
                                    for (var i = 0; i < relation.Keys.Count; i++)
                                    {
                                        var tagType = attributeRegistry.GetAttributeValueId(OsmAttribute.TagType,
                                            block.StringTable[relation.Keys[i]]);
                                        if (!usedTagTypes.Contains(tagType))
                                        {
                                            rel.Tags.Add(new Tag
                                            {
                                                Value = block.StringTable[relation.Values[i]],
                                                Typ = tagType
                                            });
                                            usedTagTypes.Add(tagType);
                                        }
                                    }
                                }
                                yield return rel;
                            }
                        }
                    }
                }
            }
        }

        public IEnumerable<Way> ReadWays(string fileName, AttributeRegistry attributeRegistry)
        {
            using (var file = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                BlobHeader blobHeader = null;

                while ((blobHeader = ReadBlobHeader(file)) != null)
                {
                    var block = ReadBlob(file, blobHeader) as PrimitiveBlock;
                    if (block != null)
                    {
                        foreach (var group in block.PrimitiveGroup)
                        {
                            if (group.Ways == null)
                                continue;
                            foreach (var way in group.Ways)
                            {
                                var tags = new List<Tag>();
                                var usedTagTypes = new HashSet<int>();
                                if (way.Keys != null)
                                {
                                    for (var i = 0; i < way.Keys.Count; i++)
                                    {
                                        var tagType = attributeRegistry.GetAttributeValueId(OsmAttribute.TagType,
                                            block.StringTable[way.Keys[i]]);
                                        if (!usedTagTypes.Contains(tagType))
                                        {
                                            tags.Add(new Tag
                                            {
                                                Value = block.StringTable[way.Values[i]],
                                                Typ = tagType
                                            });
                                            usedTagTypes.Add(tagType);
                                        }
                                    }
                                }

                                long refStore = 0;
                                var refs = new List<long>(way.Refs.Count);
                                foreach (var t in way.Refs)
                                {
                                    refStore += t;
                                    refs.Add(refStore);
                                }

                                yield return new Way
                                {
                                    WayId = way.ID,
                                    NodeRefs = refs,
                                    Tags = tags
                                };
                            }
                        }
                    }
                }
            }
        }

        private IEnumerable<Node> ProcessDenseNodes(PrimitiveBlock block, PrimitiveGroup group,
            AttributeRegistry attributeRegistry)
        {
            if (group.DenseNodes == null)
            {
                yield break;
            }

            long idStore = 0;
            long latStore = 0;
            long lonStore = 0;

            var keyValueIndex = 0;


            for (var i = 0; i < group.DenseNodes.Id.Count; i++)
            {
                idStore += group.DenseNodes.Id[i];
                lonStore += group.DenseNodes.Longitude[i];
                latStore += group.DenseNodes.Latitude[i];

                var lat = 1E-09*(block.LatOffset + (block.Granularity*latStore));
                var lon = 1E-09*(block.LonOffset + (block.Granularity*lonStore));

                var tags = new List<Tag>();
                if (group.DenseNodes.KeysVals.Count > 0)
                {
                    while (group.DenseNodes.KeysVals[keyValueIndex] != 0)
                    {
                        var key = block.StringTable[group.DenseNodes.KeysVals[keyValueIndex++]];
                        var value = block.StringTable[group.DenseNodes.KeysVals[keyValueIndex++]];
                        var tagType = attributeRegistry.GetAttributeValueId(OsmAttribute.TagType, key);
                        var tag = new Tag
                        {
                            Typ = tagType,
                            Value = value
                        };
                        tags.Add(tag);
                    }
                    keyValueIndex++;
                }

                var node = new Node
                {
                    NodeId = idStore,
                    Latitude = lat,
                    Longitude = lon,
                    Tags = tags
                };
                yield return node;
            }
        }

        private IEnumerable<Node> ProcessNodes(PrimitiveBlock block, PrimitiveGroup group,
            AttributeRegistry attributeRegistry)
        {
            if (group.Nodes == null)
            {
                yield break;
            }

            foreach (var node in group.Nodes)
            {
                var lat = 1E-09*(block.LatOffset + (block.Granularity*node.Latitude));
                var lon = 1E-09*(block.LonOffset + (block.Granularity*node.Longitude));


                var tags = new List<Tag>();
                if (node.Keys != null)
                {
                    tags.AddRange(node.Keys.Select((t, i) => new Tag
                    {
                        Typ = attributeRegistry.GetAttributeValueId(OsmAttribute.TagType, block.StringTable[t]),
                        Value = block.StringTable[node.Values[i]]
                    }));
                }

                yield return new Node
                {
                    Latitude = lat,
                    Longitude = lon,
                    NodeId = node.ID,
                    Tags = tags
                };
            }
        }

        private BlobHeader ReadBlobHeader(Stream inputStream)
        {
            if (inputStream.Position < inputStream.Length)
            {
                return Serializer.DeserializeWithLengthPrefix<BlobHeader>(inputStream, PrefixStyle.Fixed32BigEndian);
            }
            return null;
        }

        private object ReadBlob(Stream inputStream, BlobHeader header)
        {
            var buffer = new byte[header.DataSize];
            inputStream.Read(buffer, 0, header.DataSize);
            Blob blob;
            using (var s = new MemoryStream(buffer))
            {
                blob = Serializer.Deserialize<Blob>(s);
            }

            Stream blobContentStream = null;
            try
            {
                if (blob.Raw != null)
                {
                    blobContentStream = new MemoryStream(blob.Raw);
                }
                else if (blob.ZlibData != null)
                {
                    var deflateStreamData = new MemoryStream(blob.ZlibData);
                    //skip ZLIB header
                    deflateStreamData.Seek(2, SeekOrigin.Begin);
                    blobContentStream = new DeflateStream(deflateStreamData, CompressionMode.Decompress);
                }

                if (header.Type.Equals("OSMData", StringComparison.InvariantCultureIgnoreCase))
                {
                    if ((blob.RawSize.HasValue && blob.RawSize > MaxDataBlockSize) ||
                        (blob.RawSize.HasValue == false && blobContentStream.Length > MaxDataBlockSize))
                    {
                        throw new InvalidDataException("Invalid OSMData block");
                    }

                    return Serializer.Deserialize<PrimitiveBlock>(blobContentStream);
                }
                else if (header.Type.Equals("OSMHeader", StringComparison.InvariantCultureIgnoreCase))
                {
                    if ((blob.RawSize.HasValue && blob.RawSize > MaxHeaderBlockSize) ||
                        (blob.RawSize.HasValue == false && blobContentStream.Length > MaxHeaderBlockSize))
                    {
                        throw new InvalidDataException("Invalid OSMHeader block");
                    }

                    return Serializer.Deserialize<OsmHeader>(blobContentStream);
                }
                else
                {
                    return null;
                }
            }
            finally
            {
                if (blobContentStream != null)
                {
                    blobContentStream.Close();
                    blobContentStream.Dispose();
                }
            }
        }
    }
}