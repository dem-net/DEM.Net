using DEM.Net.Core.Imagery;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace DEM.Net.Core
{
    public static class IServiceCollectionExtension
    {
        public static IServiceCollection AddDemNetCore(this IServiceCollection services)
        {
            services
                    .AddSingleton<IGDALVRTFileService,GDALVRTFileService>()
                    .AddSingleton<IRasterService, RasterService>()
                    .AddTransient<IElevationService, ElevationService>()
                    .AddTransient<IMeshService, MeshService>()
                    .AddTransient<IImageryService, ImageryService>();

            return services;
        }
    }
}
