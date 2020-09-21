using DEM.Net.Core.Datasets;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core.EarthData
{
    public class NasaGranuleFileService : IDEMDataSetIndex
    {
        private readonly ILogger<NasaGranuleFileService> logger;
        private ConcurrentDictionary<string, List<DEMFileSource>> _cacheByDemName;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly EarthdataLoginConnector rasterDownloader;

        public NasaGranuleFileService(ILogger<NasaGranuleFileService> logger, EarthdataLoginConnector rasterDownloader, IHttpClientFactory httpClientFactory)
        {
            this.logger = logger;
            this.rasterDownloader = rasterDownloader;
            this.httpClientFactory = httpClientFactory;
        }
        public void Setup(DEMDataSet dataSet, string dataSetLocalDir)
        {
            try
            {

                if (dataSet == null)
                    throw new ArgumentNullException(nameof(dataSet), "Dataset is null.");

                NasaGranuleDataSource dataSource = dataSet.DataSource as NasaGranuleDataSource;
                if (dataSource is null)
                    throw new ArgumentException(nameof(dataSet), $"Dataset source is {dataSet.DataSource.DataSourceType}, only {nameof(DEMDataSourceType.NasaEarthData)} sources are supported by {nameof(NasaGranuleFileService)} null.");

                if (!Directory.Exists(dataSetLocalDir))
                {
                    Directory.CreateDirectory(dataSetLocalDir);
                }

                string indexFileName = Path.Combine(dataSetLocalDir, UrlHelper.GetFileNameFromUrl(dataSource.IndexFilePath));

                bool download = !File.Exists(indexFileName);


                List<NasaDemFile> links = null;
                if (download)
                {
                    // Fetch Earth data collection page by page until we have no result left
                    //
                    this.logger.LogInformation($"Fetching granules from collection {dataSource.CollectionId} to disk... This will be done once.");
                    bool hasData = true;
                    int pageIndex = 0;
                    int PAGE_SIZE = 2000;
                    links = new List<NasaDemFile>(30000);
                    var httpClient = httpClientFactory.CreateClient();
                    do
                    {
                        pageIndex++;
                        logger.LogInformation($"Getting entries on page {pageIndex} with page size of {PAGE_SIZE} ({(pageIndex - 1) * PAGE_SIZE} entries so far)...");
                        var url = dataSource.GetUrl(PAGE_SIZE, pageIndex);
                        
                        var json = httpClient.GetStringAsync(url).GetAwaiter().GetResult();
                        hasData = !string.IsNullOrWhiteSpace(json);
                        if (hasData)
                        {
                            var result = NasaCmrGranuleResult.FromJson(json);
                            hasData = result.Feed.Entry.Any();
                            if (hasData)
                            {
                                // Only retrieve bbox and dem file link (zip file)
                                links.AddRange(result.Feed.Entry.Select(GetNasaDemFile).Where(file => file != null));
                            }
                        }
                    }
                    while (hasData);

                    var jsonResult = JsonConvert.SerializeObject(links);
                    File.WriteAllText(indexFileName, JsonConvert.SerializeObject(links));
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



        private NasaDemFile GetNasaDemFile(Entry entry)
        {
            try
            {
                if (entry == null)
                    throw new ArgumentNullException(nameof(entry), "Entry is mandatory.");
                if (entry.Boxes == null || entry.Boxes.Count == 0)
                    throw new ArgumentNullException(nameof(entry.Boxes), "Boxes should contain at least an element.");
                if (entry.Links == null || entry.Links.Count == 0)
                    throw new ArgumentNullException(nameof(entry.Links), "Links should contain at least an element.");

                var link = entry.Links.FirstOrDefault(l => l.Type == TypeEnum.ApplicationZip);
                if (link == null)
                    throw new ArgumentNullException(nameof(link), "ApplicationZip Link is mandatory.");

                return new NasaDemFile(entry.ProducerGranuleId, entry.Boxes.First(), link.Href.AbsoluteUri);
            }
            catch (Exception ex)
            {
                logger.LogWarning("Error parsing Nasa entry. File will be ignored: " + ex.Message);
                return null;
            }

        }

        private List<DEMFileSource> GetSources(DEMDataSet dataSet, string indexFileName)
        {
            List<NasaDemFile> nasaDemFiles = JsonConvert.DeserializeObject<List<NasaDemFile>>(File.ReadAllText(indexFileName));
            var dataSetLocalDir = Path.GetDirectoryName(indexFileName);

            BoundingBox GetBBox(string box)
            {
                // box is ymin xmin ymax xmax
                var coords = box.Split(' ').Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToArray();

                return new BoundingBox(coords[1], coords[3], coords[0], coords[2]);
            };

            return nasaDemFiles.Select(file => new DEMFileSource()
            {
                BBox = GetBBox(file.Box),
                SourceFileName = file.GranuleId,
                SourceFileNameAbsolute = file.ZipFileLink,
                LocalFileName = Path.Combine(dataSetLocalDir, "Granules", this.FileNameFromGranuleId(file.GranuleId, dataSet))
            }).ToList();
        }

        public IEnumerable<DEMFileSource> GetCoveredFileSources(DEMDataSet dataset, BoundingBox bbox)
        {
            foreach (DEMFileSource source in this.GetFileSources(dataset))
            {
                if (bbox == null || source.BBox.Intersects(bbox))
                {
                    yield return source;
                }
            }
        }

        public IEnumerable<DEMFileSource> GetFileSources(DEMDataSet dataSet)
        {
            if (_cacheByDemName.ContainsKey(dataSet.Name))
            {
                foreach (var item in _cacheByDemName[dataSet.Name])
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
        }

        public void DownloadRasterFile(DemFileReport report, DEMDataSet dataset)
        {
            try
            {
                // Download file
                var zipFileName = Path.Combine(Path.GetDirectoryName(report.LocalName), report.Source.SourceFileName);
                rasterDownloader.Download(report.URL, zipFileName);

                // Post action
                // - Unzip file 
                // - Keep only _dem.tif file
                // - rename it as .tif
                // - suppress other files

                using (var archive = ZipFile.Open(zipFileName, ZipArchiveMode.Read))
                {
                    var geoTiffFile = archive.Entries.FirstOrDefault(e => e.Name.ToLower().EndsWith(dataset.FileFormat.FileExtension));
                    if (geoTiffFile == null)
                    {
                        this.logger.LogError($"Cannot find any {dataset.FileFormat.FileExtension} file into archive {report.LocalName}");
                    }
                    else
                    {
                        geoTiffFile.ExtractToFile(report.LocalName, true);
                    }
                }
                File.Delete(zipFileName);

            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"Error while downloading raster {report.URL}: {ex.Message}");
                throw;
            }

        }

        private string FileNameFromGranuleId(string granuleId, DEMDataSet dataSet)
        {
            var fileName = string.Concat(Path.GetFileNameWithoutExtension(granuleId), dataSet.FileFormat.FileExtension);
            return fileName;
        }
    }
}
