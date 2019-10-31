using DEM.Net.Core.Datasets;
using DEM.Net.Core.EarthData;
using DEM.Net.Core.Imagery;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace DEM.Net.Core
{
    public delegate IDEMDataSetIndex RasterIndexServiceResolver(DEMDataSourceType dataSourceType);

    public static class IServiceCollectionExtension
    {
        public static IServiceCollection AddDemNetCore(this IServiceCollection services)
        {
            services.AddSingleton<GDALVRTFileService>();
            services.AddTransient<NasaGranuleFileService>();
            services.AddTransient<EarthdataLoginConnector>();

            services.AddTransient<RasterIndexServiceResolver>(serviceProvider => dataSourceType =>
            {
                switch (dataSourceType)
                {
                    case Datasets.DEMDataSourceType.GDALVrt:
                    case Datasets.DEMDataSourceType.LocalFile:
                        return serviceProvider.GetService<GDALVRTFileService>();
                    case Datasets.DEMDataSourceType.NASA:
                        return serviceProvider.GetService<NasaGranuleFileService>();
                    default:
                        throw new KeyNotFoundException(); // or maybe return null, up to you
                }
            });

            services
                    .AddSingleton<IDEMDataSetIndex,GDALVRTFileService>()
                    .AddSingleton<IRasterService, RasterService>()
                    .AddTransient<IElevationService, ElevationService>()
                    .AddTransient<IMeshService, MeshService>()
                    .AddSingleton<IImageryService, ImageryService>();

            return services;
        }
    }
}
