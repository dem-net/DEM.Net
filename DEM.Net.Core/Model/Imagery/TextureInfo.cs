// TextureInfo.cs
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core.Imagery
{
    public class TextureInfo
    {

        public string FilePath { get; set; }
        public string FileName { get; }
        public TextureImageFormat ImageFormat { get; }
        public int Width { get; }
        public int Height { get; }
        public int ProjectedZoom { get; }
        public BoundingBox ProjectedBounds { get; }
        public int? TileCount { get; set; }

        public TextureInfo(string filePath, TextureImageFormat imageFormat, int width, int height, int zoom = 0, BoundingBox projectedBounds = null, int? tileCount = null)
        {
            this.FilePath = filePath;
            this.FileName = Path.GetFileName(filePath);
            this.ImageFormat = imageFormat;
            this.Width = width;
            this.Height = height;
            this.ProjectedZoom = zoom;
            this.ProjectedBounds = projectedBounds;
            this.TileCount = tileCount;
        }


    }
}
