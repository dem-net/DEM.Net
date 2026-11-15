using System;

namespace DEM.Net.Core.Datasets
{
    /// <summary>
    /// Data source backed by a XYZ (slippy map) pyramid of elevation images, e.g. Mapterhorn Terrarium tiles
    /// (https://mapterhorn.com). One dataset is registered per zoom level so that each dataset has a fixed resolution.
    /// Tiles are downloaded on demand and cached as files named <c>{z}_{x}_{y}{extension}</c>.
    /// </summary>
    public class XyzTileDataSource : IDEMDataSource
    {
        /// <summary>Tile URL template, with {z}, {x} and {y} placeholders.</summary>
        public string UrlTemplate { get; }

        /// <summary>Zoom level of this dataset.</summary>
        public int Zoom { get; }

        /// <summary>Tile width and height in pixels.</summary>
        public int TileSize { get; }

        /// <summary>
        /// Highest zoom level available everywhere on land (Mapterhorn: 12, Copernicus GLO-30). Used as the max zoom
        /// where the coverage index lists nothing better. Missing tiles at any zoom (HTTP 404) are resampled from
        /// their closest available ancestor.
        /// </summary>
        public int FullCoverageMaxZoom { get; }

        /// <summary>
        /// Optional JSON file listing the areas and max zoom levels available (Mapterhorn download_urls.json format).
        /// </summary>
        public string CoverageIndexUrl { get; }

        public string IndexFilePath => null;

        public DEMDataSourceType DataSourceType => DEMDataSourceType.XyzTiles;

        public XyzTileDataSource(string urlTemplate, int zoom, int tileSize, int fullCoverageMaxZoom, string coverageIndexUrl = null)
        {
            UrlTemplate = urlTemplate ?? throw new ArgumentNullException(nameof(urlTemplate));
            if (zoom < 0 || zoom > 24) throw new ArgumentOutOfRangeException(nameof(zoom));
            if (tileSize <= 0) throw new ArgumentOutOfRangeException(nameof(tileSize));
            Zoom = zoom;
            TileSize = tileSize;
            FullCoverageMaxZoom = fullCoverageMaxZoom;
            CoverageIndexUrl = coverageIndexUrl;
        }

        public string GetTileUrl(int z, int x, int y) => UrlTemplate
            .Replace("{z}", z.ToString())
            .Replace("{x}", x.ToString())
            .Replace("{y}", y.ToString());
    }
}
