using DEM.Net.Extension.Osm.Buildings;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace DEM.Net.Extension.Osm
{
    
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddDemNetOsmExtension(this IServiceCollection services)
        {
            
            services.AddTransient<BuildingService>();
            services.AddTransient<WayService>();

            return services;
        }
    }
}
