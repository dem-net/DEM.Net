using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Imagery
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
