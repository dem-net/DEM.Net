// TileRange.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core.Imagery
{
    public class TileRange
    {
        private List<MapTile> _tiles = new List<MapTile>();
        private object _syncLock = new object();

        public TileRange(ImageryProvider provider)
        {
            TileSize = provider.TileSize;
        }
        public TileRange(int tileSize)
        {
            TileSize = tileSize;
        }
        public TileRange(MapTileInfo start, MapTileInfo end, int tileSize)
        {
            TileSize = tileSize;
            this.Start = start; this.End = end;
        }

        public void Add(MapTile tile)
        {
            lock (_syncLock)
            {
                _tiles.Add(tile);
            }
        }
        public void AddRange(IEnumerable<MapTile> tiles)
        {
            lock (_syncLock)
            {
                _tiles.AddRange(tiles);
            }
        }

        public IReadOnlyList<MapTile> Tiles
        {
            get
            {
                return _tiles.AsReadOnly();
            }
        }
        public IEnumerable<MapTileInfo> TilesInfo
        {
            get
            {
                for (int x = Start.X; x <= End.X; x++)
                    for (int y = Start.Y; y <= End.Y; y++)
                    {
                        yield return new MapTileInfo(x, y, Start.Zoom, this.TileSize);
                    }
            }
        }

        public TileRange ZoomIn()
        {
            if (this.Start.Zoom == 23) return this;

            return new TileRange(Start.ZoomIn("0"), End.ZoomIn("3"), this.TileSize);
        }

        public TileRange ZoomOut()
        {
            if (this.Start.Zoom == 1) return this;

            return new TileRange(Start.ZoomOut(), End.ZoomOut(), this.TileSize);
        }

        public int TileSize { get; set; }
        public MapTileInfo Start { get; set; }
        public MapTileInfo End { get; set; }
        public int NumCols => End.X - Start.X + 1;
        public int NumRows => End.Y - Start.Y + 1;
        public int Count => NumCols * NumRows;
        public int Width => NumCols * TileSize;
        public int Height => NumRows * TileSize;

        public int Zoom => Start.Zoom;

        public BoundingBox AreaOfInterest { get; internal set; }

    }
}
