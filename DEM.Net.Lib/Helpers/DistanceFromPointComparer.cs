using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib
{
     internal class DistanceFromPointComparer : IComparer<GeoPoint>
    {
        private readonly GeoPoint _reference;

        public DistanceFromPointComparer(GeoPoint reference)
        {
            _reference = reference;
        }
        public int Compare(GeoPoint x, GeoPoint y)
        {
            return _reference.DistanceSquaredTo(x)
                    .CompareTo(_reference.DistanceSquaredTo(y));
        }


    }
}
