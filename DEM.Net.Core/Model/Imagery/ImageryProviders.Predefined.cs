using System.Linq;

namespace DEM.Net.Core.Imagery
{
    public partial class ImageryProvider
    {
        public const string ATTRIBUTION_SUBJECT = "Imagery";
#if DEBUG
        public static readonly ImageryProvider DebugProvider = new TileDebugProvider(null);
#endif
        //https://t0.tiles.virtualearth.net/tiles/a1202221311130.jpeg?g=990&mkt=en-us&n=z
        //public static readonly ImageryProvider BingMapsSatellite = new ImageryProvider
        //{
        //    Name = "BingMaps-Satellite",
        //    Attribution = new Attribution(ATTRIBUTION_SUBJECT, "Bing Maps", "https://www.microsoft.com/en-us/maps/product/terms-april-2011", "© Bing Maps"),
        //    UrlModel = new UrlModel("https://t{s}.tiles.virtualearth.net/tiles/a{quadkey}.jpeg?g=990&mkt=en-us&n=z", new[] { "0", "1", "2", "3", "4", "5", "6", "7" }),
        //    TileSize = 256,
        //    MaxZoom = 19
        //};
        public static readonly ImageryProvider MapTilerSatellite = new ImageryProvider
        {
            Name = "MapTiler-Satellite",
            Attribution = new Attribution(ATTRIBUTION_SUBJECT, "MapTiler Cloud Satellite", "https://www.maptiler.com", "© MapTiler- © OpenStreetMap and contributors"),
            UrlModel = new UrlModel("https://api.maptiler.com/maps/satellite-v4/{z}/{x}/{y}.jpg?key={t}", null),
            TokenUserSecretsKey = "MapTilerKey",
            TileSize = 512,
            MaxZoom = 20
        };
        public static readonly ImageryProvider MapTilerSatelliteHybrid = new ImageryProvider
        {
            Name = "MapTiler-Satellite-Hybrid",
            Attribution = new Attribution(ATTRIBUTION_SUBJECT, "MapTiler Cloud Satellite Hybrid", "https://www.maptiler.com", "© MapTiler- © OpenStreetMap and contributors"),
            UrlModel = new UrlModel("https://api.maptiler.com/maps/hybrid-v4/{z}/{x}/{y}.jpg?key={t}", null),
            TokenUserSecretsKey = "MapTilerKey",
            TileSize = 512,
            MaxZoom = 20
        };
        public static readonly ImageryProvider MapBoxSatellite = new ImageryProvider
        {
            Name = "MapBox-Satellite",
            Attribution = new Attribution(ATTRIBUTION_SUBJECT, "MapBox Satellite", "https://www.mapbox.com", "© MapBox - OpenStreetMap contributors"),
            UrlModel = new UrlModel("https://api.mapbox.com/v4/mapbox.satellite/{z}/{x}/{y}@2x.jpg?access_token={t}", null),
            //UrlModel = new UrlModel("https://api.mapbox.com/styles/v1/mapbox/satellite-v9/tiles/256/{z}/{x}/{y}?access_token={t}", null),
            TokenUserSecretsKey = "MapBoxToken",
            TileSize = 512,
            MaxZoom = 23
        };
        public static readonly ImageryProvider MapBoxSatelliteStreet = new ImageryProvider
        {
            Name = "MapBox-SatelliteStreet",
            Attribution = new Attribution(ATTRIBUTION_SUBJECT, "MapBox Satellite Street", "https://www.mapbox.com", "© MapBox - OpenStreetMap contributors"),
            TokenUserSecretsKey = "MapBoxToken",
            UrlModel = new UrlModel("https://api.mapbox.com/styles/v1/mapbox/satellite-streets-v12/tiles/{z}/{x}/{y}?access_token={t}", null),
            TileSize = 512,
            MaxZoom = 23
        };
        public static readonly ImageryProvider MapBoxStreets = new ImageryProvider
        {
            Name = "MapBox-Streets",
            Attribution = new Attribution(ATTRIBUTION_SUBJECT, "MapBox Streets", "https://www.mapbox.com", "© MapBox - OpenStreetMap contributors"),
            TokenUserSecretsKey = "MapBoxToken",
            UrlModel = new UrlModel("https://api.mapbox.com/styles/v1/mapbox/streets-v11/tiles/{z}/{x}/{y}@2x?access_token={t}", null),
            TileSize = 512,
            MaxZoom = 23
        };
        public static readonly ImageryProvider MapBoxLight = new ImageryProvider
        {
            Name = "MapBox-Light",
            Attribution = new Attribution(ATTRIBUTION_SUBJECT, "MapBox Light", "https://www.mapbox.com", "© MapBox - OpenStreetMap contributors"),
            TokenUserSecretsKey = "MapBoxToken",
            UrlModel = new UrlModel("https://api.mapbox.com/styles/v1/mapbox/light-v10/tiles/{z}/{x}/{y}@2x?access_token={t}", null),
            TileSize = 512,
            MaxZoom = 23
        };
        public static readonly ImageryProvider MapBoxDark = new ImageryProvider
        {
            Name = "MapBox-Dark",
            Attribution = new Attribution(ATTRIBUTION_SUBJECT, "MapBox Dark", "https://www.mapbox.com", "© MapBox - OpenStreetMap contributors"),
            TokenUserSecretsKey = "MapBoxToken",
            UrlModel = new UrlModel("https://api.mapbox.com/styles/v1/mapbox/dark-v10/tiles/{z}/{x}/{y}@2x?access_token={t}", null),
            TileSize = 512,
            MaxZoom = 23
        };
        private static string StadiaAttribution = "&copy; <a href=\"https://stadiamaps.com/\" target=\"_blank\">Stadia Maps</a> &copy; <a href=\"https://openmaptiles.org/\" target=\"_blank\">OpenMapTiles</a> &copy; <a href=\"https://www.openstreetmap.org/copyright\" target=\"_blank\">OpenStreetMap</a>";
        public static readonly ImageryProvider StamenToner = new ImageryProvider
        {
            Name = "Stadia-Stamen-Toner",
            TokenUserSecretsKey = "StadiaMapsApiKey",
            Attribution = new Attribution(ATTRIBUTION_SUBJECT, "Stamen Toner", "https://stadiamaps.com/", StadiaAttribution),
            UrlModel = new UrlModel("https://tiles.stadiamaps.com/tiles/stamen_toner/{z}/{x}/{y}@2x.png?api_key={t}", null),
            TileSize = 512,
            MaxZoom = 20
        };
        public static readonly ImageryProvider StamenWaterColor = new ImageryProvider
        {
            Name = "Stadia-Stamen-Watercolor",
            TokenUserSecretsKey = "StadiaMapsApiKey",
            Attribution = new Attribution(ATTRIBUTION_SUBJECT, "Stamen Watercolor", "https://stadiamaps.com/", StadiaAttribution),
            UrlModel = new UrlModel("https://tiles.stadiamaps.com/data/stamen_watercolor_v1/{z}/{x}/{y}.jpg?api_key={t}", null),
            TileSize = 256,
            MaxZoom = 20
        };
        public static readonly ImageryProvider StamenTerrain = new ImageryProvider
        {
            Name = "Stadia-Stamen-Terrain",
            TokenUserSecretsKey = "StadiaMapsApiKey",
            Attribution = new Attribution(ATTRIBUTION_SUBJECT, "Stamen Terrain", "https://stadiamaps.com/", StadiaAttribution),
            UrlModel = new UrlModel("https://tiles.stadiamaps.com/tiles/stamen_terrain/{z}/{x}/{y}@2x.png?api_key={t}", null),
            TileSize = 512,
            MaxZoom = 20
        };
        public static readonly ImageryProvider StadiaOutdoors = new ImageryProvider
        {
            Name = "Stadia-Stamen-Outdoors",
            TokenUserSecretsKey = "StadiaMapsApiKey",
            Attribution = new Attribution(ATTRIBUTION_SUBJECT, "Stadia Outdoors", "https://stadiamaps.com/", StadiaAttribution),
            UrlModel = new UrlModel("https://tiles.stadiamaps.com/tiles/outdoors/{z}/{x}/{y}@2x.png?api_key={t}", null),
            TileSize = 512,
            MaxZoom = 20
        };
        public static readonly ImageryProvider OpenTopoMap = new ImageryProvider
        {
            Name = "OpenTopoMap",
            Attribution = new Attribution(ATTRIBUTION_SUBJECT, "OpenTopoMap",
                                            "Map data: © <a href=\"https://www.openstreetmap.org/copyright\">OpenStreetMap</a> contributors, <a href=\"http://viewfinderpanoramas.org\"> SRTM</a> | Map style: &copy; <a href=\"https://opentopomap.org\" > OpenTopoMap</a> (<a href=\"https://creativecommons.org/licenses/by-sa/3.0/\" > CC-BY-SA</a>)"),
            UrlModel = new UrlModel("https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png", new[] { "a", "b", "c" }),
            MaxZoom = 17
        };
        public static readonly ImageryProvider EsriWorldImagery = new ImageryProvider
        {
            Name = "Esri.WorldImagery",
            Attribution = new Attribution(ATTRIBUTION_SUBJECT, "Esri World Imagery", "https://services.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer", "Source: Esri, DigitalGlobe, GeoEye, Earthstar Geographics, CNES/Airbus DS, USDA, USGS, AeroGRID, IGN, and the GIS User Community"),
            UrlModel = new UrlModel("https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}", null),
            MaxZoom = 18,
            PrivateUseOnly = true
        };
        public static readonly ImageryProvider ThunderForestOutdoors = new ImageryProvider
        {
            Name = "ThunderForest-Outdoors",
            Attribution = new Attribution(ATTRIBUTION_SUBJECT, "ThunderForest", "https://www.thunderforest.com", "Maps © www.thunderforest.com, Data © www.osm.org/copyright"),
            UrlModel = new UrlModel("https://tile.thunderforest.com/outdoors/{z}/{x}/{y}.png?apikey={t}", null),
            TokenUserSecretsKey = "ThunderForestApiKey",
            MaxZoom = 22
        };
        public static readonly ImageryProvider ThunderForestLandscape = new ImageryProvider
        {
            Name = "ThunderForest-Landscape",
            Attribution = new Attribution(ATTRIBUTION_SUBJECT, "ThunderForest", "https://www.thunderforest.com", "Maps © www.thunderforest.com, Data © www.osm.org/copyright"),
            UrlModel = new UrlModel("https://tile.thunderforest.com/landscape/{z}/{x}/{y}.png?apikey={t}", null),
            TokenUserSecretsKey = "ThunderForestApiKey",
            MaxZoom = 22
        };
        public static readonly ImageryProvider ThunderForestNeighbourhood = new ImageryProvider
        {
            Name = "ThunderForest-Neighbourhood",
            Attribution = new Attribution(ATTRIBUTION_SUBJECT, "ThunderForest", "https://www.thunderforest.com", "Maps © www.thunderforest.com, Data © www.osm.org/copyright"),
            UrlModel = new UrlModel("https://tile.thunderforest.com/neighbourhood/{z}/{x}/{y}.png?apikey={t}", null),
            TokenUserSecretsKey = "ThunderForestApiKey",
            MaxZoom = 22
        };
        public static readonly ImageryProvider OrtoIGNes = new ImageryProvider
        {
            Name = "OrtoIGNes (Spain only)",
            Attribution = new Attribution(ATTRIBUTION_SUBJECT, "IGN.es", "https://www.ign.es", "ign.es"),
            UrlModel = new UrlModel("http://www.ign.es/wmts/pnoa-ma?SERVICE=WMTS&VERSION=1.0.0&REQUEST=GetTile&LAYER=OI.OrthoimageCoverage&FORMAT=image/jpeg&TILEMATRIXSET=GoogleMapsCompatible&TILEMATRIX={z}&TILEROW={y}&TILECOL={x}", null),
            MaxZoom = 20
        };
        public static readonly ImageryProvider SwissImage = new ImageryProvider
        {
            Name = "SwissImage (Switzerland only)",
            Attribution = new Attribution(ATTRIBUTION_SUBJECT, "swisstopo", "https://www.swisstopo.admin.ch", "ign.es"),
            UrlModel = new UrlModel("https://wmts.geo.admin.ch/1.0.0/ch.swisstopo.swissimage/default/current/3857/{z}/{x}/{y}.jpeg", null),
            MaxZoom = 20
        };
        public static readonly ImageryProvider OrtoIGNfr = new ImageryProvider
        {
            Name = "Orthophoto IGN (France only)",
            Attribution = new Attribution(ATTRIBUTION_SUBJECT, "IGN.fr", "https://geoservices.ign.fr", "Ign.fr"),
            UrlModel = new UrlModel("https://data.geopf.fr/wmts?SERVICE=WMTS&VERSION=1.0.0&REQUEST=GetTile&LAYER=ORTHOIMAGERY.ORTHOPHOTOS&TILEMATRIXSET=PM&TILEMATRIX={z}&TILECOL={x}&TILEROW={y}&STYLE=normal&FORMAT=image/jpeg", null),
            MaxZoom = 19
        };
        
        

    }
}
