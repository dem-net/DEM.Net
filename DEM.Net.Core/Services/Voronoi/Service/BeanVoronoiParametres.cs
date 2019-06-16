using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DEM.Net.Core.Voronoi
{
	public class BeanVoronoiParametres
	{
		public enumVoronoiStrategieObjetsInvalides gestionObjetsInvalides { get; set; }
		public enumVoronoiStrategiePointsDupliques gestionPointsDupliques { get; set; }
		public enumVoronoiStrategieDistanceTropGrande gestionDepassementTerritoire {get; set;}

		public bool reductionCoordonneesVf { get; set; }

		//Gestion des points périphériques
		public int nbPointsLargeur;
		public int nbPointsHauteur;
		public double txReductionMargeEnX;
		public double txReductionMargeEnY;
		public bool miseAJourDeLEncombrement;
	}
}
