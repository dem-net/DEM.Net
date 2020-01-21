using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Research.Science.Data;
using Microsoft.Extensions.Logging;
using DEM.Net.Core;

namespace DEM.Net.Importers.netCDF
{
    public class Sample
    {
        private readonly ILogger _logger;

        public Sample(ILogger<Sample> logger)
        {
            _logger = logger;
        }
        public void Run()
        {
            try
            {
                string dataFile = Path.Combine(Directory.GetCurrentDirectory(), "Data", "gebco_2019_chile.nc");


                using (IRasterFile netCdfRaster = new NetCdfFile(dataFile))
                {

                    _logger.LogDebug(((NetCdfFile)netCdfRaster).GetMetadataReport());

                   var metadata =  netCdfRaster.ParseMetaData(new DEMFileDefinition(DEMFileType.CF_NetCDF, DEMFileRegistrationMode.Cell));

                    var hMap = netCdfRaster.GetHeightMap(metadata);
                    var bbox = hMap.BoundingBox;
                    //ds.Metadata[vname] = data;
                }
                
            }
            catch(FileNotFoundException fnfEx)
            {
                _logger.LogError(fnfEx,fnfEx.Message);
                throw;
            }
            catch (Exception ex)
            {
                ex = ex.GetBaseException();
                _logger.LogError(ex, ex.Message);
                throw;
            }

        }

        public string GetMetadataReport(DataSet dataset)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"=================== Dataset {dataset.Name}");
            sb.AppendLine("Metadata:");
            foreach (var data in dataset.Metadata)
            {
                sb.AppendLine($"{data.Key}:");
                sb.AppendLine($"{data.Value}");
            }
            sb.AppendLine($"=================== Variables ");
            foreach (var v in dataset.Variables)
            {
                sb.AppendLine($"{v.Name}:");
                sb.AppendLine(v.ToString());
            }

            return sb.ToString();
        }
    }
}

