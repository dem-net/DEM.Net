using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using BitMiracle.LibTiff.Classic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

namespace DEM.Net.Lib
{
	public class GeoTiff : IDisposable
	{
		Tiff _tiff;
		string _tiffPath;

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

		private FileMetadata ParseMetadata()
		{
			FileMetadata metadata = new FileMetadata(_tiffPath);
			///
			FieldValue[] modelPixelScaleTag = _tiff.GetField(TiffTag.GEOTIFF_MODELPIXELSCALETAG);
			FieldValue[] modelTiepointTag = _tiff.GetField(TiffTag.GEOTIFF_MODELTIEPOINTTAG);

			byte[] modelPixelScale = modelPixelScaleTag[1].GetBytes();
			double pixelSizeX = BitConverter.ToDouble(modelPixelScale, 0);
			double pixelSizeY = BitConverter.ToDouble(modelPixelScale, 8) * -1;
			metadata.pixelSizeX = pixelSizeX;
			metadata.pixelSizeY = pixelSizeY;
			metadata.PixelScaleX = BitConverter.ToDouble(modelPixelScale, 0);
			metadata.PixelScaleY = BitConverter.ToDouble(modelPixelScale, 8);

			// Ignores first set of model points (3 bytes) and assumes they are 0's...
			byte[] modelTransformation = modelTiepointTag[1].GetBytes();
			double originLon = BitConverter.ToDouble(modelTransformation, 24);
			double originLat = BitConverter.ToDouble(modelTransformation, 32);

			double startLat = originLat + (pixelSizeY / 2.0);
			double startLon = originLon + (pixelSizeX / 2.0);
			metadata.StartLat = startLat;
			metadata.StartLon = startLon;

			var scanline = new byte[_tiff.ScanlineSize()];
			metadata.ScanlineSize = _tiff.ScanlineSize();
			//TODO: Check if band is stored in 1 byte or 2 bytes. 
			//If 2, the following code would be required
			var scanline16Bit = new ushort[_tiff.ScanlineSize() / 2];
			Buffer.BlockCopy(scanline, 0, scanline16Bit, 0, scanline.Length);


			///
			metadata.Height = _tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
			metadata.Width = _tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();

			// Grab some raster metadata
			metadata.BitsPerSample = _tiff.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();
			var sampleFormat = _tiff.GetField(TiffTag.SAMPLEFORMAT);
			// Add other information about the data
			metadata.SampleFormat = sampleFormat[0].Value.ToString();
			// TODO: Read this from tiff metadata or determine after parsing
			metadata.NoDataValue = "-10000";

			metadata.WorldUnits = "meter";

			DumpTiffTags();

			return metadata;
		}

		public void DumpTiffTags()
		{
			StringBuilder sb = new StringBuilder();
			foreach (var value in Enum.GetValues(typeof(TiffTag)))
			{
				TiffTag tag = (TiffTag)value;
				FieldValue[] values = _tiff.GetField(tag);
				if (values != null)
				{
					sb.AppendLine(value + ": ");
					foreach (var fieldValue in values)
					{
						sb.Append("\t");
						sb.AppendLine(fieldValue.Value.ToString());
					}
				}
			}
			Console.WriteLine(sb.ToString());
		}

		private HeightMap ParseGeoData(FileMetadata metadata)
		{
			HeightMap heightMap = new HeightMap(metadata.Width, metadata.Height);
			heightMap.FileMetadata = metadata;

			byte[] scanline = new byte[metadata.ScanlineSize];
			ushort[] scanline16Bit = new ushort[metadata.ScanlineSize / 2];
			Buffer.BlockCopy(scanline, 0, scanline16Bit, 0, scanline.Length);

			double currentLat = metadata.StartLat;
			double currentLon = metadata.StartLon;

			for (int y = 0; y < metadata.Height; y++)
			{
				_tiff.ReadScanline(scanline, y);
				Buffer.BlockCopy(scanline, 0, scanline16Bit, 0, scanline.Length);

				double latitude = currentLat + (metadata.pixelSizeY * y);
				for (int x = 0; x < scanline16Bit.Length; x++)
				{
					double longitude = currentLon + (metadata.pixelSizeX * x);

					float heightValue = (float)scanline16Bit[x];
					if (heightValue < 32768)
					{
						heightMap.Mininum = Math.Min(heightMap.Mininum, heightValue);
						heightMap.Maximum = Math.Max(heightMap.Maximum, heightValue);
					}
					else
					{
						heightValue = -10000;
					}
					heightMap.Coordinates.Add(new GeoPoint(latitude, longitude, heightValue, x, y));

				}
			}

			return heightMap;
		}


		public HeightMap ConvertToHeightMap()
		{
			var metadata = ParseMetadata();
			HeightMap heightMap = ParseGeoData(metadata);
			return heightMap;
		}
	}
}
