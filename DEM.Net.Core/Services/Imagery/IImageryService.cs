using System;
using System.Collections.Generic;
using System.Numerics;


namespace DEM.Net.Core.Imagery
{
    public interface IImageryService
    {
        Uri BuildUri(ImageryProvider provider, int x, int y, int zoom);

        bool IsTokenConfigurationValid(ImageryProvider provider);
        List<Vector2> ComputeUVMap(HeightMap heightMap, TextureInfo textureInfo);
        TextureInfo ConstructTexture(TileRange tiles, BoundingBox bbox, string fileName, TextureImageFormat mimeType);

        TextureInfo ConstructTextureWithGpxTrack(TileRange tiles, BoundingBox bbox, string fileName, TextureImageFormat mimeType, IEnumerable<GeoPoint> gpxPoints);

        TextureInfo GenerateNormalMap(HeightMap heightMap, string outputDirectory, string fileName = "normalmap.jpg");
        TextureInfo GenerateHeightMap(HeightMap heightMap, string outputDirectory, string fileName = "heightmap.png");

        List<ImageryProvider> GetRegisteredProviders();

        TileRange ComputeBoundingBoxTileRange(BoundingBox bbox, ImageryProvider provider, int minTilesPerImage = 4);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tiles">Tile calculated from ComputeBoundingBoxTileRange</param>
        /// <param name="provider"></param>
        /// <returns></returns>
        TileRange DownloadTiles(TileRange tiles, ImageryProvider provider);

        TileRange DownloadTiles(BoundingBox bbox, ImageryProvider provider, int minTilesPerImage = 4);

    }
}