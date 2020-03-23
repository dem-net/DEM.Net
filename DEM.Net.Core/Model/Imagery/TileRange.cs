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
    public class TileRange : IEnumerable<MapTile>
    {
        private List<MapTile> _tiles;
        private object _syncLock = new object();
        public TileRange(ImageryProvider provider)
        {
            Provider = provider;
            _tiles = new List<MapTile>();
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

        public ImageryProvider Provider { get; set; }
        public MapTileInfo Start { get; set; }
        public MapTileInfo End { get; set; }
        public int NumCols => End.X - Start.X + 1;
        public int NumRows => End.Y - Start.Y + 1;
        public int Count => NumCols * NumRows;

        public BoundingBox AreaOfInterest { get; internal set; }

        public IEnumerator<MapTile> GetEnumerator()
        {
            lock (_syncLock)
            {
                return _tiles.GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            lock (_syncLock)
            {
                return _tiles.GetEnumerator();
            }
        }


        public IEnumerable<MapTileInfo> EnumerateRange()
        {
            for (int x = Start.X; x <= End.X; x++)
                for (int y = Start.Y; y <= End.Y; y++)
                {
                    yield return new MapTileInfo(x, y, Start.Zoom);
                }
        }
    }
}
