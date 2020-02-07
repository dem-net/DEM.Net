//---------------------------------------------------------------------
//---------------------------------------------------------------------
// Library/Code is from http://spatial.litesolutions.net
//---------------------------------------------------------------------
//---------------------------------------------------------------------

using System;
using ProtoBuf;

namespace DEM.Net.Extension.Osm.Protobuf
{
    /// <summary>
    ///     Represetns data transfer object used by PBF serializer for Entity metadata.
    /// </summary>
    [Serializable, ProtoContract(Name = "Info")]
    internal class PbfMetadata
    {
        #region Public Propeties

        /// <summary>
        ///     Gets or sets changeset ID.
        /// </summary>
        [ProtoMember(3, Name = "changeset", IsRequired = false)]
        public long? Changeset { get; set; }

        /// <summary>
        ///     Gets or sets Timestamp.
        /// </summary>
        [ProtoMember(2, Name = "timestamp", IsRequired = false)]
        public long? Timestamp { get; set; }

        /// <summary>
        ///     Gets or sets UserID.
        /// </summary>
        [ProtoMember(4, Name = "uid", IsRequired = false)]
        public int? UserID { get; set; }

        /// <summary>
        ///     Gets or sets index of the username in string table.
        /// </summary>
        [ProtoMember(5, Name = "user_sid", IsRequired = false)]
        public int? UserNameIndex { get; set; }

        /// <summary>
        ///     Gets or sets version of the entity.
        /// </summary>
        [ProtoMember(1, Name = "version", IsRequired = false)]
        public int? Version { get; set; }

        #endregion
    }
}