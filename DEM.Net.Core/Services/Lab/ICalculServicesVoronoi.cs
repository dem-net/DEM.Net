using System.Collections.Generic;

namespace DEM.Net.Core.Services.Lab
{
    public interface ICalculServicesVoronoi
    {
        BeanTopologieFacettes GetTopologieVoronoi(List<BeanPoint_internal> p_points, int p_srid);
    }
}