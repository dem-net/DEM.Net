using DEM.Net.Lib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleApp
{
    class DatasetSamples
    {
        
        public static void Run()
        {

            RasterService rasterService = new RasterService();
            string sampleName = "DatasetSamples";
           
            Logger.Info("============================");
            Logger.Info($"= {sampleName}");
            Logger.Info("============================");
            Logger.Info($"= {sampleName} : Datadirectory report");


            Logger.Info($"Local data directory : {rasterService.LocalDirectory}");

            Logger.RestartPerf(sampleName);

            Logger.Info(rasterService.GenerateReportAsString());

            Logger.StopPerf(sampleName);

            Logger.Info("=");
            Logger.Info("============================");
            
        }


    }
}
