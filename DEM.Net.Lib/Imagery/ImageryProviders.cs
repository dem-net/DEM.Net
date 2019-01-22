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
        public string TokenAppSettingsKey { get; set; }
        public int TileSize => 256;
        public int MaxZoom { get; set; } = 19;

        public static ImageryProvider Osm = new ImageryProvider()
        {
            Name = "OpenStreetMap",
            Attribution = new Attribution("© OpenStreetMap contributors", "https://www.openstreetmap.org/copyright"),
            UrlModel = new UrlModel("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", new[] { "a", "b", "c" }),
            MaxZoom = 16
        };

        public static ImageryProvider MapBoxSatellite = new ImageryProvider()
        {
            Name = "MapBox-Satellite",
            Attribution = new Attribution("© MapxBox - OpenStreetMap contributors", "https://www.mapbox.com"),
            UrlModel = new UrlModel("https://api.mapbox.com/v4/mapbox.satellite/{z}/{x}/{y}.png?access_token={t}", null),
            TokenAppSettingsKey = "mapboxtoken",
            MaxZoom = 23
        };
        public static ImageryProvider MapBoxStreets = new ImageryProvider()
        {
            Name = "MapBox-Streets",
            Attribution = new Attribution("© MapxBox - OpenStreetMap contributors", "https://www.mapbox.com"),
            TokenAppSettingsKey = "mapboxtoken",
            UrlModel = new UrlModel("https://api.mapbox.com/styles/v1/xfischer/cjbtijn5qahc92qs2yghsy58p/tiles/256/{z}/{x}/{y}?access_token={t}", null),
            MaxZoom = 23
        };
    }
}
