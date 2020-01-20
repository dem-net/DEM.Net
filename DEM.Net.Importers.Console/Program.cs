using DEM.Net.Importers.netCDF;
using Microsoft.Extensions.Logging;
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
            
            Sample sample = new Sample();
            sample.Run(null);

            System.Console.ReadLine();
        }
    }
}
