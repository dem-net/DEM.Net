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
    ///     Represents content of the fileblock's data stream.
    /// </summary>
    [Serializable, ProtoContract(Name = "PrimitiveBlock")]
    internal class PrimitiveBlock
    {
        #region Private Fields

        private int _granularity = 100;
        private int _date_granularity = 1000;

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets StringTable with all strings used in the block.
        /// </summary>
        [ProtoMember(1, IsRequired = true, Name = "stringtable")]
        public StringTable StringTable { get; set; }

        /// <summary>
        ///     Gets or sets PrimitiveGroup object with OSM entities.
        /// </summary>
        [ProtoMember(2, Name = "primitivegroup")]
        public List<PrimitiveGroup> PrimitiveGroup { get; set; }

        /// <summary>
        ///     Gets or sets granularity of the position data. Default value is 1000.
        /// </summary>
        [ProtoMember(16, IsRequired = false, Name = "granularity")]
        public int Granularity
        {
            get { return _granularity; }
            set { _granularity = value; }
        }

        /// <summary>
        ///     Gets or sets latitude offset.
        /// </summary>
        [ProtoMember(19, IsRequired = false, Name = "lat_offset")]
        public long LatOffset { get; set; }

        /// <summary>
        ///     Gets or sets longitude offset.
        /// </summary>
        [ProtoMember(20, IsRequired = false, Name = "lon_offset")]
        public long LonOffset { get; set; }

        /// <summary>
        ///     Gets or sets granularity of the DateTime data. Default value is 100.
        /// </summary>
        [ProtoMember(18, IsRequired = false, Name = "date_granularity")]
        public int DateGranularity
        {
            get { return _date_granularity; }
            set { _date_granularity = value; }
        }

        #endregion
    }
}