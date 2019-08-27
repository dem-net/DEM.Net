using System;
using System.Collections.Generic;
using System.Numerics;


namespace DEM.Net.Core.Imagery
{
    public interface IImageryService
    {
        Uri BuildUri(ImageryProvider provider, int x, int y, int zoom);
        List<Vector2> ComputeUVMap(HeightMap heightMap, TextureInfo textureInfo);
        TextureInfo ConstructTexture(TileRange tiles, BoundingBox bbox, string fileName, TextureImageFormat mimeType);

        TextureInfo ConstructTextureWithGpxTrack(TileRange tiles, BoundingBox bbox, string fileName, TextureImageFormat mimeType, IEnumerable<GeoPoint> gpxPoints);
        TileRange DownloadTiles(BoundingBox bbox, ImageryProvider provider, int minTilesPerImage = 4);
        TextureInfo GenerateNormalMap(HeightMap heightMap, string outputDirectory, string fileName = "normalmap.jpg");

        Dictionary<string, string> GetConfiguredTokens();

        List<ImageryProvider> GetRegisteredProviders();

    }
}