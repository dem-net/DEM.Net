using DEM.Net.Core.Configuration;
using DEM.Net.Core.Imagery;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SixLabors.Shapes;
using System;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
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

        public byte[] GetTile(Uri tileUri, ImageryProvider provider, MapTileInfo tileInfo)
        {
            var contentBytes = cache.GetOrCreate(tileUri, entry =>
            {
                entry.SetSlidingExpiration(memoryExpirationMinutes);

                // Check if present on disk
                if (options.UseImageryDiskCache)
                {
                    var tileFile = GetTileDiskPath(provider, tileInfo);
                    if (File.Exists(tileFile) && (DateTime.Now - File.GetLastWriteTime(tileFile)) <= diskExpirationHours)
                    {
                        return File.ReadAllBytes(tileFile);
                    }


                    HttpClient httpClient = httpClientFactory.CreateClient();
                    var bytes = httpClient.GetByteArrayAsync(tileUri).GetAwaiter().GetResult();
                    File.WriteAllBytes(tileFile, bytes);

                    return bytes;
                }
                else
                {
                    HttpClient httpClient = httpClientFactory.CreateClient();
                    return httpClient.GetByteArrayAsync(tileUri).GetAwaiter().GetResult();
                }


            });

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
