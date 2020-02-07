//---------------------------------------------------------------------
//---------------------------------------------------------------------
// Library/Code is from http://spatial.litesolutions.net
//---------------------------------------------------------------------
//---------------------------------------------------------------------

using System;
using System.Collections.Generic;
using ProtoBuf;

namespace DEM.Net.Extension.Osm.Protobuf
{
    /// <summary>
    ///     Represetns data transfer object used by PBF serializer for nodes.
    /// </summary>
    [Serializable, ProtoContract(Name = "Node")]
    internal class PbfNode
    {
        #region Public Properties

        /// <summary>
        ///     Gets or sets ID of the node.
        /// </summary>
        [ProtoMember(1, IsRequired = true, Name = "id", DataFormat = DataFormat.ZigZag)]
        public long ID { get; set; }

        /// <summary>
        ///     Gets or sets indexes of tag's keys in string table.
        /// </summary>
        [ProtoMember(2, Name = "keys", Options = MemberSerializationOptions.Packed)]
        public List<uint> Keys { get; set; }

        /// <summary>
        ///     Gets or sets indexes of tag's values in string table.
        /// </summary>
        [ProtoMember(3, Name = "vals", Options = MemberSerializationOptions.Packed)]
        public List<uint> Values { get; set; }

        /// <summary>
        ///     Gets or sets entity metadata.
        /// </summary>
        [ProtoMember(4, IsRequired = false, Name = "info")]
        public PbfMetadata Metadata { get; set; }

        /// <summary>
        ///     Gets or sets Latitude of the node as number of granularity steps from LatOffset.
        /// </summary>
        [ProtoMember(8, IsRequired = true, Name = "lat", DataFormat = DataFormat.ZigZag)]
        public long Latitude { get; set; }

        /// <summary>
        ///     Gets or sets Longitude of the node as number of granularity steps from LonOffset.
        /// </summary>
        [ProtoMember(9, IsRequired = true, Name = "lon", DataFormat = DataFormat.ZigZag)]
        public long Longitude { get; set; }

        #endregion
    }
}