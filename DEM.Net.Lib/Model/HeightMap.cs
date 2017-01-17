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
		public FileMetadata FileMetadata { get; internal set; }

		private string _fileTitle;

		public string FileTitle
		{
			get
			{
				if (_fileTitle == null)
				{
					_fileTitle = Path.GetFileName(FileMetadata.Filename);
				}
				return _fileTitle;
			}
		}

		public HeightMap(int width, int height)
		{
			Width = width;
			Height = height;
			Coordinates = new List<GeoPoint>(width * height);
		}

		public List<GeoPoint> Coordinates { get; set; }

		public float Mininum
		{
			get { return FileMetadata.MininumAltitude; }
			internal set { FileMetadata.MininumAltitude = value; }
		}
		public float Maximum
		{
			get { return FileMetadata.MaximumAltitude; }
			internal set { FileMetadata.MaximumAltitude = value; }
		}
		public float Range
		{
			get { return Maximum - Mininum; }
		}

		public int Width { get; private set; }
		public int Height { get; private set; }
	}
}
