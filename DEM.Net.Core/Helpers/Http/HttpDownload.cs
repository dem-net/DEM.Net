using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DEM.Net.Core;

internal static class HttpDownload
{
    /// <summary>Sent to tile servers, some of which (swisstopo, OSM-based ones) ask clients to identify themselves.</summary>
    public const string UserAgent = "DEM.Net (+https://github.com/dem-net/DEM.Net)";

    /// <summary>
    /// Streams a remote file to disk. The content goes to a temporary file first, moved over
    /// <paramref name="localFileName"/> once complete: an interrupted download never looks like a valid file.
    /// </summary>
    /// <returns>False when the server answered 404 and <paramref name="notFoundIsMissing"/> is true (nothing written).</returns>
    public static async Task<bool> DownloadToFileAsync(HttpClient client, string url, string localFileName,
        CancellationToken cancellationToken, Action<HttpRequestMessage>? configureRequest = null, bool notFoundIsMissing = false)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(localFileName))!);
        string tempFile = TempFileName(localFileName);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            configureRequest?.Invoke(request);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (notFoundIsMissing && response.StatusCode == HttpStatusCode.NotFound)
                return false;
            response.EnsureSuccessStatusCode();

            await using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
            await using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true))
            {
                await stream.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
            }

            File.Move(tempFile, localFileName, overwrite: true);
            return true;
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    /// <summary>Downloads a small resource in memory. Returns null when the server answered 404.</summary>
    public static async Task<byte[]?> TryGetBytesAsync(HttpClient client, string url, CancellationToken cancellationToken,
        Action<HttpRequestMessage>? configureRequest = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        configureRequest?.Invoke(request);
        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// A temporary file next to <paramref name="fileName"/>, unique so that concurrent writers of the same file never
    /// share it. Ends with .tmp: never matches the raster or manifest search patterns.
    /// </summary>
    public static string TempFileName(string fileName) => $"{fileName}.{Guid.NewGuid():N}.tmp";

    /// <summary>Writes a file through a temporary file, so that readers never see a partial file.</summary>
    public static async Task WriteAllTextAtomicAsync(string fileName, string contents, CancellationToken cancellationToken)
    {
        string tempFile = TempFileName(fileName);
        await File.WriteAllTextAsync(tempFile, contents, cancellationToken).ConfigureAwait(false);
        File.Move(tempFile, fileName, overwrite: true);
    }
}
