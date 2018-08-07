using BitMiracle.LibTiff.Classic;
using DEM.Net.Lib.IO;
using DEM.Net.Lib.Services;
using System;

namespace DEM.Net.Lib
{

	public class GeoTiff : IGeoTiff
	{
		Tiff _tiff;
		string _tiffPath;
		static NoLogTiffErrorHandler _errorHandler = new NoLogTiffErrorHandler();

		internal Tiff TiffFile
		{
			get { return _tiff; }
		}

		public string FilePath
		{
			get { return _tiffPath; }
		}



		public GeoTiff(string tiffPath)
		{
			_tiffPath = tiffPath;
			Tiff.SetErrorHandler(_errorHandler);
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

		public float ParseGeoDataAtPoint(FileMetadata metadata, int x, int y)
		{
			float heightValue = 0;
			try
			{
				byte[] scanline = new byte[metadata.ScanlineSize];
				ushort[] scanline16Bit = new ushort[metadata.ScanlineSize / 2];

				TiffFile.ReadScanline(scanline, y);
				Buffer.BlockCopy(scanline, 0, scanline16Bit, 0, scanline.Length);

				heightValue = ParseGeoDataAtPoint(metadata, x, scanline16Bit);
			}
			catch (Exception e)
			{
				throw new Exception($"Error in ParseGeoDataAtPoint: {e.Message}");
			}
			return heightValue;
		}

		public float ParseGeoDataAtPoint(FileMetadata metadata, int x, ushort[] scanline16Bit)
		{
			float heightValue = 0;
			try
			{
				heightValue = (float)scanline16Bit[x];
				if (heightValue > 32768)
				{
					heightValue = metadata.NoDataValueFloat;
				}
			}
			catch (Exception e)
			{
				throw new Exception($"Error in ParseGeoDataAtPoint: {e.Message}");
			}

			return heightValue;
		}

		public FileMetadata ParseMetaData()
		{
			FileMetadata metadata = new FileMetadata(FilePath);

			///
			metadata.Height = TiffFile.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
			metadata.Width = TiffFile.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();

			///
			FieldValue[] modelPixelScaleTag = TiffFile.GetField(TiffTag.GEOTIFF_MODELPIXELSCALETAG);
			FieldValue[] modelTiepointTag = TiffFile.GetField(TiffTag.GEOTIFF_MODELTIEPOINTTAG);

			byte[] modelPixelScale = modelPixelScaleTag[1].GetBytes();
			double pixelSizeX = BitConverter.ToDouble(modelPixelScale, 0);
			double pixelSizeY = BitConverter.ToDouble(modelPixelScale, 8) * -1;
			metadata.pixelSizeX = pixelSizeX;
			metadata.pixelSizeY = pixelSizeY;
			metadata.PixelScaleX = BitConverter.ToDouble(modelPixelScale, 0);
			metadata.PixelScaleY = BitConverter.ToDouble(modelPixelScale, 8);

			// Ignores first set of model points (3 bytes) and assumes they are 0's...
			byte[] modelTransformation = modelTiepointTag[1].GetBytes();
			metadata.OriginLongitude = BitConverter.ToDouble(modelTransformation, 24);
			metadata.OriginLatitude = BitConverter.ToDouble(modelTransformation, 32);


			double startLat = metadata.OriginLatitude + (pixelSizeY / 2.0);
			double startLon = metadata.OriginLongitude + (pixelSizeX / 2.0);
			metadata.StartLat = startLat;
			metadata.StartLon = startLon;

			var scanline = new byte[TiffFile.ScanlineSize()];
			metadata.ScanlineSize = TiffFile.ScanlineSize();
			//TODO: Check if band is stored in 1 byte or 2 bytes. 
			//If 2, the following code would be required
			var scanline16Bit = new ushort[TiffFile.ScanlineSize() / 2];
			Buffer.BlockCopy(scanline, 0, scanline16Bit, 0, scanline.Length);


			// Grab some raster metadata
			metadata.BitsPerSample = TiffFile.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();
			var sampleFormat = TiffFile.GetField(TiffTag.SAMPLEFORMAT);
			// Add other information about the data
			metadata.SampleFormat = sampleFormat[0].Value.ToString();
			// TODO: Read this from tiff metadata or determine after parsing
			metadata.NoDataValue = "-10000";

			metadata.WorldUnits = "meter";

			return metadata;
		}
	}
}
