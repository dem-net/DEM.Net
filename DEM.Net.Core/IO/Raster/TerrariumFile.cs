using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using DEM.Net.Core.Imagery;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;

namespace DEM.Net.Core;

/// <summary>
/// Terrarium encoded elevation tile (PNG or WebP) of a Web Mercator XYZ pyramid, e.g. Mapterhorn.
/// height = (R * 256 + G + B / 256) - 32768 meters.
/// File names must be <c>{z}_{x}_{y}.ext</c>: georeferencing is derived from the tile coordinates,
/// in EPSG:3857 meters, as a cell registered raster (pixel edges at tile edges, samples at pixel centers).
/// </summary>
/// <remarks>See https://github.com/tilezen/joerd/blob/master/docs/formats.md#terrarium</remarks>
public class TerrariumFile : IRasterFile
{
    public const float NoDataValue = -32768f;

    private readonly string _filePath;
    private readonly int _zoom, _tileX, _tileY;
    private float[] _heights;
    private int _size;

    public TerrariumFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new ArgumentException($"File {filePath} does not exists.");

        _filePath = filePath;
        (_zoom, _tileX, _tileY) = ParseTileName(filePath);
    }

    /// <summary>Parses <c>{z}_{x}_{y}</c> from a tile file name.</summary>
    public static (int z, int x, int y) ParseTileName(string filePath)
    {
        var parts = Path.GetFileNameWithoutExtension(filePath).Split('_');
        if (parts.Length != 3
            || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out int z)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int x)
            || !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out int y))
            throw new ArgumentException($"Terrarium tile file name must be {{z}}_{{x}}_{{y}}: {filePath}", nameof(filePath));
        return (z, x, y);
    }

    public static string GetTileFileName(int z, int x, int y, string extension) => $"{z}_{x}_{y}{extension}";

    private float[] Heights
    {
        get
        {
            if (_heights == null)
            {
                (_heights, _size) = DecodedTiles.GetOrDecode(_filePath);
            }
            return _heights;
        }
    }

    /// <summary>
    /// Number of decoded tiles kept in memory, shared by all readers (about 1 MB each for 512 px tiles). Line
    /// elevations (OSM roads, GPX) open the same tiles once per line: without it, each opening decodes the image again.
    /// </summary>
    public static int DecodedTileCacheCapacity
    {
        get => DecodedTiles.Capacity;
        set => DecodedTiles.Capacity = value;
    }

    private static readonly DecodedTileCache DecodedTiles = new DecodedTileCache(128);

    /// <summary>LRU of decoded heights by file path. Arrays are shared: they must not be modified.</summary>
    private sealed class DecodedTileCache
    {
        private sealed class Entry
        {
            public string Path;
            public DateTime LastWriteUtc;
            public float[] Heights;
            public int Size;
        }

        private readonly object _lock = new object();
        private readonly Dictionary<string, LinkedListNode<Entry>> _byPath = new Dictionary<string, LinkedListNode<Entry>>(StringComparer.Ordinal);
        private readonly LinkedList<Entry> _lru = new LinkedList<Entry>();
        private int _capacity;

        public DecodedTileCache(int capacity) => _capacity = capacity;

        public int Capacity
        {
            get { lock (_lock) return _capacity; }
            set { lock (_lock) { _capacity = Math.Max(0, value); Trim(); } }
        }

        public (float[] heights, int size) GetOrDecode(string path)
        {
            DateTime lastWrite = File.GetLastWriteTimeUtc(path);
            lock (_lock)
            {
                if (_byPath.TryGetValue(path, out var node))
                {
                    if (node.Value.LastWriteUtc == lastWrite)
                    {
                        _lru.Remove(node);
                        _lru.AddFirst(node);
                        return (node.Value.Heights, node.Value.Size);
                    }
                    _lru.Remove(node);
                    _byPath.Remove(path);
                }
            }

            // Decode outside the lock: concurrent requests may decode the same tile, the last one is kept
            float[] heights = Decode(File.ReadAllBytes(path), out int size);
            lock (_lock)
            {
                if (_capacity > 0)
                {
                    if (_byPath.TryGetValue(path, out var existing))
                    {
                        _lru.Remove(existing);
                        _byPath.Remove(path);
                    }
                    var entry = new Entry { Path = path, LastWriteUtc = lastWrite, Heights = heights, Size = size };
                    _byPath[path] = _lru.AddFirst(entry);
                    Trim();
                }
            }
            return (heights, size);
        }

        private void Trim()
        {
            while (_lru.Count > _capacity)
            {
                var last = _lru.Last;
                _lru.RemoveLast();
                _byPath.Remove(last.Value.Path);
            }
        }
    }

    private int Size
    {
        get
        {
            _ = Heights;
            return _size;
        }
    }

    public static float DecodeHeight(byte r, byte g, byte b) => r * 256f + g + b / 256f - 32768f;

    /// <summary>Decodes a square Terrarium image to row major heights.</summary>
    public static float[] Decode(byte[] imageBytes, out int size)
    {
        using (var image = Image.Load<Rgb24>(SafeImageDecoding.Options, imageBytes))
        {
            if (image.Width != image.Height)
                throw new InvalidDataException($"Terrarium tile must be square, got {image.Width}x{image.Height}.");

            size = image.Width;
            var pixels = new Rgb24[size * size];
            image.CopyPixelDataTo(pixels);

            var heights = new float[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                heights[i] = DecodeHeight(pixels[i].R, pixels[i].G, pixels[i].B);
            }
            return heights;
        }
    }

    /// <summary>Encodes row major heights to a lossless Terrarium WebP file (1/256 m precision).</summary>
    public static void EncodeWebP(float[] heights, int size, string filePath)
    {
        if (heights.Length != size * size)
            throw new ArgumentException("Heights length must be size * size.", nameof(heights));

        var pixels = new Rgb24[heights.Length];
        for (int i = 0; i < heights.Length; i++)
        {
            double v = Math.Max(0d, Math.Min(65535.99, heights[i] + 32768d));
            int whole = (int)Math.Floor(v);
            pixels[i] = new Rgb24((byte)(whole >> 8), (byte)(whole & 0xFF), (byte)Math.Floor((v - whole) * 256d));
        }

        using (var image = Image.LoadPixelData<Rgb24>(pixels, size, size))
        {
            image.SaveAsWebp(filePath, new WebpEncoder { FileFormat = WebpFileFormatType.Lossless });
        }
    }

    public void Dispose()
    {
        _heights = null;
    }

    public FileMetadata ParseMetaData(DEMFileDefinition fileFormat)
    {
        var metadata = new FileMetadata(_filePath, fileFormat);
        int size = Size;
        BoundingBox tile = WebMercatorTiles.TileBounds(_zoom, _tileX, _tileY);
        double pixelSize = (tile.xMax - tile.xMin) / size;

        metadata.Width = size;
        metadata.Height = size;
        metadata.PixelScaleX = pixelSize;
        metadata.PixelScaleY = pixelSize;
        metadata.pixelSizeX = pixelSize;
        metadata.pixelSizeY = -pixelSize;

        // Cell registration (same conventions as GeoTiff.ParseMetaData): Data* bounds are the corners of the
        // first and last pixels, physical bounds are the tile edges.
        metadata.DataStartLon = tile.xMin;
        metadata.DataEndLon = tile.xMin + (size - 1) * pixelSize;
        metadata.DataEndLat = tile.yMax;
        metadata.DataStartLat = tile.yMax - (size - 1) * pixelSize;
        metadata.PhysicalStartLon = tile.xMin;
        metadata.PhysicalEndLon = tile.xMax;
        metadata.PhysicalEndLat = tile.yMax;
        metadata.PhysicalStartLat = tile.yMin;

        metadata.BitsPerSample = 32;
        metadata.SampleFormat = RasterSampleFormat.FLOATING_POINT;
        metadata.ScanlineSize = size * sizeof(float);
        metadata.NoDataValue = NoDataValue.ToString(CultureInfo.InvariantCulture);
        metadata.WorldUnits = "meter";

        float min = float.MaxValue, max = float.MinValue;
        foreach (var h in Heights)
        {
            if (h < min) min = h;
            if (h > max) max = h;
        }
        metadata.MinimumAltitude = min;
        metadata.MaximumAltitude = max;

        return metadata;
    }

    public float GetElevationAtPoint(FileMetadata metadata, int x, int y)
    {
        int size = Size;
        x = Math.Max(0, Math.Min(size - 1, x));
        y = Math.Max(0, Math.Min(size - 1, y));
        return Heights[x + y * size];
    }

    public HeightMap GetHeightMap(FileMetadata metadata)
    {
        var bbox = new BoundingBox(metadata.PhysicalStartLon, metadata.PhysicalEndLon, metadata.PhysicalStartLat, metadata.PhysicalEndLat);
        return GetHeightMapInBBox(bbox, metadata, NoDataValue);
    }

    public HeightMap GetHeightMapInBBox(BoundingBox bbox, FileMetadata metadata, float noDataValue = float.MinValue)
    {
        int size = Size;
        float[] heights = Heights;

        // Cell registration, same sampling as GeoTiff.GetHeightMapInBBox (tiled cell path): no overlapping pixel
        int yStart = (int)Math.Floor((bbox.yMax - metadata.DataEndLat) / metadata.pixelSizeY);
        int yEnd = (int)Math.Ceiling((bbox.yMin - metadata.DataEndLat) / metadata.pixelSizeY);
        int xStart = (int)Math.Floor((bbox.xMin - metadata.DataStartLon) / metadata.pixelSizeX);
        int xEnd = (int)Math.Ceiling((bbox.xMax - metadata.DataStartLon) / metadata.pixelSizeX);

        xStart = Math.Max(0, xStart);
        xEnd = Math.Min(metadata.Width - 1, xEnd);
        yStart = Math.Max(0, yStart);
        yEnd = Math.Min(metadata.Height - 1, yEnd);

        var heightMap = new HeightMap(Math.Max(0, xEnd - xStart + 1), Math.Max(0, yEnd - yStart + 1));
        heightMap.Count = heightMap.Width * heightMap.Height;
        var coords = new GeoPoint[heightMap.Count];
        int coordIndex = 0;
        heightMap.BoundingBox = new BoundingBox(0, 0, 0, 0);

        double endLat = metadata.DataEndLat + metadata.pixelSizeY / 2d;
        double startLon = metadata.DataStartLon + metadata.pixelSizeX / 2d;

        for (int y = yStart; y <= yEnd; y++)
        {
            double latitude = endLat + (metadata.pixelSizeY * y);
            if (y == yStart)
            {
                heightMap.BoundingBox.yMax = latitude;
                heightMap.BoundingBox.xMin = startLon + (metadata.pixelSizeX * xStart);
                heightMap.BoundingBox.xMax = startLon + (metadata.pixelSizeX * xEnd);
            }
            if (y == yEnd)
            {
                heightMap.BoundingBox.yMin = latitude;
            }

            int row = y * size;
            for (int x = xStart; x <= xEnd; x++)
            {
                double longitude = startLon + (metadata.pixelSizeX * x);
                float heightValue = heights[row + x];

                if (heightValue < -15000 || heightValue > 10000)
                    heightValue = noDataValue;

                heightMap.Minimum = Math.Min(heightMap.Minimum, heightValue);
                heightMap.Maximum = Math.Max(heightMap.Maximum, heightValue);

                coords[coordIndex++] = new GeoPoint(latitude, longitude, heightValue);
            }
        }

        heightMap.BoundingBox.zMin = heightMap.Minimum;
        heightMap.BoundingBox.zMax = heightMap.Maximum;
        Debug.Assert(coordIndex == coords.Length);

        heightMap.Coordinates = coords;
        return heightMap;
    }
}
