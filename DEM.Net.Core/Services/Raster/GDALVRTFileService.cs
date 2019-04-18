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
using System.Collections.Generic;
using System.Diagnostics;
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
        private bool _useMemCache = false;
        Dictionary<string, List<GDALSource>> _cacheByDemName;
        private readonly string _localDirectory;
        private string _vrtFileName;
        private Uri _remoteVrtUri;
        private Uri _localVrtUri;
        private DEMDataSet _dataSet;
        private const int MAX_AGE_DAYS = 30;

        public DEMDataSet Dataset { get { return _dataSet; } }

        public GDALVRTFileService(string localDirectory, DEMDataSet dataSet)
        {
            _localDirectory = localDirectory;
            _dataSet = dataSet;
            _cacheByDemName = new Dictionary<string, List<GDALSource>>();
        }

        /// <summary>
        /// Ensures local directories are created and download VRT file if needed
        /// </summary>
        public void Setup(bool useMemoryCache)
        {
            try
            {
                _useMemCache = useMemoryCache;

                if (_dataSet == null)
                    throw new ArgumentNullException("Dataset is null.");

                Trace.TraceInformation($"Setup for {_dataSet.Name} dataset.");

                if (!Directory.Exists(_localDirectory))
                {
                    Directory.CreateDirectory(_localDirectory);
                }

                _vrtFileName = Path.Combine(_localDirectory, UrlHelper.GetFileNameFromUrl(_dataSet.VRTFileUrl));
                _localVrtUri = new Uri(Path.GetFullPath(_vrtFileName), UriKind.Absolute);
                _remoteVrtUri = new Uri(_dataSet.VRTFileUrl, UriKind.Absolute);

                bool download = true;
                if (File.Exists(_vrtFileName))
                {
                    // Download if too old file
                    if ((DateTime.Now - File.GetLastWriteTime(_vrtFileName)).TotalDays > MAX_AGE_DAYS)
                    {
                        Trace.TraceInformation("VRT file is too old.");
                    }
                    else
                    {
                        download = false;
                    }
                }

                if (download)
                {
                    Trace.TraceInformation($"Downloading file from {_dataSet.VRTFileUrl}...");
                    using (HttpClient client = new HttpClient())
                    {
                        using (HttpResponseMessage response = client.GetAsync(_dataSet.VRTFileUrl).Result)
                        {
                            using (FileStream fs = new FileStream(_vrtFileName, FileMode.Create, FileAccess.Write))
                            {
                                var contentbytes = client.GetByteArrayAsync(_dataSet.VRTFileUrl).Result;
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
                if (_useMemCache)
                {
                    if (_cacheByDemName == null)
                    {
                        _cacheByDemName = new Dictionary<string, List<GDALSource>>();
                    }
                    if (_cacheByDemName.ContainsKey(_vrtFileName) == false)
                    {
                        _cacheByDemName[_vrtFileName] = this.Sources().ToList();
                    }

                }

            }
            catch (Exception ex)
            {
                Trace.TraceError("Unhandled exception: " + ex.Message);
                Trace.TraceInformation(ex.ToString());
                throw;
            }
        }

        private double[] _geoTransform;
        private Dictionary<string, string> _properties;
        /// <summary>
        /// Enumerates throught all the sources
        /// Supports only VRTRasterBand with ComplexSource or SimpleSource
        /// </summary>
        /// <returns></returns>
        public IEnumerable<GDALSource> Sources()
        {
            if (_useMemCache && _cacheByDemName.ContainsKey(_vrtFileName))
            {
                foreach (var item in _cacheByDemName[_vrtFileName])
                {
                    yield return item;
                }
            }
            else
            {
                // Create an XmlReader
                using (FileStream fileStream = new FileStream(_vrtFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
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
                            source.SourceFileNameAbsolute = new Uri(_remoteVrtUri, source.SourceFileName).ToString();
                            source.LocalFileName = new Uri(_localVrtUri, source.SourceFileName).LocalPath;

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
                Trace.TraceError($"Error parsing GDAL source: {ex.Message}");
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
