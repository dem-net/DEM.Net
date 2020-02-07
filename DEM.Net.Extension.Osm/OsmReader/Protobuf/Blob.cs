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
    ///     Represents content of the file block.
    /// </summary>
    [Serializable, ProtoContract]
    internal class Blob
    {
        #region Public Properties

        /// <summary>
        ///     Gets or sets blob content if no compression is used.
        /// </summary>
        [ProtoMember(1, IsRequired = false, Name = "raw")]
        public byte[] Raw { get; set; }

        /// <summary>
        ///     Gets or sets uncopressed size of the blob content if ZLIB compression is used.
        /// </summary>
        [ProtoMember(2, IsRequired = false, Name = "raw_size")]
        public int? RawSize { get; set; }

        /// <summary>
        ///     Gets or sets blob content if ZLIB compression is used.
        /// </summary>
        [ProtoMember(3, IsRequired = false, Name = "zlib_data")]
        public byte[] ZlibData { get; set; }

        #endregion
    }
}