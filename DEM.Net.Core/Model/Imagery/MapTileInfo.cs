// MapTileInfo.cs
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core.Imagery
{
    public class MapTileInfo
    {

        public MapTileInfo(int x, int y, int zoom)
        {
            X = x;
            Y = y;
            Zoom = zoom;
        }
        public MapTileInfo(PointInt xy, int zoom)
        {
            X = xy.X;
            Y = xy.Y;
            Zoom = zoom;
        }

        public int X { get; set; }
        public int Y { get; set; }
        public int Zoom { get; set; }

        public int TileSize => 256;

        public TileRange ZoomIn()
        {
            if (Zoom == 23) return new TileRange(this, this, this.TileSize);

            return new TileRange(this.ZoomIn("0"), this.ZoomIn("3"), this.TileSize);
        }
        public TileRange ToTileRange()
        {
            return new TileRange(this, this, this.TileSize);
        }
        public MapTileInfo ZoomIn(string quadIndex)
        {
            if (Zoom == 23) return this;

            TileUtils.QuadKeyToTileXY(string.Concat(TileUtils.TileXYToQuadKey(X, Y, Zoom), quadIndex), out int x0, out int y0, out int z0);
            return new MapTileInfo(x0, y0, z0);
        }
        public MapTileInfo ZoomOut()
        {
            if (Zoom == 1) return this;

            var quadKey = TileUtils.TileXYToQuadKey(X, Y, Zoom);
            TileUtils.QuadKeyToTileXY(quadKey.Substring(0, quadKey.Length - 1), out int x0, out int y0, out int z0);
            return new MapTileInfo(x0, y0, z0);
        }

        public override string ToString()
        {
            return $"{X}_{Y}_z{Zoom}";
        }

        public BoundingBox BoundingBox
        {
            get
            {
                var bboxTopLeft = TileUtils.TileXYToPixelXY(this.X, this.Y);
                var bboxBottomRight = TileUtils.TileXYToPixelXY(this.X + 1, this.Y + 1);
                var coordTopLeft = TileUtils.PixelXYToLatLong(bboxTopLeft.X, bboxTopLeft.Y, Zoom);
                var coordBottomRight= TileUtils.PixelXYToLatLong(bboxBottomRight.X-1, bboxBottomRight.Y-1, Zoom);
                
                return new BoundingBox(coordTopLeft.Long, coordBottomRight.Long, coordBottomRight.Lat, coordTopLeft.Lat);
            }
        }
    }
}
