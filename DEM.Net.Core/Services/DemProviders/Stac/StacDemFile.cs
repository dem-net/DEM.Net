using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DEM.Net.Core.EarthData;

namespace DEM.Net.Core.Stac;

/// <summary>
/// Extract of relevant granule information for DEM Net
/// (whole granule is too big)
/// </summary>
public class StacDemFile(string fileId, List<float> box, string href, DateTimeOffset dateTime) : IEquatable<StacDemFile>
{
    public string FileId { get; } = fileId;
    public List<float> Box { get; } = box;
    public string Href { get; } = href;
    public DateTimeOffset DateTime { get; } = dateTime;

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
