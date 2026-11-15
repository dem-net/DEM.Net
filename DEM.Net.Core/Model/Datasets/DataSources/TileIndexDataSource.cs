using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DEM.Net.Core.Datasets
{
    /// <summary>
    /// Data source described by a GeoJSON tile index: one polygon feature per raster file.
    /// The index is downloaded once, then each tile is downloaded on demand from the URL
    /// built from its feature properties (e.g. FABDEM v1.2 on Hugging Face).
    /// </summary>
    public class TileIndexDataSource : IDEMDataSource
    {
        /// <summary>URL of the GeoJSON FeatureCollection listing the tiles.</summary>
        public string IndexUrl { get; }

        /// <summary>Local index file name, stored in the dataset directory.</summary>
        public string IndexFilePath { get; }

        /// <summary>Builds the tile download URL from the feature properties.</summary>
        [JsonIgnore]
        public Func<IReadOnlyDictionary<string, string>, string> TileUrl { get; }

        /// <summary>Builds the local file name from the feature properties. Defaults to the last segment of the tile URL.</summary>
        [JsonIgnore]
        public Func<IReadOnlyDictionary<string, string>, string> TileFileName { get; }

        public DEMDataSourceType DataSourceType => DEMDataSourceType.TileIndex;

        public TileIndexDataSource(string indexUrl, string indexFilePath,
            Func<IReadOnlyDictionary<string, string>, string> tileUrl,
            Func<IReadOnlyDictionary<string, string>, string> tileFileName = null)
        {
            IndexUrl = indexUrl ?? throw new ArgumentNullException(nameof(indexUrl));
            IndexFilePath = indexFilePath ?? throw new ArgumentNullException(nameof(indexFilePath));
            TileUrl = tileUrl ?? throw new ArgumentNullException(nameof(tileUrl));
            TileFileName = tileFileName;
        }
    }
}
