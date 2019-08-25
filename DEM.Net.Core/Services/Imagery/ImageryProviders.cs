// ImageryProviders.cs
//
// Author:
//       Xavier Fischer 
//
// Copyright (c) 2019 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core.Imagery
{
    public class ImageryProvider
    {
        public string Name { get; set; }
        public UrlModel UrlModel { get; set; }
        public Attribution Attribution { get; set; }
        public string TokenAppSettingsKey { get; set; }
        public int TileSize { get; set; } = 256;
        public int MaxDegreeOfParallelism { get; set; } = 4;
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
            MaxZoom = 16,
            MaxDegreeOfParallelism = 2
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
        public static ImageryProvider OpenTopoMap = new ImageryProvider()
        {
            Name = "OpenTopoMap",
            Attribution = new Attribution("Map data: OpenStreetMap and contributors, viewfinderpanoramas.org, SRTM. Map style: OpenTopoMap under CC-BY-SA.",
                                            "Map data: © <a href=\"https://www.openstreetmap.org/copyright\">OpenStreetMap</a> contributors, <a href=\"http://viewfinderpanoramas.org\"> SRTM</a> | Map style: &copy; <a href=\"https://opentopomap.org\" > OpenTopoMap</a> (<a href=\"https://creativecommons.org/licenses/by-sa/3.0/\" > CC-BY-SA</a>)"),
            UrlModel = new UrlModel("https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png", new[] { "a", "b", "c" }),
            MaxZoom = 17
        };
        public static ImageryProvider EsriWorldImagery = new ImageryProvider()
        {
            Name = "Esri.WorldImagery",
            Attribution = new Attribution("Tiles © Esri - Source: Esri, i-cubed, USDA, USGS, AEX, GeoEye, Getmapping, Aerogrid, IGN, IGP, UPR-EGP, and the GIS User Community",
                                            "Tiles © Esri - Source: Esri, i-cubed, USDA, USGS, AEX, GeoEye, Getmapping, Aerogrid, IGN, IGP, UPR-EGP, and the GIS User Community"),
            UrlModel = new UrlModel("https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}", null),
            MaxZoom = 18
        };
        

    }
}
