using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Research.Science.Data;
using Microsoft.Research.Science.Data.NetCDF4;
using Microsoft.Extensions.Logging;

namespace DEM.Net.Importers.netCDF
{
    public class Sample
    {

        public void Run(ILogger logger)
        {
            try
            {
                string dataFile = Path.Combine(Directory.GetCurrentDirectory(), "Data", "gebco_2019_ajaccio.nc");

                using (var ds = DataSet.Open(dataFile, ResourceOpenMode.ReadOnly))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Metadata:");
                    foreach (var data in ds.Metadata)
                    {
                        sb.AppendLine($"{data.Key}: {data.Value}");
                    }
                    sb.AppendLine("Variables:");
                    foreach (var v in ds.Variables)
                    {
                        sb.AppendLine(v.ToString());
                    }


                    
                    //logger.LogDebug(sb.ToString());
                    Console.WriteLine(sb.ToString());
                    //ds.Metadata[vname] = data;
                }
                
            }
            catch (Exception)
            {

                throw;
            }

        }
    }
}

