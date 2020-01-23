using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Logging;
using DEM.Net.Core;
using DEM.Net.Core.netCDF;

namespace DEM.Net.Importers.Console
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
                string dataFile = @"C:\Users\xfischer\Downloads\ETOPO1_Ice_g_gmt4.nc"; // Path.Combine(Directory.GetCurrentDirectory(), "Data", "gebco_2019_ajaccio.nc");


                using (IRasterFile netCdfRaster = new NetCdfFile(dataFile, netCDFField.Create<double>("y"), netCDFField.Create<double>("x"), netCDFField.Create<int>("z")))
                {

                    _logger.LogDebug(((NetCdfFile)netCdfRaster).GetMetadataReport());

                    var metadata = netCdfRaster.ParseMetaData(new DEMFileDefinition(DEMFileType.CF_NetCDF, DEMFileRegistrationMode.Cell));

                    var hMap = netCdfRaster.GetHeightMap(metadata);
                    var bbox = hMap.BoundingBox;
                    var bboxS = bbox.Scale(0.3);

                    hMap = netCdfRaster.GetHeightMapInBBox(bboxS, metadata);
                    //ds.Metadata[vname] = data;

                    netCdfRaster.GetElevationAtPoint(metadata, 10, 10);
                }

            }
            catch (FileNotFoundException fnfEx)
            {
                _logger.LogError(fnfEx, fnfEx.Message);
                throw;
            }
            catch (Exception ex)
            {
                ex = ex.GetBaseException();
                _logger.LogError(ex, ex.Message);
                throw;
            }

        }

    }
}

