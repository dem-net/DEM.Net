using DEM.Net.Core.Configuration;
using DEM.Net.Core.Imagery;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Path = System.IO.Path;

namespace DEM.Net.Core.Services.Imagery
{
    /// <summary>
    /// Two level cache : memory / disk
    /// </summary>
    public class ImageryCache
    {
        public const string IMAGERY_DIR = "imagery";
        private readonly IMemoryCache cache;
        private readonly DEMNetOptions options;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly string _localDirectory;
        private readonly TimeSpan diskExpirationHours;
        private readonly TimeSpan memoryExpirationMinutes;

        public ImageryCache(IMemoryCache cache,
            IOptions<DEMNetOptions> options,
            IHttpClientFactory clientFactory)
        {
            this.cache = cache;

            this.options = options.Value;
            this.diskExpirationHours = TimeSpan.FromHours(options.Value.ImageryDiskCacheExpirationHours);
            this.memoryExpirationMinutes = TimeSpan.FromMinutes(options.Value.ImageryCacheExpirationMinutes);

            this.httpClientFactory = clientFactory;
            this._localDirectory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), RasterService.APP_NAME, IMAGERY_DIR);
            CreateDirectoryIfNotExists(_localDirectory);
        }

        public async Task<byte[]> GetTileAsync(Uri tileUri, ImageryProvider provider, MapTileInfo tileInfo, CancellationToken cancellationToken = default)
        {
            if (cache.TryGetValue(tileUri, out byte[] contentBytes))
                return contentBytes;

            string tileFile = options.UseImageryDiskCache ? GetTileDiskPath(provider, tileInfo) : null;
            if (tileFile != null && File.Exists(tileFile) && (DateTime.Now - File.GetLastWriteTime(tileFile)) <= diskExpirationHours)
            {
                contentBytes = await File.ReadAllBytesAsync(tileFile, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                HttpClient httpClient = httpClientFactory.CreateClient();
                contentBytes = await httpClient.GetByteArrayAsync(tileUri, cancellationToken).ConfigureAwait(false);
                if (tileFile != null && contentBytes.Length > 0)
                {
                    // a concurrent reader never sees a partial tile
                    string tempFile = HttpDownload.TempFileName(tileFile);
                    await File.WriteAllBytesAsync(tempFile, contentBytes, cancellationToken).ConfigureAwait(false);
                    File.Move(tempFile, tileFile, overwrite: true);
                }
            }

            cache.Set(tileUri, contentBytes, new MemoryCacheEntryOptions { SlidingExpiration = memoryExpirationMinutes });
            return contentBytes;
        }

        private string GetTileDiskPath(ImageryProvider provider, MapTileInfo tileInfo)
        {
            var providerDir = Path.Combine(_localDirectory, provider.Name);
            CreateDirectoryIfNotExists(providerDir);

            var zoomDir = Path.Combine(providerDir, tileInfo.Zoom.ToString());
            CreateDirectoryIfNotExists(zoomDir);

            var xDir = Path.Combine(zoomDir, tileInfo.X.ToString());
            CreateDirectoryIfNotExists(xDir);

            return Path.Combine(xDir, string.Concat(tileInfo.Y, ".png"));
        }

        public string GetReport()
        {
            int numFiles = 0;
            double totalSizeMB = 0;
            foreach (var entry in Directory.EnumerateFileSystemEntries(_localDirectory, "*.*", SearchOption.AllDirectories))
            {
                if (Directory.Exists(entry))
                    continue;
                numFiles += 1;
                totalSizeMB += new FileInfo(entry).Length / 1024d / 1024d;
            }

            return $"{numFiles:N0} files, total size {totalSizeMB:N1} MB";
        }

        private void CreateDirectoryIfNotExists(string directory)
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }
    }
}
