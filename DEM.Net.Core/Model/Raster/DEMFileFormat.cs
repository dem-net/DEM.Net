// DEMFileFormat.cs
//
// Author:
//       Xavier Fischer 
//
// Copyright (c) 2019 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core
{
    /// <summary>
    /// File specification for a given dataset. Used in raster services to handle properly different cases of files encountered
    /// Here is defined the format of raster file (extension, raster type, overlaps)
    /// </summary>
	public class DEMFileDefinition
	{
        public DEMFileDefinition(string name, DEMFileFormat format, string extension, bool onePixelOverlap)
        {
            this.Name = name;
            this.Format = format;
            this.FileExtension = extension;
            this.OnePixelOverlap = onePixelOverlap;
        }

        public DEMFileDefinition()
        {
        }
        /// <summary>
        /// Common name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Physical raster files extension (format: ".ext")
        /// </summary>
		public string FileExtension { get; set; }

        /// <summary>
        /// Some tiled DEM have 1px overlap around each tile (like NASA files). Set to true to handle properly those files
        /// </summary>
        public bool OnePixelOverlap { get;  set; }
        /// <summary>
        /// Physical file format enumeration
        /// </summary>
        public DEMFileFormat Format { get; set; }

		public override string ToString()
		{
			return Name;
		}
	}

    public static class DEMFileDefinitions
    {
        public static DEMFileDefinition SRTM_HGT => new DEMFileDefinition("Nasa SRTM HGT", DEMFileFormat.SRTM_HGT, ".hgt", onePixelOverlap: true);
        public static DEMFileDefinition GEOTIFF  => new DEMFileDefinition("GeoTiff file", DEMFileFormat.GEOTIFF, ".tif", onePixelOverlap: false);

        public static DEMFileDefinition Overlapped(DEMFileDefinition d)
        {
            return new DEMFileDefinition(d.FileExtension, d.Format, d.FileExtension, true);
        }
    }

    public enum DEMFileFormat
    {
        /// <summary>
        /// Shuttle Radar Topography Mission (SRTM) Data file.
        /// </summary>
        SRTM_HGT,
        /// <summary>
        /// "Georeferenced TIFF file"
        /// </summary>
        GEOTIFF,
        /// <summary>
        /// Network Common Data Form (Climat and Forecast)
        /// </summary>
        CF_NetCDF
    }
}
