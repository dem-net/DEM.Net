// GDALVRTFileService.cs
//
// Author:
//       Xavier Fischer
//
// Copyright (c) 2019 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the right
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DEM.Net.Core
{
    /// <summary>
    /// Remote GDAL VRT file handling
    /// Downloads and enumerates through tiles referenced in VRT file
    /// </summary>
    public class GDALVRTFileService : IDEMDataSetIndex
    {
        private const int MAX_AGE_DAYS = 100;
        private static readonly object DOWNLOAD_LOCKER = new object();
        private readonly ILogger<GDALVRTFileService> _logger;
        private ConcurrentDictionary<string, List<DEMFileSource>> _cacheByDemName;
        private readonly IHttpClientFactory _httpClientFactory;

        public GDALVRTFileService(IHttpClientFactory httpClientFactory, ILogger<GDALVRTFileService> logger = null)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _cacheByDemName = new ConcurrentDictionary<string, List<DEMFileSource>>();
        }
        public void Reset()
        {
            _cacheByDemName = new ConcurrentDictionary<string, List<DEMFileSource>>();
        }

        /// <summary>
        /// Ensures local directories are created and download VRT file if needed
        /// </summary>
        void IDEMDataSetIndex.Setup(DEMDataSet dataSet, string dataSetLocalDir)
        {
            try
            {

                if (dataSet == null)
                    throw new ArgumentNullException(nameof(dataSet), "Dataset is null.");

                if (_cacheByDemName.ContainsKey(dataSet.Name))
                    return;

                _logger?.LogInformation($"Setup for {dataSet.Name} dataset.");

                if (!Directory.Exists(dataSetLocalDir))
                {
                    Directory.CreateDirectory(dataSetLocalDir);
                }

                string vrtFileName = Path.Combine(dataSetLocalDir, UrlHelper.GetFileNameFromUrl(dataSet.DataSource.IndexFilePath));


                bool download = true;
                if (File.Exists(vrtFileName))
                {
                    // Download if too old file
                    if ((DateTime.Now - File.GetLastWriteTime(vrtFileName)).TotalDays > MAX_AGE_DAYS)
                    {
                        _logger?.LogInformation("VRT file is too old.");
                    }
                    else if (IsCorrupted(dataSet, vrtFileName))
                    {
                        _logger?.LogInformation("VRT file is corrupted.");
                    }
                    else
                    {
                        download = false;
                    }
                }

                if (download)
                {
                    lock (DOWNLOAD_LOCKER)
                    {
                        if (download)
                        {
                            _logger?.LogInformation($"Downloading index file from {dataSet.DataSource.IndexFilePath}... This file will be downloaded once and stored locally.");

                            HttpClient client = _httpClientFactory == null ? new HttpClient() : _httpClientFactory.CreateClient();

                            using (HttpResponseMessage response = client.GetAsync(dataSet.DataSource.IndexFilePath).Result)
                            using (FileStream fs = new FileStream(vrtFileName, FileMode.Create, FileAccess.Write))
                            {
                                var contentbytes = client.GetByteArrayAsync(dataSet.DataSource.IndexFilePath).Result;
                                fs.Write(contentbytes, 0, contentbytes.Length);
                            }
                            download = false;
                        }
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

        private bool IsCorrupted(DEMDataSet dataSet, string vrtFileName)
        {
            bool ok = false;
            try
            {
                ok = !this.GetSources(dataSet, vrtFileName).Any();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, $"VRT file corrupted: {ex.Message}");
                ok = true;
            }
            return ok;
        }


        /// <summary>
        /// Enumerates throught all the sources
        /// Supports only VRTRasterBand with ComplexSource or SimpleSource
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Enumerates through all sources and find the ones intersecting with the provider BBox
        /// </summary>
        /// <param name="dataset"></param>
        /// <param name="bbox"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Downloads a raster file using downloader configured for the dataset
        /// </summary>
        /// <param name="report">Report item return by GenerateReport methods</param>
        /// <param name="dataset"></param>
        public void DownloadRasterFile(DemFileReport report, DEMDataSet dataset)
        {
            // Create directories if not existing
            new FileInfo(report.LocalName).Directory.Create();

            HttpClient client = _httpClientFactory == null ? new HttpClient() : _httpClientFactory.CreateClient();

            var contentbytes = client.GetByteArrayAsync(report.URL).Result;
            using (FileStream fs = new FileStream(report.LocalName, FileMode.Create, FileAccess.Write))
            {
                fs.Write(contentbytes, 0, contentbytes.Length);
            }


        }

        private IEnumerable<DEMFileSource> GetSources(DEMDataSet dataSet, string vrtFileName)
        {
            Uri localVrtUri = new Uri(Path.GetFullPath(vrtFileName), UriKind.Absolute);
            Uri remoteVrtUri = new Uri(dataSet.DataSource.IndexFilePath, UriKind.Absolute);
            double[] geoTransform;
            var registration = dataSet.FileFormat.Registration;
            Dictionary<string, string> properties;

            // Create an XmlReader
            using (FileStream fileStream = new FileStream(vrtFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (XmlReader reader = XmlReader.Create(fileStream))
            {
                if (reader.ReadToFollowing("GeoTransform"))
                {
                    geoTransform = ParseGeoTransform(reader.ReadElementContentAsString());
                }
                else
                    throw new Exception("GeoTransform element not found!");

                string sourceName = "";
                if (reader.ReadToFollowing("VRTRasterBand"))
                {
                    properties = new Dictionary<string, string>();
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            if (reader.Name == "ComplexSource" || reader.Name == "SimpleSource")
                            {
                                sourceName = reader.Name;
                                break;
                            }
                            properties[reader.Name] = reader.ReadElementContentAsString();
                        }
                    }


                    bool isOnFirstSource = true;
                    while (isOnFirstSource || reader.ReadToFollowing(sourceName))
                    {
                        DEMFileSource source = ParseGDALSource(reader);

                        // SetLocalFileName
                        source.SourceFileNameAbsolute = new Uri(remoteVrtUri, source.SourceFileName).ToString();
                        source.LocalFileName = new Uri(localVrtUri, source.SourceFileName).LocalPath;

                        // Transform origin
                        // Xp = padfTransform[0] + P * padfTransform[1] + L * padfTransform[2];
                        // Yp = padfTransform[3] + P * padfTransform[4] + L * padfTransform[5];
                        source.OriginLon = geoTransform[0] + source.DstxOff * geoTransform[1] + source.DstyOff * geoTransform[2];
                        source.OriginLat = geoTransform[3] + source.DstxOff * geoTransform[4] + source.DstyOff * geoTransform[5];
                        source.DestLon = geoTransform[0] + (source.DstxOff + source.DstxSize) * geoTransform[1] + (source.DstyOff + source.DstySize) * geoTransform[2];
                        source.DestLat = geoTransform[3] + (source.DstxOff + source.DstxSize) * geoTransform[4] + (source.DstyOff + source.DstySize) * geoTransform[5];

                        if (registration == DEMFileRegistrationMode.Grid)
                        {
                            source.BBox = new BoundingBox(Math.Round(source.OriginLon + geoTransform[1] / 2, 10),
                                                                        Math.Round(source.DestLon - +geoTransform[1] / 2, 10),
                                                                        Math.Round(source.DestLat - geoTransform[5] / 2, 10),
                                                                        Math.Round(source.OriginLat + geoTransform[5] / 2, 10));
                        }
                        else
                        {
                            source.OriginLon = Math.Round(source.OriginLon, 10);
                            source.OriginLat = Math.Round(source.OriginLat, 10);
                            source.DestLon = Math.Round(source.DestLon, 10);
                            source.DestLat = Math.Round(source.DestLat, 10);
                            source.BBox = new BoundingBox(source.OriginLon, source.DestLon, source.DestLat, source.OriginLat);
                        }


                        isOnFirstSource = false;

                        yield return source;
                    }
                }
            }


        }

        private DEMFileSource ParseGDALSource(XmlReader reader)
        {
            DEMFileSource source = new DEMFileSource();
            try
            {
                source.Type = reader.Name;
                int depth = reader.Depth;
                while (reader.Read())
                {
                    if (reader.Depth == depth)
                        break;

                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        //<xs:element name="SourceFilename" type="SourceFilenameType"/>
                        //<xs:element name="OpenOptions" type="OpenOptionsType"/>
                        //<xs:element name="SourceBand" type="xs:string"/>  <!-- should be refined into xs:nonNegativeInteger or mask,xs:nonNegativeInteger -->
                        //<xs:element name="SourceProperties" type="SourcePropertiesType"/>
                        //<xs:element name="SrcRect" type="RectType"/>
                        //<xs:element name="DstRect" type="RectType"/>

                        switch (reader.Name)
                        {
                            case "SourceFilename":

                                if (reader.HasAttributes)
                                {
                                    while (reader.MoveToNextAttribute())
                                    {
                                        switch (reader.Name)
                                        {
                                            case "relativeToVRT":
                                                reader.ReadAttributeValue();
                                                source.IsPathRelative = reader.Value == "1";
                                                break;
                                        }
                                    }
                                    reader.MoveToElement();
                                }
                                source.SourceFileName = reader.ReadElementContentAsString();

                                break;

                            case "DstRect":
                                //<DstRect xOff="249600" yOff="8400" xSize="1201" ySize="1201" />
                                if (reader.HasAttributes)
                                {
                                    while (reader.MoveToNextAttribute())
                                    {
                                        switch (reader.Name)
                                        {
                                            case "xOff":
                                                reader.ReadAttributeValue();
                                                source.DstxOff = int.Parse(reader.Value);
                                                break;
                                            case "yOff":
                                                reader.ReadAttributeValue();
                                                source.DstyOff = int.Parse(reader.Value);
                                                break;
                                            case "xSize":
                                                reader.ReadAttributeValue();
                                                source.DstxSize = int.Parse(reader.Value);
                                                break;
                                            case "ySize":
                                                reader.ReadAttributeValue();
                                                source.DstySize = int.Parse(reader.Value);
                                                break;
                                        }
                                    }
                                    reader.MoveToElement();
                                }

                                break;
                            case "NODATA":

                                source.NoData = reader.ReadElementContentAsDouble();
                                break;

                            default:

                                source.Properties[reader.Name] = reader.ReadElementContentAsString();
                                break;
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error parsing GDAL source: {ex.Message}");
            }

            return source;
        }

        private double[] ParseGeoTransform(string geoTransform)
        {
            double[] geoTransformArray = geoTransform.Trim().Split(',').Select(val => double.Parse(val, CultureInfo.InvariantCulture)).ToArray();

            if (geoTransformArray.Length != 6)
            {
                throw new Exception("GeoTransform is not valid. 6 elements accounted.");
            }

            return geoTransformArray;
        }


    }
}
