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

		private FileMetadata ParseMetadata(string filename)
		{
			FileMetadata metadata = new FileMetadata(filename);
			_tiff = Tiff.Open(filename, "r");

			///

			int height = _tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
			FieldValue[] modelPixelScaleTagTest = _tiff.GetField((TiffTag)33550);
			FieldValue[] modelTiepointTagTest = _tiff.GetField((TiffTag)33922);

			byte[] modelPixelScaleTest = modelPixelScaleTagTest[1].GetBytes();
			double pixelSizeX = BitConverter.ToDouble(modelPixelScaleTest, 0);
			double pixelSizeY = BitConverter.ToDouble(modelPixelScaleTest, 8) * -1;
			metadata.pixelSizeX = pixelSizeX;
			metadata.pixelSizeY = pixelSizeY;

			byte[] modelTransformationTest = modelTiepointTagTest[1].GetBytes();
			double originLon = BitConverter.ToDouble(modelTransformationTest, 24);
			double originLat = BitConverter.ToDouble(modelTransformationTest, 32);


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

			FieldValue[] modelPixelScaleTag = _tiff.GetField((TiffTag)33550);
			FieldValue[] modelTiepointTag = _tiff.GetField((TiffTag)33922);

			byte[] modelPixelScale = modelPixelScaleTag[1].GetBytes();
			metadata.PixelScaleX = BitConverter.ToDouble(modelPixelScale, 0);
			metadata.PixelScaleY = BitConverter.ToDouble(modelPixelScale, 8);

			// Ignores first set of model points (3 bytes) and assumes they are 0's...
			byte[] modelTransformation = modelTiepointTag[1].GetBytes();
			metadata.OriginLongitude = BitConverter.ToDouble(modelTransformation, 24);
			metadata.OriginLatitude = BitConverter.ToDouble(modelTransformation, 32);

			// Grab some raster metadata
			metadata.BitsPerSample = _tiff.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();

			// Add other information about the data
			metadata.SampleFormat = "Single";
			// TODO: Read this from tiff metadata or determine after parsing
			metadata.NoDataValue = "-10000";

			metadata.WorldUnits = "meter";

			return metadata;


		}

		private static void PrintTagInfo(Tiff tiff, TiffTag tiffTag)
		{
			try
			{
				var field = tiff.GetField(tiffTag);
				if (field != null)
				{
					Console.WriteLine($"{tiffTag}");
					for (int i = 0; i < field.Length; i++)
					{
						Console.WriteLine($"  [{i}] {field[i].Value}");
						byte[] bytes = field[i].Value as byte[];
						if (bytes != null)
						{
							Console.WriteLine($"    Length: {bytes.Length}");
							if (bytes.Length % 8 == 0)
							{
								for (int k = 0; k < bytes.Length / 8; k++)
								{
									Console.WriteLine($"      [{k}] {BitConverter.ToDouble(bytes, k * 8)}");
								}
							}

							try
							{
								Console.WriteLine($"   > {System.Text.Encoding.ASCII.GetString(bytes).Trim()} < ");
							}
							catch (Exception ex)
							{

							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"ERROR: {tiffTag}");
			}
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


		public HeightMap ConvertToHeightMap(string inputFile)
		{
			var metadata = ParseMetadata(inputFile);
			HeightMap heightMap = ParseGeoData(metadata);
			return heightMap;
		}
	}
}
