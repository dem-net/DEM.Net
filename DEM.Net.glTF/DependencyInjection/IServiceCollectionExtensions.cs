using DEM.Net.Core;
using DEM.Net.Core.Imagery;
using DEM.Net.glTF.Export;
using DEM.Net.glTF.SharpglTF;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace DEM.Net.glTF
{
    public static class IServiceCollectionExtension
    {
        public static IServiceCollection AddDemNetglTF(this IServiceCollection services)
        {
            services
                    .AddTransient<SharpGltfService>()
                    .AddTransient<ISTLExportService, STLExportService>();

            return services;
        }
    }
}
