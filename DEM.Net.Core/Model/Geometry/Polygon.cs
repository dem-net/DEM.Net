using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SixLabors.ImageSharp.ColorSpaces;

namespace DEM.Net.Core;

public class Polygon<T>(List<T> outerRing, params List<T>[] innerRings) : IPolygon<T>
{
    public List<T> ExteriorRing { get; set; } = outerRing;

    public List<List<T>> InteriorRings { get; set; } = innerRings == null ? null! : new List<List<T>>(innerRings);

    public int NumPoints => ExteriorRing.Count + (InteriorRings?.Sum(r => r.Count) ?? 0);
}
