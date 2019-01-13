using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Imagery
{
    public class LatLong
    {
        double Lat;
        double Long;

        public LatLong(double lat, double lon)
        {
            Lat = lat;
            Long = lon;
        }

        public LatLong() : this(0,0)
        {

        }

        public override string ToString()
        {
            return $"lat={Lat}, Long={Long}";
        }
    }
}
