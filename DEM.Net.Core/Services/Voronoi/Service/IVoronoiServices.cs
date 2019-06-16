using System.Collections.Generic;
using DEM.Net.Core.Services.Lab;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;

namespace DEM.Net.Core.Voronoi
{
    public interface IVoronoiServices
    {
       
        BeanAlimentationVoronoi GetBeanAlimentationVoronoiByDicoPoints(Dictionary<int, Point> p_DicoPoints, int p_srid);
        BeanAlimentationVoronoi GetBeanAlimentationVoronoiByDicoPointsEtMbo(Dictionary<int, Point> p_DicoPoints, double[] p_coodMinMaxXy, int p_srid);
        //
        BeanTopologieFacettes GetBeanTopologieByVoronoiGraph(VoronoiGraph p_voronoiGraph, BeanAlimentationVoronoi p_beanAlimentationVoronoi);
        BeanTopologieFacettes GetTopologieVoronoiByDicoPoints(Dictionary<int, Point> p_dicoPointsAvecId, int p_srid, enumVoronoiStrategiePointsDupliques p_strategieSiDuplication = enumVoronoiStrategiePointsDupliques.arretTraitement);
        BeanTopologieFacettes GetTopologieVoronoiByDicoPointsEtMbo(Dictionary<int, Point> p_dicoPointsAvecId, double[] p_coodMinMaxXy, int p_srid);
        //
        BeanTriangulationDelaunay GetTriangulationDeDelaunayByDicoPoints(Dictionary<int, Point> p_dicoPointsAvecId, int p_srid);
        BeanTriangulationDelaunay GetTriangulationDelaunayByVoronoiGraph(VoronoiGraph p_voronoiGraph, BeanAlimentationVoronoi p_beanAlimentationVoronoi);
        Dictionary<int, IGeometry> GetArcsDelaunayGeometryByTriangulationDelaunay(BeanTriangulationDelaunay p_triangulationDelaunay);
    }
}