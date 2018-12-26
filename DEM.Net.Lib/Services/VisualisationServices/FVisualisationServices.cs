using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Services.VisualisationServices
{
   public class FVisualisationServices
    {
        public static IStatsPopulationServices createSeuillageServices()
        {
            return new StatsPopulationServices();
        }
        public static IVisualisationSpatialTraceServices createVisualisationSpatialTraceServices()
        {
            return new VisualisationSpatialTraceServices();
        }
    }
}
