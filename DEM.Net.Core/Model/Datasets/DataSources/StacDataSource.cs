using DEM.Net.Core.Stac;
using System;
using System.Collections.Generic;
using System.Text;

namespace DEM.Net.Core.Datasets
{
    /// <summary>
    /// Data source abstracting Stac
    /// </summary>
    public class StacDataSource : IDEMDataSource
    {
        public Uri GetUrl(int pageSize, int pageIndex)
        {
            return new Uri($"https://cmr.earthdata.nasa.gov/search/granules.json?collection_concept_id={Collection}&page_num={pageIndex}&page_size={pageSize}&sort_key=-entry_title");
        }
        public string Url { get; }
        public string IndexFilePath { get; }
        public string Collection { get; private set; }
        public Predicate<Asset> Filter { get; }

        public DEMDataSourceType DataSourceType => DEMDataSourceType.Stac;

        public StacDataSource(string url, string indexFilePath, string collection, Predicate<Asset> filter)
        {
            this.Url = url;
            this.IndexFilePath = indexFilePath;
            this.Collection = collection;
            this.Filter = filter;
        }

    }
}
