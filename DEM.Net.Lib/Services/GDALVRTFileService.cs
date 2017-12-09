using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DEM.Net.Lib.Services
{
    public class GDALVRTFileService : IDisposable
    {
        private readonly string localDirectory;
        private string _vrtFileName;
        private readonly DEMDataSet dataSet;

        public GDALVRTFileService(string localDirectory, DEMDataSet dataSet)
        {
            this.localDirectory = localDirectory;
            this.dataSet = dataSet;
        }

        public void Dispose()
        {
        }

        /// <summary>
        /// Ensures local directories are created and download VRT file if needed
        /// TODO : check local file age and download again if obsolete
        /// </summary>
        internal void Setup()
        {
            try
            {
                if (dataSet == null)
                    throw new ArgumentNullException("Dataset is null.");

                Trace.TraceInformation($"Setup for {dataSet.Name} dataset.");
                
                if (!Directory.Exists(localDirectory))
                {
                    Directory.CreateDirectory(localDirectory);
                }

                _vrtFileName = Path.Combine(localDirectory, UrlHelper.GetFileNameFromUrl(dataSet.VRTFileUrl));

                bool download = true;
                if (File.Exists(_vrtFileName))
                {
                    // Download if too old file
                    if ((DateTime.Now - File.GetCreationTime(_vrtFileName)).TotalDays > 30)
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
                    Trace.TraceInformation($"Donwloading file from {dataSet.VRTFileUrl}...");
                    using (WebClient webClient = new WebClient())
                    {
                        webClient.DownloadFile(dataSet.VRTFileUrl, _vrtFileName);
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
        private double _noDataValue;
        private Dictionary<string, string> _properties;
        /// <summary>
        /// Enumerates throught all the sources
        /// Supports only VRTRasterBand with ComplexSource or SimpleSource
        /// </summary>
        /// <returns></returns>
        public IEnumerable<GDALSource> Sources()
        {



            // Create an XmlReader
            using (XmlReader reader = XmlReader.Create(_vrtFileName))
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

                        switch(reader.Name)
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
            return geoTransform.Trim().Split(',').Select(val => double.Parse(val, CultureInfo.InvariantCulture)).ToArray();
        }
    }
}
