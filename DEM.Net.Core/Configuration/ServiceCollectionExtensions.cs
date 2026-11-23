using System;
using System.Collections.Generic;
using System.Text;
using DEM.Net.Core.Datasets;
using DEM.Net.Core.EarthData;
using DEM.Net.Core.Imagery;
using DEM.Net.Core.RemoteFile;
using DEM.Net.Core.Services.Imagery;
using DEM.Net.Core.Stac;
using DEM.Net.Core.TileIndex;
using DEM.Net.Core.XyzTiles;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace DEM.Net.Core;

public delegate IDEMDataSetIndex RasterIndexServiceResolver(DEMDataSourceType dataSourceType);

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddDemNetCore(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddHttpClient();
        services.AddOptions();

        services.AddSingleton<TileMemoryCache>();
        services.AddSingleton<GDALVRTFileService>();
        services.AddSingleton<LocalFileSystemIndex>();
        services.AddSingleton<NasaGranuleFileService>();
        services.AddSingleton<EarthdataLoginConnector>();
        services.AddSingleton<StacFileService>();
        services.AddSingleton<TileIndexFileService>();
        services.AddSingleton<XyzTileIndexService>();
        services.AddSingleton<RemoteFileService>();
        services.AddSingleton<TileDownloadThrottle>();

        services.AddTransient<RasterIndexServiceResolver>(serviceProvider => dataSourceType =>
        {
            return dataSourceType switch
            {
                Datasets.DEMDataSourceType.GDALVrt => serviceProvider.GetService<GDALVRTFileService>()!,
                Datasets.DEMDataSourceType.NasaEarthData => serviceProvider.GetService<NasaGranuleFileService>()!,
                Datasets.DEMDataSourceType.Stac => serviceProvider.GetService<StacFileService>()!,
                Datasets.DEMDataSourceType.TileIndex => serviceProvider.GetService<TileIndexFileService>()!,
                Datasets.DEMDataSourceType.XyzTiles => serviceProvider.GetService<XyzTileIndexService>()!,
                Datasets.DEMDataSourceType.LocalFileSystem => serviceProvider.GetService<LocalFileSystemIndex>()!,
                Datasets.DEMDataSourceType.RemoteFile => serviceProvider.GetService<RemoteFileService>()!,
                _ => throw new KeyNotFoundException(),// or maybe return null, up to you
            };
        });

        services
                .AddSingleton<IDEMDataSetIndex,GDALVRTFileService>()
                .AddSingleton<RasterService>()
                .AddTransient<ElevationService>()
                .AddTransient<MeshService>()
                .AddTransient<AdornmentsService>()
                .AddTransient<ImageryCache>()
                .AddSingleton<ImageryService>();

        services.AddLazyCache();
        return services;
    }
}
