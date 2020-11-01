using System;
using System.Collections.Generic;
using System.Text;

namespace DEM.Net.Core.Datasets
{
    // https://e4ftl01.cr.usgs.gov/ASTT/ASTGTM.003/2000.03.01/
    /// <summary>
    /// Data source abstracting Nasa's Earth Data Common Metadata Repository service (CMR API)
    /// Url looks like https://cmr.earthdata.nasa.gov/search/granules.json?collection_concept_id=C1575726572-LPDAAC_ECS&page_num=1&page_size=10&sort_key=-entry_title
    /// </summary>
    public class NasaGranuleDataSource : IDEMDataSource
    {
        public Uri GetUrl(int pageSize, int pageIndex)
        {
            return new Uri($"https://cmr.earthdata.nasa.gov/search/granules.json?collection_concept_id={CollectionId}&page_num={pageIndex}&page_size={pageSize}&sort_key=-entry_title");
        }
        public string IndexFilePath { get; }
        public string CollectionId { get; private set; }

        public DEMDataSourceType DataSourceType => DEMDataSourceType.NasaEarthData;

        public NasaGranuleDataSource(string indexFilePath, string collectionId)
        {
            this.IndexFilePath = indexFilePath;
            this.CollectionId = collectionId;
        }

    }
}
