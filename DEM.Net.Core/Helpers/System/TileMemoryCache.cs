using System;
using DEM.Net.Core.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace DEM.Net.Core;

/// <summary>
/// Memory cache for downloaded tiles (imagery tiles, DEM ancestor tiles), bounded in bytes by
/// <see cref="DEMNetOptions.TileMemoryCacheSizeMB"/>: the least recently used entries are evicted first.
/// </summary>
/// <remarks>
/// Separate from the application <see cref="IMemoryCache"/>: a size limit there would require a size on every
/// entry of the host application.
/// </remarks>
public sealed class TileMemoryCache : IDisposable
{
    /// <summary>Accounted size of a small entry (flag, empty result).</summary>
    public const long SmallEntrySize = 64;

    private readonly MemoryCache _cache;

    public TileMemoryCache(IOptions<DEMNetOptions> options)
    {
        SizeLimit = Math.Max(1, options.Value.TileMemoryCacheSizeMB) * 1024L * 1024L;
        _cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = SizeLimit, CompactionPercentage = 0.25, TrackStatistics = true });
    }

    /// <summary>Maximum total size of the cached entries, in bytes.</summary>
    public long SizeLimit { get; }

    public bool TryGetValue<T>(object key, out T value)
    {
        if (_cache.TryGetValue(key, out object? cached) && cached is T typed)
        {
            value = typed;
            return true;
        }
        value = default!;
        return false;
    }

    /// <summary>Caches a value with a sliding expiration.</summary>
    /// <param name="sizeBytes">Approximate memory used by the value, in bytes (an entry larger than the limit is not cached).</param>
    public void Set<T>(object key, T value, long sizeBytes, TimeSpan slidingExpiration)
        => _cache.Set(key, value, new MemoryCacheEntryOptions { Size = Math.Max(1, sizeBytes), SlidingExpiration = slidingExpiration });

    /// <summary>Caches a value with an absolute expiration relative to now.</summary>
    public void SetAbsolute<T>(object key, T value, long sizeBytes, TimeSpan expiration)
        => _cache.Set(key, value, new MemoryCacheEntryOptions { Size = Math.Max(1, sizeBytes), AbsoluteExpirationRelativeToNow = expiration });

    /// <summary>Total accounted size of the cached entries, in bytes (null when statistics are not available).</summary>
    public long? CurrentSize => _cache.GetCurrentStatistics()?.CurrentEstimatedSize;

    public void Dispose() => _cache.Dispose();
}
