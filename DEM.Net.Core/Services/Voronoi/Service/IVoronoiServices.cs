using GeoAPI.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DEM.Net.Core.FortuneVoronoi
{
	public interface IVoronoiServices
	{

		BeanTopologie GetTopologieVoronoiByDicoPoints(Dictionary<int, IGeometry> p_dicoPointsAvecId, enumVoronoiStrategiePointsDupliques p_strategieSiDuplication = enumVoronoiStrategiePointsDupliques.arretTraitement);

		BeanTriangulationDelaunay GetTriangulationDeDelaunayByDicoPoints(Dictionary<int, IGeometry> p_dicoPointsAvecId);
		Dictionary<int, IGeometry> GetArcsDelaunayGeometryByTriangulationDelaunay(BeanTriangulationDelaunay p_triangulationDelaunay);

		BeanTopologie GetTopologieVoronoiByDicoPointsEtMbo(Dictionary<int, IGeometry> p_dicoPointsAvecId, double[] p_coodMinMaxXy);

		/// <summary>
		/// Permet de générer une couverture voronoi en fusionnant les polygones choisis
		/// </summary>
		/// <param name="p_dicoPointsDesSurfaces">Points servants au calcul</param>
		/// <param name="p_dicoDesPointsParSurfaces">la clé contient les id des points/surfaces et la liste des id de points devant être regroupés sur la surface associée</param>
		/// <param name="p_coodMinMaxXy"></param>
		/// <returns></returns>
		BeanTopologie GetTopologieVoronoiByDicoPointsEtRegroupementDesPointsEtMbo(Dictionary<int, IGeometry> p_dicoPointsDesSurfaces, Dictionary<int, List<int>> p_dicoDesPointsParSurfaces, double[] p_coodMinMaxXy);
		
	
		BeanTopologie GetTopologieVoronoiByDicoPointsEtRegroupementDesPoints(Dictionary<int, IGeometry> p_dicoPointsDesSurfaces, Dictionary<int, List<int>> p_dicoDesPointsParSurfaces);

		BeanAlimentationVoronoi GetBeanAlimentationVoronoiByDicoPoints(Dictionary<int, IGeometry> p_DicoPoints);

		//Delaunay
		BeanTriangulationDelaunay GetTriangulationDelaunayByVoronoiGraph(VoronoiGraph p_voronoiGraph, BeanAlimentationVoronoi p_beanAlimentationVoronoi);
		string GetHCodeArcDelaunay(int p_id1, int p_id2, bool p_nonOrdonnance_vf, char p_separateur = '_');
		int[] GetIdPointsDelaunayByHCodeArc(string p_codeArcDelaunay, char p_separateur = '_');
		//Fin Delaunay

		VoronoiGraph GetVoronoiGraph(BeanAlimentationVoronoi p_beanAlimentationVoronoi);
		BeanTopologie GetBeanTopologieByVoronoiGraph(VoronoiGraph p_voronoiGraph, BeanAlimentationVoronoi p_beanAlimentationVoronoi);

		//IGeometry GetLineStringByVectors(Vector p_point1, Vector p_point2, int p_SRid = 2154);
		//void visuTriangulationDelaunay(BeanTriangulationDelaunay v_triangulationDelaunay);

	}
}
