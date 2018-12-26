using DEM.Net.Lib.Services.VisualisationServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.TestWinForm
{
    public class FServicesApplicatifs
    {
        public static IServicesApplicatifs createEchantillonsTestsServices()
        {
            return new EchantillonsTestsServices();
        }
        public static IStatsPopulationServices createStatsPopServices()
        {
            return new StatsPopulationServices();
        }
        public static IVisualisationSpatialTraceServices createVisuSpatialTrace()
        {
            return new VisualisationSpatialTraceServices();
        }
    }
}
