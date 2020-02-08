//---------------------------------------------------------------------
//---------------------------------------------------------------------
// Library/Code is from http://spatial.litesolutions.net
//---------------------------------------------------------------------
//---------------------------------------------------------------------

using System.Collections.Generic;
using ProtoBuf;

namespace DEM.Net.Extension.Osm.Protobuf
{
    /// <summary>
    ///     Represetns data transfer object used by PBF serializer for relations.
    /// </summary>
    [ProtoContract(Name = "Relation")]
    internal class PbfRelation
    {
        #region Private Fields

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the PbfRelation class with internal fields initialized to default capacity.
        /// </summary>
        public PbfRelation()
        {
            MemberIds = new List<long>();
            RolesIndexes = new List<int>();
            Types = new List<PbfRelationMemberType>();
        }

        /// <summary>
        ///     Initializes a new instance of the PbfRelation class with internal fields initialized to specified capacity.
        /// </summary>
        /// <param name="capacity">The desired capacity of internal fields.</param>
        public PbfRelation(int capacity)
        {
            MemberIds = new List<long>(capacity);
            RolesIndexes = new List<int>(capacity);
            Types = new List<PbfRelationMemberType>(capacity);
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets ID of the relation.
        /// </summary>
        [ProtoMember(1, Name = "id", IsRequired = true)]
        public long ID { get; set; }

        /// <summary>
        ///     Gets or sets relation metadata.
        /// </summary>
        [ProtoMember(4, Name = "info", IsRequired = false)]
        public PbfMetadata Metadata { get; set; }

        /// <summary>
        ///     Gets or sets indexes of tag's keys in string table.
        /// </summary>
        [ProtoMember(2, Name = "keys", Options = MemberSerializationOptions.Packed)]
        public IList<uint> Keys { get; set; }

        /// <summary>
        ///     Gets or sets indexes of tag's values in string table.
        /// </summary>
        [ProtoMember(3, Name = "vals", Options = MemberSerializationOptions.Packed)]
        public IList<uint> Values { get; set; }

        /// <summary>
        ///     Gets or sets IDs of the relation members. This propeerty is delta encoded.
        /// </summary>
        [ProtoMember(9, Name = "memids", Options = MemberSerializationOptions.Packed, DataFormat = DataFormat.ZigZag)]
        public IList<long> MemberIds { get; set; }

        /// <summary>
        ///     Gets or sets index of the role in string table for appropriate members.
        /// </summary>
        [ProtoMember(8, Name = "roles_sid", Options = MemberSerializationOptions.Packed)]
        public IList<int> RolesIndexes { get; set; }

        /// <summary>
        ///     Gets or sets type of the relation members.
        /// </summary>
        [ProtoMember(10, Name = "types", Options = MemberSerializationOptions.Packed)]
        public IList<PbfRelationMemberType> Types { get; set; }

        #endregion
    }
}