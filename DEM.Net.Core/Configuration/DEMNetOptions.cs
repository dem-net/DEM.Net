using System;
using System.Collections.Generic;
using System.Text;
using DEM.Net.Core.Imagery;

namespace DEM.Net.Core.Configuration;

public class DEMNetOptions
{
    public float RenderGpxZTranslateTrackMeters { get; set; } = 5f;

    public float RenderGpxTrailWidthMeters { get; set; } = 15f;
    public List<ImageryProvider> ImageryProviders { get; set; } = [];

    public bool UseImageryDiskCache { get; set; } = true;
    public float ImageryDiskCacheExpirationHours { get; set; } = 5f;

    public float ImageryCacheExpirationMinutes { get; set; } = 5f;

    /// <summary>
    /// Maximum memory (MB) used by the downloaded tiles kept in memory: imagery tiles and the ancestor tiles of
    /// tile pyramid datasets. Least recently used tiles are evicted first.
    /// </summary>
    public int TileMemoryCacheSizeMB { get; set; } = 256;

    public bool PowerOfTwoImages { get; set; } = false;

    /// <summary>
    /// When true, PNG textures are streamed to disk tile row by tile row (low memory, faster)
    /// instead of being composed in memory. JPEG textures and PowerOfTwoImages always use the in-memory path.
    /// </summary>
    public bool MassiveImageGenerator { get; set; } = true;

    public string LocalDirectory { get; set; } = null;

    // Set to true for ensure tiles stitch correctly when tiling
    public bool ReduceMeshPreserveEdges { get; set; } = false;
}
