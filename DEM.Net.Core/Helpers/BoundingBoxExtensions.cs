using System;
using System.Collections.Generic;
using System.Text;

namespace DEM.Net.Core
{
    public static class BoundingBoxExtensions
    {
        public static bool Intersects(this BoundingBox bbox1, BoundingBox bbox2)
        {
            return (bbox1.xMax >= bbox2.xMin && bbox1.xMin <= bbox2.xMax) && (bbox1.yMax >= bbox2.yMin && bbox1.yMin <= bbox2.yMax);
        }
        public static bool Intersects(this BoundingBox bbox1, double lat, double lon)
        {
            return (bbox1.xMax >= lon && bbox1.xMin <= lon) && (bbox1.yMax >= lat && bbox1.yMin <= lat);
        }
    }
}
