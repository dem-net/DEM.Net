using SixLabors.ImageSharp.ColorSpaces;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace DEM.Net.Core
{
    public class Polygon<T> : IPolygon<T>
    {
        public Polygon(List<T> outerRing, params List<T>[] innerRings)
        {
            this.ExteriorRing = outerRing;
            this.InteriorRings = innerRings == null ? null : new List<List<T>>(innerRings);
        }
        public List<T> ExteriorRing { get; set; }

        public List<List<T>> InteriorRings { get; set; }

        public int NumPoints => ExteriorRing.Count + (InteriorRings?.Sum(r => r.Count) ?? 0);
    }
}
