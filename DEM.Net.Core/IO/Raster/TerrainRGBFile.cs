using DEM.Net.Core.Imagery;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core
{
    public class TerrainRGBFile : IRasterFile
    {
        private string filePath;
        private MapTileInfo tileInfo;
        private Rgb24[] pixelData;

        public TerrainRGBFile(string filePath, int tileSize = 512)
        {
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
                throw new ArgumentException($"File {filePath} does not exists.");

            this.filePath = filePath;
            var y = int.Parse(Path.GetFileNameWithoutExtension(fileInfo.Name));
            var x = int.Parse(fileInfo.Directory.Name);
            var z = int.Parse(fileInfo.Directory.Parent.Name);

            tileInfo = new MapTileInfo(x, y, z, tileSize);

            pixelData = new Rgb24[tileSize * tileSize];
            
            using (var image = Image.Load<Rgb24>(filePath))
            {
                image.CopyPixelDataTo(pixelData);
            }
        }

        public void Dispose()
        {
            pixelData = null;
        }

        public float GetElevationAtPoint(FileMetadata metadata, int x, int y)
        {
            var color = this.pixelData[x + y * tileInfo.TileSize];
            var height = -10000f + ((color.R * 256 * 256 + color.G * 256 + color.B) * 0.1f);
            return height;
        }

        public HeightMap GetHeightMap(FileMetadata metadata)
        {
            throw new NotImplementedException();
        }

        public HeightMap GetHeightMapInBBox(BoundingBox bbox, FileMetadata metadata, float noDataValue = float.MinValue)
        {
            throw new NotImplementedException();
        }

        public FileMetadata ParseMetaData(DEMFileDefinition fileFormat)
        {
            throw new NotImplementedException();
        }
    }
}
