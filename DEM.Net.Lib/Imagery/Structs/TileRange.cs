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
        public TileRange(ImageryProvider provider)
        {
            Provider = provider;
            _tiles = new List<MapTile>();
        }

        public void Add(MapTile tile)
        {
            _tiles.Add(tile);
        }

        public ImageryProvider Provider { get; set; }
        public PointInt Start { get; set; }
        public PointInt End { get; set; }
        public int NumCols => End.X - Start.X + 1;
        public int NumRows => End.Y - Start.Y + 1;

        public BoundingBox AreaOfInterest { get; internal set; }

        public IEnumerator<MapTile> GetEnumerator()
        {
            return _tiles.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _tiles.GetEnumerator();
        }
    }
}
