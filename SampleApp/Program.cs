using DEM.Net.Lib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleApp
{
	class Program
	{
		//public const string tiffPath = @"..\..\..\SampleData\sample.tif";
		//public const string tiffPath = @"..\..\..\SampleData\srtm_38_04.tif"; // from http://dwtkns.com/srtm/ SRTM Tile Grabber
		public const string tiffPath = @"..\..\..\SampleData\N043E005_AVE_DSM.tif"; // from http://www.opentopography.org/


		static void Main(string[] args)
		{

			string fileTitle = Path.GetFileNameWithoutExtension(tiffPath);

			// Save metadata
			var outputJsonPath = Path.Combine(Path.GetDirectoryName(tiffPath), "out", fileTitle + ".json");
			if (File.Exists(outputJsonPath)) File.Delete(outputJsonPath);

			var bitmapPath = Path.Combine(Path.GetDirectoryName(tiffPath), "out", fileTitle + ".bmp");
			if (File.Exists(bitmapPath)) File.Delete(bitmapPath);

			HeightMap heightMap = null;
			using (GeoTiff tiffConverter = new GeoTiff())
			{
				heightMap = tiffConverter.ConvertToHeightMap(tiffPath);
			}
			// Json manifest
			File.WriteAllText(outputJsonPath, JsonConvert.SerializeObject(heightMap.FileMetadata, Formatting.Indented));

			// Bitmap
			DiagnosticUtils.OutputDebugBitmap(heightMap, bitmapPath);

			// Save to SQL
			SqlDemRepository.ClearFileData(heightMap);
			SqlDemRepository.SaveHeightmap(heightMap);

		}
	}
}