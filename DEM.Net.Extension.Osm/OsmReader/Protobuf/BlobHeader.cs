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
    ///     Represents header of the fileblock.
    /// </summary>
    [Serializable, ProtoContract]
    internal class BlobHeader
    {
        #region Public Properties

        /// <summary>
        ///     Gets or sets type of the fileblock.
        /// </summary>
        /// <remarks>
        ///     Supported values are 'OSMHeader' and 'OSMData'.
        /// </remarks>
        [ProtoMember(1, IsRequired = true, Name = "type")]
        public string Type { get; set; }

        /// <summary>
        ///     Gets or sets an arbitrary blob that may include metadata about the following blob. For future use.
        /// </summary>
        [ProtoMember(2, IsRequired = false, Name = "indexdata")]
        public byte[] IndexData { get; set; }

        /// <summary>
        ///     Gets or sets size of the subsequent Blob message.
        /// </summary>
        [ProtoMember(3, IsRequired = true, Name = "datasize")]
        public int DataSize { get; set; }

        #endregion
    }
}