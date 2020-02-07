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
    ///     Represetns data transfer object used by PBF serializer for metadata in dense format.
    /// </summary>
    [Serializable, ProtoContract(Name = "DenseInfo")]
    internal class PbfDenseMetadata
    {
        #region Private ImplicitFields

        private IList<long> _changeset = new List<long>();
        private IList<long> _timestamp = new List<long>();
        private IList<int> _userId = new List<int>();
        private IList<int> _userNameIndex = new List<int>();
        private IList<int> _version = new List<int>();
        private IList<bool> _visible = new List<bool>();

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the DenseInfo class with internal fields initialized to default capacity.
        /// </summary>
        public PbfDenseMetadata()
        {
            _changeset = new List<long>();
            _timestamp = new List<long>();
            _userId = new List<int>();
            _userNameIndex = new List<int>();
            _version = new List<int>();
            _visible = new List<bool>();
        }

        /// <summary>
        ///     Initializes a new instance of the DenseInfo class with internal fields initialized to specified capacity.
        /// </summary>
        /// <param name="capacity">The desired capacity of internal fields.</param>
        public PbfDenseMetadata(int capacity)
        {
            _changeset = new List<long>(capacity);
            _timestamp = new List<long>(capacity);
            _userId = new List<int>(capacity);
            _userNameIndex = new List<int>(capacity);
            _version = new List<int>(capacity);
            _visible = new List<bool>(capacity);
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets changeset id for corresponding node in DenseNodes. Property is delta encoded.
        /// </summary>
        [ProtoMember(3, Name = "changeset", Options = MemberSerializationOptions.Packed, DataFormat = DataFormat.ZigZag)
        ]
        public IList<long> Changeset
        {
            get { return _changeset; }
            set { _changeset = value; }
        }

        /// <summary>
        ///     Gets or sets timestamp as number of DateGranularity from UNIX 1970 epoch for the corresponding node in DenseNodes.
        ///     Property is delta encoded.
        /// </summary>
        /// <example>
        ///     DateTime LastChange = _unixEpoch.AddMilliseconds(timestamp * block.DateGranularity).
        /// </example>
        [ProtoMember(2, Name = "timestamp", Options = MemberSerializationOptions.Packed, DataFormat = DataFormat.ZigZag)
        ]
        public IList<long> Timestamp
        {
            get { return _timestamp; }
            set { _timestamp = value; }
        }

        /// <summary>
        ///     Gets or sets UserId for corresponding node in DenseNodes. Property is delta encoded.
        /// </summary>
        [ProtoMember(4, Name = "uid", Options = MemberSerializationOptions.Packed, DataFormat = DataFormat.ZigZag)]
        public IList<int> UserId
        {
            get { return _userId; }
            set { _userId = value; }
        }

        /// <summary>
        ///     Gets or sets index of the UserName in StringTable for corresponding node in DenseNodes. Property is delta encoded.
        /// </summary>
        [ProtoMember(5, Name = "user_sid", Options = MemberSerializationOptions.Packed, DataFormat = DataFormat.ZigZag)]
        public IList<int> UserNameIndex
        {
            get { return _userNameIndex; }
            set { _userNameIndex = value; }
        }

        /// <summary>
        ///     Gets or sets version of the corresponding node in DenseNodes.
        /// </summary>
        [ProtoMember(1, Name = "version", Options = MemberSerializationOptions.Packed)]
        public IList<int> Version
        {
            get { return _version; }
            set { _version = value; }
        }

        /// <summary>
        ///     Gets or sets visible attribute for corresponding node in DenseNodes.
        /// </summary>
        [ProtoMember(6, Name = "visible", Options = MemberSerializationOptions.Packed)]
        public IList<bool> Visible
        {
            get { return _visible; }
            set { _visible = value; }
        }

        #endregion
    }
}