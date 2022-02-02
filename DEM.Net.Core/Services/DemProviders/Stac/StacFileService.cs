using DEM.Net.Core.Datasets;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;

namespace DEM.Net.Core.Stac
{
    public class StacFileService : IDEMDataSetIndex
    {
        private readonly ILogger<StacFileService> logger;
        private ConcurrentDictionary<string, List<DEMFileSource>> _cacheByDemName;
        private readonly IHttpClientFactory httpClientFactory;

        public StacFileService(ILogger<StacFileService> logger, IHttpClientFactory httpClientFactory)
        {
            this.logger = logger;
            this.httpClientFactory = httpClientFactory;
        }
        public void Setup(DEMDataSet dataSet, string dataSetLocalDir)
        {
            try
            {

                if (dataSet == null)
                    throw new ArgumentNullException(nameof(dataSet), "Dataset is null.");

                StacDataSource dataSource = dataSet.DataSource as StacDataSource;
                if (dataSource is null)
                    throw new ArgumentException(nameof(dataSet), $"Dataset source is {dataSet.DataSource.DataSourceType}, only {nameof(DEMDataSourceType.Stac)} sources are supported by {nameof(StacFileService)} null.");

                if (!Directory.Exists(dataSetLocalDir))
                {
                    Directory.CreateDirectory(dataSetLocalDir);
                }

                string indexFileName = Path.Combine(dataSetLocalDir, UrlHelper.GetFileNameFromUrl(dataSource.IndexFilePath));

                bool download = !File.Exists(indexFileName);

                List<StacDemFile> links = null;
                if (download)
                {
                    Uri baseUrl = new Uri(dataSource.Url);
                    // Fetch collection page by page until we have no result left
                    //
                    this.logger.LogInformation($"Fetching items from collection {dataSource.Collection} to disk... This will be done once.");
                    bool hasData = true;
                    int pageIndex = 0;
                    string initialUrl = string.Concat(baseUrl, $"/collections/{dataSource.Collection}/items");
                    links = new List<StacDemFile>(30000);
                    var httpClient = httpClientFactory.CreateClient();
                    do
                    {
                        pageIndex++;
                        logger.LogInformation($"Getting entries on page {pageIndex}... ({links.Count} assets collected so far)");

                        var json = httpClient.GetStringAsync(initialUrl).GetAwaiter().GetResult();
                        hasData = !string.IsNullOrWhiteSpace(json);
                        if (hasData)
                        {
                            var result = CollectionItems.FromJson(json);
                            hasData = result.Features.Count > 0;
                            if (hasData)
                            {
                                // Only retrieve bbox and dem file link (zip file)
                                links.AddRange(result.Features.Select(_ => GetStacDemFile(dataSource, _)).Where(file => file != null));
                            }

                            initialUrl = result.Links.FirstOrDefault(l => l.Rel == Rel.Next)?.Href?.ToString();
                            hasData = initialUrl != null;
                        }
                    }
                    while (hasData);

                    File.WriteAllText(indexFileName, JsonConvert.SerializeObject(links, Formatting.Indented));
                    logger.LogInformation($"{links.Count} entries written to index file {indexFileName}");
                }


                // Cache
                if (_cacheByDemName == null)
                {
                    _cacheByDemName = new ConcurrentDictionary<string, List<DEMFileSource>>();
                }
                if (_cacheByDemName.ContainsKey(dataSet.Name) == false)
                {
                    _cacheByDemName[dataSet.Name] = this.GetSources(dataSet, indexFileName);
                }

            }
            catch (Exception ex)
            {
                this.logger?.LogError("Unhandled exception: " + ex.Message);
                this.logger?.LogInformation(ex.ToString());
                throw;
            }
        }
        private List<DEMFileSource> GetSources(DEMDataSet dataSet, string indexFileName)
        {
            List<StacDemFile> stacDemFiles = JsonConvert.DeserializeObject<List<StacDemFile>>(File.ReadAllText(indexFileName));
            var dataSetLocalDir = Path.GetDirectoryName(indexFileName);

            return stacDemFiles.Select(file => new DEMFileSource()
            {
                BBox = new BoundingBox(file.Box[0], file.Box[2], file.Box[1], file.Box[3]),
                SourceFileName = file.FileId,
                SourceFileNameAbsolute = file.Href,
                LocalFileName = Path.Combine(dataSetLocalDir, "Features", file.FileId)
            }).ToList();
        }

        private StacDemFile GetStacDemFile(StacDataSource dataSource, Feature f)
        {
            foreach (var asset in f.Assets)
            {
                if (dataSource.Filter(asset.Value))
                {
                    return new StacDemFile(asset.Key, f.Bbox, asset.Value.Href.ToString());
                }                
            }
            return null;            
        }

        public void DownloadRasterFile(DemFileReport report, DEMDataSet dataset)
        {
            try
            {
                //logger.LogInformation($"Downloading {report.URL}...");

                var dirName = Path.GetDirectoryName(report.LocalName);
                if (!Directory.Exists(dirName))
                {
                    Directory.CreateDirectory(dirName);
                }
                // Execute the request
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(report.URL);
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    // Now access the data
                    long length = response.ContentLength;
                    string type = response.ContentType;
                    using (Stream stream = response.GetResponseStream())
                    {
                        // Process the stream data (e.g. save to file)
                        using (FileStream fs = new FileStream(report.LocalName, FileMode.Create, FileAccess.Write))
                        {
                            stream.CopyTo(fs);
                            fs.Close();
                        }
                        stream.Close();
                    }
                    response.Close();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error while downloading DEM file: {ex.Message}");
                throw;
            }
        }

        public IEnumerable<DEMFileSource> GetCoveredFileSources(DEMDataSet dataset, BoundingBox bbox)
        {
            foreach (DEMFileSource source in this.GetFileSources(dataset))
            {
                if (bbox == null || source.BBox.ReprojectTo(source.BBox.SRID, dataset.SRID).Intersects(bbox))
                {
                    yield return source;
                }
            }
        }

        public IEnumerable<DEMFileSource> GetFileSources(DEMDataSet dataset)
        {
            if (_cacheByDemName.ContainsKey(dataset.Name))
            {
                foreach (var item in _cacheByDemName[dataset.Name])
                {
                    yield return item;
                }
            }
            else
            {
                throw new Exception("Must call Init(dataSet) first !");
            }
        }

        public void Reset()
        {
            //throw new NotImplementedException();
        }

    }
}
