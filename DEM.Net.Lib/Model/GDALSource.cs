// GDALSource.cs
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

namespace DEM.Net.Lib
{
    public class GDALSource
    {

        public bool RelativeToVRT { get; set; }
        public string SourceFileName { get; set; }
        public string Type { get; internal set; }
        public Dictionary<string,string> Properties { get; set; }
        public int DstxOff { get; internal set; }
        public int DstyOff { get; internal set; }
        public int DstxSize { get; internal set; }
        public int DstySize { get; internal set; }
        public double NoData { get; internal set; }
        public string SourceFileNameAbsolute { get; internal set; }
        public string LocalFileName { get; internal set; }
        public double OriginLon { get; internal set; }
        public double OriginLat { get; internal set; }
        public double DestLon { get; internal set; }
        public double DestLat { get; internal set; }
        public BoundingBox BBox { get; internal set; }

        public GDALSource()
        {
            Properties = new Dictionary<string, string>();
        }
    }
}
