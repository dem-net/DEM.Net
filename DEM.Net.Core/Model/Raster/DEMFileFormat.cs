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

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core
{
    /// <summary>
    /// File specification for a given dataset. Used in raster services to handle properly different cases of files encountered
    /// Here is defined the format of raster file (extension, raster type, grid registration)
    /// </summary>
    [ProtoContract]
	public class DEMFileDefinition
    {
        public DEMFileDefinition(string name, DEMFileType format, string extension, DEMFileRegistrationMode registration)
        {
            this.Name = name;
            this.Type = format;
            this.FileExtension = extension;
            this.Registration = registration;
        }
        public DEMFileDefinition(DEMFileType format, DEMFileRegistrationMode registration)
        {
            this.Name = null;
            this.Type = format;
            this.FileExtension = null;
            this.Registration = registration;
        }

        public DEMFileDefinition()
        {
        }
        /// <summary>
        /// Common name
        /// </summary>
        [ProtoMember(1)]
        public string Name { get; set; }
        /// <summary>
        /// Physical raster files extension (format: ".ext")
        /// </summary>
		[ProtoMember(2)]
        public string FileExtension { get; set; }

        /// <summary>
        /// Physical file format enumeration
        /// </summary>
        [ProtoMember(3)]
        public DEMFileType Type { get; set; }

        /// <summary>
        /// Grid/node-registered: cells are centered on lines of latitude and longitude (usually there is one pixel overlap for each tile).
        /// Cell/pixel-registered: cell edges are along lines of latitude and longitude.
        /// Good explanation here : https://www.ngdc.noaa.gov/mgg/global/gridregistration.html
        /// </summary>
        [ProtoMember(4)]
        public DEMFileRegistrationMode Registration { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
