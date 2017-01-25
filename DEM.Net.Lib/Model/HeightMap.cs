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
			Coordinates = new List<GeoPoint>(width * height);
		}

		public List<GeoPoint> Coordinates { get; set; }

		public float Mininum { get; set; }
        public float Maximum { get; set; }
        public float Range
		{
			get { return Maximum - Mininum; }
		}

		public int Width { get; private set; }
		public int Height { get; private set; }
	}
}
