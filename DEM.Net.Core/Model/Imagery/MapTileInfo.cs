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

        public MapTileInfo(int x, int y, int zoom, int tileSize)
        {
            X = x;
            Y = y;
            Zoom = zoom;
            TileSize = tileSize;
        }
        public MapTileInfo(Point<int> xy, int zoom, int tileSize)
        {
            X = xy.X;
            Y = xy.Y;
            Zoom = zoom;
            TileSize = tileSize;
        }

        public int X { get; set; }
        public int Y { get; set; }
        public int Zoom { get; set; }

        public int TileSize { get; set; }

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
            return new MapTileInfo(x0, y0, z0, this.TileSize);
        }
        public MapTileInfo ZoomOut()
        {
            if (Zoom == 1) return this;

            var quadKey = TileUtils.TileXYToQuadKey(X, Y, Zoom);
            TileUtils.QuadKeyToTileXY(quadKey.Substring(0, quadKey.Length - 1), out int x0, out int y0, out int z0);
            return new MapTileInfo(x0, y0, z0, this.TileSize);
        }

        public override string ToString()
        {
            return $"{X}_{Y}_z{Zoom}";
        }

        public BoundingBox BoundingBox
        {
            get
            {
                var bboxTopLeft = TileUtils.TileXYToGlobalPixel(this.X, this.Y, this.TileSize);
                var bboxBottomRight = TileUtils.TileXYToGlobalPixel(this.X + 1, this.Y + 1, this.TileSize);
                var coordTopLeft = TileUtils.GlobalPixelToPosition(bboxTopLeft, Zoom, this.TileSize);
                var coordBottomRight= TileUtils.GlobalPixelToPosition(new Point<double>(bboxBottomRight.X-1, bboxBottomRight.Y-1), Zoom, this.TileSize);
                
                return new BoundingBox(coordTopLeft.Long, coordBottomRight.Long, coordBottomRight.Lat, coordTopLeft.Lat);
            }
        }
    }
}
