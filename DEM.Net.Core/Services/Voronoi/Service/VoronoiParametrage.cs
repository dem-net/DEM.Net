using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DEM.Net.Core.Voronoi
{
	public class VoronoiParametrage
	{
		public BeanVoronoiParametres GetParametresStandardVoronoi()
		{
			BeanVoronoiParametres v_beanParametres = new BeanVoronoiParametres();
			try 
			{
				v_beanParametres.gestionObjetsInvalides = enumVoronoiStrategieObjetsInvalides.arretTraitement;
				v_beanParametres.gestionPointsDupliques = enumVoronoiStrategiePointsDupliques.arretTraitement;
				v_beanParametres.gestionDepassementTerritoire = enumVoronoiStrategieDistanceTropGrande.arretTraitement;
				//
				v_beanParametres.reductionCoordonneesVf = true;

				//
				v_beanParametres.nbPointsHauteur = 3;
				v_beanParametres.nbPointsLargeur = 3;
				v_beanParametres.txReductionMargeEnX = 1;
				v_beanParametres.txReductionMargeEnY = 1;
				v_beanParametres.miseAJourDeLEncombrement = true;
			}
			catch (Exception v_ex)
			{
                throw v_ex;
			}
			return v_beanParametres;
		}
	}
}
