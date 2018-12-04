using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Services.Lab
{
    public class FServices
    {
        public static  ICalculServices_Low createCalculLow()
        {
            return new CalculServices_Low();
        }
        public static IUtilitairesServices createUtilitaires()
        {
            return new UtilitairesServices();
        }
        public static ICalculServices_Medium createCalculMedium()
        {
            return new CalculServices_Medium();
        }
    }
}
