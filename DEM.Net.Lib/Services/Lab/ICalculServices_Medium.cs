using System.Collections.Generic;

namespace DEM.Net.Lib.Services.Lab
{
    public interface ICalculServices_Medium
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="p_points"></param>
        /// <returns></returns>
        BeanTopologieFacettes GetInitialisationTin(List<BeanPoint_internal> p_points);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p_points"></param>
        /// <returns></returns>
        List<BeanPoint_internal> GetConvexHull2D(IEnumerable<BeanPoint_internal> p_points);
     }
}