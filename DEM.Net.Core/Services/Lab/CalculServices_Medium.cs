// CalculServices_Medium.cs
//
// Author:
//       Frédéric Aubin
//
// Copyright (c) 2019 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using DEM.Net.Core.Services.VisualisationServices;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core.Services.Lab
{
    public class CalculServices_Medium : ICalculServices_Medium, ICalculServicesMedium_testDivers
    {
        #region TIN
        public BeanParametresDuTin GetParametresDuTinParDefaut()
        {
            BeanParametresDuTin v_parametresDuTin = new BeanParametresDuTin();
            try
            {
                v_parametresDuTin.p11_initialisation_determinationFrontieres = enumModeDelimitationFrontiere.mboSimple;
                v_parametresDuTin.p12_extensionSupplementaireMboEnM = 2000;
                v_parametresDuTin.p13_modeCalculZParDefaut = enumModeCalculZ.alti_0;
                v_parametresDuTin.p14_altitudeParDefaut = 0;
                //
                v_parametresDuTin.p16_initialisation_modeChoixDuPointCentral.p00_methodeChoixDuPointCentral = enumMethodeChoixDuPointCentral.pointLePlusExcentre;
                //
                v_parametresDuTin.p21_enrichissement_modeChoixDuPointCentral.p00_methodeChoixDuPointCentral = enumMethodeChoixDuPointCentral.pointLePlusExcentre;
                v_parametresDuTin.p21_enrichissement_modeChoixDuPointCentral.p01_excentrationMinimum = 20;
                //
            }
            catch (Exception)
            {

                throw;
            }
            return v_parametresDuTin;
        }
        /// <summary>
        /// Partant d'une liste de points de dimension '2D1/2' (2 composantes X et Y + un attribut d'élévation)
        /// on initialise le TIN:
        /// - en créant des facettes triangulaires à partir:
        ///    -du point N d'élévation maximum
        ///    -des points du convexhull des points projetés dans le plan P0, d'élévation constante, min des élévations
        ///    (et excluant le point N, s'il appartient au convexhull)
        /// - en les 'associant' les unes aux autres par l'intermédiaire de leurs arcs partagés
        ///   (on vt savoir, pour chaque 'facette', qu'elles sont ses 'voisines'.
        /// - en effectuant la partition des points entre les facettes du TIN.
        /// Usage: va permettre d'amorcer la recherche des points saillants.
        /// </summary>
        /// <param name="p_points"></param>
        /// <returns></returns>
        public BeanTopologieFacettes GetInitialisationTin(List<BeanPoint_internal> p_points, BeanParametresDuTin p_parametresDuTin)
        {
            BeanTopologieFacettes v_topologieFacette = null;
            try
            {
                //
                if (p_parametresDuTin == null)
                {
                    p_parametresDuTin = GetParametresDuTinParDefaut();
                }

                //1-Extraction des points frontières:
                List<BeanPoint_internal> v_pointsFrontieres = null;
                switch (p_parametresDuTin.p11_initialisation_determinationFrontieres)
                {
                    case enumModeDelimitationFrontiere.convexHull:
                        v_pointsFrontieres = GetConvexHull2D(p_points);
                        break;
                    case enumModeDelimitationFrontiere.mboSimple:
                        if (p_parametresDuTin.p13_modeCalculZParDefaut == enumModeCalculZ.alti_saisie)
                        {
                            v_pointsFrontieres = GetMbo2D(p_points, p_parametresDuTin.p14_altitudeParDefaut, p_parametresDuTin.p12_extensionSupplementaireMboEnM);
                        }
                        else
                        {
                            v_pointsFrontieres = GetMbo2D(p_points, p_parametresDuTin.p13_modeCalculZParDefaut, p_parametresDuTin.p12_extensionSupplementaireMboEnM);
                        }
                        break;
                    case enumModeDelimitationFrontiere.pointsProchesDuMbo:
                        v_pointsFrontieres = GetMboPointsProches(p_points, p_parametresDuTin.p15_nbrePointsSupplMultiples4);
                        break;
                    default:
                        throw new Exception("Méthode " + p_parametresDuTin.p11_initialisation_determinationFrontieres + "non implémentée.");
                }
                //
                //2-Extraction du 'meilleur point'
                BeanPoint_internal v_meilleurPoint;
                switch (p_parametresDuTin.p16_initialisation_modeChoixDuPointCentral.p00_methodeChoixDuPointCentral)
                {
                    case enumMethodeChoixDuPointCentral.pointLePlusExcentre:
                        double v_MaxAbs = Math.Max(p_points.Select(c => c.p10_coord[2]).Max(), Math.Abs(p_points.Select(c => c.p10_coord[2]).Min()));
                        v_meilleurPoint = p_points.Where(c => Math.Abs(c.p10_coord[2]) == v_MaxAbs).First();                  
                        break;
                    default:
                        throw new Exception("Méthode " + p_parametresDuTin.p16_initialisation_modeChoixDuPointCentral.p00_methodeChoixDuPointCentral + "non implémentée.");
                }

                //3-Calcul les facettes du convexHull étendu au point d'altitude maxi.
                List<BeanFacette_internal> v_facettesInitiales;
                v_facettesInitiales = GetFacettesInitialesByPolygoneConvexe(v_pointsFrontieres, v_meilleurPoint, p_points);

                //4-On injecte dans le bean topologie
                v_topologieFacette = new BeanTopologieFacettes(p_points);
              
                v_topologieFacette.PointsFacAjouter(v_pointsFrontieres);
                v_topologieFacette.PointFacAjouter(v_meilleurPoint);
                //
                List<BeanArc_internal> v_arcsFacette = v_facettesInitiales.SelectMany(c => c.p02_arcs).Distinct().ToList();
                v_topologieFacette.ArcsAjouter(v_arcsFacette);

                v_topologieFacette.FacettesAjouter(v_facettesInitiales);
            }
            catch (Exception)
            {

                throw;
            }
            return v_topologieFacette;
        }
        public void AugmenteDetailsTinByRef(ref BeanTopologieFacettes p_topologieFacette, BeanParametresDuTin p_parametresDuTin)
        {
            try
            {
                //On initialise le plus grand écart sur les facettes existantes
                //(ATTENTION: il vaudrait mieux le faire lors de l'étape précédente.
                //? L'étape d'augmentation de la précision pourrait être rejouée plusieurs fois en augmentant l'écart minimum.
                //
                BeanFacette_internal v_facette;
                bool v_renvoyerNullSiInfALExtensionMinimale_vf = false;
                foreach (int v_idFacette in p_topologieFacette.p13_facettesById.Keys)
                {
                    v_facette = p_topologieFacette.p13_facettesById[v_idFacette];
                    GetAndSetByRefPointExcentreDeLaFacette(ref v_facette, p_parametresDuTin.p16_initialisation_modeChoixDuPointCentral, v_renvoyerNullSiInfALExtensionMinimale_vf);
                }

                //On crée une liste chaînée des facettes sur le critère de plus grand écart, triée par ordre décroissant.
                //?1-On va toujours traiter la facette avec l'écart maxi (donc celle en tête de liste)
                //2-On va être amené à supprimer et à rajouter des facettes=>il semble souhaitable d'éviter les recréations des listes
                //Note encore: On ne garde dans le tri que les facettes avec un écart sup à l'écart minimum souhaité (inutile de trier les autres)

                List<BeanFacette_internal> v_facettesTriees;
                v_facettesTriees = p_topologieFacette.p13_facettesById.OrderByDescending(c => c.Value.p21_plusGrandEcartAbsolu).Select(c => c.Value).ToList();
                int v_indice = 0;
                for (; v_indice < v_facettesTriees.Count - 1; v_indice++)
                {
                    if (v_facettesTriees[v_indice + 1].p21_plusGrandEcartAbsolu < p_parametresDuTin.p21_enrichissement_modeChoixDuPointCentral.p01_excentrationMinimum)
                    {
                        break;
                    }
                    if (v_indice > 0)
                    {
                        v_facettesTriees[v_indice].p23_facetteEcartSup = v_facettesTriees[v_indice - 1];
                    }
                    v_facettesTriees[v_indice].p24_facetteEcartInf = v_facettesTriees[v_indice + 1];
                }
                if (v_indice > 0)
                {
                    v_facettesTriees[v_indice].p23_facetteEcartSup = v_facettesTriees[v_indice - 1];
                    //=>+ Référencement de la première cellule
                    p_topologieFacette.p21_facetteAvecEcartAbsoluMax = v_facettesTriees.First();

                    //On parcourt la liste chaînée:
                    do
                    {
                        TraitementDeLaFacetteMaxiByRef(ref p_topologieFacette, p_topologieFacette.p21_facetteAvecEcartAbsoluMax, p_parametresDuTin);
                    }
                    while (p_topologieFacette.p21_facetteAvecEcartAbsoluMax != null);
                }
            }
            catch (Exception)
            {
                throw;
            }

        }
        #endregion TIN

        public List<BeanPoint_internal> GetOrdonnancementPointsFacette(List<BeanPoint_internal> p_pointsFacettes, bool p_renvoyerNullSiColineaires_vf, bool p_sensHoraireSinonAntiHoraire_vf)
        {
            List<BeanPoint_internal> v_pointsOrdonnances = new List<BeanPoint_internal>();
            try
            {
                Dictionary<int, double[]> v_pointsAOrdonnancer = p_pointsFacettes.ToDictionary(c => c.p00_id, c => c.p10_coord);
                List<int> v_idOrdonnances = FLabServices.createCalculLow().GetOrdonnancement(v_pointsAOrdonnancer, p_renvoyerNullSiColineaires_vf, p_sensHoraireSinonAntiHoraire_vf);
                if (p_renvoyerNullSiColineaires_vf && v_idOrdonnances == null)
                {
                    return null;
                }
                foreach (int v_id in v_idOrdonnances)
                {
                    v_pointsOrdonnances.Add(p_pointsFacettes.Where(c => c.p00_id == v_id).First());
                }
            }
            catch (Exception)
            {

                throw;
            }
            return v_pointsOrdonnances;
        }

        #region SERVICES GEOMETRIQUES DIVERS (Convex hull, mbo, centroïdes,...)
        public List<BeanPoint_internal> GetConvexHull2D(IEnumerable<BeanPoint_internal> p_points)
        {
            List<BeanPoint_internal> p_pointsOrdonnesConvexHull = new List<BeanPoint_internal>();
            try
            {
                //1-On créé un segment S0 unissant les points extrêmes du nuage:
                double[] v_centroide = GetCentroide(p_points);
                //
                BeanPoint_internal p_point0;
                p_point0 = GetIdPointLePlusEloigneDuPointRef(p_points, v_centroide);
                BeanPoint_internal p_point0_Oppose;
                p_point0_Oppose = GetIdPointLePlusEloigneDuPointRef(p_points, p_point0.p10_coord);
                //
                ICalculServices_Low v_calcul = new CalculServices_Low();
                Dictionary<int, double[]> v_coordDansRef;

                //2-On effectue une partition du plan délimité par la droite passant par les 2 pts de S0:
                v_coordDansRef = GetCoordonneesDansNewReferentiel2D(p_points, p_point0.p10_coord, p_point0_Oppose.p10_coord, null);
                HashSet<int> v_idPositifs = new HashSet<int>(v_coordDansRef.Where(c => c.Value[1] > 0).Select(c => c.Key).ToList());
                //(Les points '0' ne nous intéressent pas: ils ne peuvent pas appartenir au CH (sauf les 2 extrêmes déjà identifiés)
                List<BeanPoint_internal> v_pointsPositifs = p_points.Where(c => v_idPositifs.Contains(c.p00_id)).ToList();
                List<BeanPoint_internal> v_pointsNegatifs = p_points.Where(c => !v_idPositifs.Contains(c.p00_id)).ToList();


                p_pointsOrdonnesConvexHull.Add(p_point0);

                //3-On créé 2 arcs orientés à partie de S0 et on les injecte dans une 'pile':
                Stack<BeanArc_internal> v_pileLifo = new Stack<BeanArc_internal>();
                //ATTENTION à l'ordre d'injection (voir plus bas)

                BeanArc_internal v_arcDescendant = new BeanArc_internal(p_point0_Oppose, p_point0, v_pointsNegatifs);
                v_pileLifo.Push(v_arcDescendant);
                BeanArc_internal v_arcMontant = new BeanArc_internal(p_point0, p_point0_Oppose, v_pointsPositifs);
                v_pileLifo.Push(v_arcMontant);

                //4-On effectue une recherche en profondeur depuis la 'gauche' de l'arbre vers la 'droite'
                //=>Sur le premier arc A1, on cherche le point le + excentré à gauche, on en déduit 2 arcs A2 et A3 
                //et on récupère les pts à gauche de ces arcs
                //récursivement sur l'arc A2 jusqu'à ce qu'il n'y ait plus de pts extérieurs, puis arc A3,...
                //ATTENTION: l'ordre d'insertion dans la pile est important.
                BeanArc_internal v_arcToExplore;
                List<BeanArc_internal> v_arcsResultants;
                int p_compteurPasses = 0;
                while (v_pileLifo.Count > 0)
                {
                    p_compteurPasses++;
                    v_arcToExplore = v_pileLifo.Pop();
                    v_arcsResultants = GetArcsConvexesGauches(v_arcToExplore);

                    //S'il n'y a pas d'arcs 'plus à gauche', alors le point terminal appartient au convexhull
                    //Sinon, on met les arcs dans la pile.
                    if (v_arcsResultants == null)
                    {
                        p_pointsOrdonnesConvexHull.Add(v_arcToExplore.p12_pointFin);
                    }
                    else
                    {
                        v_pileLifo.Push(v_arcsResultants[1]);
                        v_pileLifo.Push(v_arcsResultants[0]);
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
            return p_pointsOrdonnesConvexHull;
        }
        public List<BeanPoint_internal> GetMbo2D(IEnumerable<BeanPoint_internal> p_points, enumModeCalculZ p_modeDeCalculZ, double p_extensionMboEnM)
        {
            List<BeanPoint_internal> p_pointsOrdonnesMbo = new List<BeanPoint_internal>();
            try
            {
                double v_alti;
                switch (p_modeDeCalculZ)
                {
                    case enumModeCalculZ.alti_min:
                        v_alti = p_points.Min(c => c.p10_coord[2]);
                        break;
                    case enumModeCalculZ.alti_0:
                        v_alti = 0;
                        break;
                    default:
                        throw new Exception("Méthode " + p_modeDeCalculZ + " non implémentée.");
                }
                //
                p_pointsOrdonnesMbo = GetMbo2D(p_points, v_alti, p_extensionMboEnM);
            }
            catch (Exception)
            {

                throw;
            }
            return p_pointsOrdonnesMbo;
        }
        public List<BeanPoint_internal> GetMbo2D(IEnumerable<BeanPoint_internal> p_points, double p_altiZ, double p_extensionMboEnM)
        {
            List<BeanPoint_internal> p_pointsOrdonnesMbo = new List<BeanPoint_internal>();
            try
            {
                int v_srid = p_points.First().p11_srid;
                //
                double v_minX = p_points.Min(c => c.p10_coord[0]) - p_extensionMboEnM;
                double v_minY = p_points.Min(c => c.p10_coord[1]) - p_extensionMboEnM;
                double v_maxX = p_points.Max(c => c.p10_coord[0]) + p_extensionMboEnM;
                double v_maxY = p_points.Max(c => c.p10_coord[1]) + p_extensionMboEnM;
                //
                BeanPoint_internal v_point;
                //Bas gauche
                v_point = new BeanPoint_internal(v_minX, v_minY, p_altiZ, v_srid);
                p_pointsOrdonnesMbo.Add(v_point);
                //Haut gauche
                v_point = new BeanPoint_internal(v_minX, v_maxY, p_altiZ, v_srid);
                p_pointsOrdonnesMbo.Add(v_point);
                //Haut droit
                v_point = new BeanPoint_internal(v_maxX, v_maxY, p_altiZ, v_srid);
                p_pointsOrdonnesMbo.Add(v_point);
                //Bas droit
                v_point = new BeanPoint_internal(v_maxX, v_minY, p_altiZ, v_srid);
                p_pointsOrdonnesMbo.Add(v_point);
                //Bas gauche
                v_point = new BeanPoint_internal(v_minX, v_minY, p_altiZ, v_srid);
                p_pointsOrdonnesMbo.Add(v_point);
            }
            catch (Exception)
            {

                throw;
            }
            return p_pointsOrdonnesMbo;
        }
        public List<BeanPoint_internal> GetMboPointsProches(IEnumerable<BeanPoint_internal> p_points, int p_nbrePointsCalageSupplSouhaitesMultiplesDe4)
        {
            List<BeanPoint_internal> v_pointsOut = new List<BeanPoint_internal>();
            try
            {
                int v_idPointOut;
                List<BeanPoint_internal> p_pointsOrdonnesMbo = new List<BeanPoint_internal>();
                int v_srid = p_points.First().p11_srid;
                //
                double v_minX = p_points.Min(c => c.p10_coord[0]);
                double v_minY = p_points.Min(c => c.p10_coord[1]);
                double v_maxX = p_points.Max(c => c.p10_coord[0]);
                double v_maxY = p_points.Max(c => c.p10_coord[1]);
                //
                HashSet<int> v_pointsDejaTraites = new HashSet<int>();
                Dictionary<int, BeanPoint_internal> v_dicoPointsSource = p_points.ToDictionary(c => c.p00_id, c => c);
                Dictionary<int, double[]> v_pointsATester = p_points.ToDictionary(c => c.p00_id, c => c.p10_coord);
                //
                double[] v_coordPointAppui;
                List<double[]> v_listePointsDAppui = new List<double[]>();

                v_coordPointAppui = new double[2] { v_minX, v_minY };
                v_listePointsDAppui.Add(v_coordPointAppui);

                v_coordPointAppui = new double[2] { v_minX, v_maxY };
                v_listePointsDAppui.Add(v_coordPointAppui);

                v_coordPointAppui = new double[2] { v_maxX, v_maxY };
                v_listePointsDAppui.Add(v_coordPointAppui);

                v_coordPointAppui = new double[2] { v_maxX, v_minY };
                v_listePointsDAppui.Add(v_coordPointAppui);

                //
                if (p_nbrePointsCalageSupplSouhaitesMultiplesDe4 > 0)
                {
                    int v_nbrePointsSupplParArete = (int)Math.Ceiling(p_nbrePointsCalageSupplSouhaitesMultiplesDe4 / 4d);
                    double v_ecartX = v_maxX - v_minX;
                    double v_ecartY = v_maxY - v_minY;
                    double v_decalageEnX = v_ecartX / (v_nbrePointsSupplParArete + 1);
                    double v_decalageEnY = v_ecartY / (v_nbrePointsSupplParArete + 1);
                    for (int v_nbrePointsSupp = 1; v_nbrePointsSupp <= v_nbrePointsSupplParArete; v_nbrePointsSupp++)
                    {
                        v_coordPointAppui = new double[2] { v_minX + (v_nbrePointsSupp * v_decalageEnX), v_minY };
                        v_listePointsDAppui.Add(v_coordPointAppui);
                        v_coordPointAppui = new double[2] { v_minX + (v_nbrePointsSupp * v_decalageEnX), v_maxY };
                        v_listePointsDAppui.Add(v_coordPointAppui);
                        v_coordPointAppui = new double[2] { v_minX, v_minY + (v_nbrePointsSupp * v_decalageEnY) };
                        v_listePointsDAppui.Add(v_coordPointAppui);
                        v_coordPointAppui = new double[2] { v_maxX, v_minY + (v_nbrePointsSupp * v_decalageEnY) };
                        v_listePointsDAppui.Add(v_coordPointAppui);
                    }
                }
                //

                foreach (double[] v_pointDAppui in v_listePointsDAppui)
                {
                    v_idPointOut = FLabServices.createCalculLow().GetPointLePlusProcheDePoint0XY(v_pointsATester, v_pointDAppui);
                    if (!v_pointsDejaTraites.Contains(v_idPointOut))
                    {
                        v_pointsDejaTraites.Add(v_idPointOut);
                        v_pointsOut.Add(v_dicoPointsSource[v_idPointOut]);
                    }
                }
                //A REVOIR
                v_pointsOut = GetOrdonnancementPointsFacette(v_pointsOut, false, true);
                //(On rajoute le point initial afin de décrire une surface fermée.
                v_pointsOut.Add(v_pointsOut.First());
            }
            catch (Exception)
            {

                throw;
            }
            return v_pointsOut;
        }
         public BeanPoint_internal GetIdPointLePlusEloigneDuPointRef(IEnumerable<BeanPoint_internal> p_points, double[] p_pointRef)
        {
            BeanPoint_internal v_point;
            try
            {
                ICalculServices_Low v_calcul = new CalculServices_Low();
                double v_distanceMax = p_points.Select(c => v_calcul.GetDistanceEuclidienneCarreeXY(c.p10_coord, p_pointRef)).Max();
                v_point = p_points.Where(c => v_calcul.GetDistanceEuclidienneCarreeXY(c.p10_coord, p_pointRef) == v_distanceMax).First();
            }
            catch (Exception)
            {

                throw;
            }
            return v_point;
        }
        public double[] GetCentroide(IEnumerable<BeanPoint_internal> p_points)
        {
            double[] v_centroide = new double[3];
            try
            {
                v_centroide[0] = p_points.Average(c => c.p10_coord[0]);
                v_centroide[1] = p_points.Average(c => c.p10_coord[1]);
                v_centroide[2] = p_points.Average(c => c.p10_coord[2]);
            }
            catch (Exception)
            {
                throw;
            }
            return v_centroide;
        }
        #endregion SERVICES GEOMETRIQUES DIVERS (Convex hull, mbo, centroïdes,...)

        #region PRIVATE CALCUL TIN


        private void TraitementDeLaFacetteMaxiByRef(ref BeanTopologieFacettes p_topologieFacette, BeanFacette_internal p_facetteATraiter, BeanParametresDuTin p_parametresDuTin)
        {
            try
            {
                //On traite la "première facette" (c'est à dire celle avec l'écart maxi)
                //A l'intérieur du tétraèdre les modifications ne sont pas possibles (tous les couples de facettes sont concaves)
                BeanResultatConversions_internal v_rapportResultTetraedre;
                v_rapportResultTetraedre = GetTetraedreByFacette(ref p_topologieFacette, p_facetteATraiter.p00_idFacette, p_facetteATraiter.p22_pointPlusGrandEcart);
                List<int> v_idNouvellesFacettesBrutes = new List<int>(v_rapportResultTetraedre.p02_newFacettes.Select(c => c.p00_idFacette));


                //1-On teste si le tétraèdre contient des facettes 'plates' (=ayant moins de 2 dimensions dans le plan xy)
                //En pratique, il en contient, au plus, une si le triangle original n'était pas lui-même 'plat'.
                //On considère seulement les cas pour lesquels de telles 'facettes ' possédent un arc 'frontiere' (extérieur!) ?
                //=>On peut/doit les supprimer toutes de suite.
                //(Les éventuelles 'plates non frontières' seront traités systématiquement à l'étape suivante.
                if (v_rapportResultTetraedre.p02_newFacettes.Where(c => c.p03_estVerticale_vf).Count() > 0)
                {
                    BeanFacette_internal v_facettePlate;
                    v_facettePlate = v_rapportResultTetraedre.p02_newFacettes.Where(c => c.p03_estVerticale_vf).First();
                    //
                    if (v_facettePlate.p02_arcs.Where(c => c.p20_statutArc == enumStatutArc.arcExterne).Count() > 0)
                    {
                        List<BeanArc_internal> v_arcsNonExternes = v_facettePlate.p02_arcs.Where(c => c.p20_statutArc != enumStatutArc.arcExterne).ToList();
                        v_arcsNonExternes[0].p20_statutArc = enumStatutArc.arcExterne;
                        if (v_arcsNonExternes[0].p21_facetteGauche.p00_idFacette == v_facettePlate.p00_idFacette)
                        {
                            v_arcsNonExternes[0].p21_facetteGauche = null;
                        }
                        else
                        {
                            v_arcsNonExternes[0].p22_facetteDroite = null;
                        }
                        //
                        v_arcsNonExternes[1].p20_statutArc = enumStatutArc.arcExterne;
                        if (v_arcsNonExternes[1].p21_facetteGauche.p00_idFacette == v_facettePlate.p00_idFacette)
                        {
                            v_arcsNonExternes[1].p21_facetteGauche = null;
                        }
                        else
                        {
                            v_arcsNonExternes[1].p22_facetteDroite = null;
                        }
                        //
                        BeanArc_internal v_arcASupprimer = v_facettePlate.p02_arcs.Where(c => c.p20_statutArc == enumStatutArc.arcExterne).ToList().First();
                        p_topologieFacette.ArcSupprimer(v_arcASupprimer);
                        p_topologieFacette.FacetteSupprimer(v_facettePlate);
                    }
                }


                //Maintenant...on doit tester le contact de chaque facette du tétraèdre avec les facettes extérieures, si elles existent.
                //Cela doit se faire autour des arètes extérieures
                //Certains couples seront modifiés, d'autres pas (mais ils auront été qd même contrôlés et validés).
                //Lorsqu'il y a modif du couple (bascule des triangles du quadrilatère), 
                //les couples périphériques deviennent, eux-mêmes, candidats à la bascule.
                //On remonte les arcs du bord du tétraèdre et non situés sur la bordure extérieure
                List<string> v_hcodeArcsATester = v_rapportResultTetraedre.p03_arcsCandidatsOut.Select(c => c.p01_hcodeArc).ToList();
                HashSet<string> v_HSCodesArcsATester = new HashSet<string>(v_hcodeArcsATester);

                //=>Pour chacun de ces arcs, on effectue le contrôle de Delaunay ET s'il y a lieu, on effectue la bascule
                BeanResultatConversions_internal v_rapportResultBascule;
                string v_codeArcCandidat;
                for (int v_indiceArc = 0; v_indiceArc < v_hcodeArcsATester.Count; v_indiceArc++)
                {
                    v_codeArcCandidat = v_hcodeArcsATester[v_indiceArc];
                    if (p_topologieFacette.p12_arcsByCode.ContainsKey(v_codeArcCandidat))
                    {
                        v_rapportResultBascule = TestEtBascule_DelaunayByRef(ref p_topologieFacette, v_codeArcCandidat);
                        if (v_rapportResultBascule.p00_modif_vf)
                        {
                            foreach (BeanArc_internal v_newArcCandidat in v_rapportResultBascule.p03_arcsCandidatsOut)
                            {
                                //FAU correction 22/04/2019
                                if(v_newArcCandidat.p20_statutArc!=enumStatutArc.arcExterne)
                                {
                                    v_codeArcCandidat = v_newArcCandidat.p01_hcodeArc;
                                    if (!v_HSCodesArcsATester.Contains(v_codeArcCandidat))
                                    {
                                        v_HSCodesArcsATester.Add(v_codeArcCandidat);
                                        v_hcodeArcsATester.Add(v_codeArcCandidat);
                                    }
                                }                            
                            }
                            v_idNouvellesFacettesBrutes.AddRange(v_rapportResultBascule.p02_newFacettes.Select(c => c.p00_idFacette));
                        }
                    }
                }

                //On calcule l'excentration de chaque nouvelle facette et on l'introduit dans la chaine de tri
                double v_ecartMini = p_parametresDuTin.p21_enrichissement_modeChoixDuPointCentral.p01_excentrationMinimum;
                v_idNouvellesFacettesBrutes = v_idNouvellesFacettesBrutes.Distinct().ToList();
                BeanFacette_internal v_facettePourMaj;
                bool v_nullSiInfEcentrationMinimale_vf = false;

               foreach (int v_idNewFacette in v_idNouvellesFacettesBrutes)
                {
                    //[on doit tester car certaines des facettes listées originellement ont été supprimées s'il y a eu des 'bascules'
                    if (!p_topologieFacette.p13_facettesById.ContainsKey(v_idNewFacette))
                    {
                        continue;
                    }
                    v_facettePourMaj = p_topologieFacette.p13_facettesById[v_idNewFacette];
                    GetAndSetByRefPointExcentreDeLaFacette(ref v_facettePourMaj, p_parametresDuTin.p21_enrichissement_modeChoixDuPointCentral, v_nullSiInfEcentrationMinimale_vf);
                    InsertDansListeChaineeDesFacettes(ref p_topologieFacette, v_facettePourMaj, v_ecartMini);
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        private BeanResultatConversions_internal GetTetraedreByFacette(ref BeanTopologieFacettes p_topologieCible, int p_idFacetteSource, BeanPoint_internal p_pointCentral)
        {
            BeanResultatConversions_internal v_beanRapportOut = new BeanResultatConversions_internal();
            try
            {
                v_beanRapportOut.p00_modif_vf = true;
                v_beanRapportOut.p01_idFacettesSupprimees.Add(p_idFacetteSource);

                //On remonte les données de la facette
                BeanFacette_internal v_facetteSource = p_topologieCible.p13_facettesById[p_idFacetteSource];
                List<BeanPoint_internal> v_pointsFacetteSource = v_facetteSource.p01_pointsDeFacette;
                List<BeanArc_internal> v_arcsBases = v_facetteSource.p02_arcs;
                List<BeanPoint_internal> v_pointsInclus = v_facetteSource.p10_pointsInclus;

                //On marque le 'point central' (=le point le plus excentré en z) comme 'point facette':
                p_pointCentral.p21_estPointFacette_vf = true;
                p_pointCentral.p22_estPointInclus_vf = true;
                p_topologieCible.PointFacAjouter(p_pointCentral);

                //On désaffecte les points inclus (pour permettre leur réaffectation aux nouvelles facettes
                BeanPoint_internal v_pointInclus;
                for (int v_indicePoint = 0; v_indicePoint < v_facetteSource.p10_pointsInclus.Count; v_indicePoint++)
                {
                    v_pointInclus = v_facetteSource.p10_pointsInclus[v_indicePoint];
                    if (!v_pointInclus.p21_estPointFacette_vf)
                    {
                        v_pointInclus.p22_estPointInclus_vf = false;
                    }
                }

                //On initialise les nouveaux arcs 'rayonnants' (=partant du centre vers la périphérie)
                //A VOIR -1?
                BeanArc_internal v_arc;
                List<BeanArc_internal> v_arcsRayonnants = new List<BeanArc_internal>();
                for (int v_indicePointBase = 0; v_indicePointBase < v_pointsFacetteSource.Count; v_indicePointBase++)
                {
                    v_arc = new BeanArc_internal(p_pointCentral, v_pointsFacetteSource[v_indicePointBase]);
                    v_arc.p20_statutArc = enumStatutArc.arcNONCandidatASuppression;

                    v_arcsRayonnants.Add(v_arc);
                    p_topologieCible.ArcAjouter(v_arc);
                    v_beanRapportOut.p04_arcsAExclureOut.Add(v_arc);
                }

                //=>On peut, maintenant, génèrer les facettes
                BeanFacette_internal v_facette;
                List<int> v_indicesPoints = new List<int> { 0, 1, 2, 0 };
                BeanPoint_internal v_point2;
                BeanPoint_internal v_point3;
                BeanArc_internal v_arcDescendant;
                BeanArc_internal v_arcMontant;
                BeanArc_internal v_arcBase;
                List<BeanArc_internal> v_arcCandidatBase;
                List<BeanFacette_internal> v_facettesNonPlates = new List<BeanFacette_internal>();
                for (int i = 0; i < 3; i++)
                {
                    v_point2 = v_pointsFacetteSource[v_indicesPoints[i]];
                    v_point3 = v_pointsFacetteSource[v_indicesPoints[i + 1]];
                    //
                    v_facette = new BeanFacette_internal();
                    v_facette.p01_pointsDeFacette.Add(p_pointCentral);
                    v_facette.p01_pointsDeFacette.Add(v_point2);
                    v_facette.p01_pointsDeFacette.Add(v_point3);
                  
                    v_arcDescendant = v_arcsRayonnants.Where(c => c.p12_pointFin.p01_hCodeGeog == v_point2.p01_hCodeGeog).First();
                    v_arcDescendant.p21_facetteGauche = v_facette;
                    //
                    v_arcMontant = v_arcsRayonnants.Where(c => c.p12_pointFin.p01_hCodeGeog == v_point3.p01_hCodeGeog).First();
                    v_arcMontant.p22_facetteDroite = v_facette;
                    //note: on considère l'arc comme 'montant' mais c'est une 'vue de l'esprit'
                    //(on parcourt le triangle en pensée! Point central->descente vers point 1->longer l'arc extérieur->puis..remonter!)
                    //mais 'physiquement' le sens de cet arc est inverse (du point central vers l'extérieur) 
                    //C'est donc bien le côté droit qui est doit être renseigné 
                    //Le côté 'gauche' est traité dans le triangle connexe pour lequel cet arc sera 'descendant'

                    v_arcCandidatBase = v_arcsBases.Where(c => c.p11_pointDbt.p01_hCodeGeog == v_point2.p01_hCodeGeog && c.p12_pointFin.p01_hCodeGeog == v_point3.p01_hCodeGeog).ToList();
                    if (v_arcCandidatBase.Count > 0)
                    {
                        v_arcBase = v_arcCandidatBase.First();
                    }
                    else
                    {
                        v_arcCandidatBase = v_arcsBases.Where(c => c.p12_pointFin.p01_hCodeGeog == v_point2.p01_hCodeGeog && c.p11_pointDbt.p01_hCodeGeog == v_point3.p01_hCodeGeog).ToList();
                        v_arcBase = v_arcCandidatBase.First();
                    }
                    
                    //Complétage de l'affectation des facettes aux arcs:
                    if (v_arcBase.p21_facetteGauche != null && v_arcBase.p21_facetteGauche.p00_idFacette == p_idFacetteSource)
                    {
                        v_arcBase.p21_facetteGauche = v_facette;
                    }
                    else
                    {
                        v_arcBase.p22_facetteDroite = v_facette;
                    }
                    //Les arcs bases de la facette d'origine, SI ils ne sont pas en frontière de zone, deviennent candidats à suppression
                    if (v_arcBase.p20_statutArc != enumStatutArc.arcExterne)
                    {
                        v_arcBase.p20_statutArc = enumStatutArc.arcCandidatASuppression;
                        v_beanRapportOut.p03_arcsCandidatsOut.Add(v_arcBase);
                    }
                    //
                    v_facette.p02_arcs.Add(v_arcDescendant);
                    v_facette.p02_arcs.Add(v_arcBase);
                    v_facette.p02_arcs.Add(v_arcMontant);

                    //On teste si la facette est 'verticale' (en fait si les points sont alignés dans le plan XY)
                    v_facette.p03_estVerticale_vf= IsTriangleEstPlatDansLePlanXY(v_point2, v_point3, p_pointCentral);
                    //SI la facette est 'verticale' (cas courant lorsque la grille est régulière), la facette n'est pas exploitable.
                    //=>Elle devra subir un traitement spécifique dans le cadre des 'bascules'
                    if(!v_facette.p03_estVerticale_vf)
                    {
                        v_facettesNonPlates.Add(v_facette);
                    }

                    ////Récupération des points inclus:
                    //RattachePointsToFacette(ref v_pointsInclus, ref v_facette);
                    //
                    p_topologieCible.FacetteAjouter(v_facette);

                    v_beanRapportOut.p02_newFacettes.Add(v_facette);
                }
                //On réaffecte les points sur les facettes 'non plates'
                int v_nbreFacettesNonPlates = v_facettesNonPlates.Count;
                if (v_nbreFacettesNonPlates != 2 && v_nbreFacettesNonPlates != 3)
                {
                    throw new Exception("Nbre de facettes 'non plates' (" + v_nbreFacettesNonPlates + " incorrect.");
                }
                if (v_nbreFacettesNonPlates == 2)
                {
                    BeanFacette_internal v_fac1 = v_facettesNonPlates[0];
                    BeanFacette_internal v_fac2 = v_facettesNonPlates[1];
                    RattachePointsToCoupleDeFacettes(ref v_pointsInclus, ref v_fac1, ref v_fac2);
                }
                if (v_nbreFacettesNonPlates == 3)
                {
                    BeanFacette_internal v_facT;
                    foreach (BeanFacette_internal v_fac in v_facettesNonPlates)
                    {
                        v_facT = v_fac;
                        RattachePointsToFacetteInTetraedre(ref v_pointsInclus, ref v_facT);
                    }
                }

                //On supprime la facette d'origine
                p_topologieCible.FacetteSupprimer(p_idFacetteSource);
            }
            catch (Exception)
            {

                throw;
            }
            return v_beanRapportOut;
        }
        private bool IsTriangleEstPlatDansLePlanXY(BeanPoint_internal p_point1, BeanPoint_internal p_point2, BeanPoint_internal p_point3)
        {
            bool v_retour = true;
            try
            {
                v_retour=FLabServices.createCalculLow().AreDroitesParallelesXY(p_point1.p10_coord, p_point2.p10_coord, p_point1.p10_coord, p_point3.p10_coord);
            }
            catch (Exception)
            {
                throw;
            }
            return v_retour;
        }
        
        private BeanResultatConversions_internal TestEtBascule_DelaunayByRef(ref BeanTopologieFacettes p_topologieFacette, string p_hcodeArcCandidatASuppression)
        {
            BeanResultatConversions_internal v_beanRapportOut = new BeanResultatConversions_internal();
            try
            {
                //A-RECUP des DONNEES UTILES

                //L'arc sépare 2 triangles juxtaposés formant un quadrilatère.
                //On récupère cet arc...
                BeanArc_internal v_arcToTest = p_topologieFacette.p12_arcsByCode[p_hcodeArcCandidatASuppression];

                //...et les 2 autres points du 'quadrilatère'
                BeanFacette_internal v_facetteGauche = v_arcToTest.p21_facetteGauche;
                BeanFacette_internal v_facetteDroite = v_arcToTest.p22_facetteDroite;
                BeanPoint_internal v_pointGaucheNewArc = v_facetteGauche.p01_pointsDeFacette.Where(c => c.p01_hCodeGeog != v_arcToTest.p11_pointDbt.p01_hCodeGeog && c.p01_hCodeGeog != v_arcToTest.p12_pointFin.p01_hCodeGeog).First();
                BeanPoint_internal v_pointDroitNewArc = v_facetteDroite.p01_pointsDeFacette.Where(c => c.p01_hCodeGeog != v_arcToTest.p11_pointDbt.p01_hCodeGeog && c.p01_hCodeGeog != v_arcToTest.p12_pointFin.p01_hCodeGeog).First();

                //PHASE A: TESTS - Faut-il 'basculer' (=privilégier les 2 autres triangles séparés par la 2de 'diagonale')?
                List<BeanPoint_internal> v_ptsDeLArcTeste = new List<BeanPoint_internal>() { v_arcToTest.p11_pointDbt, v_arcToTest.p12_pointFin };

                //Des facettes sont-elles sans dimension dans le plan XY?
                //Si une des facettes est plate=>il faut inverser
                bool v_isFacetteGauchePlate = IsTriangleEstPlatDansLePlanXY(v_arcToTest.p11_pointDbt, v_arcToTest.p12_pointFin, v_pointGaucheNewArc);
                bool v_isFacetteDroitePlate = IsTriangleEstPlatDansLePlanXY(v_arcToTest.p11_pointDbt, v_arcToTest.p12_pointFin, v_pointDroitNewArc);
                if (v_isFacetteGauchePlate || v_isFacetteDroitePlate)
                {
                    bool v_testToDebug_vf = true;
                }

               if (!v_isFacetteGauchePlate && !v_isFacetteDroitePlate)
                {//Sinon=>on poursuit le test
                    //=>On teste si les cercles circonscrits à l'un et l'autre triangle incluent "le 4ème point" (=celui appartenant à l'autre triangle et pas à l'arc) 
                    //(On utilise ici une méthode 'explicite' qui calcule le centre, le rayon afférent et l'écart à ce rayon:
                    //=>Ne nous semble pas plus coûteux que par le test du déterminant qui implique de connaître l'ordonnancement horaire/anti-horaire des points des triangles)
                    List<double[]> v_pointsDuTriangleAvantBascule;
                    v_pointsDuTriangleAvantBascule = v_facetteGauche.p01_pointsDeFacette.Select(t => t.p10_coord).ToList();
                    bool v_isPointDansLeCercle1_vf = FLabServices.createCalculLow().IsPointDDansCercleCirconscritAuTriangleExplicite(v_pointsDuTriangleAvantBascule, v_pointDroitNewArc.p10_coord);
                    v_pointsDuTriangleAvantBascule = v_facetteDroite.p01_pointsDeFacette.Select(t => t.p10_coord).ToList();
                    bool v_isPointDansLeCercle2_vf = FLabServices.createCalculLow().IsPointDDansCercleCirconscritAuTriangleExplicite(v_pointsDuTriangleAvantBascule, v_pointGaucheNewArc.p10_coord);

                    //Si les 2 cercles circonscrits sont "vides"=>alors, la conformation est OK=> inutile de modifier.
                    if (!v_isPointDansLeCercle1_vf && !v_isPointDansLeCercle2_vf)
                    {
                        v_arcToTest.p20_statutArc = enumStatutArc.arcNONCandidatASuppression;
                        v_beanRapportOut.p00_modif_vf = false;
                        return v_beanRapportOut;
                    }

                    //On ne peut, toutefois, modifier que si le quadrilatère est strictement convexe 
                    //(Si ce n'est pas le cas, la "2ème diagonale" est  partiellement extérieure ou sur confondues avec 2 arètes du quadrilatère; la modif n'est pas possible
                    //On teste si l'"arc de remplacement" (correspondant à cette "2de diagonale") intersecte strictement l'arc à remplacer dans le plan XY?
                    //List<BeanPoint_internal> v_ptsDeLArcTeste = new List<BeanPoint_internal>() { v_arcToTest.p11_pointDbt, v_arcToTest.p12_pointFin };
                    if (!FLabServices.createCalculLow().AreSegmentsSequants(v_arcToTest.p11_pointDbt.p10_coord, v_arcToTest.p12_pointFin.p10_coord, v_pointGaucheNewArc.p10_coord, v_pointDroitNewArc.p10_coord))
                    {
                        v_beanRapportOut.p00_modif_vf = false;
                        return v_beanRapportOut;
                    }
                }


                //B-PHASE B 'BASCULE':
                //=>On va effectuer la découpe du 'quadrilatère' selon la 2de diagonale, en 2 nouveax triangles
                //=>On va également référencer les 4 arètes externes.
                //? Elles vont être, elles-mêmes, candidates, en sortie à de nouveaux tests de partition.
                //On pourrait renvoyer un simple bool (modifié/non modifié) et récupérer en sortie ces arcs ou/et les flaguer comme 'candidats' 
                //mais permet d'éviter des filtres inutiles
                //Noter encore: on ne met pas à jour ici le statut 'candidat à modif': risquerait de perturber la version 1 du projet: 
                //VOIR A LA REFACTO

                v_beanRapportOut.p00_modif_vf = true;
                v_beanRapportOut.p01_idFacettesSupprimees.Add(v_facetteGauche.p00_idFacette);
                v_beanRapportOut.p01_idFacettesSupprimees.Add(v_facetteDroite.p00_idFacette);

                //Action...:
                //On avait des triangles respectivement à droite et à gauche de l'arc test.
                //=>On créé 2 nouveaux triangles.
                //Utilisant la "2de diagonale", l'un sera'à droite' de ce nouvel arc, l'autre à 'gauche'...
                //...mais, dans le référentiel déterminé par les 2 diagonales,
                //on va donc considérer que ces 2 triangles sont l'un  'haut', l'autre 'bas'.

                BeanArc_internal v_newArc = new BeanArc_internal(v_pointGaucheNewArc, v_pointDroitNewArc);

                BeanFacette_internal v_newFacetteHaute = new BeanFacette_internal();
                BeanFacette_internal v_newFacetteBasse = new BeanFacette_internal();
                v_beanRapportOut.p02_newFacettes.Add(v_newFacetteHaute);
                v_beanRapportOut.p02_newFacettes.Add(v_newFacetteBasse);

                //La facette 'haute' va être déclarée comme celle pour laquelle le '1er point' de l'arc test est 'au-dessus' du 'nouvel arc' 
                //Le choix est donc arbitraire mais on a besoin de le faire pour effectuer, plus tard, la répartition des 'points inclus' entre l'une et l'autre facette
                bool v_facetteHauteAuDessus_vf;
                Dictionary<int, double[]> v_positionDesPointsDeLArcTest_ParRapportAuNouvelArc;
                v_positionDesPointsDeLArcTest_ParRapportAuNouvelArc = GetCoordonneesDansNewReferentiel2D(v_ptsDeLArcTeste, v_pointGaucheNewArc.p10_coord, v_pointDroitNewArc.p10_coord);

                if (v_positionDesPointsDeLArcTest_ParRapportAuNouvelArc
                    .Where(c => c.Key == v_arcToTest.p11_pointDbt.p00_id)
                    .Where(c => c.Value[1] > 0).Count() == 1)
                {
                    v_newArc.p21_facetteGauche = v_newFacetteHaute;
                    v_newArc.p22_facetteDroite = v_newFacetteBasse;
                    v_facetteHauteAuDessus_vf = true;
                }
                else
                {
                    v_newArc.p22_facetteDroite = v_newFacetteHaute;
                    v_newArc.p21_facetteGauche = v_newFacetteBasse;
                    v_facetteHauteAuDessus_vf = false;
                }

                //=>On effectue la scission...:
                BeanArc_internal v_arcMontant;
                BeanArc_internal v_arcDescendant;

                //=>Traitement de la facette haute
                v_newFacetteHaute.p01_pointsDeFacette.Add(v_arcToTest.p11_pointDbt);
                v_newFacetteHaute.p01_pointsDeFacette.Add(v_pointGaucheNewArc);
                v_newFacetteHaute.p01_pointsDeFacette.Add(v_pointDroitNewArc);
                //
                v_newFacetteHaute.p02_arcs.Add(v_newArc);

                //L'arc 'montant' est censé partir du point gauche du nouvel arc vers le point opposé à cet arc, 
                //ici le pt de début de l'arc à tester
                //Toutefois:
                //-cet arc existe déjà (il appartient à la facette gauche)
                //-son sens peut être inverse
                v_arcMontant = v_facetteGauche.p02_arcs.Where(c =>
                (
                (c.p11_pointDbt.p01_hCodeGeog == v_pointGaucheNewArc.p01_hCodeGeog && c.p12_pointFin.p01_hCodeGeog == v_arcToTest.p11_pointDbt.p01_hCodeGeog)
                ||
                (c.p12_pointFin.p01_hCodeGeog == v_pointGaucheNewArc.p01_hCodeGeog && c.p11_pointDbt.p01_hCodeGeog == v_arcToTest.p11_pointDbt.p01_hCodeGeog)
                )
                ).First();
                //On doit donc indiquer sur l'arc la nouvelle facette à utiliser sur le côté correspondant 
                //(remplace celle de la facette source, amenée à disparaître)
                if (v_arcMontant.p21_facetteGauche != null && v_arcMontant.p21_facetteGauche.p00_idFacette == v_facetteGauche.p00_idFacette)
                {
                    v_arcMontant.p21_facetteGauche = v_newFacetteHaute;
                }
                else
                {
                    v_arcMontant.p22_facetteDroite = v_newFacetteHaute;
                }
                v_newFacetteHaute.p02_arcs.Add(v_arcMontant);
                v_beanRapportOut.p03_arcsCandidatsOut.Add(v_arcMontant);

                //L'arc 'descendant' est censé partir du  pt de début de l''arc à tester' et redescende sur le point droit du nouvel arc
                //Toutefois:
                //-cet arc existe déjà (il appartient à la facette droite)
                //-son sens peut être inverse
                v_arcDescendant = v_facetteDroite.p02_arcs.Where(c =>
                (
                (c.p11_pointDbt.p01_hCodeGeog == v_arcToTest.p11_pointDbt.p01_hCodeGeog && c.p12_pointFin.p01_hCodeGeog == v_pointDroitNewArc.p01_hCodeGeog)
                ||
                (c.p11_pointDbt.p01_hCodeGeog == v_pointDroitNewArc.p01_hCodeGeog && c.p12_pointFin.p01_hCodeGeog == v_arcToTest.p11_pointDbt.p01_hCodeGeog)
                )
                ).First();

                if (v_arcDescendant.p21_facetteGauche != null && v_arcDescendant.p21_facetteGauche.p00_idFacette == v_facetteDroite.p00_idFacette)
                {
                    v_arcDescendant.p21_facetteGauche = v_newFacetteHaute;
                }
                else
                {
                    v_arcDescendant.p22_facetteDroite = v_newFacetteHaute;
                }
                v_newFacetteHaute.p02_arcs.Add(v_arcDescendant);
                v_beanRapportOut.p03_arcsCandidatsOut.Add(v_arcDescendant);



                //Traitement de la facette 'basse'
                v_newFacetteBasse.p01_pointsDeFacette.Add(v_arcToTest.p12_pointFin);
                v_newFacetteBasse.p01_pointsDeFacette.Add(v_pointGaucheNewArc);
                v_newFacetteBasse.p01_pointsDeFacette.Add(v_pointDroitNewArc);

                //
                v_newFacetteBasse.p02_arcs.Add(v_newArc);
                //
                v_arcDescendant = v_facetteGauche.p02_arcs.Where(c =>
                (
                (c.p11_pointDbt.p01_hCodeGeog == v_pointGaucheNewArc.p01_hCodeGeog && c.p12_pointFin.p01_hCodeGeog == v_arcToTest.p12_pointFin.p01_hCodeGeog)
                ||
                (c.p12_pointFin.p01_hCodeGeog == v_pointGaucheNewArc.p01_hCodeGeog && c.p11_pointDbt.p01_hCodeGeog == v_arcToTest.p12_pointFin.p01_hCodeGeog)
                )
                ).First();
                //(L'arc 'descendant' est issu de la facette gauche [...].
                if (v_arcDescendant.p21_facetteGauche != null && v_arcDescendant.p21_facetteGauche.p00_idFacette == v_facetteGauche.p00_idFacette)
                {
                    v_arcDescendant.p21_facetteGauche = v_newFacetteBasse;
                }
                else
                {
                    v_arcDescendant.p22_facetteDroite = v_newFacetteBasse;
                }
                v_newFacetteBasse.p02_arcs.Add(v_arcDescendant);
                v_beanRapportOut.p03_arcsCandidatsOut.Add(v_arcDescendant);

                v_arcMontant = v_facetteDroite.p02_arcs.Where(c =>
                (
                (c.p11_pointDbt.p01_hCodeGeog == v_arcToTest.p12_pointFin.p01_hCodeGeog && c.p12_pointFin.p01_hCodeGeog == v_pointDroitNewArc.p01_hCodeGeog)
                ||
                (c.p11_pointDbt.p01_hCodeGeog == v_pointDroitNewArc.p01_hCodeGeog && c.p12_pointFin.p01_hCodeGeog == v_arcToTest.p12_pointFin.p01_hCodeGeog)
                )
                ).First();
                //(L'arc 'montant' est issu de la facette droite [...].
                if (v_arcMontant.p21_facetteGauche != null && v_arcMontant.p21_facetteGauche.p00_idFacette == v_facetteDroite.p00_idFacette)
                {
                    v_arcMontant.p21_facetteGauche = v_newFacetteBasse;
                }
                else
                {
                    v_arcMontant.p22_facetteDroite = v_newFacetteBasse;
                }
                v_newFacetteBasse.p02_arcs.Add(v_arcMontant);
                v_beanRapportOut.p03_arcsCandidatsOut.Add(v_arcMontant);



                //REAFFECTATION des POINTS INCLUS
                List<BeanPoint_internal> v_tousPoints = new List<BeanPoint_internal>();
                v_tousPoints.AddRange(v_facetteGauche.p10_pointsInclus);
                v_tousPoints.AddRange(v_facetteDroite.p10_pointsInclus);

                Dictionary<int, double[]> v_coordPointsInclusParRapportAuNouvelArc;
                v_coordPointsInclusParRapportAuNouvelArc = GetCoordonneesDansNewReferentiel2D(v_tousPoints, v_pointGaucheNewArc.p10_coord, v_pointDroitNewArc.p10_coord);

                HashSet<int> v_idPointsAuDessus = new HashSet<int>(v_coordPointsInclusParRapportAuNouvelArc.Where(c => c.Value[1] >= 0).Select(c => c.Key).ToList());
                List<BeanPoint_internal> v_pointsAuDessus = new List<BeanPoint_internal>();
                List<BeanPoint_internal> v_pointsAuDessous = new List<BeanPoint_internal>();

                foreach (BeanPoint_internal v_point in v_tousPoints)
                {
                    if (v_idPointsAuDessus.Contains(v_point.p00_id))
                    {
                        v_pointsAuDessus.Add(v_point);
                    }
                    else
                    {
                        v_pointsAuDessous.Add(v_point);
                    }
                }
                if (v_facetteHauteAuDessus_vf)
                {
                    v_newFacetteHaute.p10_pointsInclus = v_pointsAuDessus;
                    v_newFacetteBasse.p10_pointsInclus = v_pointsAuDessous;
                }
                else
                {
                    v_newFacetteHaute.p10_pointsInclus = v_pointsAuDessous;
                    v_newFacetteBasse.p10_pointsInclus = v_pointsAuDessus;
                }




                //MISE A JOUR TOPOLOGIE:
                //1-Les arcs:

                //(Controle 'pustule': ponctuellement (1/10 000)=>1 arc doublonné
                //=>A APPROFONDIR: Est-ce que cela ne pourrait pas être dû à l'existance de couples de triangles 'plats' ?
                List<BeanArc_internal> v_doublonsArcs = new List<BeanArc_internal>();
                if (!p_topologieFacette.p12_arcsByCode.ContainsKey(v_newArc.p01_hcodeArc))
                {
                    p_topologieFacette.ArcAjouter(v_newArc);
                }
                else
                {
                    v_doublonsArcs.Add(v_newArc);
                }
                p_topologieFacette.ArcSupprimer(p_hcodeArcCandidatASuppression);

                //2- Les facettes:

                p_topologieFacette.FacetteAjouter(v_newFacetteHaute);
                p_topologieFacette.FacetteAjouter(v_newFacetteBasse);
                p_topologieFacette.FacetteSupprimer(v_facetteGauche);
                p_topologieFacette.FacetteSupprimer(v_facetteDroite);
            }
            catch (Exception)
            {
                throw;
            }
            return v_beanRapportOut;
        }

    

        private BeanPoint_internal GetAndSetByRefPointExcentreDeLaFacette(ref BeanFacette_internal p_facette, BeanParametresChoixDuPointCentral p_paramDeChoixDuPointCentral, bool p_nullSiInfALExcentrationMinimale_vf)
        {
            BeanPoint_internal v_point = null;
            try
            {
                if (p_facette.p10_pointsInclus == null || p_facette.p10_pointsInclus.Count == 0)
                {
                    return null;
                }
                //
                Dictionary<int, double[]> v_coordPointsDansReferentielDeLaFacette;
                v_coordPointsDansReferentielDeLaFacette = GetCoordonneesPointsDansLeRefDuPlanXYDeLaFacette(ref p_facette);

                //
                int p_idPointCible;
                double v_valeurMaxiAbs = 0;
                switch (p_paramDeChoixDuPointCentral.p00_methodeChoixDuPointCentral)
                {
                    case enumMethodeChoixDuPointCentral.pointLePlusExcentre:
                        v_valeurMaxiAbs = Math.Max(v_coordPointsDansReferentielDeLaFacette.Max(c => c.Value[2]), Math.Abs(v_coordPointsDansReferentielDeLaFacette.Min(c => c.Value[2])));
                        if (v_valeurMaxiAbs < p_paramDeChoixDuPointCentral.p01_excentrationMinimum && p_nullSiInfALExcentrationMinimale_vf)
                        {
                            return null;
                        }
                        p_idPointCible = v_coordPointsDansReferentielDeLaFacette.Where(c => Math.Abs(c.Value[2]) == v_valeurMaxiAbs).Select(c => c.Key).First();
                        break;
                    default:
                        throw new Exception("Méthode " + p_paramDeChoixDuPointCentral.p00_methodeChoixDuPointCentral + " non implémentée.");
                }
                v_point = p_facette.p10_pointsInclus.Where(c => c.p00_id == p_idPointCible).First();
                v_point.p31_ecartAbsAuPlanCourant = v_valeurMaxiAbs;
                p_facette.p21_plusGrandEcartAbsolu = v_valeurMaxiAbs;
                p_facette.p22_pointPlusGrandEcart = v_point;
            }
            catch (Exception)
            {

                throw;
            }
            return v_point;
        }
        private void InsertDansListeChaineeDesFacettes(ref BeanTopologieFacettes p_topologieFacette, BeanFacette_internal p_facetteAInserer, double p_ecartMini)
        {
            if (p_facetteAInserer.p21_plusGrandEcartAbsolu < p_ecartMini)
            {
                return;
            }
            if (p_topologieFacette.p21_facetteAvecEcartAbsoluMax == null) //(Vrai si la liste est vide
            {
                p_topologieFacette.p21_facetteAvecEcartAbsoluMax = p_facetteAInserer;
                return;
            }

            //
            bool v_insertionFaite_vf = false;
            BeanFacette_internal v_facetteCourante = p_topologieFacette.p21_facetteAvecEcartAbsoluMax;
            while (!v_insertionFaite_vf)
            {
                if (p_facetteAInserer.p21_plusGrandEcartAbsolu > v_facetteCourante.p21_plusGrandEcartAbsolu)
                {
                    p_facetteAInserer.p23_facetteEcartSup = v_facetteCourante.p23_facetteEcartSup;
                    p_facetteAInserer.p24_facetteEcartInf = v_facetteCourante;
                    //
                    if (p_facetteAInserer.p23_facetteEcartSup != null)
                    {
                        p_facetteAInserer.p23_facetteEcartSup.p24_facetteEcartInf = p_facetteAInserer;
                    }
                    if (p_facetteAInserer.p24_facetteEcartInf != null)
                    {
                        p_facetteAInserer.p24_facetteEcartInf.p23_facetteEcartSup = p_facetteAInserer;
                    }
                    //
                    v_insertionFaite_vf = true;
                    break;
                }
                if (v_facetteCourante.p24_facetteEcartInf == null)
                {
                    p_facetteAInserer.p23_facetteEcartSup = v_facetteCourante;
                    if (p_facetteAInserer.p23_facetteEcartSup != null)
                    {
                        p_facetteAInserer.p23_facetteEcartSup.p24_facetteEcartInf = p_facetteAInserer;
                    }
                    //
                    v_insertionFaite_vf = true;
                    break;
                }
                v_facetteCourante = v_facetteCourante.p24_facetteEcartInf;
            }
            //
            if (p_facetteAInserer.p23_facetteEcartSup == null && p_facetteAInserer.p24_facetteEcartInf != null)
            {
                p_topologieFacette.p21_facetteAvecEcartAbsoluMax = p_facetteAInserer;
            }

        }
        private void InsertDansListeChaineeDesFacettes(ref BeanTopologieFacettes p_topologieFacette, List<BeanFacette_internal> p_facettesAInserer, double p_ecartMini)
        {
            List<BeanFacette_internal> v_facettesFiltreesOrdonnees = p_facettesAInserer.Where(c => c.p21_plusGrandEcartAbsolu >= p_ecartMini).OrderByDescending(c => c.p21_plusGrandEcartAbsolu).ToList();

            if (v_facettesFiltreesOrdonnees.Count == 0)
            {
                return;
            }

            if (p_topologieFacette.p21_facetteAvecEcartAbsoluMax == null) //(Vrai si la liste est vide
            {
                p_topologieFacette.p21_facetteAvecEcartAbsoluMax = v_facettesFiltreesOrdonnees.First();
                if (v_facettesFiltreesOrdonnees.Count > 1)
                {
                    v_facettesFiltreesOrdonnees = v_facettesFiltreesOrdonnees.GetRange(1, v_facettesFiltreesOrdonnees.Count - 1).ToList();
                }
                else
                {
                    return;
                }
            }
            //
            BeanFacette_internal v_facetteCourante = p_topologieFacette.p21_facetteAvecEcartAbsoluMax;

            int v_indexFacetteAInsereCourante = 0;
            BeanFacette_internal p_facetteAInserer = v_facettesFiltreesOrdonnees[v_indexFacetteAInsereCourante];
            int v_nbreFacettesAInserer = v_facettesFiltreesOrdonnees.Count;
            //
            bool v_test = true;
            while (v_test)
            {
                if (p_facetteAInserer.p21_plusGrandEcartAbsolu > v_facetteCourante.p21_plusGrandEcartAbsolu)
                {
                    p_facetteAInserer.p23_facetteEcartSup = v_facetteCourante.p23_facetteEcartSup;
                    p_facetteAInserer.p24_facetteEcartInf = v_facetteCourante;
                    //
                    if (p_facetteAInserer.p23_facetteEcartSup != null)
                    {
                        p_facetteAInserer.p23_facetteEcartSup.p24_facetteEcartInf = p_facetteAInserer;
                    }
                    if (p_facetteAInserer.p24_facetteEcartInf != null)
                    {
                        p_facetteAInserer.p24_facetteEcartInf.p23_facetteEcartSup = p_facetteAInserer;
                    }
                    if (p_facetteAInserer.p23_facetteEcartSup == null && p_facetteAInserer.p24_facetteEcartInf != null)
                    {
                        p_topologieFacette.p21_facetteAvecEcartAbsoluMax = p_facetteAInserer;
                    }
                    //
                    v_indexFacetteAInsereCourante++;
                    if (v_indexFacetteAInsereCourante >= v_nbreFacettesAInserer)
                    {
                        break;
                    }
                    p_facetteAInserer = v_facettesFiltreesOrdonnees[v_indexFacetteAInsereCourante];
                    continue;
                }
                if (v_facetteCourante.p24_facetteEcartInf == null)
                {
                    p_facetteAInserer.p23_facetteEcartSup = v_facetteCourante;
                    if (p_facetteAInserer.p23_facetteEcartSup != null)
                    {
                        p_facetteAInserer.p23_facetteEcartSup.p24_facetteEcartInf = p_facetteAInserer;
                    }
                    //
                    v_indexFacetteAInsereCourante++;
                    if (v_indexFacetteAInsereCourante >= v_nbreFacettesAInserer)
                    {
                        break;
                    }
                    p_facetteAInserer = v_facettesFiltreesOrdonnees[v_indexFacetteAInsereCourante];
                    continue;
                }
                v_facetteCourante = v_facetteCourante.p24_facetteEcartInf;
            }
            //


        }
   
        private Dictionary<int, double[]> GetCoordonneesPointsDansLeRefDuPlanXYDeLaFacette(ref BeanFacette_internal p_facette)
        {
            Dictionary<int, double[]> v_coords = new Dictionary<int, double[]>();
            try
            {
                //On crée un référentiel à partir de la facette
                List<double[]> v_coordDesPointsFacettes;
                //(On utilise le point d'altitude maxi comme point d'origine
                v_coordDesPointsFacettes = p_facette.p01_pointsDeFacette.OrderByDescending(c => c.p10_coord[2]).Select(c => c.p10_coord).ToList();
                //(On utilise la normale du plan comme axe z
                double[] v_normaleDuPlan = p_facette.getNormaleDuPlan();
                //v_normaleDuPlan = FLabServices.createCalculLow().GetNormaleDuPlan(v_coordDesPointsFacettes[0], v_coordDesPointsFacettes[2], v_coordDesPointsFacettes[1]);
                //p_facette.p31_normaleDuPlan = v_normaleDuPlan;
                //
                double[] v_vector1 = FLabServices.createCalculLow().GetVectorBrutFromTwoPoints(v_coordDesPointsFacettes[0], v_coordDesPointsFacettes[1]);
                v_vector1 = FLabServices.createCalculLow().GetNormalisationVecteurXYZ(v_vector1);
                double[] v_vector2 = FLabServices.createCalculLow().GetVectorBrutFromTwoPoints(v_coordDesPointsFacettes[0], v_coordDesPointsFacettes[2]);
                v_vector2 = FLabServices.createCalculLow().GetNormalisationVecteurXYZ(v_vector2);
                //=>On calcule la matrice de changement de repère
                double[,] v_matriceDeRotation = FLabServices.createCalculLow().GetMatriceInverse3x3(v_vector1, v_vector2, v_normaleDuPlan);

                ////Pour debug: test inversion de matrice 
                //double[,] v_matrice= FLabServices.createCalculLow().GetMatrice3x3FromVectors(v_vector1, v_vector2, v_normaleDuPlan);
                //bool v_isInversionOk_vf=FLabServices.createCalculLow().IsInversionMatriceOk(v_matriceDeRotation, v_matrice);
                ////Fin pour debug: test inversion de matrice 

                //=>On applique la transformation à chacun des points:
                double[] v_coordDansLeRepereDuPlan;
                double[] v_vector;
                foreach (BeanPoint_internal v_point in p_facette.p10_pointsInclus)
                {
                    v_vector = FLabServices.createCalculLow().GetVectorBrutFromTwoPoints(v_coordDesPointsFacettes[0], v_point.p10_coord);
                    v_coordDansLeRepereDuPlan = FLabServices.createCalculLow().GetProduitMatriceParVector(v_matriceDeRotation, v_vector);
                    v_coords.Add(v_point.p00_id, v_coordDansLeRepereDuPlan);
                }
            }
            catch (Exception)
            {

                throw;
            }
            return v_coords;
        }
        private List<BeanFacette_internal> GetFacettesInitialesByPolygoneConvexe(List<BeanPoint_internal> p_pointsBase, BeanPoint_internal p_pointCentral, List<BeanPoint_internal> p_tousPointsInclus)
        {
            List<BeanFacette_internal> v_facettesOut = new List<BeanFacette_internal>();
            try
            {
                //On marque le 'point central' comme 'point facette':
                p_pointCentral.p21_estPointFacette_vf = true;
                p_pointCentral.p22_estPointInclus_vf = true;

                //Le 'point central' appartient-il au convexHull?
                int v_nbreOccurrencesPointCentralInConvexHull = p_pointsBase.Where(c => c.p00_id == p_pointCentral.p00_id).Count();
                //Si c'est le cas on filtre le CHull
                //(Note: on peut envisager qu'il apparaissent même 2 fois s'il est point de début ET SI le polygone est explicitement décrit
                if (v_nbreOccurrencesPointCentralInConvexHull >= 1)
                {
                    p_pointsBase = p_pointsBase.Where(c => c.p00_id != p_pointCentral.p00_id).ToList();
                }
                //3-facettage initial:
                //On créé 2 listes:
                //1 pour les "arcs bases" (=ceux composant le convexhull),
                //1 pour les arcs convergeant au sommet

                BeanArc_internal v_arc;
                List<BeanArc_internal> v_arcsBases = new List<BeanArc_internal>();
                List<BeanArc_internal> v_arcsRayonnants = new List<BeanArc_internal>();
                //On en profite pour marquer ces points comme extrêmité de 'facettes'
                p_pointsBase[0].p21_estPointFacette_vf = true;
                p_pointsBase[0].p22_estPointInclus_vf = true;

                //Note: Les points de base sont censés décrire un polygone=>le dernier point devrait donc avoir les mêmes coord que le premier)
                //Note 2: les 2 listes doivent être construites en parallèle: 
                //on utilise plus bas les indices de liste pour regrouper arcs bases et arcs rayonnants en facette
                for (int v_indicePointBase = 0; v_indicePointBase < p_pointsBase.Count - 1; v_indicePointBase++)
                {
                    //On crée les arcs 'bases'
                    v_arc = new BeanArc_internal(p_pointsBase[v_indicePointBase], p_pointsBase[v_indicePointBase + 1]);
                    v_arc.p20_statutArc = enumStatutArc.arcExterne;
                    v_arcsBases.Add(v_arc);

                    //  [/On en profite pour marquer ces points comme extrêmité de 'facettes'
                    p_pointsBase[v_indicePointBase + 1].p21_estPointFacette_vf = true;
                    p_pointsBase[v_indicePointBase + 1].p22_estPointInclus_vf = true;


                    //On crée les arcs 'rayonnant' depuis le point central vers les points bases
                    v_arc = new BeanArc_internal(p_pointCentral, p_pointsBase[v_indicePointBase]);
                    v_arcsRayonnants.Add(v_arc);
                }

                //On rajoute privisoirement le 1er arc convergeant à la fin de la liste des arcs convergeants.
                //?Va simplifier l'algo en créant un "anneau".
                v_arcsRayonnants.Add(v_arcsRayonnants.First());


                //On détermine le côté 'intérieur': va permettre de définir l'affectation à gauche/droite des facettes
                bool v_interieurEstGauche_vf = true;
                double[] v_coordDUnPointInterieur = p_pointCentral.p10_coord;
                if (v_nbreOccurrencesPointCentralInConvexHull == 0)
                {
                    v_coordDUnPointInterieur = GetCentroide(p_pointsBase);
                }
                double[] v_coordDUnPointInterieurRef;
                v_coordDUnPointInterieurRef = FLabServices.createCalculLow().GetCoordDansNewRepereXY(v_coordDUnPointInterieur, p_pointsBase[0].p10_coord, p_pointsBase[1].p10_coord);
                if (v_coordDUnPointInterieurRef[1] < 0)
                {
                    v_interieurEstGauche_vf = false;
                }


                //On peut, maintenant, créer les facettes
                BeanFacette_internal v_facette;
                BeanArc_internal v_arcDescendant;
                BeanArc_internal v_arcMontant;
                List<BeanFacette_internal> v_facettesNonPlates = new List<BeanFacette_internal>();
                List<BeanFacette_internal> v_facettesPlates = new List<BeanFacette_internal>();
                for (int v_indiceArcBase = 0; v_indiceArcBase < v_arcsBases.Count; v_indiceArcBase++)
                {
                    v_facette = new BeanFacette_internal();
                    //
                    v_facette.p01_pointsDeFacette.Add(p_pointCentral);
                    v_facette.p01_pointsDeFacette.Add(v_arcsBases[v_indiceArcBase].p11_pointDbt);
                    v_facette.p01_pointsDeFacette.Add(v_arcsBases[v_indiceArcBase].p12_pointFin);
                    //
                    v_arcDescendant = v_arcsRayonnants[v_indiceArcBase];
                    v_arcMontant = v_arcsRayonnants[v_indiceArcBase + 1];
                    if (v_interieurEstGauche_vf)
                    {
                        v_arcDescendant.p21_facetteGauche = v_facette;
                        v_arcsBases[v_indiceArcBase].p21_facetteGauche = v_facette;
                        v_arcMontant.p22_facetteDroite = v_facette;
                    }
                    else
                    {
                        v_arcDescendant.p22_facetteDroite = v_facette;
                        v_arcsBases[v_indiceArcBase].p22_facetteDroite = v_facette;
                        v_arcMontant.p21_facetteGauche = v_facette;
                    }
                    v_facette.p02_arcs.Add(v_arcDescendant);
                    v_facette.p02_arcs.Add(v_arcsBases[v_indiceArcBase]);
                    v_facette.p02_arcs.Add(v_arcMontant);

                    //On teste si la facette est 'verticale' (en fait si les points sont alignés dans le plan XY)
                    v_facette.p03_estVerticale_vf = IsTriangleEstPlatDansLePlanXY(v_facette.p01_pointsDeFacette[0], v_facette.p01_pointsDeFacette[1], v_facette.p01_pointsDeFacette[2]);
                    //SI la facette est 'verticale' (cas courant lorsque la grille est régulière), la facette n'est pas exploitable.
                    //=>Elle devra subir un traitement spécifique dans le cadre des 'bascules'
                    if (!v_facette.p03_estVerticale_vf)
                    {
                        v_facettesNonPlates.Add(v_facette);
                        v_facettesOut.Add(v_facette);
                    }
                    else
                    {
                        v_facettesPlates.Add(v_facette);
                    }
                }
                //On réaffecte les points sur les facettes 'non plates'
                BeanFacette_internal v_facT;
                foreach (BeanFacette_internal v_fac in v_facettesNonPlates)
                {
                    v_facT = v_fac;
                    RattachePointsToFacetteInTetraedre(ref p_tousPointsInclus, ref v_facT);
                }

                //On traite les 'facettes plates'
                //?Le double traitement sur les points inclus?
                //Lorsque une facette est 'plate' (ou 'verticale'), les points qui sont colinéaires à son arc externe
                //ne sont pas remontés par la méthode ni affectés aux facettes bordières qui seraient également candidates à les accueillir.
                //=>Du coup, on rattache les points  à la 'facette plate', non plus par inclusion dans le polygone 
                //mais par appartenance au segment décrivant cette 'facette' du coup à 1 seule dimension dans XY.
                //Le pb potentiel dans ce cas est qu'un même point pourrait candidater à 2 facettes.
                //On aurait pu proposer une attribution aléatoire mais on préfère proposer d'abord l'approche générique PUIS traiter le résidu.
                //On en profite également pour supprimer la facette plate (désactiver l'arc externe -normalement 1 seul sauf cas où le maxi est sur un point d'appui-
                //et transformer les arcs internes en arcs externes)
                List<BeanPoint_internal> v_pointsNonInclus;
                v_pointsNonInclus = p_tousPointsInclus.Where(c => c.p22_estPointInclus_vf == false).ToList();
                if (v_facettesPlates.Count>0)
                {
                    if (v_pointsNonInclus.Count > 0)
                    {
                        BeanFacette_internal v_facetteToModif;
                        double p_toleranceEnM = 0.01;
                        BeanFacette_internal v_facettePourCompletagePoints;
                        List<BeanPoint_internal> v_ptsTouchantLaFacettePlate;
                        List<BeanPoint_internal> v_pointsUtiles;
                        foreach (BeanFacette_internal v_facettePlate in v_facettesPlates)
                        {
                            v_facetteToModif = v_facettePlate;
                            RattachePointsTouchantFacettePlate(ref v_pointsNonInclus, ref v_facetteToModif, p_toleranceEnM);
                            //
                            v_ptsTouchantLaFacettePlate=v_facetteToModif.p10_pointsInclus.ToList();
                            //=>Désaffectation (bof!)
                            foreach(BeanPoint_internal v_pt in v_ptsTouchantLaFacettePlate)
                            {
                                v_pt.p22_estPointInclus_vf = false;
                            }

                            List<BeanArc_internal> v_arcsNonExternes = v_facettePlate.p02_arcs.Where(c => c.p20_statutArc != enumStatutArc.arcExterne).ToList();
                            v_pointsUtiles = v_ptsTouchantLaFacettePlate;
                            foreach (BeanArc_internal v_arcNonExterne in v_arcsNonExternes)
                            {
                                v_arcNonExterne.p20_statutArc= enumStatutArc.arcExterne;
                                if (v_arcNonExterne.p21_facetteGauche.p00_idFacette == v_facettePlate.p00_idFacette)
                                {
                                    v_arcNonExterne.p21_facetteGauche = null;
                                    v_facettePourCompletagePoints = v_arcNonExterne.p22_facetteDroite;
                                }
                                else
                                {
                                    v_arcNonExterne.p22_facetteDroite = null;
                                    v_facettePourCompletagePoints = v_arcNonExterne.p21_facetteGauche;
                                } 
                                RattachePointsInclusOuTouchantFacette(ref v_pointsUtiles, ref v_facettePourCompletagePoints);
                                //
                                v_pointsUtiles = v_ptsTouchantLaFacettePlate.Where(c => c.p22_estPointInclus_vf == false).ToList();
                            }

                           // BeanArc_internal v_arcASupprimer = v_facettePlate.p02_arcs.Where(c => c.p20_statutArc == enumStatutArc.arcExterne).ToList().First();
                            //
                            v_pointsNonInclus = p_tousPointsInclus.Where(c => c.p22_estPointInclus_vf == false).ToList();
                        }
                    }
                }
               



                //On supprime l'arc supplémentaire rajouté
                v_arcsRayonnants.RemoveAt(v_arcsRayonnants.Count - 1);

                //On supprime de la liste le dernier point contenu dans le convexhull
                //(Sauf si le point maxi était aussi le premier -et donc le dernier- point du convexhull!)
                if (v_nbreOccurrencesPointCentralInConvexHull < 2)
                {
                    p_pointsBase.RemoveAt(p_pointsBase.Count - 1);
                }
            }
            catch (Exception)
            {

                throw;
            }
            return v_facettesOut;
        }

        /// <summary>
        /// Soit, en entrée, une 'facette' et une liste de points,
        /// on veut identifier les points inclus dans la facette et les rattacher, par ref, à cette facette.
        /// ATTENTION TRES IMPORTANT: 
        /// Cette opération est faîte pour être répétée sur des 'sous-facettes' 
        /// (entendre "les 'facettes' émergeant à partir d'1 facette, par rajout d'un 'point central' pour création d'un tétraèdre").
        /// Or, tous les points inclus dans la facette d'origine doivent être affectés 1 fois et 1 seule à une facette (pas d'oubli, pas de doublon).
        /// Pour cela, on introduit une dissymétrie: 
        /// on présume les points ordonnés dans un sens anti-horaire et le premier point comme le 'point central'.
        /// Partant de là, on prend:
        /// tous les points STRICTEMENT à 'droite' de l'arc 'descendant' ET (à 'gauche' de l'arc 'montant' OU sur cet arc 'montant')
        /// Le 3ème arc -opposé au 'point central'- est considéré comme 'arc base' et on conserve tous les points au-dessus ou 'sur' lui 
        /// SAUF si le point est également sur l'arc descendant.
        /// IMPACT? Lors des créations de sous-facettes, il faut utiliser des triptyques de points tous ordonnés dans le même sens 
        /// (soit horaire soit anti-horaire) et il est fortement conseillé que le premier point envoyé soit le 'point central'
        /// </summary>
        /// <param name="p_pointsInclus"></param>
        /// <param name="p_facette"></param>
        private void RattachePointsToFacetteInTetraedre(ref List<BeanPoint_internal> p_pointsInclus, ref BeanFacette_internal p_facette)
        {
            try
            {
                List<BeanPoint_internal> v_pointsToTest = p_pointsInclus
                    .Where(c => !c.p21_estPointFacette_vf)
                    .Where(c => !c.p22_estPointInclus_vf).ToList();
                Dictionary<int, double[]> v_coordRef;
                HashSet<int> v_idPointsUtiles;

                //Je commence par le 'point haut'
                //'x'>0 et 'y'>=0 ('arc droit'=>abscisses, 'arc gauche'=>ordonnées)
                v_coordRef = GetCoordonneesDansNewReferentiel2D(v_pointsToTest, p_facette.p01_pointsDeFacette[0].p10_coord, p_facette.p01_pointsDeFacette[1].p10_coord, p_facette.p01_pointsDeFacette[2].p10_coord);

                if(v_coordRef==null) //SI la facette est 'plate' (possible à ce stade)=>Les points ne peuvent pas être "contenus". 
                    //On risque par contre de perdre des points eux-mêmes strictement alignés sur l'alignement précédent
                {
                    p_facette.p10_pointsInclus = new List<BeanPoint_internal>();
                    return;
                }
                    
                v_idPointsUtiles = new HashSet<int>(v_coordRef.Where(c => c.Value[0] > 0 && c.Value[1] >= 0).Select(c => c.Key).ToList());
                v_pointsToTest = v_pointsToTest.Where(c => v_idPointsUtiles.Contains(c.p00_id)).ToList();
                //Puis...
                v_coordRef = GetCoordonneesDansNewReferentiel2D(v_pointsToTest, p_facette.p01_pointsDeFacette[1].p10_coord, p_facette.p01_pointsDeFacette[0].p10_coord, p_facette.p01_pointsDeFacette[2].p10_coord);
                //'x'>=0 et 'y'>=0 ('arc gauche'=>abscisses, 'arc base'=>ordonnées)
                v_idPointsUtiles = new HashSet<int>(v_coordRef.Where(c => c.Value[0] >= 0 && c.Value[1] >= 0).Select(c => c.Key).ToList());
                v_pointsToTest = v_pointsToTest.Where(c => v_idPointsUtiles.Contains(c.p00_id)).ToList();
                BeanPoint_internal v_point;
                for (int v_indicePoint = 0; v_indicePoint < v_pointsToTest.Count; v_indicePoint++)
                {
                    v_point = v_pointsToTest[v_indicePoint];
                    v_point.p22_estPointInclus_vf = true;
                }
               p_facette.p10_pointsInclus.AddRange(v_pointsToTest);

                //[NOTE: Attention aux '>='! :
                //=>On doit gérer les symétries de manière à ce que chaque point soit affecté à 1 et à 1 seul triangle:
                //Donc:
                //-les points sur l'arc base doivent, bien sur, être conservés
                //-(Conventionnellement!), les points sur l'arc "de gauche" sont affectés au triangle courant
                //et, par suite, les points sur l'arc "de droite" sont laissés au triangle suivant
            }
            catch (Exception)
            {

                throw;
            }
        }
        private void RattachePointsInclusOuTouchantFacette(ref List<BeanPoint_internal> p_pointsInclus, ref BeanFacette_internal p_facette)
        {
            try
            {
                //Récup des points dispo:
                List<BeanPoint_internal> v_pointsToTest = p_pointsInclus
                    .Where(c => !c.p21_estPointFacette_vf)
                    .Where(c => !c.p22_estPointInclus_vf).ToList();
                if (v_pointsToTest.Count == 0)
                {
                    return;
                }

                Dictionary<int, double[]> v_coordRef;
                HashSet<int> v_idPointsUtiles;
                //Je commence par le 'point haut'
                //'x'>0 et 'y'>=0 ('arc droit'=>abscisses, 'arc gauche'=>ordonnées)
                v_coordRef = GetCoordonneesDansNewReferentiel2D(v_pointsToTest, p_facette.p01_pointsDeFacette[0].p10_coord, p_facette.p01_pointsDeFacette[1].p10_coord, p_facette.p01_pointsDeFacette[2].p10_coord);

                if (v_coordRef == null) //SI la facette est 'plate' (possible à ce stade)=>Les points ne peuvent pas être "contenus". 
                                        //On risque par contre de perdre des points eux-mêmes strictement alignés sur l'alignement précédent
                {
                    p_facette.p10_pointsInclus = new List<BeanPoint_internal>();
                    return;
                }

                v_idPointsUtiles = new HashSet<int>(v_coordRef.Where(c => c.Value[0] >= 0 && c.Value[1] >= 0).Select(c => c.Key).ToList());
                v_pointsToTest = v_pointsToTest.Where(c => v_idPointsUtiles.Contains(c.p00_id)).ToList();
                //Puis...
                v_coordRef = GetCoordonneesDansNewReferentiel2D(v_pointsToTest, p_facette.p01_pointsDeFacette[1].p10_coord, p_facette.p01_pointsDeFacette[0].p10_coord, p_facette.p01_pointsDeFacette[2].p10_coord);
                //'x'>=0 et 'y'>=0 ('arc gauche'=>abscisses, 'arc base'=>ordonnées)
                v_idPointsUtiles = new HashSet<int>(v_coordRef.Where(c => c.Value[0] >= 0 && c.Value[1] >= 0).Select(c => c.Key).ToList());
                v_pointsToTest = v_pointsToTest.Where(c => v_idPointsUtiles.Contains(c.p00_id)).ToList();
                BeanPoint_internal v_point;
                for (int v_indicePoint = 0; v_indicePoint < v_pointsToTest.Count; v_indicePoint++)
                {
                    v_point = v_pointsToTest[v_indicePoint];
                    v_point.p22_estPointInclus_vf = true;
                }
                p_facette.p10_pointsInclus.AddRange(v_pointsToTest);
            }
            catch (Exception)
            {

                throw;
            }
        }
        private void RattachePointsToCoupleDeFacettes(ref List<BeanPoint_internal> p_tousPoints, ref BeanFacette_internal p_facette1, ref BeanFacette_internal p_facette2)
        {
            try
            {
                Dictionary<int, double[]> v_coordPointsInclus = p_tousPoints.ToDictionary(c => c.p00_id, c => c.p10_coord);
                //
                BeanArc_internal v_arcInterfacade = GetArcCommunEntreFacettes(p_facette1, p_facette2);
                //On identifie le 3ème point de la première façade...
                HashSet<string> v_pointsDeLArc = new HashSet<string>();
                v_pointsDeLArc.Add(v_arcInterfacade.p11_pointDbt.p01_hCodeGeog);
                v_pointsDeLArc.Add(v_arcInterfacade.p12_pointFin.p01_hCodeGeog);
                BeanPoint_internal v_3emePointFacette1 = p_facette1.p01_pointsDeFacette.Where(c => !v_pointsDeLArc.Contains(c.p01_hCodeGeog)).First();
                BeanPoint_internal v_3emePointFacette2 = p_facette2.p01_pointsDeFacette.Where(c => !v_pointsDeLArc.Contains(c.p01_hCodeGeog)).First();
               
                //...Et on va l'inclure dans la liste des points 
                //Son positionnement (au-dessus ou au-dessous) par rapport à l'arc va permettre de savoir où affecter les pts 'au-dessus' et 'ceux au-dessous'
                int v_pseudoId3emePoint = -1;
                if (v_coordPointsInclus.ContainsKey(-1))
                {
                    v_pseudoId3emePoint = v_coordPointsInclus.Min(c => c.Key) - 1;
                }
                v_coordPointsInclus.Add(v_pseudoId3emePoint, v_3emePointFacette1.p10_coord);

                //On calcule les coord des points par rapport à l'arc:
                Dictionary<int, double[]> v_coordPointsInclusParRapportAuNouvelArc;
                v_coordPointsInclusParRapportAuNouvelArc = GetCoordonneesDansNewReferentiel2D(v_coordPointsInclus, v_arcInterfacade.p11_pointDbt.p10_coord, v_arcInterfacade.p12_pointFin.p10_coord);
               
                //On identifie les id des points 'au-dessus'
                List<int> v_idPointsAuDessus;
                v_idPointsAuDessus=v_coordPointsInclusParRapportAuNouvelArc.Where(c => c.Value[1] >= 0 && c.Key != v_pseudoId3emePoint).Select(c => c.Key).ToList();
                HashSet<int> v_hsIdPointAuDessus = new HashSet<int>(v_idPointsAuDessus);

                //On partitionne en 2 listes
                List<BeanPoint_internal> v_listeDessus = new List<BeanPoint_internal>();
                List<BeanPoint_internal> v_listeDessous = new List<BeanPoint_internal>();
                //
                BeanPoint_internal v_ptModif;
                HashSet<string> v_pointsFacettes = new HashSet<string>();
                v_pointsFacettes.Add(v_arcInterfacade.p11_pointDbt.p01_hCodeGeog);
                v_pointsFacettes.Add(v_arcInterfacade.p12_pointFin.p01_hCodeGeog);
                v_pointsFacettes.Add(v_3emePointFacette1.p01_hCodeGeog);
                v_pointsFacettes.Add(v_3emePointFacette2.p01_hCodeGeog);

                foreach (BeanPoint_internal v_point in p_tousPoints)
                {
                    if(v_pointsFacettes.Contains(v_point.p01_hCodeGeog))
                   {
                        continue;
                    }
                    v_ptModif = v_point;
                    v_ptModif.p22_estPointInclus_vf = true;
                    if(v_hsIdPointAuDessus.Contains(v_ptModif.p00_id))
                    {
                        v_listeDessus.Add(v_ptModif);
                    }
                    else
                    {
                        v_listeDessous.Add(v_ptModif);
                    }
                }
                //=>On affecte l'une et l'autre liste
                if(v_coordPointsInclusParRapportAuNouvelArc[v_pseudoId3emePoint][1]>=0)
                {
                    p_facette1.p10_pointsInclus = v_listeDessus;
                    p_facette2.p10_pointsInclus = v_listeDessous;
                }
                else
                {
                    p_facette1.p10_pointsInclus = v_listeDessous;
                    p_facette2.p10_pointsInclus = v_listeDessus;
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
        private void RattachePointsTouchantFacettePlate(ref List<BeanPoint_internal> p_pointsInclus, ref BeanFacette_internal p_facettePlate, double p_toleranceEnUnitesDeReference)
        {
            try
            {
                if(!IsTriangleEstPlatDansLePlanXY(p_facettePlate.p01_pointsDeFacette[0], p_facettePlate.p01_pointsDeFacette[1], p_facettePlate.p01_pointsDeFacette[2]))
                {
                    throw new Exception("La facette a une surface dans le plan XY=>non conforme à l'attendu.");
                }
                //Récup des points dispo:
                List<BeanPoint_internal> v_pointsToTest = p_pointsInclus
                    .Where(c => !c.p21_estPointFacette_vf)
                    .Where(c => !c.p22_estPointInclus_vf).ToList();
                if(v_pointsToTest.Count==0)
                {
                    return;
                }
                //Récup du plus grand arc et de sa longueur
                double v_longueurPlusGrandArc = p_facettePlate.p02_arcs.Max(c => c.p32_longueurArcDansPlanXy);
                BeanArc_internal v_plusGrandArc = p_facettePlate.p02_arcs.Where(c => c.p32_longueurArcDansPlanXy == v_longueurPlusGrandArc).First();
                
                //Calcul des coordonnées des points dans un repère orthon de base 'plus grand arc'
                Dictionary<int,double[]> v_coordRef = GetCoordonneesDansNewReferentiel2D(v_pointsToTest, v_plusGrandArc.p11_pointDbt.p10_coord, v_plusGrandArc.p12_pointFin.p10_coord, null);

                //On retient les points de coord y 'APPROXIMATIVEMENT' =0 (donc présumés sur l'axe des abscisses) 
                //ET de norme comprise entre 0 et 1, non inclus (donc présumés strictement inclus dans le segment)

                HashSet<int>  v_idPointsUtiles = new HashSet<int>(v_coordRef.Where(c =>  c.Value[1] <= p_toleranceEnUnitesDeReference && c.Value[1] >=(-1)* p_toleranceEnUnitesDeReference && c.Value[0] > 0 && c.Value[0] < v_longueurPlusGrandArc).Select(c => c.Key).ToList());
                v_pointsToTest = v_pointsToTest.Where(c => v_idPointsUtiles.Contains(c.p00_id)).ToList();
                BeanPoint_internal v_point;
                for (int v_indicePoint = 0; v_indicePoint < v_pointsToTest.Count; v_indicePoint++)
                {
                    v_point = v_pointsToTest[v_indicePoint];
                    v_point.p22_estPointInclus_vf = true;
                }
                //
                p_facettePlate.p10_pointsInclus.AddRange(v_pointsToTest);
            }
            catch (Exception)
            {

                throw;
            }
        }

        private BeanArc_internal GetArcCommunEntreFacettes(BeanFacette_internal p_facette1, BeanFacette_internal p_facette2)
        {
            BeanArc_internal v_arcOut = null;
            try
            {
                HashSet<string> v_arcsFacettes2 = new HashSet<string>(p_facette2.p02_arcs.Select(c => c.p01_hcodeArc).ToList());
                foreach(BeanArc_internal v_arc in p_facette1.p02_arcs)
                {
                    if(v_arcsFacettes2.Contains(v_arc.p01_hcodeArc))
                    {
                        v_arcOut = v_arc;
                        break;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return v_arcOut;
        }
        private double getDistanceXyCarreeEntrePoints(BeanPoint_internal p_pt1, BeanPoint_internal p_pt2)
        {
            return ((p_pt2.p10_coord[1] - p_pt1.p10_coord[1]) * (p_pt2.p10_coord[1] - p_pt1.p10_coord[1])) +
                ((p_pt2.p10_coord[0] - p_pt1.p10_coord[0]) * (p_pt2.p10_coord[0] - p_pt1.p10_coord[0]));
        }

        private List<BeanPoint_internal> GetPointsInclusDansLeQuadrantDroitHautXY(IEnumerable<BeanPoint_internal> p_pointsAReferencer, double[] p_coordPointOrigine, double[] p_coordPoint2Abs, double[] p_coordPoint3Ord,bool p_strictementAuDessus_vf, bool p_strictementADroite_vf)
        {
            List<BeanPoint_internal> v_pointsOut = new List<BeanPoint_internal>();
            try
            {
                if(FLabServices.createCalculLow().AreDroitesParallelesXY(p_coordPointOrigine, p_coordPoint2Abs, p_coordPointOrigine, p_coordPoint3Ord))
                {
                    throw new Exception("Les points sont alignés.");
                }

                Dictionary<int, double[]> v_coordRef;
                v_coordRef = GetCoordonneesDansNewReferentiel2D(p_pointsAReferencer, p_coordPointOrigine, p_coordPoint2Abs, p_coordPoint3Ord);
                //Traitement à droite
                HashSet<int> v_idPointsADroite;
                 if(p_strictementADroite_vf)
                {
                    v_idPointsADroite= new HashSet<int>(v_coordRef.Where(c => c.Value[0] > 0 ).Select(c => c.Key).ToList());
                }
                else
                {
                    v_idPointsADroite = new HashSet<int>(v_coordRef.Where(c => c.Value[0] >= 0).Select(c => c.Key).ToList());
                }
                //
                if(v_idPointsADroite.Count==0)
                {
                    return v_pointsOut;
                }
                
                //Traitement à gauche
                HashSet<int> v_idPointsAuDessus;
                if (p_strictementAuDessus_vf)
                {
                    v_idPointsAuDessus = new HashSet<int>(v_coordRef.Where(c => v_idPointsADroite.Contains(c.Key) && c.Value[1] > 0).Select(c => c.Key).ToList());
                }
                else
                {
                    v_idPointsAuDessus = new HashSet<int>(v_coordRef.Where(c => v_idPointsADroite.Contains(c.Key) && c.Value[1] >= 0).Select(c => c.Key).ToList());
                }
                //
                if (v_idPointsAuDessus.Count == 0)
                {
                    return v_pointsOut;
                }
                return p_pointsAReferencer.Where(c => v_idPointsAuDessus.Contains(c.p00_id)).ToList();

            }
            catch (Exception)
            {

                throw;
            }
        }
        #endregion PRIVATE CALCUL TIN

        #region PRIVATE CALCUL CONVEXHULL
        /// <summary>
        /// Soit, en entrée, un segment de droite orienté S1 et un nuage de points N, décrits dans un BeanArc
        /// On veut génèrer 2 nouveaux arcs S2 et S3 tels que:
        /// S2 débute au premier point de S1, joigne le point Nx de N le plus éloigné orthogonalement, à gauche, de S1 avec une distance strictement >0
        /// S3 débute en Nx et aboutisse au dernier de S1
        /// S'il n'existe aucun point 'à gauche' de S1=>on retourne null.
        /// Attention: il n'y a pas de contrôle que le projeté de Nx sur S1 est inclus dans le segment S1.
        /// Usage originel de la méthode: génération du convexhull
        /// </summary>
        /// <param name="p_arcEnrichiSource"></param>
        /// <returns></returns>
        private List<BeanArc_internal> GetArcsConvexesGauches(BeanArc_internal p_arcEnrichiSource)
        {
            List<BeanArc_internal> v_pointConvex = new List<BeanArc_internal>();
            try
            {
                Dictionary<int, double[]> v_coordDansRef;
                v_coordDansRef = GetCoordonneesDansNewReferentiel2D(p_arcEnrichiSource.p31_pointsAssocies, p_arcEnrichiSource.p11_pointDbt.p10_coord, p_arcEnrichiSource.p12_pointFin.p10_coord, null);
                //
                HashSet<int> v_idPositifs = new HashSet<int>(v_coordDansRef.Where(c => c.Value[1] > 0).Select(c => c.Key).ToList());
                if (v_idPositifs.Count == 0)
                {
                    return null;
                }
                //(Les points '0' ne nous intéressent pas: ils ne peuvent pas appartenir au CH (sauf les 2 extrêmes mais qui sont déjà identifiés)
                List<BeanPoint_internal> v_pointsPositifs = p_arcEnrichiSource.p31_pointsAssocies.Where(c => v_idPositifs.Contains(c.p00_id)).ToList();

                int v_idPt1 = -1;
                double v_ecartMax = v_coordDansRef.Select(c => c.Value[1]).Max();
                if (v_ecartMax == 0)
                {
                    return null;
                }
                v_idPt1 = v_coordDansRef.Where(c => c.Value[1] == v_ecartMax).First().Key;
                //(Les arrondis font que, malgré tout, la distance de projection pt être indûment considérée comme non nulle
                //et qu'un point de l'arc original soit retenu
                if (v_idPt1 == p_arcEnrichiSource.p11_pointDbt.p00_id || v_idPt1 == p_arcEnrichiSource.p12_pointFin.p00_id)
                {
                    return null;
                }
                BeanPoint_internal v_points1 = v_pointsPositifs.Where(c => c.p00_id == v_idPt1).ToList().First();

                BeanArc_internal v_arc1 = new BeanArc_internal(p_arcEnrichiSource.p11_pointDbt, v_points1, v_pointsPositifs);
                BeanArc_internal v_arc2 = new BeanArc_internal(v_points1, p_arcEnrichiSource.p12_pointFin, v_pointsPositifs);
                //
                v_pointConvex.Add(v_arc1);
                v_pointConvex.Add(v_arc2);
            }
            catch (Exception)
            {
                throw;
            }
            return v_pointConvex;
        }
        #endregion PRIVATE CALCUL CONVEXHULL

        #region UTILITAIRES CALCUL
        /// <summary>
        /// Transpose les 'points à référencer' dans un repère 2D:
        /// d'origine p_coordPoint0
        /// de premier vecteur directeur i: p_coordPoint0=>p_coordPoint2Abs
        /// de second vecteur directeur j: p_coordPoint0=>p_coordPoint3Ord_orthoSiNull OU d'un vecteur normal à i, 
        ///   de même norme et d'origine p_coordPoint0 si p_coordPoint3Ord_orthoSiNull est null 
        ///   SI les points du repères sont colinéaires =>renvoie null
        /// </summary>
        /// <param name="p_pointsAReferencer"></param>
        /// <param name="p_coordPointOrigine"></param>
        /// <param name="p_coordPoint2Abs"></param>
        /// <param name="p_coordPoint3Ord_orthoSiNull"></param>
        /// <returns></returns>
        public Dictionary<int, double[]> GetCoordonneesDansNewReferentiel2D(IEnumerable<BeanPoint_internal> p_pointsAReferencer, double[] p_coordPointOrigine, double[] p_coordPoint2Abs, double[] p_coordPoint3Ord_orthoSiNull = null)
        {
            Dictionary<int, double[]> v_coords = new Dictionary<int, double[]>();
            try
            {
                ICalculServices_Low v_calcul = new CalculServices_Low();
                bool v_normaliser_vf = true;
                double[,] v_matriceDeConversion;
                v_matriceDeConversion = v_calcul.GetMatriceChangementDeRepereXY(p_coordPointOrigine, p_coordPoint2Abs, p_coordPoint3Ord_orthoSiNull, v_normaliser_vf);
                //
                if (v_matriceDeConversion==null)
                {
                    return null;
                }
                double[] v_coord;
                foreach (BeanPoint_internal v_point in p_pointsAReferencer)
                {
                    v_coord = v_calcul.GetCoordDansNewRepereXY(v_matriceDeConversion, p_coordPointOrigine, v_point.p10_coord);
                    v_coords.Add(v_point.p00_id, v_coord);
                }
            }
            catch (Exception)
            {
                throw;
            }
            return v_coords;
        }
        public Dictionary<int, double[]> GetCoordonneesDansNewReferentiel2D(Dictionary<int,double[]> p_coordPointsAReferencer, double[] p_coordPointOrigine, double[] p_coordPoint2Abs, double[] p_coordPoint3Ord_orthoSiNull = null)
        {
            Dictionary<int, double[]> v_coords = new Dictionary<int, double[]>();
            try
            {
                ICalculServices_Low v_calcul = new CalculServices_Low();
                bool v_normaliser_vf = true;
                double[,] v_matriceDeConversion;
                v_matriceDeConversion = v_calcul.GetMatriceChangementDeRepereXY(p_coordPointOrigine, p_coordPoint2Abs, p_coordPoint3Ord_orthoSiNull, v_normaliser_vf);
                //
                if (v_matriceDeConversion == null)
                {
                    return null;
                }
                double[] v_coord;
                foreach (KeyValuePair<int, double[]> v_point in p_coordPointsAReferencer)
                {
                    v_coord = v_calcul.GetCoordDansNewRepereXY(v_matriceDeConversion, p_coordPointOrigine, v_point.Value);
                    v_coords.Add(v_point.Key, v_coord);
                }
            }
            catch (Exception)
            {
                throw;
            }
            return v_coords;
        }
        public double[] GetCoordonneesDansNewReferentiel2D(BeanPoint_internal p_pointAReferencer, double[] p_coordPointOrigine, double[] p_coordPoint2Abs, double[] p_coordPoint3Ord_orthoSiNull = null)
        {
            double[] v_coord = null;
            Dictionary<int, double[]> v_coords = new Dictionary<int, double[]>();
            try
            {
                ICalculServices_Low v_calcul = new CalculServices_Low();
                bool v_normaliser_vf = true;
                double[,] v_matriceDeConversion;
                v_matriceDeConversion = v_calcul.GetMatriceChangementDeRepereXY(p_coordPointOrigine, p_coordPoint2Abs, p_coordPoint3Ord_orthoSiNull, v_normaliser_vf);
                v_coord = v_calcul.GetCoordDansNewRepereXY(v_matriceDeConversion, p_coordPointOrigine, p_pointAReferencer.p10_coord);
            }
            catch (Exception)
            {

                throw;
            }
            return v_coord;
        }
        #endregion UTILITAIRES CALCUL

        #region UTILITAIRES DIVERS
        private string GetHCodeCoupleFacettes(BeanFacette_internal p_facette1, BeanFacette_internal p_facette2)
        {
            return Math.Min(p_facette1.p00_idFacette, p_facette2.p00_idFacette) + "_" + Math.Max(p_facette1.p00_idFacette, p_facette2.p00_idFacette);
        }
        public Dictionary<string, int> GetEtComptePointsDoublonnes(List<BeanPoint_internal> p_pointsToTest)
        {
            Dictionary<string, int> v_dicoDoublons = new Dictionary<string, int>();

            v_dicoDoublons = p_pointsToTest.GroupBy(c => c.p01_hCodeGeog).ToDictionary(c => c.Key, c => c.Count());
            v_dicoDoublons = v_dicoDoublons.Where(c => c.Value > 1).ToDictionary(c => c.Key, c => c.Value);

            return v_dicoDoublons;
        }
        public double GetLongueurArcAuCarre(BeanPoint_internal p_point1, BeanPoint_internal p_point2)
        {
            return FLabServices.createCalculLow().GetDistanceEuclidienneCarreeXYZ(p_point1.p10_coord, p_point2.p10_coord);
        }
        #endregion UTILITAIRES DIVERS

        
    

        public List<string> GetOrdonnancementArcsAutourPointFacette(BeanPoint_internal p_pointFacette, int p_idPremierArc, bool p_sensHoraireSinonAntihoraire_vf)
        {
            List<string> v_codeArcsOrdonnes = null;
            try
            {
                if (p_pointFacette.p41_arcsAssocies == null)
                {
                    return null;
                }
                if (p_pointFacette.p41_arcsAssocies.Count == 0)
                {
                    return new List<string>();
                }
                //
                Dictionary<int, double[]> v_arcsToTest = new Dictionary<int, double[]>();
                Dictionary<int, double[]> v_d1 = p_pointFacette.p41_arcsAssocies.Where(c => c.Value.p11_pointDbt.p00_id == p_pointFacette.p00_id).ToDictionary(c => c.Value.p00_idArc, c => c.Value.p12_pointFin.p10_coord);
                Dictionary<int, double[]> v_d2 = p_pointFacette.p41_arcsAssocies.Where(c => c.Value.p12_pointFin.p00_id == p_pointFacette.p00_id).ToDictionary(c => c.Value.p00_idArc, c => c.Value.p11_pointDbt.p10_coord);
                v_arcsToTest = v_d1.Union(v_d2).ToDictionary(c => c.Key, c => c.Value);
                //
                List<int> v_idArcsOrdonnes = FLabServices.createCalculLow().GetOrdonnancement(v_arcsToTest, p_pointFacette.p10_coord, p_idPremierArc, p_sensHoraireSinonAntihoraire_vf);
                Dictionary<int, string> v_correspondanceIdCode = p_pointFacette.p41_arcsAssocies.ToDictionary(c => c.Value.p00_idArc, c => c.Value.p01_hcodeArc);
                v_codeArcsOrdonnes = new List<string>();
                foreach (int v_id in v_idArcsOrdonnes)
                {
                    v_codeArcsOrdonnes.Add(v_correspondanceIdCode[v_id]);
                }
            }
            catch (Exception)
            {
                throw;
            }
            return v_codeArcsOrdonnes;
        }

        public void RecalculFacettes(ref BeanTopologieFacettes p_topol)
        {
            try
            {
                List<BeanFacette_internal> v_facettes = new List<BeanFacette_internal>();
                //
                BeanArc_internal v_arcInitial = p_topol.p12_arcsByCode.Values.First();
                Dictionary<string, int> v_nbreBordsTraites;
                v_nbreBordsTraites = p_topol.p12_arcsByCode.ToDictionary(c => c.Key, c => 0);
                //
                BeanFacette_internal v_facette;
                BeanArc_internal v_arcATester;
                int v_avct;
                foreach (string v_codeArc in v_nbreBordsTraites.Keys)
                {
                    v_avct = v_nbreBordsTraites[v_codeArc];
                    if (v_avct == 2)
                    {
                        continue;
                    }
                    v_arcATester = p_topol.p12_arcsByCode[v_codeArc];
                    if (v_avct == 0)
                    {
                        v_facette = ConstruitFacette(v_arcATester, ref p_topol, ref v_nbreBordsTraites, true);
                        v_facettes.Add(v_facette);
                        v_facette = ConstruitFacette(v_arcATester, ref p_topol, ref v_nbreBordsTraites, false);
                        v_facettes.Add(v_facette);
                        v_avct = 2;
                    }
                    if (v_avct == 1)
                    {
                        if(v_arcATester.p21_facetteGauche!=null)
                        {
                            v_facette = ConstruitFacette(v_arcATester, ref p_topol, ref v_nbreBordsTraites, true);
                        }
                        else
                        {
                            v_facette = ConstruitFacette(v_arcATester, ref p_topol, ref v_nbreBordsTraites, false);
                        }
                    }
                }
                p_topol.p13_facettesById = v_facettes.ToDictionary(c => c.p00_idFacette, c => c);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public Geometry GetGeometryPolygoneFacetteEtOrdonnePointsFacette(ref BeanFacette_internal p_facette, ref BeanTopologieFacettes p_topologieFacette)
        {
            Geometry v_geom = null;
            try
            {
                if(p_facette.p02_arcs==null || p_facette.p02_arcs.Count<3)
                {
                    return null;
                }
                Dictionary<string, BeanPoint_internal> v_pointsDuPolygone = new Dictionary<string, BeanPoint_internal>();
                BeanArc_internal v_arcInitial;
                BeanArc_internal v_arcToTest;
                BeanArc_internal v_arcCourant;
                
                Dictionary<string, BeanArc_internal> v_arcsParcelle = p_facette.p02_arcs.ToDictionary(c => c.p01_hcodeArc, c => c);
                Dictionary<string, bool> v_avanctTraittArcs = p_facette.p02_arcs.ToDictionary(c => c.p01_hcodeArc, c => false);
                //
                bool v_estClos_vf = false;
                v_arcInitial = v_arcsParcelle.First().Value;
                v_pointsDuPolygone.Add(v_arcInitial.p11_pointDbt.p01_hCodeGeog, v_arcInitial.p11_pointDbt);
                v_pointsDuPolygone.Add(v_arcInitial.p12_pointFin.p01_hCodeGeog, v_arcInitial.p12_pointFin);
                int v_nbreArcsATraiter = v_avanctTraittArcs.Count;
                int v_nbreArcsTraites = 1;
                //
                v_arcCourant = v_arcInitial;
                v_avanctTraittArcs[v_arcCourant.p01_hcodeArc] = true;
                //
                List<BeanArc_internal> v_pseudoListeArcSuivant;
                while(v_nbreArcsTraites<= v_nbreArcsATraiter+1)
                {
                    v_pseudoListeArcSuivant=v_arcCourant.p12_pointFin.p41_arcsAssocies
                    .Where(c => v_arcsParcelle.ContainsKey(c.Key) && c.Key != v_arcCourant.p01_hcodeArc)
                    .Select(c => c.Value)
                    .ToList();
                  
                    if(v_pseudoListeArcSuivant.Count!=1)
                    {
                        break;
                    }
                    v_arcToTest= v_pseudoListeArcSuivant.First();
                    if(v_nbreArcsTraites== v_nbreArcsATraiter && v_arcToTest.p01_hcodeArc==v_arcInitial.p01_hcodeArc)
                    {
                        v_estClos_vf = true;
                        break;
                    }
                  
                    if(v_avanctTraittArcs[v_arcToTest.p01_hcodeArc]==false)
                    {
                        v_arcCourant = v_arcToTest;
                    }
                    else
                    {
                        v_pseudoListeArcSuivant = v_arcCourant.p11_pointDbt.p41_arcsAssocies
                   .Where(c => v_arcsParcelle.ContainsKey(c.Key) && c.Key != v_arcCourant.p01_hcodeArc)
                   .Select(c => c.Value)
                   .ToList();

                        if (v_pseudoListeArcSuivant.Count != 1)
                        {
                            break;
                        }
                        v_arcToTest = v_pseudoListeArcSuivant.First();
                        if (v_nbreArcsTraites == v_nbreArcsATraiter && v_arcToTest.p01_hcodeArc == v_arcInitial.p01_hcodeArc)
                        {
                            v_estClos_vf = true;
                            break;
                        }
                        if (v_avanctTraittArcs[v_arcToTest.p01_hcodeArc] == false)
                        {
                            v_arcCourant = v_arcToTest;
                        }
                        else
                        {
                            return null;
                        }
                    }
                 
                    if(!v_pointsDuPolygone.ContainsKey(v_arcCourant.p11_pointDbt.p01_hCodeGeog))
                    {
                        v_pointsDuPolygone.Add(v_arcCourant.p11_pointDbt.p01_hCodeGeog, v_arcCourant.p11_pointDbt);
                    }
                    if (!v_pointsDuPolygone.ContainsKey(v_arcCourant.p12_pointFin.p01_hCodeGeog))
                    {
                        v_pointsDuPolygone.Add(v_arcCourant.p12_pointFin.p01_hCodeGeog, v_arcCourant.p12_pointFin);
                    }
                    v_nbreArcsTraites++;
                    v_avanctTraittArcs[v_arcCourant.p01_hcodeArc] = true;
                }
                //
                if (v_estClos_vf != true )
                {
                    return null;
                }
                //
                List<double[]> v_coordPoints;
                v_coordPoints = v_pointsDuPolygone.Values.Select(c => c.p10_coord).ToList();
                v_coordPoints.Add(v_coordPoints.First());
                //
                int v_srid = v_pointsDuPolygone.Values.First().p11_srid;
                //
                p_facette.p01_pointsDeFacette = v_pointsDuPolygone.Values.ToList();
                v_geom =FLabServices.createUtilitaires().GetGeometryPolygon(v_coordPoints, v_srid);
            }
            catch (Exception)
            {
                throw;
            }
            return v_geom;
        }
        private BeanFacette_internal ConstruitFacette(BeanArc_internal p_arcATesterDebut,ref BeanTopologieFacettes p_topol, ref Dictionary<string, int> p_nbreBordsTraitesParArc, bool p_aDroite_sinonAGauche)
        {
            BeanFacette_internal v_facette = new BeanFacette_internal();
            try
            {
                v_facette.p02_arcs = new List<BeanArc_internal>();
                v_facette.p01_pointsDeFacette = new List<BeanPoint_internal>();
                //
                if(p_aDroite_sinonAGauche)
                {
                    p_arcATesterDebut.p22_facetteDroite = v_facette;
                }
                else
                {
                    p_arcATesterDebut.p21_facetteGauche = v_facette;
                }
                p_nbreBordsTraitesParArc[p_arcATesterDebut.p01_hcodeArc]++;
                //
                v_facette.p02_arcs.Add(p_arcATesterDebut);
                v_facette.p01_pointsDeFacette.Add(p_arcATesterDebut.p11_pointDbt);
                //
               BeanArc_internal v_arcSuivant;
                v_arcSuivant = GetArcSuivant(p_arcATesterDebut, ref p_topol, p_aDroite_sinonAGauche);
                while (v_arcSuivant.p01_hcodeArc!= p_arcATesterDebut.p01_hcodeArc)
                {
                    v_facette.p02_arcs.Add(v_arcSuivant);
                    v_facette.p01_pointsDeFacette.Add(v_arcSuivant.p11_pointDbt);
                    //
                    if (p_aDroite_sinonAGauche)
                    {
                        v_arcSuivant.p22_facetteDroite = v_facette;
                    }
                    else
                    {
                        v_arcSuivant.p21_facetteGauche = v_facette;
                    }
                    p_nbreBordsTraitesParArc[v_arcSuivant.p01_hcodeArc]++;
                }
            }
            catch (Exception)
            {

                throw;
            }
            return v_facette;
        }
        private BeanArc_internal GetArcSuivant(BeanArc_internal p_arcCourant,ref BeanTopologieFacettes p_topol, bool p_aDroite_sinonAGauche)
        {
            BeanArc_internal v_arcRetour = null;
            try
            {
                BeanPoint_internal v_pointFin = p_arcCourant.p12_pointFin;
                List<string> v_arcsSuivantsOrdonnes = v_pointFin.p42_ordonnancementHorairesArcs;
              
                for (int v_index = 0; v_index < v_arcsSuivantsOrdonnes.Count; v_index++)
                {
                    if (v_arcsSuivantsOrdonnes[v_index] == p_arcCourant.p01_hcodeArc)
                    {
                        if(!p_aDroite_sinonAGauche)//A gauche
                        {
                            if (v_index > 0)
                            {
                                v_arcRetour = p_topol.p12_arcsByCode[v_arcsSuivantsOrdonnes[v_index - 1]];
                            }
                            else
                            {
                                v_arcRetour = p_topol.p12_arcsByCode[v_arcsSuivantsOrdonnes.Last()];
                            }
                        }
                        /////
                        ///  if(p_aDroite_sinonAGauche) //A droite
                        {
                            if (v_index < v_arcsSuivantsOrdonnes.Count - 1)
                            {
                                v_arcRetour = p_topol.p12_arcsByCode[v_arcsSuivantsOrdonnes[v_index + 1]];
                            }
                            else
                            {
                                v_arcRetour = p_topol.p12_arcsByCode[v_arcsSuivantsOrdonnes.First()];
                            }
                        }
                        break;
                    }
                }//FIN FOR
                ReorienteArcSiBesoin(ref v_arcRetour, ref p_topol, v_pointFin);
            }
            catch (Exception)
            {
                throw;
            }
            return v_arcRetour;
        }
        public bool ReorienteArcSiBesoin(ref BeanArc_internal p_arc, ref BeanTopologieFacettes p_topol,BeanPoint_internal p_ptDebut)
        {
            if (p_arc.p12_pointFin == p_ptDebut)
            {
                InverserArc(ref p_arc, ref p_topol);
                return true;
            }
          if (p_arc.p11_pointDbt== p_ptDebut)
            {
                return false;
            }
            throw new Exception("Le point " + p_ptDebut.p00_id + " n'est pas un point de l'arc " + p_arc.p00_idArc);
        }
        public void InverserArc(ref BeanArc_internal p_arc, ref BeanTopologieFacettes p_topol)
        {
            BeanPoint_internal p_ptDebut = p_arc.p11_pointDbt;
            BeanPoint_internal p_ptFin = p_arc.p12_pointFin;
            BeanFacette_internal p_facG = p_arc.p21_facetteGauche;
            BeanFacette_internal p_facD = p_arc.p22_facetteDroite;
            //
            p_arc.p12_pointFin = p_ptDebut;
            p_arc.p11_pointDbt = p_ptFin;
            p_arc.p21_facetteGauche = p_facD;
            p_arc.p22_facetteDroite = p_facG;
        }
        public bool SupprimerUneFacette(ref BeanTopologieFacettes p_topologieFacette,ref BeanFacette_internal p_facetteASupprimer,bool p_seulementSiFacetteExterne_vf)
        {
            bool v_parcelleSupprimee_vf=false;
            try
            {
                if (p_seulementSiFacetteExterne_vf && p_facetteASupprimer.p02_arcs.Where(c => c.p20_statutArc == enumStatutArc.arcExterne).Count()==0)
                {
                    return false;
                }
                List<BeanArc_internal> v_arcsSupprimer = p_facetteASupprimer.p02_arcs.Where(c => c.p20_statutArc == enumStatutArc.arcExterne).ToList();
               
                List<BeanArc_internal> v_arcsNonExternes = p_facetteASupprimer.p02_arcs.Where(c => c.p20_statutArc != enumStatutArc.arcExterne).ToList();
                foreach(BeanArc_internal v_arcNonExterne in v_arcsNonExternes)
                {
                    v_arcNonExterne.p20_statutArc = enumStatutArc.arcExterne;
                    if (v_arcNonExterne.p21_facetteGauche.p00_idFacette == p_facetteASupprimer.p00_idFacette)
                    {
                        v_arcNonExterne.p21_facetteGauche = null;
                    }
                    else
                    {
                        v_arcNonExterne.p22_facetteDroite = null;
                    }
                }
                p_topologieFacette.FacetteSupprimer(p_facetteASupprimer);
                List<string> v_hcodesASupprimer=v_arcsSupprimer.Select(c => c.p01_hcodeArc).ToList();
                BeanArc_internal v_arcASupprimer;
                foreach (string v_codeArcASupprimer in v_hcodesASupprimer)
                {
                    v_arcASupprimer = p_topologieFacette.p12_arcsByCode[v_codeArcASupprimer];
                    p_topologieFacette.ArcSupprimer(v_arcASupprimer);
                }
            }
            catch (Exception)
            {
                throw;
            }
            return v_parcelleSupprimee_vf;
        }
    }
}