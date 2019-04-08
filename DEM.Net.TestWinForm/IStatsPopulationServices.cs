using System.Collections.Generic;
using DEM.Net.Lib.Services.Lab;

namespace DEM.Net.TestWinForm
{
    public interface IStatsPopulationServices
    {
        /// <summary>
        /// Permet de faire des regroupements des points dans des classes en fonction de leur valeur d'élévation.
        /// </summary>
        /// <param name="p_points"></param>
        /// <param name="p_nbreClasses"></param>
        /// <param name="p_modeSeuillage"></param>
        /// <returns></returns>
        Dictionary<string, List<BeanPoint_internal>> GetPointsParClasseOrdonnees(List<BeanPoint_internal> p_points, int p_nbreClasses, enumModeSeuillage p_modeSeuillage);
    }
}