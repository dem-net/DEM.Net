using DEM.Net.Core.EarthData;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace DEM.Net.Core.Stac
{
    /// <summary>
    /// Extract of relevant granule information for DEM Net
    /// (whole granule is too big)
    /// </summary>
    public class StacDemFile : IEquatable<StacDemFile>
    {
        public string FileId { get; }
        public List<float> Box { get; }
        public string Href { get; }
        public DateTimeOffset DateTime { get; } // from feature

        public StacDemFile(string fileId, List<float> box, string href, DateTimeOffset dateTime)
        {
            this.FileId = fileId;
            this.Box = box;
            this.Href = href;
            this.DateTime = dateTime;
        }

        public bool Equals(StacDemFile other)
        {
            return this.Box.SequenceEqual(other.Box);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.Box[0], this.Box[1], this.Box[2], this.Box[3]);
        }

        public override bool Equals(object obj)
        {
            if (this==null)
                return false;

            if (obj is StacDemFile other)
                return this.Box.SequenceEqual(other.Box);

            return false;
        }
    }
}
