using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DEM.Net.Core;
using DEM.Net.Core.Configuration;
using DEM.Net.Core.Imagery;
using DEM.Net.Core.Services.Imagery;
using Microsoft.Extensions.Options;
using Xunit;

namespace DEM.Net.xUnit.Test;

public class ImageryFallbackTests
{
    private static readonly ImageryProvider Fallback = new()
    {
        Name = "Fallback-Test",
        UrlModel = new UrlModel("https://fallback.example.com/{z}/{x}/{y}.jpg?key={t}", null!),
        TokenUserSecretsKey = nameof(AppSecrets.MapTilerKey),
        TileSize = 512
    };

    private static ImageryProvider Regional(ImageryProvider? fallback) => new()
    {
        Name = "Regional-Test",
        UrlModel = new UrlModel("https://regional.example.com/{z}/{x}/{y}.jpg", null!),
        FallbackProvider = fallback
    };

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]   // IGN.fr
    [InlineData(HttpStatusCode.BadRequest)] // SwissImage: "Tile out of bounds"
    public async Task AMissingTileIsTakenFromTheFallback(HttpStatusCode missing)
    {
        // tile 10/2 is outside the regional coverage
        var handler = new StubHandler(uri => uri.Host == "regional.example.com" && uri.AbsolutePath == "/5/10/2.jpg"
            ? missing : HttpStatusCode.OK);
        var tiles = new TileRange(new MapTileInfo(10, 1, 5, 256), new MapTileInfo(10, 2, 5, 256), 256);

        tiles = await Service(handler).DownloadTilesAsync(tiles, Regional(Fallback));

        Assert.Equal(2, tiles.Tiles.Count);
        Assert.Equal("fallback.example.com", tiles.Tiles.Single(t => t.TileInfo.Y == 2).Uri!.Host);
        Assert.Equal("regional.example.com", tiles.Tiles.Single(t => t.TileInfo.Y == 1).Uri!.Host);
        Assert.Same(Fallback, tiles.FallbackProviderUsed);
        Assert.Contains("https://fallback.example.com/5/10/2.jpg?key=secret", handler.Requests.Keys);
    }

    [Fact]
    public async Task NoFallbackIsReportedWhenEveryTileIsFound()
    {
        var handler = new StubHandler(_ => HttpStatusCode.OK);
        var tiles = new TileRange(new MapTileInfo(10, 1, 5, 256), new MapTileInfo(11, 2, 5, 256), 256);

        tiles = await Service(handler).DownloadTilesAsync(tiles, Regional(Fallback));

        Assert.Equal(4, tiles.Tiles.Count);
        Assert.Null(tiles.FallbackProviderUsed);
        Assert.DoesNotContain(handler.Requests.Keys, u => u.Contains("fallback"));
    }

    [Fact]
    public async Task WithoutAFallbackAMissingTileStillFails()
    {
        var handler = new StubHandler(_ => HttpStatusCode.NotFound);
        var tiles = new TileRange(new MapTileInfo(10, 1, 5, 256), new MapTileInfo(10, 1, 5, 256), 256);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => Service(handler).DownloadTilesAsync(tiles, Regional(null)));
        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
    }

    [Fact]
    public void IgnFranceAndSwissImageFallBackToMapTilerSatellite()
    {
        Assert.Same(ImageryProvider.MapTilerSatellite, ImageryProvider.OrtoIGNfr.FallbackProvider);
        Assert.Same(ImageryProvider.MapTilerSatellite, ImageryProvider.SwissImage.FallbackProvider);
    }

    [Fact]
    public void RegisteredProvidersAreThePredefinedOnesOnly()
    {
        // FallbackProvider is an instance field of type ImageryProvider: it must not be taken for a predefined provider
        var providers = Service(new StubHandler(_ => HttpStatusCode.OK)).GetRegisteredProviders();

        Assert.Contains(ImageryProvider.OrtoIGNfr, providers);
        Assert.Contains(ImageryProvider.MapTilerSatellite, providers);
        Assert.Equal(providers.Count, providers.Distinct().Count());
        Assert.DoesNotContain(null, providers);
    }

    private static ImageryService Service(StubHandler handler)
    {
        var options = Options.Create(new DEMNetOptions { UseImageryDiskCache = false });
        var cache = new ImageryCache(new TileMemoryCache(options), options, new StubClientFactory(handler), new TileDownloadThrottle());
        return new ImageryService(null!, Options.Create(new AppSecrets { MapTilerKey = "secret" }), options, cache);
    }

    private sealed class StubClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class StubHandler(Func<Uri, HttpStatusCode> status) : HttpMessageHandler
    {
        public ConcurrentDictionary<string, int> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.AddOrUpdate(request.RequestUri!.ToString(), 1, (_, n) => n + 1);
            return Task.FromResult(new HttpResponseMessage(status(request.RequestUri!)) { Content = new ByteArrayContent([1, 2, 3]) });
        }
    }
}
