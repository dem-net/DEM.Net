using BitMiracle.LibTiff.Classic;
using DEM.Net.Lib.Services;
using System;

namespace DEM.Net.Lib
{
	public class GeoTiff : IDisposable
	{
		Tiff _tiff;
		string _tiffPath;

		internal Tiff TiffFile
		{
			get { return _tiff; }
		}

		public GeoTiff(string tiffPath)
		{
			_tiffPath = tiffPath;
			_tiff = Tiff.Open(tiffPath, "r");
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				_tiff?.Dispose();
			}
		}

		~GeoTiff()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}


	}
}
