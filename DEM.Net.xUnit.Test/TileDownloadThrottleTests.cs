using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using DEM.Net.Core.Imagery;
using DEM.Net.Core.Services.Imagery;
using Xunit;

namespace DEM.Net.xUnit.Test;

public class TileDownloadThrottleTests
{
    private static ImageryProvider Provider(string url, int maxConcurrent, string[]? servers = null) => new()
    {
        Name = url,
        UrlModel = new UrlModel(url, servers!),
        MaxConcurrentDownloads = maxConcurrent
    };

    private static TileDownloadThrottle FastThrottle() => new()
    {
        BaseRetryDelay = TimeSpan.FromMilliseconds(10),
        MaxRetryDelay = TimeSpan.FromSeconds(2)
    };

    [Fact]
    public async Task ConcurrentRequestsOnOneHostNeverExceedTheCap()
    {
        var handler = new CountingHandler(delay: TimeSpan.FromMilliseconds(30));
        var client = new HttpClient(handler);
        var throttle = FastThrottle();
        var provider = Provider("https://tiles.example.com/{z}/{x}/{y}.png", maxConcurrent: 3);

        // two "requests" of 10 tiles each, run together
        var downloads = Enumerable.Range(0, 2).Select(r => Parallel.ForEachAsync(Enumerable.Range(0, 10),
            new ParallelOptions { MaxDegreeOfParallelism = 8 },
            async (i, ct) => await throttle.FetchAsync(client, new Uri($"https://tiles.example.com/{r}/{i}.png"), provider, ct)));
        await Task.WhenAll(downloads);

        Assert.Equal(20, handler.Requests);
        Assert.Equal(3, handler.MaxInFlight);
    }

    [Fact]
    public async Task ProvidersOnTheSameHostShareOneCap()
    {
        var handler = new CountingHandler(delay: TimeSpan.FromMilliseconds(30));
        var client = new HttpClient(handler);
        var throttle = FastThrottle();
        var streets = Provider("https://api.mapbox.com/styles/v1/mapbox/streets-v11/tiles/{z}/{x}/{y}?access_token={t}", 2);
        var dark = Provider("https://api.mapbox.com/styles/v1/mapbox/dark-v10/tiles/{z}/{x}/{y}?access_token={t}", 2);

        var tasks = Enumerable.Range(0, 12).Select(i =>
            throttle.FetchAsync(client, new Uri($"https://api.mapbox.com/{i}"), i % 2 == 0 ? streets : dark));
        await Task.WhenAll(tasks);

        Assert.Equal(2, handler.MaxInFlight);
    }

    [Theory]
    [InlineData("https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png", "tile.opentopomap.org")]
    [InlineData("https://api.mapbox.com/v4/mapbox.satellite/{z}/{x}/{y}@2x.jpg?access_token={t}", "api.mapbox.com")]
    [InlineData("https://data.geopf.fr/wmts?SERVICE=WMTS&TILEMATRIX={z}", "data.geopf.fr")]
    [InlineData("http://www.ign.es/wmts/pnoa-ma?SERVICE=WMTS", "www.ign.es")]
    public void HostKeyIgnoresTheServerPlaceholder(string urlFormat, string expected)
    {
        Assert.Equal(expected, TileDownloadThrottle.GetHostKey(Provider(urlFormat, 1, ["a", "b", "c"])));
    }

    [Fact]
    public async Task TooManyRequestsIsRetriedWithRetryAfter()
    {
        var handler = new ScriptedHandler(
            () => SlowDown(HttpStatusCode.TooManyRequests, TimeSpan.FromMilliseconds(1)),
            () => SlowDown(HttpStatusCode.ServiceUnavailable, null),
            () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2, 3]) });
        var throttle = FastThrottle();

        byte[] bytes = await throttle.FetchAsync(new HttpClient(handler), new Uri("https://t.example.com/1"), Provider("https://t.example.com/{z}", 1));

        Assert.Equal([1, 2, 3], bytes);
        Assert.Equal(3, handler.Calls);
    }

    [Fact]
    public async Task GivesUpAfterMaxRetries()
    {
        var handler = new ScriptedHandler(() => SlowDown(HttpStatusCode.TooManyRequests, null));
        var throttle = FastThrottle();

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
            throttle.FetchAsync(new HttpClient(handler), new Uri("https://t.example.com/1"), Provider("https://t.example.com/{z}", 1)));

        Assert.Equal(HttpStatusCode.TooManyRequests, ex.StatusCode);
        Assert.Equal(throttle.MaxRetries + 1, handler.Calls);
    }

    [Fact]
    public async Task OtherErrorsAreNotRetried()
    {
        var handler = new ScriptedHandler(() => new HttpResponseMessage(HttpStatusCode.NotFound));

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            FastThrottle().FetchAsync(new HttpClient(handler), new Uri("https://t.example.com/1"), Provider("https://t.example.com/{z}", 1)));

        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task CancellationStopsARetryWait()
    {
        var handler = new ScriptedHandler(() => SlowDown(HttpStatusCode.TooManyRequests, TimeSpan.FromSeconds(30)));
        var throttle = new TileDownloadThrottle { MaxRetryDelay = TimeSpan.FromSeconds(30) };
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var provider = Provider("https://t.example.com/{z}", 1);

        var started = DateTime.UtcNow;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            throttle.FetchAsync(new HttpClient(handler), new Uri("https://t.example.com/1"), provider, cts.Token));
        Assert.True(DateTime.UtcNow - started < TimeSpan.FromSeconds(5));

        // the permit was released: the next download goes through
        handler.Respond = () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([7]) };
        Assert.Equal([7], await throttle.FetchAsync(new HttpClient(handler), new Uri("https://t.example.com/2"), provider));
    }

    [Fact]
    public async Task SendsAUserAgent()
    {
        ProductInfoHeaderValue[]? userAgent = null;
        var handler = new ScriptedHandler(() => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([1]) })
        {
            OnRequest = r => userAgent = r.Headers.UserAgent.ToArray()
        };

        await FastThrottle().FetchAsync(new HttpClient(handler), new Uri("https://t.example.com/1"), Provider("https://t.example.com/{z}", 1));

        Assert.NotNull(userAgent);
        Assert.Contains(userAgent!, p => p.Product?.Name == "DEM.Net");
    }

    [Fact]
    public void PredefinedProvidersHaveLimitsAndNoEsri()
    {
        Assert.Equal(2, ImageryProvider.OpenTopoMap.MaxConcurrentDownloads);
        Assert.Equal(2, ImageryProvider.OpenTopoMap.MaxDegreeOfParallelism);
        Assert.Equal(8, ImageryProvider.SwissImage.MaxConcurrentDownloads);
        Assert.Equal(32, ImageryProvider.MapBoxSatelliteStreet.MaxConcurrentDownloads);
        Assert.DoesNotContain(typeof(ImageryProvider).GetFields(), f => f.Name.Contains("Esri"));
    }

    private static HttpResponseMessage SlowDown(HttpStatusCode status, TimeSpan? retryAfter)
    {
        var response = new HttpResponseMessage(status);
        if (retryAfter is TimeSpan delta)
            response.Headers.RetryAfter = new RetryConditionHeaderValue(delta);
        return response;
    }

    private sealed class CountingHandler(TimeSpan delay) : HttpMessageHandler
    {
        private int _inFlight;
        private int _maxInFlight;
        private int _requests;
        public int MaxInFlight => _maxInFlight;
        public int Requests => _requests;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _requests);
            int now = Interlocked.Increment(ref _inFlight);
            int seen;
            while (now > (seen = _maxInFlight) && Interlocked.CompareExchange(ref _maxInFlight, now, seen) != seen) { }
            try
            {
                await Task.Delay(delay, cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([0]) };
            }
            finally
            {
                Interlocked.Decrement(ref _inFlight);
            }
        }
    }

    private sealed class ScriptedHandler(params Func<HttpResponseMessage>[] script) : HttpMessageHandler
    {
        private readonly Queue<Func<HttpResponseMessage>> _script = new(script);
        public int Calls { get; private set; }
        public Func<HttpResponseMessage>? Respond { get; set; }
        public Action<HttpRequestMessage>? OnRequest { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            OnRequest?.Invoke(request);
            // the last scripted response repeats
            var next = Respond ?? (_script.Count > 1 ? _script.Dequeue() : _script.Peek());
            return Task.FromResult(next());
        }
    }
}
