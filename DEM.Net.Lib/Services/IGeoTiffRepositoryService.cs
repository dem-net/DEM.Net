using System.Collections.Generic;
using BitMiracle.LibTiff.Classic;

namespace DEM.Net.Lib.Services
{
    public interface IGeoTiffRepositoryService
    {
        void DumpTiffTags(Tiff tiff);
        void GenerateDirectoryMetadata(string directoryPath);
        List<FileMetadata> GetCoveringFiles(BoundingBox bbox, List<FileMetadata> catalogSubSet = null);
        HeightMap GetHeightMap(string fileName);
        HeightMap GetHeightMap(BoundingBox bbox);        
        void WriteManifestFiles(HeightMap heightMap);
    }
}