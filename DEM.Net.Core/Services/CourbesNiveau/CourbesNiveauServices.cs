using DEM.Net.Core.Services.Lab;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spir.Commun.Service.Technical.Cartographie.Service.CourbesNiveau
{
	public class CourbesNiveauServices : ICourbesNiveauServices
	{
		#region parametres
		public BeanParametresCalculCourbesNiveau GetParametresCalculCourbesNiveauParDefaut(double p_valeurCourbe0, double p_ecartEntreCourbes, double p_pasDeDeduplicationEnM = 10)
		{
			BeanParametresCalculCourbesNiveau v_paramOut = new BeanParametresCalculCourbesNiveau();
			try
			{
				v_paramOut.p00_modeCalculCourbes = enumModeCalculCourbe.interpolationLineaireSurTriangulation;
				//
				v_paramOut.p11_modaliteDeDeduplicationGeomDesPoints = enumModeDeduplicationPoints.manhattanParArrondi;
				v_paramOut.p12_pasDeDepublicationDesPointsEnM = p_pasDeDeduplicationEnM;
				v_paramOut.p13_modeAgregationDesPoints = enumModeAgregationDesPoints.valeurMax;
				//
				v_paramOut.p21_ecartEntreCourbes = p_ecartEntreCourbes;
				v_paramOut.p22_valeurDeLaCourbe0 = 0;
				GetParametresCalculCourbesNiveau_majPasDeDuplicationByRef(ref v_paramOut, p_pasDeDeduplicationEnM);
			}
			catch (Exception)
			{

				throw;
			}
			return v_paramOut;
		}
		public void GetParametresCalculCourbesNiveau_majPasDeDuplicationByRef(ref BeanParametresCalculCourbesNiveau p_parametresAModifier, double p_pasDeDeduplicationEnM)
		{
			try
			{
				int v_nbreDecimalesDeDed=(p_pasDeDeduplicationEnM-Math.Floor(p_pasDeDeduplicationEnM)).ToString().Length-2;
				int v_nbreDecimalesPourArrondi = v_nbreDecimalesDeDed + 1;
				p_parametresAModifier.p12_pasDeDepublicationDesPointsEnM = p_pasDeDeduplicationEnM;
				p_parametresAModifier.p14_nbreDecimalesDArrondiPourCodeVertex = v_nbreDecimalesPourArrondi;
			}
			catch (Exception)
			{

				throw;
			}
		}
        #endregion parametres


        /// <summary>
        /// Méthode de calcul des courbes des niveaux par interpolation linéaire sur une couche irrégulière de points 
        /// (Fonctionne bien sur sur une couche régulière mais inutilement compliqué pour cela!)
        /// Principe: on effectue une triangulation et un pavage triangulaire de Delaunay.
        /// On élimine les triangles 'plats'
        /// On fait, pour chaque arc de chaque triangle 'non plat'  une interpolation linéaire pour identifier les points de passage des courbes 
        /// =>On créé les arcs correspondants
        /// </summary>
        /// <param name="p_pointsAltiDedupliques"></param>
        /// <param name="p_parametresCalculDesCourbes"></param>
        public BeanCourbes GetCourbesNiveau_interpolationLineaireSurTriangulation(BeanTopologieFacettes p_triangulation, BeanParametresCalculCourbesNiveau p_parametresCalculDesCourbes)
		{
			BeanCourbes v_beanResultat = new BeanCourbes();
			try
			{
                //
                Dictionary<int, BeanFacette_internal> v_trianglesAvecData = p_triangulation.p13_facettesById;
                Dictionary<int, BeanPoint_internal> p_points = p_triangulation.p11_pointsFacettesByIdPoint;
                v_beanResultat.p01_parametresCalculDesCourbes = p_parametresCalculDesCourbes;
                


                //ON POSE 3 DICOS:
                Dictionary<string, Dictionary<string, BeanFractionCourbe>> v_fractionsDeCourbesParCodeCourbe = new Dictionary<string, Dictionary<string, BeanFractionCourbe>>();
                Dictionary<string, List<int>> v_trianglesPlatsParCodeCourbe = new Dictionary<string, List<int>>();
				List<BeanArc_internal> v_arcsPeripherieToClosePolygones = new List<BeanArc_internal>();
                
                //=>Le premier dico contient les fractions de courbes issues des triangles.
                //	La clé du dico principal est le code de la courbe.
                //	Le dico inclus contient les fractions de courbes désignées par leur code fraction.
                //	? ce dico plutôt qu'une liste=>2 triangles connexes partagent un même arc; si cet arc constitue une fraction de courbe, il ne faut pas l'insérer 2 fois  
                
                //=>Le second dico contient les 'triangles plats' (= triangles dont les valeurs associées aux sommets sont égales) tels que la valeur associée est celle d'une courbe de niveau:
                //On les stocke (pour debug) MAIS on ne va... rien à en faire! 
                //??=>Ils décrivent des zones 'plates':
                //pour un même niveau, ces triangles 'plats' seraient fusionnés et seule la bordure nous interesserait.
                //Si on excepte la périphérie la plus extérieure de l'espace, ces 'bordures' sont aussi celles d'un triangle 'non plat'
                //=>Elles seront donc extraites de ces triangles 'non plats'    
                // (La clé est le code de la courbe, la valeur: l'id de l'îlot triangle)
               
                //=>Le 3ème contient la géométrie des arcs périphériques. Il doit aider à la fermeture des polygones


                //On détermine une VALEUR de REJET:
                //? une valeur purement technique permettant, dans des méthodes retournant un double,
                //d'indiquer que la valeur retournée n'est pas un résultat utilisable du calcul
				double v_valeurRejet = GetValeurRejet(p_points.Values.ToList(), p_parametresCalculDesCourbes, -99999);


                foreach (KeyValuePair<int, BeanFacette_internal> v_triangle in v_trianglesAvecData.OrderBy(c=>c.Key))
				{
                    TraitementDuTriangleCourantByRef(v_triangle.Value, ref v_trianglesPlatsParCodeCourbe, ref v_fractionsDeCourbesParCodeCourbe, ref v_arcsPeripherieToClosePolygones,p_parametresCalculDesCourbes, v_valeurRejet);
				}
			


				//On RECHERCHE les éventuels 'EXTREMUMS LOCAUX' (points 'sommets' et points 'fonds de cuvette')
                //=>vont constituer des courbes particulières se réduisant à...un point
				Dictionary<string, Dictionary<string, BeanFractionCourbe>> v_extremumsLocaux;
				v_extremumsLocaux=GetPointsExtremumsLocaux(p_triangulation, p_points, p_parametresCalculDesCourbes);
				//On les ajoute:
					if (v_extremumsLocaux != null && v_extremumsLocaux.Count>0)
				{
					//On souhaite garder uniquement ceux sur une courbe (ou plutôt sur un "niveau de courbe")
					Dictionary<string, Dictionary<string, BeanFractionCourbe>> v_extremumsLocauxFiltres = new Dictionary<string, Dictionary<string, BeanFractionCourbe>>();
					foreach (KeyValuePair<string, Dictionary<string, BeanFractionCourbe>> v_courbe in v_extremumsLocauxFiltres)
					{
						if (GetIntervalleDAppartenanceDuPoint(Convert.ToInt16(v_courbe.Key), p_parametresCalculDesCourbes).Count() == 1)
						{
							v_extremumsLocauxFiltres.Add(v_courbe.Key, v_courbe.Value);
						}
					}
					foreach (KeyValuePair< string, Dictionary < string, BeanFractionCourbe >> v_courbe in v_extremumsLocauxFiltres)
					{
						if (!v_fractionsDeCourbesParCodeCourbe.ContainsKey(v_courbe.Key))
						{
							v_fractionsDeCourbesParCodeCourbe.Add(v_courbe.Key, new Dictionary<string, BeanFractionCourbe>());
						}
						foreach(KeyValuePair<string, BeanFractionCourbe> v_point in v_courbe.Value)
						{
							v_fractionsDeCourbesParCodeCourbe[v_courbe.Key].Add(v_point.Key, v_point.Value);
						}
					}
				}

                //Indexation des classes
                int v_indexClasse= 0;
                v_beanResultat.p10_labelClassesParIndexClasse=v_fractionsDeCourbesParCodeCourbe.Select(c => Convert.ToDouble(c.Key)).OrderBy(c => c).ToDictionary(c => v_indexClasse++, c => c.ToString());
                v_beanResultat.p11_indexClasseParLabelClasse = v_beanResultat.p10_labelClassesParIndexClasse.ToDictionary(c => c.Value, c => c.Key);
               
                //ASSEMBLAGE des SEGMENTS de COURBE
                Dictionary<string, Dictionary<int, List<double[]>>> v_courbesCoordParNiveau;
                v_courbesCoordParNiveau=AssemblageDesFractionsDeCourbes(v_fractionsDeCourbesParCodeCourbe);

                //=>Application sur le bean
                v_beanResultat.p12_tousSegmentsCourbesParNiveau = v_fractionsDeCourbesParCodeCourbe;
				v_beanResultat.p13_arcsPeripherieToClosePolygones= v_arcsPeripherieToClosePolygones;
                v_beanResultat.p14_courbesAssembleesCoordParNiveau = v_courbesCoordParNiveau;

            }
			catch (Exception)
			{

				throw;
			}
			return v_beanResultat;
		}

		internal void TraitementDuTriangleCourantByRef(BeanFacette_internal p_triangleCourant, ref Dictionary<string, List<int>> p_dicoGlobalDesTrianglesPourFusion, ref Dictionary<string, Dictionary<string, BeanFractionCourbe>> p_dicoGlobalDesfractionsDeCourbesParCodeCourbe,ref List<BeanArc_internal> p_fractionsContourExterieur, BeanParametresCalculCourbesNiveau p_parametresCalculDesCourbes, double p_valeurRejet = -999999)
		{
			try
			{
                int v_srid = p_triangleCourant.p01_pointsDeFacette.First().p11_srid;

                double v_valeurCourbeTrianglePlat;
				//1-Si le triangle est 'plat'(=ses 3 sommets sont associés à une même valeur)
				if (IsTrianglePlat(p_triangleCourant))
				{
					//=>On détermine si ces sommets sont strictement sur une courbe de niveau
					//Si c'est le cas, on stocke la référence de l'îlot avec la valeur de la courbe associée: 
					//     [=>on l'agrégera, plus tard, si besoin avec d'autres îlots voisins qui seraient dans le même cas et sur la même courbe;
					//     sinon, ses arcs devront être utilisées comme fractions de courbe de niveau ]
					//Dans le cas contraire, les valeurs sont forcément entre 2 courbes=>on peut ignorer l'îlot
					v_valeurCourbeTrianglePlat = GetValeurCourbeSiTrianglePlatUtileValeurParDefautSinon(p_triangleCourant, p_parametresCalculDesCourbes, p_valeurRejet);
					if (v_valeurCourbeTrianglePlat != p_valeurRejet)
					{
						if (!p_dicoGlobalDesTrianglesPourFusion.ContainsKey(v_valeurCourbeTrianglePlat.ToString()))
						{
							p_dicoGlobalDesTrianglesPourFusion.Add(v_valeurCourbeTrianglePlat.ToString(), new List<int>());
						}
						p_dicoGlobalDesTrianglesPourFusion[v_valeurCourbeTrianglePlat.ToString()].Add(p_triangleCourant.p00_idFacette);
					}
                    //Récup des éventuels arcs 'périphériques'
                    p_fractionsContourExterieur.AddRange(p_triangleCourant.p02_arcs.Where(c => c.p20_statutArc == enumStatutArc.arcExterne).ToList());

					return;
				}
				
				//________________________________
				//2-Si le triangle n'est pas 'plat':
                //=>On est potentiellement amené à le 'découper'

				Dictionary<string, Dictionary<string, BeanPointDecoup_internal>> v_pointsDeDecoupDuTriangleByCodeCourbe = new Dictionary<string, Dictionary<string, BeanPointDecoup_internal>>();
                //(La clé du dico principal:=>le code de la courbe
                //(La clé du dico secondaire:=>le code du point

                //2a-Le triangle a t-il des sommets strictement sur des courbes de niveau ?
                double[] v_courbeAuDessousEtAuDessus;
                string v_codeCourbe;
                foreach (BeanPoint_internal v_sommet in p_triangleCourant.p01_pointsDeFacette)
				{
					v_courbeAuDessousEtAuDessus=GetIntervalleDAppartenanceDuPoint(v_sommet.p10_coord[2], p_parametresCalculDesCourbes);
					//Si une seule valeur, alors le point est sur la courbe
					if (v_courbeAuDessousEtAuDessus.Count()==1)
					{
						v_codeCourbe = v_courbeAuDessousEtAuDessus[0].ToString();
						PutPointDecoupeInPointDecoupeDuTriangleByRef(v_codeCourbe, v_sommet.p10_coord[0], v_sommet.p10_coord[1],v_srid, ref v_pointsDeDecoupDuTriangleByCodeCourbe, p_parametresCalculDesCourbes);
					}
				}


                //2b-On chercle les éventuels points de découpe des arcs du triangle
                Dictionary<string, List<BeanPointDecoup_internal>> v_pointsDecoupeDeLArc;
				foreach (BeanArc_internal v_arcDelaunay in p_triangleCourant.p02_arcs)
				{
                    //(La situation nominale est, 'au plus, 1 un seul point par arc pour une courbe donnée' 
                    //SAUF si les 2 points extrêmes sont à la même 'altitude' et appartiennent à une courbe'
                    // Dans ce cas, tout le segment doit être conservé'.
                    v_pointsDecoupeDeLArc = GetPointsDecoupesByCouplePointsAlti(v_arcDelaunay.p11_pointDbt, v_arcDelaunay.p12_pointFin, p_parametresCalculDesCourbes);
					foreach(KeyValuePair<string, List<BeanPointDecoup_internal>> v_pointsSurCourbePourLArc in v_pointsDecoupeDeLArc)
					{
						foreach(BeanPointDecoup_internal v_point in v_pointsSurCourbePourLArc.Value)
						{
							PutPointDecoupeInPointDecoupeDuTriangleByRef(v_pointsSurCourbePourLArc.Key, v_point.p01_coordX, v_point.p02_coordY, v_srid, ref v_pointsDeDecoupDuTriangleByCodeCourbe, p_parametresCalculDesCourbes);
						}
					}

                    //On effectue, dans la même passe, un traitement spécifique sur les arcs externes:
                    //?Les 'arcs extérieurs' vont être utiles pour constituer les polygones entre 2 courbes.
                    //[Note: attention! Du coup, il ne constitue pas des 'fractions' de courbe (par définition, d'altitude homogène) 
                    //mais bien une liaison entre 2 courbes
					if (v_arcDelaunay.p20_statutArc==enumStatutArc.arcExterne)
					{
                        ExtraitLesIntercourbesSurUnArcExterneByRef(v_arcDelaunay, v_pointsDecoupeDeLArc, ref p_fractionsContourExterieur);

                    }
				}

			
				//On déduit les fractions d'arcs s'il y a lieu
				if (v_pointsDeDecoupDuTriangleByCodeCourbe!=null && v_pointsDeDecoupDuTriangleByCodeCourbe.Count>0)
				{
					Dictionary<string, BeanFractionCourbe> v_fractionsDeCourbesDuTriangleByCodeCourbe;
					v_fractionsDeCourbesDuTriangleByCodeCourbe = GetFractionsCourbesByPointsDecoupeTriangle(v_pointsDeDecoupDuTriangleByCodeCourbe);

					//On les injecte dans le dico global
					foreach (KeyValuePair<string, BeanFractionCourbe> v_fraction in v_fractionsDeCourbesDuTriangleByCodeCourbe)
					{
						if (!p_dicoGlobalDesfractionsDeCourbesParCodeCourbe.ContainsKey(v_fraction.Key))
						{
							p_dicoGlobalDesfractionsDeCourbesParCodeCourbe.Add(v_fraction.Key, new Dictionary<string, BeanFractionCourbe>());
						}
						if (!p_dicoGlobalDesfractionsDeCourbesParCodeCourbe[v_fraction.Key].ContainsKey(v_fraction.Value.p00_codeFractionCourbe))
						{
							p_dicoGlobalDesfractionsDeCourbesParCodeCourbe[v_fraction.Key].Add(v_fraction.Value.p00_codeFractionCourbe, v_fraction.Value);
						}
					}
				}
			}
			catch (Exception)
			{

				throw;
			}
		}
        /// <summary>
        ///On effectue un traitement spécifique sur les arcs externes:
        ///?Les 'arcs extérieurs' vont être utiles pour constituer les polygones entre 2 courbes.
        ///[Note: attention! Du coup, il ne constitue pas des 'fractions' de courbe (par définition, d'altitude homogène) 
        ///mais bien une liaison entre 2 courbes 
        /// </summary>
        /// <param name="p_arcDelaunay"></param>
        /// <param name="p_pointsDecoupeDeLArc"></param>
        /// <param name="p_fractionsContourExterieur"></param>
        internal void ExtraitLesIntercourbesSurUnArcExterneByRef(BeanArc_internal p_arcDelaunay, Dictionary<string, List<BeanPointDecoup_internal>> p_pointsDecoupeDeLArc, ref List<BeanArc_internal> p_fractionsContourExterieur)
        {
            try
            {
                if (p_arcDelaunay.p20_statutArc != enumStatutArc.arcExterne)
                {
                    return;
                }
                    //Si l'arc extérieur n'est pas associé à un ou des points de découpe, alors on le conserve intégralement:
                    if (p_pointsDecoupeDeLArc == null || p_pointsDecoupeDeLArc.Count == 0)
                    {
                        p_fractionsContourExterieur.Add(p_arcDelaunay);
                    }
                    //Sinon, on calcule les fractions d'arcs 'entre 2 courbes'
                    else
                    {
                        //On ordonne les points de découpe par distance au premier point de l'arc
                        List<BeanPointDecoup_internal> v_points = new List<BeanPointDecoup_internal>();
                        foreach (List<BeanPointDecoup_internal> v_listePointsDec in p_pointsDecoupeDeLArc.Values)
                        {
                            v_points.AddRange(v_listePointsDec);
                        }
                        List<double[]> v_pointsOrdonnances = new List<double[]>();

                        double[] v_coordDbt = p_arcDelaunay.p11_pointDbt.p10_coord;
                        double[] v_coordFin = p_arcDelaunay.p12_pointFin.p10_coord;

                        v_pointsOrdonnances.Add(v_coordDbt);
                        v_pointsOrdonnances.AddRange(v_points
                            .OrderBy(c => ((c.p01_coordX - v_coordDbt[0]) * (c.p01_coordX - v_coordDbt[0])) + ((c.p02_coordY - v_coordDbt[1]) * (c.p02_coordY - v_coordDbt[1])))
                            .Select(c => new double[2] { c.p01_coordX, c.p02_coordY })
                            .ToList());
                        v_pointsOrdonnances.Add(v_coordFin);


                    //On calcule les fractions d'arc inter-courbe:
                    int v_srid = p_arcDelaunay.p11_pointDbt.p11_srid;
                    BeanArc_internal v_arc;
                        BeanPoint_internal v_pointDbt;
                        BeanPoint_internal v_pointFin;
                        for (int v_ind = 0; v_ind < v_pointsOrdonnances.Count - 1; v_ind++)
                        {
                            if (v_pointsOrdonnances[v_ind][0] == v_pointsOrdonnances[v_ind + 1][0] && v_pointsOrdonnances[v_ind][1] == v_pointsOrdonnances[v_ind + 1][1])
                            {
                                continue;
                            }
                            v_pointDbt = new BeanPoint_internal(v_pointsOrdonnances[v_ind][0], v_pointsOrdonnances[v_ind][1], 0, v_srid);
                            v_pointFin = new BeanPoint_internal(v_pointsOrdonnances[v_ind + 1][0], v_pointsOrdonnances[v_ind + 1][1], 0, v_srid);
                            v_arc = new BeanArc_internal(v_pointDbt, v_pointFin);

                            p_fractionsContourExterieur.Add(v_arc);
                        }
                    }
                
            }
            catch (Exception)
            {

                throw;
            }
        }
		internal Dictionary<string, BeanFractionCourbe> GetFractionsCourbesByPointsDecoupeTriangle(Dictionary<string, Dictionary<string, BeanPointDecoup_internal>> p_pointsDecoupeDuTriangle)
		{
			Dictionary<string, BeanFractionCourbe> v_fractionsDeCourbes = new Dictionary<string, BeanFractionCourbe>();
			try
			{
				//
				BeanFractionCourbe v_fractionCourbe;
				foreach(KeyValuePair<string, Dictionary<string, BeanPointDecoup_internal>> v_codeCourbe in p_pointsDecoupeDuTriangle)
				{
					v_fractionCourbe = new BeanFractionCourbe();
					v_fractionCourbe.p01_point_1 = v_codeCourbe.Value.First().Value;

					//=>Les points isolés peuvent avoir du sens (1 'pic' ou 1 'fond de cuvette') mais il semble plus pertinent de les identifier à part.
					if (v_codeCourbe.Value.Count==1) 
					{
						continue;
					}

					//=>Mais, dans le cas général, on va produire des lignes:
					v_fractionCourbe.p02_point_2 = v_codeCourbe.Value.Last().Value;
					v_fractionCourbe.p11_estLigneSinonPoint_vf = true;
					//v_fractionCourbe.p10_geom = v_topologieServices.GetLineStringByCoord(v_codeCourbe.Value.First().Value.p01_coordX, v_codeCourbe.Value.First().Value.p02_coordY, v_codeCourbe.Value.Last().Value.p01_coordX, v_codeCourbe.Value.Last().Value.p02_coordY);
					v_fractionCourbe.p20_valeurCourbe = v_codeCourbe.Key;

					//Je veux un hcode dépendant de la géométrie mais pas du sens de cette géom  
					if (v_codeCourbe.Value.First().Value.p01_coordX== v_codeCourbe.Value.Last().Value.p01_coordX)
					{
						if (v_codeCourbe.Value.First().Value.p02_coordY < v_codeCourbe.Value.Last().Value.p02_coordY)
						{
							v_fractionCourbe.p00_codeFractionCourbe = v_codeCourbe.Value.First().Value.p00_hcodePoint + "_" + v_codeCourbe.Value.Last().Value.p00_hcodePoint;
						}
						else
						{
							v_fractionCourbe.p00_codeFractionCourbe = v_codeCourbe.Value.Last().Value.p00_hcodePoint + "_" + v_codeCourbe.Value.First().Value.p00_hcodePoint ;
						}
					}
					else
					{
						if (v_codeCourbe.Value.First().Value.p01_coordX < v_codeCourbe.Value.Last().Value.p01_coordX)
						{
							v_fractionCourbe.p00_codeFractionCourbe = v_codeCourbe.Value.First().Value.p00_hcodePoint + "_" + v_codeCourbe.Value.Last().Value.p00_hcodePoint;
						}
						else
						{
							v_fractionCourbe.p00_codeFractionCourbe = v_codeCourbe.Value.Last().Value.p00_hcodePoint + "_" + v_codeCourbe.Value.First().Value.p00_hcodePoint;
						}
					}
					//
					v_fractionsDeCourbes.Add(v_codeCourbe.Key, v_fractionCourbe);
				}
			}
			catch (Exception)
			{

				throw;
			}
			return v_fractionsDeCourbes;
		}
		internal Dictionary<string, List<BeanPointDecoup_internal>> GetPointsDecoupesByCouplePointsAlti(BeanPoint_internal p_pointAlti_1, BeanPoint_internal p_pointAlti_2, BeanParametresCalculCourbesNiveau p_parametresCalculDesCourbes)
		{
			Dictionary<string, List<BeanPointDecoup_internal>> v_pointsDeDecoupeDeLArc = new Dictionary<string, List<BeanPointDecoup_internal>>();
			try
			{
                BeanPoint_internal v_pointBas;
                BeanPoint_internal v_pointHaut;
				BeanPointDecoup_internal v_pointDecoupToInsert;
				string v_codeCourbe;
                int v_srid = p_pointAlti_1.p11_srid;
                //On récupère et on ordonne les points par altitude
                if (p_pointAlti_1.p10_coord[2] <= p_pointAlti_2.p10_coord[2])
				{
					v_pointBas = p_pointAlti_1;
					v_pointHaut = p_pointAlti_2;
				}
				else
				{
					v_pointBas = p_pointAlti_2;
					v_pointHaut = p_pointAlti_1;
				}
				//
				double[] v_seuilsDuPointBas=GetIntervalleDAppartenanceDuPoint(v_pointBas.p10_coord[2], p_parametresCalculDesCourbes);
				double[] v_seuilsDuPointHaut=GetIntervalleDAppartenanceDuPoint(v_pointHaut.p10_coord[2], p_parametresCalculDesCourbes);

				//Si l'un et l'autre point sont entre les mêmes courbes=>pas de points à créer
				if (v_seuilsDuPointBas.Count()==2 && v_seuilsDuPointHaut.Count()==2 && v_seuilsDuPointBas[0]== v_seuilsDuPointHaut[0])
				{
					return v_pointsDeDecoupeDeLArc;
				}
			
				//Si l'un ou l'autre point est précisément sur une courbe, on l'injecte
				if (v_seuilsDuPointBas.Count()==1)
				{
					v_codeCourbe = v_seuilsDuPointBas[0].ToString(); //[0 car dans ce cas, un seul seuil a du être renvoyé
					v_pointDecoupToInsert = GetPointDecoupeInternal(v_pointBas.p10_coord[0], v_pointBas.p10_coord[1], v_srid, p_parametresCalculDesCourbes);
					if (!v_pointsDeDecoupeDeLArc.ContainsKey(v_codeCourbe))
					{
						v_pointsDeDecoupeDeLArc.Add(v_codeCourbe, new List<BeanPointDecoup_internal>());
					}
					v_pointsDeDecoupeDeLArc[v_codeCourbe].Add(v_pointDecoupToInsert);
				}
				
				if (v_seuilsDuPointHaut.Count() == 1)
				{
					v_codeCourbe = v_seuilsDuPointHaut[0].ToString(); //[0 car dans ce cas, un seul seuil a du être renvoyé
					v_pointDecoupToInsert = GetPointDecoupeInternal(v_pointHaut.p10_coord[0], v_pointHaut.p10_coord[1], v_srid, p_parametresCalculDesCourbes);
					if (!v_pointsDeDecoupeDeLArc.ContainsKey(v_codeCourbe))
					{
						v_pointsDeDecoupeDeLArc.Add(v_codeCourbe, new List<BeanPointDecoup_internal>());
					}
					v_pointsDeDecoupeDeLArc[v_codeCourbe].Add(v_pointDecoupToInsert);
				}

				double v_amplitudeValeurs;
				v_amplitudeValeurs = v_pointHaut.p10_coord[2] - v_pointBas.p10_coord[2];
				//Si les deux points ont les mêmes valeurs, on peut terminer (il peut donc y avoir entre 0 et 2 points en sortie
				if (v_amplitudeValeurs==0)
				{
					return v_pointsDeDecoupeDeLArc;
				}

				//Calcul des points intercalaires
				double[] v_vecteurDelaunay = new double[2];
				v_vecteurDelaunay[0] = v_pointHaut.p10_coord[0] - v_pointBas.p10_coord[0];
				v_vecteurDelaunay[1] = v_pointHaut.p10_coord[1] - v_pointBas.p10_coord[1];
				
				double[] v_vecteurDecoup = new double[2];
				double v_tx;
				double v_ecartAuPointOrigine;
				v_ecartAuPointOrigine = (v_seuilsDuPointBas.Max(c => c) - v_pointBas.p10_coord[2]);

				double v_valeurCourbePourDecoupe;
				v_valeurCourbePourDecoupe = v_seuilsDuPointBas.Max(c => c);
				if (v_ecartAuPointOrigine==0)  //(Si l'écart est 0: le point de découpe a déjà été injecté
				{
					v_ecartAuPointOrigine += p_parametresCalculDesCourbes.p21_ecartEntreCourbes;
					v_valeurCourbePourDecoupe += p_parametresCalculDesCourbes.p21_ecartEntreCourbes;
				}
				while (v_amplitudeValeurs > v_ecartAuPointOrigine)//Je mets bien strictement sup: le dernier point, si sur la courbe, a déjà été injecté.
				{
						v_tx = v_ecartAuPointOrigine / v_amplitudeValeurs;
						v_vecteurDecoup[0] = (v_vecteurDelaunay[0] * v_tx)+ (double)v_pointBas.p10_coord[0];
						v_vecteurDecoup[1] = v_vecteurDelaunay[1] * v_tx + (double)v_pointBas.p10_coord[1];
					v_pointDecoupToInsert = GetPointDecoupeInternal(v_vecteurDecoup[0], v_vecteurDecoup[1], v_srid,p_parametresCalculDesCourbes);

						v_codeCourbe = v_valeurCourbePourDecoupe.ToString();
					if (!v_pointsDeDecoupeDeLArc.ContainsKey(v_codeCourbe))
					{
						v_pointsDeDecoupeDeLArc.Add(v_codeCourbe, new List<BeanPointDecoup_internal>());
					}
					v_pointsDeDecoupeDeLArc[v_codeCourbe].Add(v_pointDecoupToInsert);
					 //
					 v_ecartAuPointOrigine += p_parametresCalculDesCourbes.p21_ecartEntreCourbes;
					v_valeurCourbePourDecoupe += p_parametresCalculDesCourbes.p21_ecartEntreCourbes;
				}
			}
			catch (Exception)
			{
				throw;
			}
			return v_pointsDeDecoupeDeLArc;
		}
		internal Dictionary<string, Dictionary<string, BeanFractionCourbe>> GetPointsExtremumsLocaux(BeanTopologieFacettes p_triangulationDelaunay, Dictionary<int, BeanPoint_internal> p_dicoDesPointsAltiByIdPoint, BeanParametresCalculCourbesNiveau p_parametresCalculDesCourbes)
		{
			Dictionary<string, Dictionary<string, BeanFractionCourbe>> v_pointsSommets = new Dictionary<string, Dictionary<string, BeanFractionCourbe>>();
			try
			{
				List<BeanPoint_internal> v_pointsAltiVoisins;
				BeanPointDecoup_internal v_pointSommet;
				BeanFractionCourbe v_courbePoint;
                int v_srid = p_triangulationDelaunay.p00_pointsSources.First().p11_srid;

                foreach (BeanPoint_internal v_pointAlti in p_dicoDesPointsAltiByIdPoint.Values)
				{
					v_pointsAltiVoisins = new List<BeanPoint_internal>();
                    foreach(BeanArc_internal v_arc in v_pointAlti.p41_arcsAssocies.Values)
                    {
                        if(v_arc.p11_pointDbt.p00_id== v_pointAlti.p00_id)
                        {
                            v_pointsAltiVoisins.Add(v_arc.p12_pointFin);
                        }
                        else
                        {
                            v_pointsAltiVoisins.Add(v_arc.p11_pointDbt);
                        }
                    }
                  
					//Si la valeur est strictement plus petite ou plus grande que celle de ses voisins=>c'est un extremum local.
					if (v_pointsAltiVoisins.Min(c => c.p10_coord[2]) > v_pointAlti.p10_coord[2] || v_pointsAltiVoisins.Max(c => c.p10_coord[2]) < v_pointAlti.p10_coord[2])
					{
						v_pointSommet = GetPointDecoupeInternal(v_pointAlti.p10_coord[0], v_pointAlti.p10_coord[1], v_srid, p_parametresCalculDesCourbes);
						//On créé une pseudo-courbe (va être, en fait, un point!)
						v_courbePoint = new BeanFractionCourbe();
						v_courbePoint.p01_point_1 = v_pointSommet;
						v_courbePoint.p11_estLigneSinonPoint_vf = false;
						v_courbePoint.p00_codeFractionCourbe = v_pointSommet.p00_hcodePoint;
						//v_courbePoint.p10_geom 
						v_courbePoint.p20_valeurCourbe = v_pointAlti.p10_coord[2].ToString();
						if (!v_pointsSommets.ContainsKey(v_pointAlti.p10_coord[2].ToString()))
						{
							v_pointsSommets.Add(v_pointAlti.p10_coord[2].ToString(), new Dictionary<string, BeanFractionCourbe>());
						}
                        v_pointsSommets[v_pointAlti.p10_coord[2].ToString()].Add(v_courbePoint.p00_codeFractionCourbe, v_courbePoint);
                    }
                }
			}
			catch (Exception)
			{

				throw;
			}
			return v_pointsSommets;
		}
		internal void PutPointDecoupeInPointDecoupeDuTriangleByRef(string v_codeCourbe, double p_coordX, double p_coordY,int p_srid, ref Dictionary<string, Dictionary<string, BeanPointDecoup_internal>> p_pointsDeDecoupDuTriangleByCodeCourbe, BeanParametresCalculCourbesNiveau p_parametresCalculDesCourbes)
		{
			try
			{
				if (!p_pointsDeDecoupDuTriangleByCodeCourbe.ContainsKey(v_codeCourbe))
				{
					p_pointsDeDecoupDuTriangleByCodeCourbe.Add(v_codeCourbe, new Dictionary<string, BeanPointDecoup_internal>());
				}
				BeanPointDecoup_internal v_pointDecoupe = GetPointDecoupeInternal(p_coordX, p_coordY, p_srid, p_parametresCalculDesCourbes);
				if (!p_pointsDeDecoupDuTriangleByCodeCourbe[v_codeCourbe].ContainsKey(v_pointDecoupe.p00_hcodePoint))
				{
					p_pointsDeDecoupDuTriangleByCodeCourbe[v_codeCourbe].Add(v_pointDecoupe.p00_hcodePoint, v_pointDecoupe);
				}
			}
			catch (Exception)
			{

				throw;
			}
		}
		internal BeanPointDecoup_internal GetPointDecoupeInternal(double p_coordX, double p_coordY,int p_srid, BeanParametresCalculCourbesNiveau p_parametresCalculDesCourbes)
		{
			BeanPointDecoup_internal v_pointDecoup = new BeanPointDecoup_internal();
			try
			{
				v_pointDecoup.p01_coordX = p_coordX;
				v_pointDecoup.p02_coordY = p_coordY;
                v_pointDecoup.p03_Srid = p_srid;

                //v_pointDecoup.p00_hcodePoint = GetHCodePoint(p_coordX, p_coordY, p_parametresCalculDesCourbes.p14_nbreDecimalesDArrondiPourCodeVertex);
                v_pointDecoup.p00_hcodePoint = GetHCodePoint(p_coordX, p_coordY);


            }
            catch (Exception)
			{

				throw;
			}
			return v_pointDecoup;
		}
       
        /// <summary>
        /// Sert simplement à générer une valeur qui sert à indiquer: 'résultat invalide'!
        /// =>Permet d'en définir une sans risque de collision avec des valeurs utiles
        /// </summary>
        /// <param name="p_pointsAlti"></param>
        /// <param name="p_parametresCalculDesCourbes"></param>
        /// <param name="v_valeurRejetParDefaut"></param>
        /// <returns></returns>
        internal double GetValeurRejet(List<BeanPoint_internal> p_pointsAlti, BeanParametresCalculCourbesNiveau p_parametresCalculDesCourbes, double v_valeurRejetParDefaut = -999999)
        {
            double v_valeurRejetOut = v_valeurRejetParDefaut;
            try
            {
                double v_valeurMin = p_pointsAlti.Min(c => c.p10_coord[2]);
                double v_courbeMinAuPire = v_valeurMin - p_parametresCalculDesCourbes.p21_ecartEntreCourbes;

                double v_valeurRejet = v_valeurRejetParDefaut;
                if (v_valeurRejet > v_courbeMinAuPire)
                {
                    v_valeurRejet = v_courbeMinAuPire - 1000;
                }
            }
            catch (Exception)
            {

                throw;
            }
            return v_valeurRejetOut;
        }



        #region Contrôle triangle plats
        /// <summary>
        ///[Il ne s'agit pas ici de la notion géométrique de 'triangle plat' (=triangle sans surface)
        ///=>Ici, on considère les valeurs associées à chaque sommet du triangle:
        /// Si toutes les valeurs sont égales, le triangle est déclaré 'plat'.
        /// Il vaudrait meiux employer le terme de 'tétraèdre plat' ou de 'pyramide plate'! 
        /// </summary>
        /// <param name="p_triangle"></param>
        /// <returns></returns>
        private bool IsTrianglePlat(BeanFacette_internal p_triangle)
		{
			return (p_triangle.p01_pointsDeFacette.Select(c => c.p10_coord[2]).Distinct().Count() == 1);
		}
		private bool IsTrianglePlatUtile(BeanFacette_internal p_trianglePlat, BeanParametresCalculCourbesNiveau p_parametresCalculDesCourbes)
		{
			try
			{
				if (!IsTrianglePlat(p_trianglePlat)) //On contrôle qd même qu'il est plat!
				{
					throw new Exception("Le triangle demandé n'est pas plat.");
				}
				double v_valeurPoints = p_trianglePlat.p01_pointsDeFacette.Select(c => c.p10_coord[2]).Distinct().First();
				double[] v_courbesEnDessousEtEnDessus;
				v_courbesEnDessousEtEnDessus = GetIntervalleDAppartenanceDuPoint(v_valeurPoints, p_parametresCalculDesCourbes);
				//=>Si une seule valeur=>le point est strictement sur la courbe
				if (v_courbesEnDessousEtEnDessus.Count()==1)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			catch (Exception)
			{

				throw;
			}
		}
		private double GetValeurCourbeSiTrianglePlatUtileValeurParDefautSinon(BeanFacette_internal p_triangle, BeanParametresCalculCourbesNiveau p_parametresCalculDesCourbes, double p_valeurParDefautSiNonConforme= -99999)
		{
			try
			{
				//1-Est-il 'plat' ? 
				if (!IsTrianglePlat(p_triangle))
				{ 
					return p_valeurParDefautSiNonConforme;
				}
				//2-
				double v_valeurPoints = p_triangle.p01_pointsDeFacette.Select(c => c.p10_coord[2]).Distinct().First();
				double[] v_courbesEnDessousEtEnDessus;
				v_courbesEnDessousEtEnDessus = GetIntervalleDAppartenanceDuPoint(v_valeurPoints, p_parametresCalculDesCourbes);
				//=>Si une seule valeur=>le point est strictement sur la courbe
				if (v_courbesEnDessousEtEnDessus.Count() == 1)
				{
					return v_courbesEnDessousEtEnDessus.First();
				}
				else
				{
					return p_valeurParDefautSiNonConforme;
				}
			}
			catch (Exception)
			{

				throw;
			}
		}
        #endregion Contrôle triangle plats

        internal string GetHCodePoint(double p_coordX, double p_coordY)
        {
            string v_hashCode;
            try
            {
                double[] v_coord = new double[2] { p_coordX, p_coordY };
                v_hashCode = FLabServices.createUtilitaires().GetHCodeGeogPoint(v_coord);
            }
            catch (Exception)
            {
                throw;
            }
            return v_hashCode;
        }
        //internal string GetHCodePoint(double p_coordX, double p_coordY, int p_nbreDecimalesPourArrondi, char p_separateur='_')
        //{
        //	string v_hashCode;
        //	try
        //	{
        //		v_hashCode = Math.Round(p_coordX, p_nbreDecimalesPourArrondi).ToString().Replace(",", ".") + p_separateur + Math.Round(p_coordY, p_nbreDecimalesPourArrondi).ToString().Replace(",", ".");
        //	}
        //	catch (Exception)
        //	{
        //		throw;
        //	}
        //	return v_hashCode;
        //}

        #region seuil d'appartenance
        /// <summary>
        /// Renvoie l'"intervalle d'appartenance de la valeur": elle peut être comprise entre 2 valeurs (=2 'courbes') 
        /// OU être strictement sur la courbe=>Dans ce cas, 1 seule valeur est renvoyée.
        /// </summary>
        /// <param name="p_valeurAClasser"></param>
        /// <param name="p_parametresCalculCourbes"></param>
        /// <returns></returns>
        private double[] GetIntervalleDAppartenanceDuPoint(double p_valeurAClasser, BeanParametresCalculCourbesNiveau p_parametresCalculCourbes)
		{
			double[] p_classeDAppartenanceDuPoint = null;
			try
			{
				double v_classe;
				v_classe=(p_valeurAClasser - p_parametresCalculCourbes.p22_valeurDeLaCourbe0) / p_parametresCalculCourbes.p21_ecartEntreCourbes ;
				int v_seuilInf = (int)Math.Floor(v_classe);
				int v_seuilSup = (int)Math.Ceiling(v_classe);
				//
			
				if (v_seuilInf != v_seuilSup)
				{
					p_classeDAppartenanceDuPoint = new double[2];
					p_classeDAppartenanceDuPoint[1] = (p_parametresCalculCourbes.p21_ecartEntreCourbes * v_seuilSup) + p_parametresCalculCourbes.p22_valeurDeLaCourbe0;
				}
				else
				{
					p_classeDAppartenanceDuPoint = new double[1];
				}
				p_classeDAppartenanceDuPoint[0] = (p_parametresCalculCourbes.p21_ecartEntreCourbes * v_seuilInf) + p_parametresCalculCourbes.p22_valeurDeLaCourbe0;
			}
			catch (Exception)
			{
				throw;
			}
			return p_classeDAppartenanceDuPoint;
		}
        #endregion seuil d'appartenance

        internal Dictionary<string, Dictionary<int, List<double[]>>> AssemblageDesFractionsDeCourbes(Dictionary<string, Dictionary<string, BeanFractionCourbe>> p_fractionCourbesParNiveau)
        {
            Dictionary<string, Dictionary<int, List<double[]>>> v_seriesCoordPoints_parNiveau=new Dictionary<string, Dictionary<int, List<double[]>>>();
            try
            {
                //On calcule les fractions continues
                //[Remontée sous forme de hcodes
                Dictionary<string, Dictionary<int, List<string>>> v_groupePointsOrdonnes_parNiveau = new Dictionary<string, Dictionary<int, List<string>>>();
                Dictionary<int, List<string>> v_seriesPointsDuNiveau;
                string v_niveauCourant;
                foreach (KeyValuePair<string,Dictionary<string, BeanFractionCourbe >> v_niveau in p_fractionCourbesParNiveau)
                {
                    v_niveauCourant = v_niveau.Key;
                    v_seriesPointsDuNiveau = GetFractionsContinuesDeLaCourbe(v_niveau.Value);
                    v_groupePointsOrdonnes_parNiveau.Add(v_niveauCourant, v_seriesPointsDuNiveau);
                }
                
                //On récupère les coordonnées:
            

                Dictionary<string, double[]> v_coordParPoint = new Dictionary<string, double[]>();
                List<BeanFractionCourbe> v_toutesFractions= p_fractionCourbesParNiveau.Values.SelectMany(c => c.Values).ToList();  
                foreach(BeanFractionCourbe v_fr in v_toutesFractions)
                {
                    if(!v_coordParPoint.ContainsKey(v_fr.p01_point_1.p00_hcodePoint))
                    {
                        v_coordParPoint.Add(v_fr.p01_point_1.p00_hcodePoint, new double[2] { v_fr.p01_point_1.p01_coordX, v_fr.p01_point_1.p02_coordY });
                    }
                    if (!v_coordParPoint.ContainsKey(v_fr.p02_point_2.p00_hcodePoint))
                    {
                        v_coordParPoint.Add(v_fr.p02_point_2.p00_hcodePoint, new double[2] { v_fr.p02_point_2.p01_coordX, v_fr.p02_point_2.p02_coordY });
                    }
                }
                v_seriesCoordPoints_parNiveau = v_groupePointsOrdonnes_parNiveau.ToDictionary(c => c.Key, c => new Dictionary<int, List<double[]>> ());
                foreach(KeyValuePair<string, Dictionary<int, List<string>>> v_niveau in v_groupePointsOrdonnes_parNiveau)
                {
                    foreach(KeyValuePair<int, List<string>> v_courbe in v_niveau.Value)
                    {
                        v_seriesCoordPoints_parNiveau[v_niveau.Key].Add(v_courbe.Key, new List<double[]>());
                        foreach(string v_hcpt in v_courbe.Value)
                        {
                            v_seriesCoordPoints_parNiveau[v_niveau.Key][v_courbe.Key].Add(v_coordParPoint[v_hcpt]);
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return v_seriesCoordPoints_parNiveau;
        }

        internal Dictionary<int, List<string>> GetFractionsContinuesDeLaCourbe(Dictionary<string,BeanFractionCourbe> p_fractionsDeMemeNiveau)
        {
            Dictionary<int, List<string>> v_pointsOrdonnesParGroupe = new Dictionary<int, List<string>>();
            try
            {
                //On référence les  points de chaque arc
                Dictionary<string, List<string>> v_pointsExtremesParArc;
                v_pointsExtremesParArc = p_fractionsDeMemeNiveau.ToDictionary(c => c.Key, c => new List<string>() { c.Value.p01_point_1.p00_hcodePoint, c.Value.p02_point_2.p00_hcodePoint });

                //et les arcs disponibles pour chaque point (cette liste se réduit au fil du traitement)
                Dictionary<string, HashSet<string>> v_arcsDisponiblesParHCodePoint;
                List<string> v_tousHCodePoint= v_pointsExtremesParArc.Values.SelectMany(c => c).Distinct().OrderBy(c => c).ToList();
                v_arcsDisponiblesParHCodePoint = v_tousHCodePoint.Distinct().OrderBy(c => c).ToDictionary(c => c, c => new HashSet<string>());
                foreach(KeyValuePair<string,BeanFractionCourbe> v_fr in p_fractionsDeMemeNiveau)
                {
                    v_arcsDisponiblesParHCodePoint[v_fr.Value.p01_point_1.p00_hcodePoint].Add(v_fr.Key);
                    v_arcsDisponiblesParHCodePoint[v_fr.Value.p02_point_2.p00_hcodePoint].Add(v_fr.Key);
                }

                //On initialise le suivi des arcs 
                Dictionary<string, bool> v_etatAvancementTraitementDesArcs;
                v_etatAvancementTraitementDesArcs = p_fractionsDeMemeNiveau.ToDictionary(c => c.Key, c => false);

                //TRAITEMENT:
                //Prncipe:
                //On choisit un point de départ: par priorité un noeud pendant s'il en existe sinon un noeud dense (plus de 2 arcs) s'il y en a 
                //sinon n'importe quel point ayant encore un arc disponible.
                //On cherche tous les points connectés à ce point originel jusqu'à :
                //soit qu'on revienne au point de départ
                //soit qu'il n'y ait plus d'arc.
                //Dans ce cas, on teste s'il existe encore des candidats points de départ (la liste de leurs arcs dispo est mise à jour au fil des traitements)
                //et on poursuit...
             
              
                string v_hCpointDepart = GetUnPointDeDepart(v_arcsDisponiblesParHCodePoint);  
                int v_idGroupe = 1;
                while (v_hCpointDepart!=null)
                {
                 
                    if(v_hCpointDepart==null)
                    {
                        break;
                    }
                    //
                    List<string> v_pointsOrdonneesDuGroupe;
                    v_pointsOrdonneesDuGroupe = GetHCPointsOrdonnesDuGroupe(v_hCpointDepart, ref v_arcsDisponiblesParHCodePoint, ref v_etatAvancementTraitementDesArcs, v_pointsExtremesParArc, p_fractionsDeMemeNiveau);
                    //
                    v_pointsOrdonnesParGroupe.Add(v_idGroupe, v_pointsOrdonneesDuGroupe);
                    //
                    v_hCpointDepart = GetUnPointDeDepart(v_arcsDisponiblesParHCodePoint);
                    v_idGroupe++;
                }
            }
            catch (Exception)
            {

                throw;
            }
            return v_pointsOrdonnesParGroupe;
        }
        private List<string> GetHCPointsOrdonnesDuGroupe(string p_hCpointDepart, ref Dictionary<string, HashSet<string>> p_arcsParHCodePoint, ref Dictionary<string, bool> p_etatAvancementTraitementDesArcs, Dictionary<string, List<string>> p_pointsExtremesParArc, Dictionary<string, BeanFractionCourbe> p_fractionsDeMemeNiveau)
        {
            List<string> v_listeOrdonneesDesPointsDuGroupe = new List<string>();
            try
            {
                string v_hCpointCourant= p_hCpointDepart;
                v_listeOrdonneesDesPointsDuGroupe.Add(p_hCpointDepart);
                while (true)
                {
                    string v_hCodeArcDispo = GetUnArcDisponiblePourLePoint(v_hCpointCourant, p_arcsParHCodePoint, p_etatAvancementTraitementDesArcs);
                    if (v_hCodeArcDispo == null)
                    {
                        break;
                    }
                    string v_hCpointSuivant = GetHCodePointSuivant(v_hCpointCourant, p_fractionsDeMemeNiveau[v_hCodeArcDispo]);
                    //On ajoute le point à la liste
                    v_listeOrdonneesDesPointsDuGroupe.Add(v_hCpointSuivant);

                    //On MAJ l'état d'avancement et on enlève la référence à l'arc pour les 2 points concernés:
                    p_etatAvancementTraitementDesArcs[v_hCodeArcDispo] = true;
                    p_arcsParHCodePoint[p_pointsExtremesParArc[v_hCodeArcDispo].First()].Remove(v_hCodeArcDispo);
                    p_arcsParHCodePoint[p_pointsExtremesParArc[v_hCodeArcDispo].Last()].Remove(v_hCodeArcDispo);

                    //Si retour au point initial=>on arrête pour ce groupe (boucle/polygone)
                    if (v_hCpointSuivant == p_hCpointDepart)
                    {
                        break;
                    }
                    v_hCpointCourant = v_hCpointSuivant;
                }
            }
            catch (Exception)
            {
                throw;
            }
            return v_listeOrdonneesDesPointsDuGroupe;
        }
        private string GetUnPointDeDepart(Dictionary<string, HashSet<string>> p_arcsParHCodePoint)
        {
            string v_hCpointDepart = null;
            try
            {
                Dictionary<string, HashSet<string>> v_pointsAvecArcsUtiles;
                v_pointsAvecArcsUtiles = p_arcsParHCodePoint.Where(c => c.Value.Count > 0).ToDictionary(c => c.Key, c => c.Value);
                if (v_pointsAvecArcsUtiles.Count == 0)
                {
                    return null;
                }

                //On privilégie, par ordre,
                //- les 'points d'extrémité' (=liés à un seul arc)
                //- les 'noeuds' (liés à plus de 2 arcs)
                //les autres cas (strictement 2 arcs) s'inscrivent forcément dans une boucle simple, et on peut prendre n'importe lequel
                List<string> v_pointsPrioriraires;
                v_pointsPrioriraires = v_pointsAvecArcsUtiles.Where(c => c.Value.Count == 1).Select(c => c.Key).ToList();
                if (v_pointsPrioriraires.Count == 0)
                {
                    v_pointsPrioriraires = v_pointsAvecArcsUtiles.Where(c => c.Value.Count > 2).Select(c => c.Key).ToList();
                }
                if (v_pointsPrioriraires.Count == 0)
                {
                    v_hCpointDepart = v_pointsAvecArcsUtiles.First().Key;
                }
                else
                {
                    v_hCpointDepart = v_pointsPrioriraires.First();
                }
            }
            catch (Exception)
            {

                throw;
            }
            return v_hCpointDepart;
        }
        private string GetUnArcDisponiblePourLePoint(string p_pointSource, Dictionary<string, HashSet<string>> p_arcsParHCodePoint, Dictionary<string, bool> p_etatAvancementTraitementDesArcs)
        {
            string v_arcOut = null;
            try
            {
                foreach(string v_codeArc in p_arcsParHCodePoint[p_pointSource])
                {
                    if(!p_etatAvancementTraitementDesArcs[v_codeArc])
                    {
                        return v_codeArc;
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
            return v_arcOut;
        }
        private string GetHCodePointSuivant(string p_hCodePointSource, BeanFractionCourbe p_arc)
        {
            string v_retour = null;
            try
            {
                if(p_arc.p01_point_1.p00_hcodePoint== p_hCodePointSource)
                {
                    return p_arc.p02_point_2.p00_hcodePoint;
                }
                if (p_arc.p02_point_2.p00_hcodePoint == p_hCodePointSource)
                {
                    return p_arc.p01_point_1.p00_hcodePoint;
                }
            }
            catch (Exception)
            {

                throw;
            }
            return v_retour;
        }
    }
}
