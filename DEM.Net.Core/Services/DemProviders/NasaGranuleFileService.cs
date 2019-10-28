using DEM.Net.Core.Datasets;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;

namespace DEM.Net.Core.EarthData
{
    public class NasaGranuleFileService : IDEMDataSetIndex
    {
        private readonly ILogger<NasaGranuleFileService> logger;
        private ConcurrentDictionary<string, List<DEMFileSource>> _cacheByDemName;
        private static HttpClient _httpClient = new HttpClient();

        public NasaGranuleFileService(ILogger<NasaGranuleFileService> logger)
        {
            this.logger = logger;
        }
        public void Setup(DEMDataSet dataSet, string dataSetLocalDir)
        {
            try
            {

                if (dataSet == null)
                    throw new ArgumentNullException(nameof(dataSet), "Dataset is null.");

                NasaGranuleDataSource dataSource = dataSet.DataSource as NasaGranuleDataSource;
                if (dataSource is null)
                    throw new ArgumentException(nameof(dataSet), $"Dataset source is {dataSet.DataSource.DataSourceType}, only {nameof(DEMDataSourceType.NASA)} sources are supported by {nameof(NasaGranuleFileService)} null.");

                this.logger?.LogInformation($"Setup for {dataSet.Name} dataset.");

                if (!Directory.Exists(dataSetLocalDir))
                {
                    Directory.CreateDirectory(dataSetLocalDir);
                }

                string indexFileName = Path.Combine(dataSetLocalDir, UrlHelper.GetFileNameFromUrl(dataSource.IndexFilePath));

                bool download = !File.Exists(indexFileName);

                if (download)
                {

                    this.logger.LogInformation($"Fetching granules from collection {dataSource.CollectionId} to disk... This will be done once.");
                    bool hasData = true;
                    int pageIndex = 0;
                    int PAGE_SIZE = 1000;
                    do
                    {
                        pageIndex++;
                        var url = dataSource.GetUrl(PAGE_SIZE, pageIndex);
                        var json = _httpClient.GetStringAsync(url).GetAwaiter().GetResult();
                        hasData = !string.IsNullOrWhiteSpace(json);
                        if (hasData)
                        {
                           var result = NasaCmrGranuleResult.FromJson(json);
                        }
                    }
                    while (hasData);
                }


                // Cache

                //if (_cacheByDemName == null)
                //{
                //    _cacheByDemName = new ConcurrentDictionary<string, List<DEMFileSource>>();
                //}
                //if (_cacheByDemName.ContainsKey(vrtFileName) == false)
                //{
                //    _cacheByDemName[dataSet.Name] = this.GetSources(dataSet, vrtFileName).ToList();
                //}


            }
            catch (Exception ex)
            {
                this.logger?.LogError("Unhandled exception: " + ex.Message);
                this.logger?.LogInformation(ex.ToString());
                throw;
            }
        }
        public IEnumerable<DEMFileSource> GetCoveredFileSources(DEMDataSet dataset, BoundingBox bbox)
        {
            throw new NotImplementedException(nameof(NasaGranuleFileService));
        }

        public IEnumerable<DEMFileSource> GetFileSources(DEMDataSet dataset)
        {
            throw new NotImplementedException(nameof(NasaGranuleFileService));
            // S83 to N82 => -83 < lat < 83
            // W180 to E179 => -180 < lon < 180
            // 
            // example with N00E006
            // 6 < lon < 7
            // 0 < lat < 1


        }

        public void Reset()
        {
        }


    }
}
