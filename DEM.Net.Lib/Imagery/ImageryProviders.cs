using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Imagery
{
    public class ImageryProvider
    {
        public string Name { get; set; }
        public UrlModel UrlModel { get; set; }
        public Attribution Attribution { get; set; }
        public int TileSize => 256;
        public int MaxZoom => 19;

        public static ImageryProvider Osm = new ImageryProvider()
        {
            Name = "OpenStreetMap"
            ,
            Attribution = new Attribution("© OpenStreetMap contributors", "https://www.openstreetmap.org/copyright")
            ,
            UrlModel = new UrlModel("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", new[] { "a", "b", "c" })
        };
    }
}
