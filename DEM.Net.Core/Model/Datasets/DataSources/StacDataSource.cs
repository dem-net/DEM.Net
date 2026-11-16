using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using DEM.Net.Core.Stac;

namespace DEM.Net.Core.Datasets;

/// <summary>
/// Data source abstracting Stac
/// </summary>
public class StacDataSource(string url, string indexFilePath, string collection, Predicate<Asset> filter) : IDEMDataSource
{
    public Uri GetUrl(int pageSize, int pageIndex)
    {
        return new Uri($"https://cmr.earthdata.nasa.gov/search/granules.json?collection_concept_id={Collection}&page_num={pageIndex}&page_size={pageSize}&sort_key=-entry_title");
    }
    public string Url { get; } = url;
    public string IndexFilePath { get; } = indexFilePath;
    public string Collection { get; private set; } = collection;

    [JsonIgnore]
    public Predicate<Asset> Filter { get; } = filter;

    public DEMDataSourceType DataSourceType => DEMDataSourceType.Stac;
}
