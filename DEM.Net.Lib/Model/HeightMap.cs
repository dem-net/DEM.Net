using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib
{
    [Serializable()]
    public class HeightMap
    {
        public HeightMap(int width, int height)
        {
            Width = width;
            Height = height;
            Coordinates = null;
        }

        private BoundingBox _bbox;
        public BoundingBox BoundingBox
        {
            get
            {
                if (_bbox == null)
                {
                    _bbox = new BoundingBox(Coordinates.Min(c => c.Longitude)
                        , Coordinates.Max(c => c.Longitude)
                        , Coordinates.Min(c => c.Latitude)
                        , Coordinates.Max(c => c.Latitude));
                }
                return _bbox;
            }
            set
            {
                _bbox = value;
            }
        }
        
        public IEnumerable<GeoPoint> Coordinates { get; set; }

        public int Count { get; set; }

        public float Mininum { get; set; }
        public float Maximum { get; set; }
        public float Range
        {
            get { return Maximum - Mininum; }
        }

        public int Width { get; private set; }
        public int Height { get; private set; }

        public HeightMap Clone()
        {
            return (HeightMap)this.MemberwiseClone();
        }


    }
}
