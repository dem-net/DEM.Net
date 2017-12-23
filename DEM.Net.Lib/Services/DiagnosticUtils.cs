using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib
{
	public class DiagnosticUtils
	{
		public static void OutputDebugBitmap(HeightMap heightMap, string bitmapFilename, float noDataValue)
		{
			OutputDebugBitmapInternal(heightMap, bitmapFilename, noDataValue, true);
		}

		public static void OutputDebugBitmap(HeightMap heightMap, string bitmapFilename)
		{
			OutputDebugBitmapInternal(heightMap, bitmapFilename, 0.0f, false);
		}

		private static void OutputDebugBitmapInternal(HeightMap heightMap, string bitmapFilename, float noDataValue, bool filterNodata)
		{
			float range = heightMap.Range;


			using (Bitmap b = new Bitmap(heightMap.Width, heightMap.Height, PixelFormat.Format8bppIndexed))
			{
				ColorPalette ncp = b.Palette;
				for (int i = 0; i < 256; i++)
					ncp.Entries[i] = Color.FromArgb(255, i, i, i);
				b.Palette = ncp;

				var BoundsRect = new Rectangle(0, 0, b.Width, b.Height);
				BitmapData bmpData = b.LockBits(BoundsRect, ImageLockMode.WriteOnly, b.PixelFormat);

				IntPtr ptr = bmpData.Scan0;

				int bytes = bmpData.Stride * b.Height;
				var rgbValues = new byte[bytes];

				// fill in rgbValues, e.g. with a for loop over an input array
				foreach (GeoPoint geoPoint in heightMap.Coordinates)
				{
					var dataValue = geoPoint.Elevation;

					ushort normalizedValue = 0;
					if (filterNodata && dataValue == noDataValue)
					{
						normalizedValue = 0;
					}
					else
					{
						normalizedValue = (ushort)((geoPoint.Elevation - heightMap.Mininum) / range * 255);
					}

					rgbValues[geoPoint.YIndex.Value * bmpData.Stride + geoPoint.XIndex.Value] = (byte)normalizedValue;
				}

				Marshal.Copy(rgbValues, 0, ptr, bytes);
				b.UnlockBits(bmpData);
				b.Save(bitmapFilename);
			}

		}
	}
}
