using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using NetTopologySuite.Geometries;
using DEM.Net.Core.Voronoi;
using NetTopologySuite.Geometries;
using Point = NetTopologySuite.Geometries.Point;

namespace DEM.Net.Core.Services.Lab
{
    public class CalculServicesVoronoi : ICalculServicesVoronoi
    {
        public BeanTopologieFacettes GetTopologieVoronoi(List<BeanPoint_internal> p_points, int p_srid)
        {
            BeanTopologieFacettes v_topol = null;
            try
            {
                Dictionary<int, Point> v_pointsGeom;
                v_pointsGeom=p_points.ToDictionary(c => c.p00_id, c => FLabServices.createUtilitaires().ConstructPoint(c.p10_coord[0], c.p10_coord[1], c.p11_srid));
                //

                v_topol = FVoronoiServices.createVoronoiServices().GetTopologieVoronoiByDicoPoints(v_pointsGeom, p_srid);
            }
            catch (Exception)
            {

                throw;
            }
            return v_topol;
        }

      }
}
