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
        public int TileSize { get; set; } = 256;
        public int MaxDegreeOfParallelism { get; set; } = 2;
        public int MaxZoom { get; set; } = 19;

        public override string ToString()
        {
            return Name;
        }

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
        public static ImageryProvider MapBoxSatelliteStreet = new ImageryProvider()
        {
            Name = "MapBox-SatelliteStreet",
            Attribution = new Attribution("© MapxBox - OpenStreetMap contributors", "https://www.mapbox.com"),
            UrlModel = new UrlModel("https://api.mapbox.com/v4/mapbox.streets-satellite/{z}/{x}/{y}.png?access_token={t}", null),
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
        public static ImageryProvider StamenTerrain = new ImageryProvider()
        {
            Name = "Stamen-Terrain",
            Attribution = new Attribution("Map tiles by Stamen Design, under CC BY 3.0. Data by OpenStreetMap, under ODbL.", "Map tiles by <a href=\"http://stamen.com\">Stamen Design</a>, under <a href=\"http://creativecommons.org/licenses/by/3.0\">CC BY 3.0</a>. Data by <a href=\"http://openstreetmap.org\">OpenStreetMap</a>, under <a href=\"http://www.openstreetmap.org/copyright\">ODbL</a>."),
            UrlModel = new UrlModel("http://{s}.tile.stamen.com/terrain/{z}/{x}/{y}.png", new[] { "a", "b", "c", "d" }),
            MaxZoom = 14
        };
        public static ImageryProvider StamenToner = new ImageryProvider()
        {
            Name = "Stamen-Toner",
            Attribution = new Attribution("Map tiles by Stamen Design, under CC BY 3.0. Data by OpenStreetMap, under ODbL.", "Map tiles by <a href=\"http://stamen.com\">Stamen Design</a>, under <a href=\"http://creativecommons.org/licenses/by/3.0\">CC BY 3.0</a>. Data by <a href=\"http://openstreetmap.org\">OpenStreetMap</a>, under <a href=\"http://www.openstreetmap.org/copyright\">ODbL</a>."),
            UrlModel = new UrlModel("http://{s}.tile.stamen.com/toner/{z}/{x}/{y}.png", new[] { "a", "b", "c", "d" }),
            MaxZoom = 14
        };
        public static ImageryProvider StamenWaterColor = new ImageryProvider()
        {
            Name = "Stamen-Watercolor",
            Attribution = new Attribution("Map tiles by Stamen Design, under CC BY 3.0. Data by OpenStreetMap, under CC BY SA.", "Map tiles by <a href=\"http://stamen.com\">Stamen Design</a>, under <a href=\"http://creativecommons.org/licenses/by/3.0\">CC BY 3.0</a>. Data by <a href=\"http://openstreetmap.org\">OpenStreetMap</a>, under <a href=\"http://creativecommons.org/licenses/by-sa/3.0\">CC BY SA</a>."),
            UrlModel = new UrlModel("http://{s}.tile.stamen.com/watercolor/{z}/{x}/{y}.jpg", new[] { "a", "b", "c", "d" }),
            MaxZoom = 14
        };
    }
}
