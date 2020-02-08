//---------------------------------------------------------------------
//---------------------------------------------------------------------
// Library/Code is from http://spatial.litesolutions.net
//---------------------------------------------------------------------
//---------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace DEM.Net.Extension.Osm.Protobuf
{
    /// <summary>
    ///     Stores all strings for Primitive block.
    /// </summary>
    [Serializable, ProtoContract(Name = "StringTable")]
    public class StringTable
    {
        #region Private Fields

        private List<byte[]> _s = new List<byte[]>();

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets collection of strings serialized as byte array.
        /// </summary>
        [ProtoMember(1, Name = "s", DataFormat = DataFormat.Default)]
        public List<byte[]> Storage
        {
            get { return _s; }
            set { _s = value; }
        }

        /// <summary>
        ///     Gets or sets string at specified position.
        /// </summary>
        /// <param name="index">The index of the string.</param>
        /// <returns>string at specified position.</returns>
        [ProtoIgnore]
        public string this[int index]
        {
            get
            {
                if (index >= Storage.Count)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                return Encoding.UTF8.GetString(Storage[index]);
            }
            set
            {
                if (index >= Storage.Count)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                Storage[index] = Encoding.UTF8.GetBytes(value);
            }
        }

        /// <summary>
        ///     Gets or sets string at specified position.
        /// </summary>
        /// <param name="index">The index of the string.</param>
        /// <returns>string at specified position.</returns>
        [ProtoIgnore]
        public string this[uint index]
        {
            get { return this[(int) index]; }
            set { this[(int) index] = value; }
        }

        #endregion
    }
}