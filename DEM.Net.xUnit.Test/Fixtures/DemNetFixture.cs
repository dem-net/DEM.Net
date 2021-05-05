using DEM.Net.Core;
using DEM.Net.Core.Configuration;
using DEM.Net.glTF;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;
using System.IO;

namespace DEM.Net.Test
{
    public class DemNetFixture
    {
        public DemNetFixture()
        {
            var builder = new ConfigurationBuilder()
           .SetBasePath(Directory.GetCurrentDirectory())
           .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
           .AddJsonFile("secrets.json", optional: true, reloadOnChange: false)
           .Build();

            var services = new ServiceCollection();
            services.AddLogging(config =>
            {
                config.AddDebug(); // Log to debug (debug window in Visual Studio or any debugger attached)
                config.AddConsole(); // Log to console (colored !)
            })
            .Configure<LoggerFilterOptions>(options =>
            {
                options.AddFilter<DebugLoggerProvider>(null /* category*/ , LogLevel.Information /* min level */);
                options.AddFilter<ConsoleLoggerProvider>(null  /* category*/ , LogLevel.Information /* min level */);
            })
            .AddDemNetCore()
            .AddDemNetglTF();

            services.Configure<AppSecrets>(builder.GetSection(nameof(AppSecrets)));
            services.Configure<DEMNetOptions>(builder.GetSection(nameof(DEMNetOptions)));

            ServiceProvider = services.BuildServiceProvider();

            // You can run additionnal startup steps here
            OnStart(ServiceProvider);
        }

        /// <summary>
        /// Custom function where you can run additionnal startup tasks
        /// </summary>
        /// <param name="services"></param>
        void OnStart(ServiceProvider services)
        {
            //foreach (var dst in DEMDataSet.RegisteredNonSingleFileDatasets)
            //    services.GetService<RasterService>().GenerateDirectoryMetadata(dst, true);

        }



        public ServiceProvider ServiceProvider { get; private set; }
    }
}
