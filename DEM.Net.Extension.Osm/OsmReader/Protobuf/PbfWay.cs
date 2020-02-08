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
    ///     Represetns data transfer object used by PBF serializer for Ways.
    /// </summary>
    [Serializable, ProtoContract(Name = "Way")]
    internal class PbfWay
    {
        #region Private ImplicitFields

        private IList<long> _refs;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the PbfWay class with internal fields initialized to default capacity.
        /// </summary>
        public PbfWay()
        {
            _refs = new List<long>();
        }

        /// <summary>
        ///     Initializes a new instance of the PbfWay class with internal fields initialized to specified capacity.
        /// </summary>
        /// <param name="capacity">The desired capacity of internal fields.</param>
        public PbfWay(int capacity)
        {
            _refs = new List<long>(capacity);
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets ID of the way.
        /// </summary>
        [ProtoMember(1, Name = "id", IsRequired = true)]
        public long ID { get; set; }

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
        ///     Gets or sets entity metadata .
        /// </summary>
        [ProtoMember(4, Name = "info", IsRequired = false)]
        public PbfMetadata Metadata { get; set; }

        /// <summary>
        ///     Gets or sets IDs of nodes referenced by the way. This property is delta encoded.
        /// </summary>
        [ProtoMember(8, Name = "refs", Options = MemberSerializationOptions.Packed, DataFormat = DataFormat.ZigZag)]
        public IList<long> Refs
        {
            get { return _refs; }
            set { _refs = value; }
        }

        #endregion
    }
}