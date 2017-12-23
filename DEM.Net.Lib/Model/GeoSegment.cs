using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib
{
    public class GeoSegment
    {
        public GeoPoint Start { get; set; }
        public GeoPoint End { get; set; }

        public GeoSegment()
        {

        }
        public GeoSegment(GeoPoint start, GeoPoint end)
        {
            Start = start;
            End = end;
        }

        public override string ToString()
        {
            return $"Segment from {Start} to {End}.";
        }
    }
}
