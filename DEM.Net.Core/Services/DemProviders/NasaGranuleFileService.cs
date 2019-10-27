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

                this.logger?.LogInformation($"Setup for {dataSet.Name} dataset.");

                if (!Directory.Exists(dataSetLocalDir))
                {
                    Directory.CreateDirectory(dataSetLocalDir);
                }

                string indexFileName = Path.Combine(dataSetLocalDir, UrlHelper.GetFileNameFromUrl(dataSet.DataSource.IndexFilePath));


                bool download = !File.Exists(indexFileName);

                if (download)
                {

                    this.logger.LogInformation($"Downloading index file from {dataSet.DataSource.IndexFilePath}... This file will be downloaded once and stored locally.");

                    using (HttpClient client = new HttpClient())
                    using (HttpResponseMessage response = client.GetStringAsync(dataSet.DataSource.IndexFilePath).Result)
                    using (FileStream fs = new FileStream(vrtFileName, FileMode.Create, FileAccess.Write))
                    {
                        var contentbytes = client.GetByteArrayAsync(dataSet.DataSource.IndexFilePath).Result;
                        fs.Write(contentbytes, 0, contentbytes.Length);
                    }

                }

                // Cache

                if (_cacheByDemName == null)
                {
                    _cacheByDemName = new ConcurrentDictionary<string, List<DEMFileSource>>();
                }
                if (_cacheByDemName.ContainsKey(vrtFileName) == false)
                {
                    _cacheByDemName[dataSet.Name] = this.GetSources(dataSet, vrtFileName).ToList();
                }


            }
            catch (Exception ex)
            {
                _logger?.LogError("Unhandled exception: " + ex.Message);
                _logger?.LogInformation(ex.ToString());
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
