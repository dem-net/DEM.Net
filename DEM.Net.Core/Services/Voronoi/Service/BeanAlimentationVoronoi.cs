using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DEM.Net.Core.Voronoi
{
	public class BeanAlimentationVoronoi
	{
		public Dictionary<int, Point> p10_dicoDesPointsSources {get; set;}
        public int p11_srid { get; set; }

        public BeanVoronoiParametres p12_parametrage { get; set; }

		//Résultats de tests
		public bool p21_territoireSuperieurA200kmVf { get; set; }  
		public bool p22_contientObjetsInvalidesVf { get; set; }
		public bool p23_contientObjetsSuperposesVf { get; set; }
		public List<int> p24_pointsInvalidesSaufSuperposes { get; set; }
		public List<int> p25_pointsSuperposes { get; set; }

		//data pour exploitation
		public Dictionary<string, int> p31_correspondanceHCodePointvsIdPoint { get; set; }
		public HashSet<Vector> p50_pointsFormatesPourInsertion { get; set; }

		public double p51_xMin { get; set; }
		public double p52_yMin { get; set; }
		public double p53_xMax { get; set; }
		public double p54_yMax { get; set; }

		public int p55_origineXCorrigee { get; set; }
		public int p56_origineYCorrigee { get; set; }

		//PROV
		public Dictionary<int, int> correspondance_IdIlot_IdPoint { get; set; }

		//Constructeur
		public BeanAlimentationVoronoi()
		{
			p10_dicoDesPointsSources = new Dictionary<int, Point>();
			p24_pointsInvalidesSaufSuperposes = new List<int>();
			p25_pointsSuperposes = new List<int>();
			p31_correspondanceHCodePointvsIdPoint = new Dictionary<string, int>();
			p50_pointsFormatesPourInsertion = new HashSet<Vector>();

			p12_parametrage = new VoronoiParametrage().GetParametresStandardVoronoi();
			//PROV
			correspondance_IdIlot_IdPoint = new Dictionary<int, int>();
		}
	}
}
