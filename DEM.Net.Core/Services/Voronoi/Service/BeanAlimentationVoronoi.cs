using GeoAPI.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DEM.Net.Core.Voronoi
{
	public class BeanAlimentationVoronoi
	{
		public Dictionary<int, IGeometry> dicoDesPointsSources {get; set;}

		public BeanVoronoiParametres parametrage { get; set; }

		//Résultats de tests
		public bool territoireSuperieurA200kmVf { get; set; }  
		public bool contientObjetsInvalidesVf { get; set; }
		public bool contientObjetsSuperposesVf { get; set; }
		public List<int> pointsInvalidesSaufSuperposes { get; set; }
		public List<int> pointsSuperposes { get; set; }

		//data pour exploitation
		public Dictionary<string, int> dicoLienCodeXyKeySource { get; set; }
		public HashSet<Vector> pointsFormatesPourInsertion { get; set; }

		public double xMin { get; set; }
		public double yMin { get; set; }
		public double xMax { get; set; }
		public double yMax { get; set; }

		public int origineXCorrigee { get; set; }
		public int origineYCorrigee { get; set; }

		//PROV
		public Dictionary<int, int> correspondance_IdIlot_IdPoint { get; set; }

		//Constructeur
		public BeanAlimentationVoronoi()
		{
			dicoDesPointsSources = new Dictionary<int, IGeometry>();
			pointsInvalidesSaufSuperposes = new List<int>();
			pointsSuperposes = new List<int>();
			dicoLienCodeXyKeySource = new Dictionary<string, int>();
			pointsFormatesPourInsertion = new HashSet<Vector>();

			parametrage = new VoronoiParametrage().GetParametresStandardVoronoi();
			//PROV
			correspondance_IdIlot_IdPoint = new Dictionary<int, int>();
		}
	}
}
