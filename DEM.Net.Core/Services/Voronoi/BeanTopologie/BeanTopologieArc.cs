using System;
using System.Collections.Generic;
using System.Linq;
using NetTopologySuite.Geometries;

namespace DEM.Net.Core.Voronoi
{
	public class BeanTopologieArc : ICloneable
	{
		public int Arc_id { get; set; }
		public bool ArcIsLine { get; set; }
		public Geometry Arc_geometry { get; set; }
		public string Arc_code_XY_pointOrigine { get; set; }
		public string Arc_code_XY_pointFin { get; set; }

		//Indique  l'ilot à droite et l'ilot à gauche, si les ilots ont été calculés
		//ATTENTION: je prévois une liste;
		//Dans la grande majorité des cas rencontré en 2D, on doit avoir un îlot à droite (inversement à gauche) et un seul
		//Cependant, les ponts, les tunnels peuvent nous générer des cas multidimensionnels 
		public List<BeanTopologieIlot> ArcListeIlotsAGauche { get; set; }
		public List<BeanTopologieIlot> ArcListeIlotsADroite { get; set; }

		//Dans le cas possible:
		//- Des portions de réseau fermées non reliées au réseau général (ex: île au milieu d'un lac)
		//- Des arcs décrivant l'anneau extérieur de l'objet 
		//=> les 2 bords de l'arc vont être dans un premier temps affectés au même îlot (le chemin à gauche = le chemin à droite)
		//Le fait que  le bord droit (inversement le bord gauche) ne soit pas inclus dans l'îlot pointé peut-être connu tôt dans les traitements.
		//L'exploitation ne peut être fait que plus tardivement voire ne nécessite pas d'être pratiquée dans certains cas mais nécessite d'être connue

		public List<bool> ArcListeCoherenceGeometriqueIlotAGauche { get; set; }
		public List<bool> ArcListeCoherenceGeometriqueIlotADroite { get; set; }

		//Indique si l'arc constitue une frontière entre îlots OU si il est inclu dans un îlot
		//Je ne prévois pas une gestion particulière dans le cas précédemment évoqué d'un tronçon limite entre plus de 2 îlots
		public enumNatureDeLArcDansLaTopologieDIlots ArcNatureDansLaTopologieDIlots;

		//
		public BeanTopologieArc(int p_IdArc, Geometry p_IGeometry)
		{
			if (new TopologieService().IsGeometryEnsembleDeLignes(p_IGeometry))
			{
				ArcIsLine = true;
				Arc_geometry = p_IGeometry;
				ArcListeIlotsAGauche = new List<BeanTopologieIlot>();
				ArcListeIlotsADroite = new List<BeanTopologieIlot>();
				ArcListeCoherenceGeometriqueIlotAGauche = new List<bool>();
				ArcListeCoherenceGeometriqueIlotADroite = new List<bool>();

				Arc_id = p_IdArc;
				double v_coordxPoint1 = (double)Arc_geometry.Coordinates[0].X;
				double v_coordyPoint1 = (double)Arc_geometry.Coordinates[0].Y;

				ITopologieService v_topologieService = new TopologieService();
				//Arc_code_XY_pointOrigine = 	v_coordxPoint1.ToString().Replace(",", ".") + "/" + v_coordyPoint1.ToString().Replace(",", ".");
				//Arc_code_XY_pointOrigine = Math.Round(v_coordxPoint1, 2).ToString().Replace(",", ".") + "/" + Math.Round(v_coordyPoint1, 2).ToString().Replace(",", ".");
				Arc_code_XY_pointOrigine = v_topologieService.GetHashCodeGeometriePoint((double)v_coordxPoint1, (double)v_coordyPoint1);

                double v_coordxPoint2 = (double)Arc_geometry.Coordinates.Last().X;
                double v_coordyPoint2 = (double)Arc_geometry.Coordinates.Last().Y;
                //Arc_code_XY_pointFin=v_coordxPoint2.ToString().Replace(",", ".") + "/" + v_coordyPoint2.ToString().Replace(",", ".");
                //Arc_code_XY_pointFin = Math.Round(v_coordxPoint2, 2).ToString().Replace(",", ".") + "/" + Math.Round(v_coordyPoint2, 2).ToString().Replace(",", ".");
                Arc_code_XY_pointFin = v_topologieService.GetHashCodeGeometriePoint((double)v_coordxPoint2, (double)v_coordyPoint2);
			}
			else
			{
				ArcIsLine = false;
			}
		}
		public BeanTopologieArc()
		{
			ArcListeIlotsAGauche = new List<BeanTopologieIlot>();
			ArcListeIlotsADroite = new List<BeanTopologieIlot>();
			ArcListeCoherenceGeometriqueIlotAGauche = new List<bool>();
			ArcListeCoherenceGeometriqueIlotADroite = new List<bool>();
		}

		public object Clone()
		{
			BeanTopologieArc v_CloneTopologieArc = (BeanTopologieArc)this.MemberwiseClone();
			v_CloneTopologieArc.ArcListeIlotsAGauche = this.ArcListeIlotsAGauche;
			v_CloneTopologieArc.ArcListeIlotsADroite = this.ArcListeIlotsADroite;
			v_CloneTopologieArc.ArcListeCoherenceGeometriqueIlotAGauche = this.ArcListeCoherenceGeometriqueIlotAGauche;
			v_CloneTopologieArc.ArcListeCoherenceGeometriqueIlotADroite = this.ArcListeCoherenceGeometriqueIlotADroite;
			v_CloneTopologieArc.ArcNatureDansLaTopologieDIlots = this.ArcNatureDansLaTopologieDIlots;
			return v_CloneTopologieArc;
		}
	}
}
