using DEM.Net.Core.Datasets;
using DEM.Net.Core.EarthData;
using DEM.Net.Core.Imagery;
using DEM.Net.Core.Services.Imagery;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace DEM.Net.Core
{
    public delegate IDEMDataSetIndex RasterIndexServiceResolver(DEMDataSourceType dataSourceType);

    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddDemNetCore(this IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddHttpClient();
            services.AddOptions();

            services.AddSingleton<GDALVRTFileService>();
            services.AddSingleton<LocalFileSystemIndex>();
            services.AddSingleton<NasaGranuleFileService>();
            services.AddSingleton<EarthdataLoginConnector>();

            services.AddTransient<RasterIndexServiceResolver>(serviceProvider => dataSourceType =>
            {
                switch (dataSourceType)
                {
                    case Datasets.DEMDataSourceType.GDALVrt:
                        return serviceProvider.GetService<GDALVRTFileService>();
                    case Datasets.DEMDataSourceType.NasaEarthData:
                        return serviceProvider.GetService<NasaGranuleFileService>();
                    case Datasets.DEMDataSourceType.LocalFileSystem:
                        return serviceProvider.GetService<LocalFileSystemIndex>();
                    default:
                        throw new KeyNotFoundException(); // or maybe return null, up to you
                }
            });

            services
                    .AddSingleton<IDEMDataSetIndex,GDALVRTFileService>()
                    .AddSingleton<RasterService>()
                    .AddTransient<ElevationService>()
                    .AddTransient<MeshService>()
                    .AddTransient<AdornmentsService>()
                    .AddTransient<ImageryCache>()
                    .AddSingleton<ImageryService>();


            return services;
        }
    }
}
