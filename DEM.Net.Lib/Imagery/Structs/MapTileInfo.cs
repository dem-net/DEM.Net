using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Imagery
{
    public class MapTileInfo
    {

        public MapTileInfo(int x, int y, int zoom)
        {
            X = x;
            Y = y;
            Zoom = zoom;
        }

        public int X { get; set; }
        public int Y { get; set; }
        public int Zoom { get; set; }
    }
}
