using System.Collections.Generic;
using DEM.Net.Core.Services.Lab;
using NetTopologySuite.Geometries;

namespace DEM.Net.Core.Voronoi
{
    public interface IVoronoiServices
    {
        BeanAlimentationVoronoi GetBeanAlimentationVoronoiByDicoPoints(Dictionary<int, NetTopologySuite.Geometries.Point> p_DicoPoints, int p_srid);
        BeanAlimentationVoronoi GetBeanAlimentationVoronoiByDicoPointsEtMbo(Dictionary<int, NetTopologySuite.Geometries.Point> p_DicoPoints, double[] p_coodMinMaxXy, int p_srid);
        BeanTopologieFacettes GetTopologieVoronoiByDicoPoints(Dictionary<int, NetTopologySuite.Geometries.Point> p_dicoPointsAvecId, int p_srid, enumVoronoiStrategiePointsDupliques p_strategieSiDuplication = enumVoronoiStrategiePointsDupliques.arretTraitement);
        BeanTopologieFacettes GetTopologieVoronoiByDicoPointsEtMbo(Dictionary<int, NetTopologySuite.Geometries.Point> p_dicoPointsAvecId, double[] p_coodMinMaxXy, int p_srid);
        BeanTriangulationDelaunay GetTriangulationDeDelaunayByDicoPoints(Dictionary<int, NetTopologySuite.Geometries.Point> p_dicoPointsAvecId, int p_srid);
        BeanTriangulationDelaunay GetTriangulationDelaunayByVoronoiGraph(VoronoiGraph p_voronoiGraph, BeanAlimentationVoronoi p_beanAlimentationVoronoi);
    }
}