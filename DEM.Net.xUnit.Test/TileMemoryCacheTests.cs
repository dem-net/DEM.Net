using System;
using System.Linq;
using DEM.Net.Core;
using DEM.Net.Core.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace DEM.Net.Test;

public class TileMemoryCacheTests
{
    [Fact]
    public void TheCachedTilesStayWithinTheSizeLimit()
    {
        using var cache = new TileMemoryCache(Options.Create(new DEMNetOptions { TileMemoryCacheSizeMB = 1 }));
        const int tileBytes = 300 * 1024;

        for (int i = 0; i < 10; i++)
            cache.Set($"tile{i}", new byte[tileBytes], tileBytes, TimeSpan.FromMinutes(5));

        Assert.Equal(1024 * 1024, cache.SizeLimit);
        Assert.InRange(cache.CurrentSize!.Value, tileBytes, cache.SizeLimit);
        int kept = Enumerable.Range(0, 10).Count(i => cache.TryGetValue($"tile{i}", out byte[] _));
        Assert.InRange(kept, 1, 3); // older entries are evicted to make room
    }

    [Fact]
    public void AnEntryLargerThanTheLimitIsNotCached()
    {
        using var cache = new TileMemoryCache(Options.Create(new DEMNetOptions { TileMemoryCacheSizeMB = 1 }));

        cache.Set("huge", new byte[2 * 1024 * 1024], 2 * 1024 * 1024, TimeSpan.FromMinutes(5));

        Assert.False(cache.TryGetValue("huge", out byte[] _));
    }

    [Fact]
    public void TryGetValueChecksTheType()
    {
        using var cache = new TileMemoryCache(Options.Create(new DEMNetOptions()));
        cache.SetAbsolute("flag", true, TileMemoryCache.SmallEntrySize, TimeSpan.FromMinutes(5));

        Assert.True(cache.TryGetValue("flag", out bool flag) && flag);
        Assert.False(cache.TryGetValue("flag", out byte[] _));
    }
}
