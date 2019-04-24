// DemFileReport.cs
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
    /// Contains information about a DEM dataset file
    /// </summary>
    public class DemFileReport
    {

        /// <summary>
        /// Gets the remote URL of the file
        /// </summary>
        /// <value>
        /// Full URL as specified in the VRT file
        /// </value>
        public string URL { get; internal set; }


        /// <summary>
        /// Gets the local path of the file after download
        /// </summary>
        /// <value>
        /// The name of the local.
        /// </value>
        public string LocalName { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether this file is existing locally.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is existing locally; otherwise, <c>false</c>.
        /// </value>
        public bool IsExistingLocally { get; internal set; }

        /// <summary>
        /// Gets the source in VRT file corresponding to this file 
        /// </summary>
        /// <value>
        /// <see cref="GDALSource"/> object
        /// </value>
        public GDALSource Source { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether this file has metadata generated.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this file has metadata generated; otherwise, <c>false</c>.
        /// </value>
        public bool IsMetadataGenerated { get; internal set; }
	}
}
