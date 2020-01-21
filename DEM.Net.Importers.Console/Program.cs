using DEM.Net.Importers.netCDF;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Importers.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            // Needs the following NuGet packages
            // - Microsoft.Extensions.DependencyInjection
            // - Microsoft.Extensions.Logging.Console
            var services = new ServiceCollection()
                        .AddLogging(config =>
                        {
                            config.AddConsole().SetMinimumLevel(LogLevel.Debug); // Log to console (colored !)
                        })
                        .AddSingleton<Sample>(); // <-- Your app gets registered here
                                                       // Everything added to the constructor of your App from DEM.Net will be injected at runtime.
                                                       // You can for example add an ILogger<ElevationApp> for your logging

            var provider = services.BuildServiceProvider();
            var app = provider.GetService<Sample>();

            app.Run();

            System.Console.ReadLine();
        }

        
    }
}
