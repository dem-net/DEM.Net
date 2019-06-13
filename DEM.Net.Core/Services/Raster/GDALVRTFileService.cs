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
    public class GDALVRTFileService : IGDALVRTFileService
    {
        private const int MAX_AGE_DAYS = 30;

        private readonly ILogger<GDALVRTFileService> _logger;
        private ConcurrentDictionary<string, List<GDALSource>> _cacheByDemName;

        public GDALVRTFileService(ILogger<GDALVRTFileService> logger = null)
        {
            _logger = logger;
            _cacheByDemName = new ConcurrentDictionary<string, List<GDALSource>>();
        }

        /// <summary>
        /// Ensures local directories are created and download VRT file if needed
        /// </summary>
        public void Setup(DEMDataSet dataSet, string dataSetLocalDir)
        {
            try
            {

                if (dataSet == null)
                    throw new ArgumentNullException("Dataset is null.");

                if (_cacheByDemName.ContainsKey(dataSet.Name))
                    return;

                _logger?.LogInformation($"Setup for {dataSet.Name} dataset.");

                if (!Directory.Exists(dataSetLocalDir))
                {
                    Directory.CreateDirectory(dataSetLocalDir);
                }

                string vrtFileName = Path.Combine(dataSetLocalDir, UrlHelper.GetFileNameFromUrl(dataSet.VRTFileUrl));
                Uri localVrtUri = new Uri(Path.GetFullPath(vrtFileName), UriKind.Absolute);
                Uri remoteVrtUri = new Uri(dataSet.VRTFileUrl, UriKind.Absolute);

                bool download = true;
                if (File.Exists(vrtFileName))
                {
                    // Download if too old file
                    if ((DateTime.Now - File.GetLastWriteTime(vrtFileName)).TotalDays > MAX_AGE_DAYS)
                    {
                        _logger?.LogInformation("VRT file is too old.");
                    }
                    else if (IsCorrupted(vrtFileName))
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
                    _logger?.LogInformation($"Downloading index file from {dataSet.VRTFileUrl}... This file will be downloaded once and stored locally.");
                    using (HttpClient client = new HttpClient())
                    {
                        using (HttpResponseMessage response = client.GetAsync(dataSet.VRTFileUrl).Result)
                        {
                            using (FileStream fs = new FileStream(vrtFileName, FileMode.Create, FileAccess.Write))
                            {
                                var contentbytes = client.GetByteArrayAsync(dataSet.VRTFileUrl).Result;
                                fs.Write(contentbytes, 0, contentbytes.Length);
                            }
                        }
                    }
                    //using (WebClient webClient = new WebClient())
                    //{
                    //    webClient.DownloadFile(_dataSet.VRTFileUrl, _vrtFileName);
                    //}
                }

                // Cache

                if (_cacheByDemName == null)
                {
                    _cacheByDemName = new ConcurrentDictionary<string, List<GDALSource>>();
                }
                if (_cacheByDemName.ContainsKey(vrtFileName) == false)
                {
                    _cacheByDemName[dataSet.Name] = this.GetSources(vrtFileName, localVrtUri, remoteVrtUri).ToList();
                }


            }
            catch (Exception ex)
            {
                _logger?.LogError("Unhandled exception: " + ex.Message);
                _logger?.LogInformation(ex.ToString());
                throw;
            }
        }

        private bool IsCorrupted(string vrtFileName)
        {
            return false;
        }

        private double[] _geoTransform;
        private Dictionary<string, string> _properties;
        /// <summary>
        /// Enumerates throught all the sources
        /// Supports only VRTRasterBand with ComplexSource or SimpleSource
        /// </summary>
        /// <returns></returns>
        public IEnumerable<GDALSource> Sources(DEMDataSet dataSet)
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

        private IEnumerable<GDALSource> GetSources(string vrtFileName, Uri localUri, Uri remoteUri)
        {

            // Create an XmlReader
            using (FileStream fileStream = new FileStream(vrtFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (XmlReader reader = XmlReader.Create(fileStream))
            {
                if (reader.ReadToFollowing("GeoTransform"))
                {
                    _geoTransform = ParseGeoTransform(reader.ReadElementContentAsString());
                }
                else
                    throw new Exception("GeoTransform element not found!");

                string sourceName = "";
                if (reader.ReadToFollowing("VRTRasterBand"))
                {
                    _properties = new Dictionary<string, string>();
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            if (reader.Name == "ComplexSource" || reader.Name == "SimpleSource")
                            {
                                sourceName = reader.Name;
                                break;
                            }
                            _properties[reader.Name] = reader.ReadElementContentAsString();
                        }
                    }

                    bool isOnFirstSource = true;
                    while (isOnFirstSource || reader.ReadToFollowing(sourceName))
                    {
                        GDALSource source = ParseGDALSource(reader);

                        // SetLocalFileName
                        source.SourceFileNameAbsolute = new Uri(remoteUri, source.SourceFileName).ToString();
                        source.LocalFileName = new Uri(localUri, source.SourceFileName).LocalPath;

                        // Transform origin
                        // Xp = padfTransform[0] + P * padfTransform[1] + L * padfTransform[2];
                        // Yp = padfTransform[3] + P * padfTransform[4] + L * padfTransform[5];
                        source.OriginLon = _geoTransform[0] + source.DstxOff * _geoTransform[1] + source.DstyOff * _geoTransform[2];
                        source.OriginLat = _geoTransform[3] + source.DstxOff * _geoTransform[4] + source.DstyOff * _geoTransform[5];
                        source.DestLon = _geoTransform[0] + (source.DstxOff + source.DstxSize) * _geoTransform[1] + (source.DstyOff + source.DstySize) * _geoTransform[2];
                        source.DestLat = _geoTransform[3] + (source.DstxOff + source.DstxSize) * _geoTransform[4] + (source.DstyOff + source.DstySize) * _geoTransform[5];
                        source.BBox = new BoundingBox(source.OriginLon, source.DestLon, source.DestLat, source.OriginLat);
                        isOnFirstSource = false;

                        yield return source;
                    }
                }
            }


        }

        private GDALSource ParseGDALSource(XmlReader reader)
        {
            GDALSource source = new GDALSource();
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
                                                source.RelativeToVRT = reader.Value == "1";
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
