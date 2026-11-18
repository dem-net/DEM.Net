using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DEM.Net.Core.Imagery;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DEM.Net.Core.Services.Imagery;

/// <summary>
/// Limits imagery tile downloads per provider host for the whole process (see <see cref="ImageryProvider.MaxConcurrentDownloads"/>),
/// and retries when the server asks to slow down (HTTP 429 or 503).
/// </summary>
/// <remarks>
/// <see cref="ImageryProvider.MaxDegreeOfParallelism"/> only limits one request: without this, N concurrent
/// generations would open N times as many connections to the same provider.
/// </remarks>
public class TileDownloadThrottle(ILogger<TileDownloadThrottle>? logger = null)
{
    private readonly ILogger<TileDownloadThrottle> _logger = logger ?? NullLogger<TileDownloadThrottle>.Instance;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _hostLimits = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Retries after a 429 or 503, before giving up.</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>First wait when the server sends no Retry-After header. Doubled at each retry.</summary>
    public TimeSpan BaseRetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Longest single wait, whatever the server asks for.</summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Downloads a tile, waiting for a permit of the provider's host first. The permit is kept while waiting
    /// before a retry, so that every request slows down on that host.
    /// </summary>
    public async Task<byte[]> FetchAsync(HttpClient httpClient, Uri tileUri, ImageryProvider provider, CancellationToken cancellationToken = default)
    {
        SemaphoreSlim limit = GetHostLimit(provider);
        await limit.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            for (int attempt = 0; ; attempt++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, tileUri);
                request.Headers.UserAgent.ParseAdd(HttpDownload.UserAgent);
                using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

                if (attempt < MaxRetries && IsSlowDown(response.StatusCode))
                {
                    TimeSpan delay = GetRetryDelay(response, attempt);
                    _logger.LogWarning("{Host} answered {Status} ({Provider}), retrying in {Delay:N1} s ({Attempt}/{MaxRetries})",
                        tileUri.Host, (int)response.StatusCode, provider.Name, delay.TotalSeconds, attempt + 1, MaxRetries);
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            limit.Release();
        }
    }

    private SemaphoreSlim GetHostLimit(ImageryProvider provider)
    {
        int max = Math.Max(1, provider.MaxConcurrentDownloads);
        return _hostLimits.GetOrAdd(GetHostKey(provider), _ => new SemaphoreSlim(max, max));
    }

    /// <summary>
    /// The host of the provider's URL template, without the server placeholder: providers on the same host
    /// (MapBox styles, OpenTopoMap's a/b/c servers) share one limit.
    /// </summary>
    internal static string GetHostKey(ImageryProvider provider)
    {
        string url = provider.UrlModel?.UrlFormat ?? provider.Name ?? string.Empty;
        url = url.Replace("{s}", string.Empty);
        int start = url.IndexOf("://", StringComparison.Ordinal);
        start = start < 0 ? 0 : start + 3;
        int end = url.IndexOfAny(['/', '?'], start);
        string host = end < 0 ? url[start..] : url[start..end];
        return host.Length == 0 ? provider.Name ?? string.Empty : host.Trim('.');
    }

    private static bool IsSlowDown(HttpStatusCode status)
        => status == HttpStatusCode.TooManyRequests || status == HttpStatusCode.ServiceUnavailable;

    private TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
    {
        TimeSpan delay = BaseRetryDelay * Math.Pow(2, attempt);
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter?.Delta is TimeSpan delta)
            delay = delta;
        else if (retryAfter?.Date is DateTimeOffset date)
            delay = date - DateTimeOffset.UtcNow;

        if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;
        return delay > MaxRetryDelay ? MaxRetryDelay : delay;
    }
}
