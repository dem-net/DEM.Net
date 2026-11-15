using System;
using System.Collections.Generic;
using DEM.Net.Core.Datasets;

namespace DEM.Net.Core
{
    public partial class DEMDataSet
    {
        public const string MapterhornName = "Mapterhorn";

        /// <summary>Zoom of the listed <see cref="Mapterhorn"/> dataset: the highest zoom with global coverage (Copernicus GLO-30).</summary>
        public const int MapterhornDefaultZoom = 12;
        public const int MapterhornMinZoom = 6;
        public const int MapterhornMaxZoom = 18;
        private const int MapterhornTileSize = 512;

        /// <summary>
        /// Mapterhorn terrain tiles (https://mapterhorn.com): global Copernicus GLO-30 merged with high resolution national
        /// open data (LiDAR down to 0.25 m in several countries). Terrarium encoded WebP tiles in a Web Mercator pyramid.
        /// This listed dataset is zoom 12 (~19 m at the equator). Use <see cref="GetMapterhorn(int)"/> for other zoom levels.
        /// </summary>
        public static DEMDataSet Mapterhorn => Datasets.Value[MapterhornName];

        /// <summary>Mapterhorn dataset at given zoom level (one dataset per zoom, unlisted except <see cref="MapterhornDefaultZoom"/>).</summary>
        public static DEMDataSet GetMapterhorn(int zoom)
        {
            if (zoom < MapterhornMinZoom || zoom > MapterhornMaxZoom)
                throw new ArgumentOutOfRangeException(nameof(zoom), $"Mapterhorn zoom must be between {MapterhornMinZoom} and {MapterhornMaxZoom}.");
            return Datasets.Value[GetMapterhornName(zoom)];
        }

        public static string GetMapterhornName(int zoom) => zoom == MapterhornDefaultZoom ? MapterhornName : $"{MapterhornName}_z{zoom}";

        /// <summary>True for the listed Mapterhorn dataset and its per zoom variants.</summary>
        public bool IsMapterhorn => Name == MapterhornName || Name.StartsWith(MapterhornName + "_z", StringComparison.Ordinal);

        private static void AddMapterhornDatasets(Dictionary<string, DEMDataSet> datasets)
        {
            for (int zoom = MapterhornMinZoom; zoom <= MapterhornMaxZoom; zoom++)
            {
                double pixelMeters = WebMercatorTiles.PixelSizeMeters(zoom, MapterhornTileSize);
                int pointsPerDegree = (int)Math.Round((1L << zoom) * MapterhornTileSize / 360d);
                string name = GetMapterhornName(zoom);

                datasets.Add(name, new DEMDataSet()
                {
                    Name = name,
                    Description = zoom == MapterhornDefaultZoom
                        ? "Mapterhorn - Global 30m (Copernicus) with high resolution data (down to 0.25m) in many countries"
                        : $"Mapterhorn - zoom {zoom} ({pixelMeters:0.##} m at equator)",
                    PublicUrl = "https://mapterhorn.com",
                    DataSource = new XyzTileDataSource("https://tiles.mapterhorn.com/{z}/{x}/{y}.webp", zoom, MapterhornTileSize,
                        fullCoverageMaxZoom: MapterhornDefaultZoom,
                        coverageIndexUrl: "https://download.mapterhorn.com/download_urls.json"),
                    FileFormat = new DEMFileDefinition("Terrarium WebP tile", DEMFileType.Terrarium, ".webp", DEMFileRegistrationMode.Cell),
                    SRID = Reprojection.SRID_PROJECTED_MERCATOR,
                    // EPSG:3857 pixel size, used to size height maps in projected units
                    ResolutionMeters = (float)pixelMeters,
                    ResolutionArcSeconds = 3600f / pointsPerDegree,
                    PointsPerDegree = pointsPerDegree,
                    NoDataValue = -32768,
                    IsListed = zoom == MapterhornDefaultZoom,
                    ExtentInfo = "Global, high resolution in many countries (see https://mapterhorn.com)",
                    Attribution = new Attribution(ATTRIBUTION_SUBJECT, "© Mapterhorn", "https://mapterhorn.com/attribution",
                        "Mapterhorn terrain tiles, compiled from various open data sources (see https://mapterhorn.com/attribution), including Copernicus DEM GLO-30 © DLR e.V. 2010-2014 and © Airbus Defence and Space GmbH 2014-2018 provided under COPERNICUS by the European Union and ESA")
                });
            }
        }
    }
}
