using BitMiracle.LibTiff.Classic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Services
{
	public static class GeoTiffService
	{

		public static FileMetadata ParseMetadata(Tiff tiff, string tiffPath)
		{
			FileMetadata metadata = new FileMetadata(tiffPath);
			///
			FieldValue[] modelPixelScaleTag = tiff.GetField(TiffTag.GEOTIFF_MODELPIXELSCALETAG);
			FieldValue[] modelTiepointTag = tiff.GetField(TiffTag.GEOTIFF_MODELTIEPOINTTAG);

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

			var scanline = new byte[tiff.ScanlineSize()];
			metadata.ScanlineSize = tiff.ScanlineSize();
			//TODO: Check if band is stored in 1 byte or 2 bytes. 
			//If 2, the following code would be required
			var scanline16Bit = new ushort[tiff.ScanlineSize() / 2];
			Buffer.BlockCopy(scanline, 0, scanline16Bit, 0, scanline.Length);


			///
			metadata.Height = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
			metadata.Width = tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();

			// Grab some raster metadata
			metadata.BitsPerSample = tiff.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();
			var sampleFormat = tiff.GetField(TiffTag.SAMPLEFORMAT);
			// Add other information about the data
			metadata.SampleFormat = sampleFormat[0].Value.ToString();
			// TODO: Read this from tiff metadata or determine after parsing
			metadata.NoDataValue = "-10000";

			metadata.WorldUnits = "meter";

			DumpTiffTags(tiff);

			return metadata;
		}

		public static void DumpTiffTags(Tiff tiff)
		{
			StringBuilder sb = new StringBuilder();
			foreach (var value in Enum.GetValues(typeof(TiffTag)))
			{
				TiffTag tag = (TiffTag)value;
				FieldValue[] values = tiff.GetField(tag);
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

		public static HeightMap ParseGeoData(Tiff tiff, FileMetadata metadata)
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
				tiff.ReadScanline(scanline, y);
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
	}
}
