using DEM.Net.Core.Imagery;
using DEM.Net.Core.Services.Mesh;
using DEM.Net.glTF.Export;
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
                    .AddTransient<IMeshService, MeshService>()
                    .AddTransient<IglTFService, glTFService>()
                    .AddTransient<ISTLExportService, STLExportService>();

            return services;
        }
    }
}
