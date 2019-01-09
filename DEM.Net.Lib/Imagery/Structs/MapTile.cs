using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Imagery
{
    public class MapTile
    {
        public MapTileInfo TileInfo { get; set; }
        public string FileExtension { get; set; }
        public int PixelSize { get; set; }
        public Uri Uri { get; set; }
        public byte[] Image { get; set; }


    }
}
