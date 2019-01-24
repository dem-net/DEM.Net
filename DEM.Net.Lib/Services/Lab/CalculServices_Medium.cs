using DEM.Net.Lib.Services.VisualisationServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace DEM.Net.Lib.Services.Lab
{
    public class CalculServices_Medium : ICalculServices_Medium, ICalculServicesMedium_testDivers
    {
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
                v_parametresDuTin.p31_nbreIterationsMaxi = 20;
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
              
                //v_topologieFacette.p11_pointsFacettesByIdPoint = v_pointsFrontieres.ToDictionary(c => c.p00_id, c => c);
                //v_topologieFacette.p11_pointsFacettesByIdPoint.Add(v_meilleurPoint.p00_id, v_meilleurPoint);
                v_topologieFacette.PointsFacAjouter(v_pointsFrontieres);
                v_topologieFacette.PointFacAjouter(v_meilleurPoint);
                //
                List<BeanArc_internal> v_arcsFacette = v_facettesInitiales.SelectMany(c => c.p02_arcs).Distinct().ToList();
                //v_topologieFacette.p12_arcsByCode = v_arcsFacette.ToDictionary(c => c.p01_hcodeArc, c => c);
                v_topologieFacette.ArcsAjouter(v_arcsFacette);

                //v_topologieFacette.p13_facettesById = v_facettesInitiales.ToDictionary(c => c.p00_idFacette, c => c);
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
                v_facettesTriees[v_indice].p23_facetteEcartSup = v_facettesTriees[v_indice - 1];
                //=>+ Référencement de la première cellule
                p_topologieFacette.p21_facetteAvecEcartAbsoluMax = v_facettesTriees.First();


                //On parcourt la liste chaînée:
                do
                {
                    TraitementDeLaFacetteMaxiByRef(ref p_topologieFacette, p_topologieFacette.p21_facetteAvecEcartAbsoluMax, p_parametresDuTin);
                }
                while (p_topologieFacette.p21_facetteAvecEcartAbsoluMax.p24_facetteEcartInf != null);

            }
            catch (Exception)
            {
                throw;
            }

        }

        public void SetLignesCretesEtTalwegByRef(ref BeanTopologieFacettes p_topologieFacette)
        {
            try
            {
                foreach (string v_codeArc in p_topologieFacette.p12_arcsByCode.Keys)
                {
                    SetLignesCretesEtTalwegByRefByArc(p_topologieFacette, v_codeArc);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        private void SetLignesCretesEtTalwegByRefByArc(BeanTopologieFacettes p_topologieFacette, string p_codeArcATraiter)
        {
            try
            {
                BeanArc_internal v_arc = p_topologieFacette.p12_arcsByCode[p_codeArcATraiter];

                //L'arc sépare 2 facettes (sauf en frontière)
                //On va exprimer les 2 points n'appartenant pas à l'arc dans le plan de plus forte pente 
                //Si les 2 points sont 'au-dessus' alors on considère que l'arc est 'talweg'
                //Si les 2 points sont 'au-dessous' alors on considère que l'arc est 'ligne de crète'
                //Les autres cas correspondent, au plus, à des ruptures de pente

                //-On détermine le plan.
                //Ce plan doit être tel que:
                //-sa pente est celle de l'arc
                //-si on translate le vecteur correspondant selon un vecteur de même élévation constante, alors ce vecteur doit être inclus dans ce plan.
                //Du coup, pour déterminer ce plan, on détermine on utilise le vecteur de l'arc et un vecteur normal en xy à ce vecteur
                double[] v_vecteurArc = FLabServices.createCalculLow().GetVectorBrutFromTwoPoints(v_arc.p11_pointDbt.p10_coord, v_arc.p12_pointFin.p10_coord);
                double[] v_vecteurNormalXy = new double[3] { -1 * v_vecteurArc[1], v_vecteurArc[0], 0 };
                double[] v_pointNormal = new double[3] { v_vecteurNormalXy[0] + v_arc.p11_pointDbt.p10_coord[0], v_vecteurNormalXy[1] + v_arc.p11_pointDbt.p10_coord[1], v_arc.p11_pointDbt.p10_coord[2] };

                double[] v_vecteurNormalAuPlanDePenteMaxi;
                v_vecteurNormalAuPlanDePenteMaxi = FLabServices.createCalculLow().GetNormaleDuPlan(v_arc.p11_pointDbt.p10_coord, v_arc.p12_pointFin.p10_coord, v_pointNormal);

                //=>On calcule la matrice inverse...
                v_vecteurArc = FLabServices.createCalculLow().GetNormalisationVecteurXYZ(v_vecteurArc);
                v_vecteurNormalXy = FLabServices.createCalculLow().GetNormalisationVecteurXYZ(v_vecteurNormalXy);
                double[,] v_matriceDeRotation = FLabServices.createCalculLow().GetMatriceInverse3x3(v_vecteurArc, v_vecteurNormalXy, v_vecteurNormalAuPlanDePenteMaxi);

                //...on l'applique sur les 2 points
                List<double[]> v_coordonnees = new List<double[]>();
                double[] v_coordDansLeRepereDuPlan;
                BeanPoint_internal v_pointATester;
                double[] v_vectorATester;
                if (v_arc.p21_facetteGauche != null)
                {
                    v_pointATester = v_arc.p21_facetteGauche.p01_pointsDeFacette.Where(c => c.p00_id != v_arc.p11_pointDbt.p00_id && c.p00_id != v_arc.p12_pointFin.p00_id).First();
                    v_vectorATester = FLabServices.createCalculLow().GetVectorBrutFromTwoPoints(v_arc.p11_pointDbt.p10_coord, v_pointATester.p10_coord);
                    v_coordDansLeRepereDuPlan = FLabServices.createCalculLow().GetProduitMatriceParVector(v_matriceDeRotation, v_vectorATester);
                    v_coordonnees.Add(v_coordDansLeRepereDuPlan);
                }
                if (v_arc.p22_facetteDroite != null)
                {
                    v_pointATester = v_arc.p22_facetteDroite.p01_pointsDeFacette.Where(c => c.p00_id != v_arc.p11_pointDbt.p00_id && c.p00_id != v_arc.p12_pointFin.p00_id).First();
                    v_vectorATester = FLabServices.createCalculLow().GetVectorBrutFromTwoPoints(v_arc.p11_pointDbt.p10_coord, v_pointATester.p10_coord);
                    v_coordDansLeRepereDuPlan = FLabServices.createCalculLow().GetProduitMatriceParVector(v_matriceDeRotation, v_vectorATester);
                    v_coordonnees.Add(v_coordDansLeRepereDuPlan);
                }
                //On exploite l'info:
                //(pour l'instant, on ne traite pas les arcs frontières)
                v_arc.p41_natureArcDansLeReseau = enumTypeArcReseau.autre;
                if (v_coordonnees.Where(c => c[2] > 0).Count() > 1)
                {
                    v_arc.p41_natureArcDansLeReseau = enumTypeArcReseau.talweg;
                    return;
                }
                if (v_coordonnees.Where(c => c[2] < 0).Count() > 1)
                {
                    v_arc.p41_natureArcDansLeReseau = enumTypeArcReseau.crete;
                    return;
                }
            }
            catch (Exception)
            {

                throw;
            }
        }



        //
        private void UpdateNormaleDuPlan(BeanFacette_internal p_facette)
        {

        }
        private void TraitementDeLaFacetteMaxiByRef(ref BeanTopologieFacettes p_topologieFacette, BeanFacette_internal p_facetteATraiter, BeanParametresDuTin p_parametresDuTin)
        {
            try
            {
                //On traite la "première facette" (c'est à dire celle avec l'écart maxi)
                //A l'intérieur du tétraèdre les modifications ne sont pas possibles (tous les couples de facettes sont concaves)
                //Maintenant...on doit tester le contact de chaque facette du tétraèdre avec les facettes extérieures, si elles existent.
                //Cela doit se faire autour des arètes extérieures
                //Certains couples seront modifiés, d'autres pas (mais ils auront été qd même contrôlés et validés).
                //Lorsqu'il y a modif du couple (bascule des triangles du quadrilatère), les couples périphériques deviennent, eux-mêmes, candidats à la bascule.

                BeanResultatConversions_internal v_rapportResultTetraedre;
                v_rapportResultTetraedre = GetTetraedreByFacette(ref p_topologieFacette, p_facetteATraiter.p00_idFacette, p_facetteATraiter.p22_pointPlusGrandEcart);

                List<int> v_idNouvellesFacettesBrutes = new List<int>(v_rapportResultTetraedre.p02_newFacettes.Select(c => c.p00_idFacette));
                List<string> v_hcodeArcsATester = v_rapportResultTetraedre.p03_arcsCandidatsOut.Select(c => c.p01_hcodeArc).ToList();
                HashSet<string> v_HSCodesArcsATester = new HashSet<string>(v_hcodeArcsATester);

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
                                if (!v_HSCodesArcsATester.Contains(v_codeArcCandidat))
                                {
                                    v_HSCodesArcsATester.Add(v_codeArcCandidat);
                                    v_hcodeArcsATester.Add(v_codeArcCandidat);
                                }
                            }
                            v_idNouvellesFacettesBrutes.AddRange(v_rapportResultBascule.p02_newFacettes.Select(c => c.p00_idFacette));
                        }
                    }
                }

                //On va calculer l'excentration de chaque parcelle et introduire la parcelle dans la chaine de tri
                double v_ecartMini = p_parametresDuTin.p21_enrichissement_modeChoixDuPointCentral.p01_excentrationMinimum;
                v_idNouvellesFacettesBrutes = v_idNouvellesFacettesBrutes.Distinct().ToList();
                BeanFacette_internal v_facettePourMaj;
                bool v_nullSiInfEcentrationMinimale_vf = false;

                //List<BeanFacette_internal> v_facetteToInsert = new List<BeanFacette_internal>();
                foreach (int v_idNewFacette in v_idNouvellesFacettesBrutes)
                {
                    if (!p_topologieFacette.p13_facettesById.ContainsKey(v_idNewFacette))
                    {
                        continue;
                    }
                    v_facettePourMaj = p_topologieFacette.p13_facettesById[v_idNewFacette];
                    GetAndSetByRefPointExcentreDeLaFacette(ref v_facettePourMaj, p_parametresDuTin.p21_enrichissement_modeChoixDuPointCentral, v_nullSiInfEcentrationMinimale_vf);
                    //v_facetteToInsert.Add(v_facettePourMaj);
                    InsertDansListeChaineeDesFacettes(ref p_topologieFacette, v_facettePourMaj, v_ecartMini);
                }
                //InsertDansListeChaineeDesFacettes(ref p_topologieFacette, v_facetteToInsert, v_ecartMini);
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

                //On marque le 'point central' comme 'point facette':
                p_pointCentral.p21_estPointFacette_vf = true;
                p_pointCentral.p22_estPointInclus_vf = true;
               // p_topologieCible.p11_pointsFacettesByIdPoint.Add(p_pointCentral.p00_id, p_pointCentral);
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

                //On initialise les nouveaux arcs:
                //A VOIR -1?
                BeanArc_internal v_arc;
                List<BeanArc_internal> v_arcsRayonnants = new List<BeanArc_internal>();
                for (int v_indicePointBase = 0; v_indicePointBase < v_pointsFacetteSource.Count; v_indicePointBase++)
                {
                    v_arc = new BeanArc_internal(p_pointCentral, v_pointsFacetteSource[v_indicePointBase]);
                    v_arc.p20_statutArc = enumStatutArc.arcNONCandidatASuppression;

                    v_arcsRayonnants.Add(v_arc);
                    //p_topologieCible.p12_arcsByCode.Add(v_arc.p01_hcodeArc, v_arc);
                    p_topologieCible.ArcAjouter(v_arc);
                    v_beanRapportOut.p04_arcsAExclureOut.Add(v_arc);
                }

                //On génère les facettes
                BeanFacette_internal v_facette;
                List<int> v_indicesPoints = new List<int> { 0, 1, 2, 0 };
                BeanPoint_internal v_point2;
                BeanPoint_internal v_point3;
                BeanArc_internal v_arcDescendant;
                BeanArc_internal v_arcMontant;
                BeanArc_internal v_arcBase;
                List<BeanArc_internal> v_arcCandidatBase;
                for (int i = 0; i < 3; i++)
                {
                    v_point2 = v_pointsFacetteSource[v_indicesPoints[i]];
                    v_point3 = v_pointsFacetteSource[v_indicesPoints[i + 1]];
                    //
                    v_facette = new BeanFacette_internal();
                    v_facette.p01_pointsDeFacette.Add(p_pointCentral);
                    v_facette.p01_pointsDeFacette.Add(v_point2);
                    v_facette.p01_pointsDeFacette.Add(v_point3);
                    //
                    v_arcDescendant = v_arcsRayonnants.Where(c => c.p12_pointFin.p01_hCodeGeog == v_point2.p01_hCodeGeog).First();
                    v_arcDescendant.p21_facetteGauche = v_facette;
                    //
                    v_arcMontant = v_arcsRayonnants.Where(c => c.p12_pointFin.p01_hCodeGeog == v_point3.p01_hCodeGeog).First();
                    v_arcMontant.p22_facetteDroite = v_facette;
                    //
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
                    //Récupération des points inclus:
                    RattachePointsToFacette(ref v_pointsInclus, ref v_facette);
                    //
                    //p_topologieCible.p13_facettesById.Add(v_facette.p00_idFacette, v_facette);
                    p_topologieCible.FacetteAjouter(v_facette);

                    v_beanRapportOut.p02_newFacettes.Add(v_facette);
                }

                //On supprime la facette d'origine
                //RemoveFacetteFromTopologieByRef(ref p_topologieCible, p_idFacetteSource);
                p_topologieCible.FacetteSupprimer(p_idFacetteSource);
            }
            catch (Exception)
            {

                throw;
            }
            return v_beanRapportOut;
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

                //=>On teste si les cercles circonscrits à l'un et l'autre triangle incluent "le 4ème point" (=celui appartenant à l'autre triangle et pas à l'arc) 
                //(On utilise ici une méthode 'explicite' qui calcule le centre, le rayon afférent et l'écart à ce rayon:
                //=>Ne nous semble pas plus coûteux que par le test du déterminant qui implique de connaître l'ordonnancement horaire/anti-horaire des points des triangles)
                List<double[]> v_pointsDuTriangleAvantBascule;
                v_pointsDuTriangleAvantBascule = v_facetteGauche.p01_pointsDeFacette.Select(t => t.p10_coord).ToList();
                bool v_isPointDansLeCercle1_vf = FLabServices.createCalculLow().IsPointDDansCercleCirconscritAuTriangleExplicite(v_pointsDuTriangleAvantBascule, v_pointDroitNewArc.p10_coord);
                v_pointsDuTriangleAvantBascule = v_facetteDroite.p01_pointsDeFacette.Select(t => t.p10_coord).ToList();
                bool v_isPointDansLeCercle2_vf = FLabServices.createCalculLow().IsPointDDansCercleCirconscritAuTriangleExplicite(v_pointsDuTriangleAvantBascule, v_pointGaucheNewArc.p10_coord);

                //Si les 2 cerccles circonscrits sont "vides"=>alors, la conformation est OK=> inutile de modifier.
                if (!v_isPointDansLeCercle1_vf && !v_isPointDansLeCercle2_vf)
                {
                    v_arcToTest.p20_statutArc = enumStatutArc.arcNONCandidatASuppression;
                    v_beanRapportOut.p00_modif_vf = false;
                    return v_beanRapportOut;
                }

                //On ne peut, toutefois, modifier que si le quadrilatère est strictement convexe 
                //(Si ce n'est pas le cas, la "2ème diagonale" est  partiellement extérieure ou sur confondues avec 2 arètes du quadrilatère; la modif n'est pas possible
                //On teste si l'"arc de remplacement" (correspondant à cette "2de diagonale") intersecte strictement l'arc à remplacer dans le plan XY?
                List<BeanPoint_internal> v_ptsDeLArcTeste = new List<BeanPoint_internal>() { v_arcToTest.p11_pointDbt, v_arcToTest.p12_pointFin };
                if (!FLabServices.createCalculLow().AreSegmentsSequants(v_arcToTest.p11_pointDbt.p10_coord, v_arcToTest.p12_pointFin.p10_coord, v_pointGaucheNewArc.p10_coord, v_pointDroitNewArc.p10_coord))
                {
                    v_beanRapportOut.p00_modif_vf = false;
                    return v_beanRapportOut;
                }



                //B-PHASE B 'BASCULE':
                //=>On va effectuer la découpe du 'quadrilatère' selon la 2de diagonale, en 2 nouveax triangles
                //=>On va également référencer les 4 arètes externes.
                //? Elles vont être, elles-mêmes, candidates, en sortie à de nouveaux tests de partition.
                //On pourrait renvoyer un simple bool (modifié/non modifié) et récupérer en sortie ces arcs ou/et les flaguer comme 'candidats' 
                //mais permet d'éviter des filtres inutiles
                //Noter encore: on ne met pas à jour ici le statut 'candidat à modif': risquerait de perturber la version 1 du projet: VOIR A LA REFACTO

                v_beanRapportOut.p00_modif_vf = true;
                v_beanRapportOut.p01_idFacettesSupprimees.Add(v_facetteGauche.p00_idFacette);
                v_beanRapportOut.p01_idFacettesSupprimees.Add(v_facetteDroite.p00_idFacette);

                //Action...:
                //On avait des triangles respectivement à droite et à gauche de l'arc test.
                //=>On créé 2 nouveaux triangles.
                //Utilisant la "2de diagonale", l'un sera, bien sur, 'à droite' de ce nouvel arc, l'autre à 'gauche'...
                //...mais, dans le référentiel déterminé par les 2 diagonales, On va donc considérer que ces 2 triangles sont l'un  'haut', l'autre 'bas'.

                BeanArc_internal v_newArc = new BeanArc_internal(v_pointGaucheNewArc, v_pointDroitNewArc);
                //v_beanRapportOut.p04_arcsAExclureOut.Add(v_newArc);

                BeanFacette_internal v_newFacetteHaute = new BeanFacette_internal();
                BeanFacette_internal v_newFacetteBasse = new BeanFacette_internal();
                v_beanRapportOut.p02_newFacettes.Add(v_newFacetteHaute);
                v_beanRapportOut.p02_newFacettes.Add(v_newFacetteBasse);

                //La facette 'haute' va être déclarée comme celle pour laquelle le '1er point' de l'arc test est 'au-dessus' du 'nouvel arc' 
                //Le choix est donc arbitraire mais on a besoin de le faire pour effectuer, plus tard, la répartition des 'points inclus' entre l'une et l'autre facette
                bool v_facetteHauteAuDessus_vf;
                Dictionary<int, double[]> v_positionDesPointsDeLArcTest_ParRapportAuNouvelArc;
                v_positionDesPointsDeLArcTest_ParRapportAuNouvelArc = GetCoordonneesDansNewReferentiel2D(v_ptsDeLArcTeste, v_pointGaucheNewArc.p10_coord, v_pointDroitNewArc.p10_coord);

                if (v_positionDesPointsDeLArcTest_ParRapportAuNouvelArc.Where(c => c.Key == v_arcToTest.p11_pointDbt.p00_id).Where(c => c.Value[1] > 0).Count() == 1)
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

                //L'arc 'montant' est censé partir du point gauche du nouvel arc vers le point opposé à cet arc, ici le pt de début de l'arc à tester
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

                //L'arc 'decendant' est censé partir du  pt de début de l''arc à tester' et redescende sur le point droit du nouvel arc
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
                    //p_topologieFacette.p12_arcsByCode.Add(v_newArc.p01_hcodeArc, v_newArc);
                    //
                    p_topologieFacette.ArcAjouter(v_newArc);
                }
                else
                {
                    v_doublonsArcs.Add(v_newArc);
                }
                //p_topologieFacette.p12_arcsByCode.Remove(p_hcodeArcCandidatASuppression);
                p_topologieFacette.ArcSupprimer(p_hcodeArcCandidatASuppression);

                //2- Les facettes:
                //p_topologieFacette.p13_facettesById.Add(v_newFacetteHaute.p00_idFacette, v_newFacetteHaute);
                //p_topologieFacette.p13_facettesById.Add(v_newFacetteBasse.p00_idFacette, v_newFacetteBasse);
                //RemoveFacetteFromTopologieByRef(ref p_topologieFacette, v_facetteGauche.p00_idFacette);
                //RemoveFacetteFromTopologieByRef(ref p_topologieFacette, v_facetteDroite.p00_idFacette);

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

        #region UTILITAIRES
        public List<string> GetOrdonnancementArcsAutourPointFacette(BeanPoint_internal p_pointFacette,int p_idPremierArc, bool p_sensHoraireSinonAntihoraire_vf)
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
        //private void RemoveFacetteFromTopologieByRef(ref BeanTopologieFacettes p_topologie, int p_idFacette)
        //{
        //    try
        //    {
        //        BeanFacette_internal v_facetteASupprimer = p_topologie.p13_facettesById[p_idFacette];
        //        //
        //        if (v_facetteASupprimer.p23_facetteEcartSup != null)
        //        {
        //            v_facetteASupprimer.p23_facetteEcartSup.p24_facetteEcartInf = v_facetteASupprimer.p24_facetteEcartInf;
        //        }

        //        if (v_facetteASupprimer.p24_facetteEcartInf != null)
        //        {
        //            v_facetteASupprimer.p24_facetteEcartInf.p23_facetteEcartSup = v_facetteASupprimer.p23_facetteEcartSup; //(Qui peut être nulle)
        //        }
        //        if (p_topologie.p21_facetteAvecEcartAbsoluMax == v_facetteASupprimer) //(La facette à supprimer était la 'première'
        //        {
        //            p_topologie.p21_facetteAvecEcartAbsoluMax = v_facetteASupprimer.p24_facetteEcartInf;
        //        }

        //        //
        //        p_topologie.p13_facettesById.Remove(p_idFacette);
        //    }
        //    catch (Exception)
        //    {

        //        throw;
        //    }
        //}


        private string GetHCodeCoupleFacettes(BeanFacette_internal p_facette1, BeanFacette_internal p_facette2)
        {
            return Math.Min(p_facette1.p00_idFacette, p_facette2.p00_idFacette) + "_" + Math.Max(p_facette1.p00_idFacette, p_facette2.p00_idFacette);
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
                double[] v_normaleDuPlan;
                v_normaleDuPlan = FLabServices.createCalculLow().GetNormaleDuPlan(v_coordDesPointsFacettes[0], v_coordDesPointsFacettes[2], v_coordDesPointsFacettes[1]);
                p_facette.p20_normaleDuPlan = v_normaleDuPlan;
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

                    //Récupération des points inclus:
                    RattachePointsToFacette(ref p_tousPointsInclus, ref v_facette);

                    //Injection de la facette dans la liste en retour
                    v_facettesOut.Add(v_facette);
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
        private void RattachePointsToFacette(ref List<BeanPoint_internal> p_pointsInclus, ref BeanFacette_internal p_facette)
        {
            try
            {
                ////PUSTULE POUR TEST: il semble y exister des points qui seraient en position de 'points facettes' non déclarés?? 
                //HashSet<string> v_hcodesPointsFacettes = new HashSet<string>(p_facette.p01_pointsDeFacette.Select(c => c.p01_hCodeGeog));
                //List<BeanPoint_internal> v_pointsLimites;
                //v_pointsLimites = p_pointsInclus
                //    .Where(c => !c.p21_estPointFacette_vf)
                //    .Where(c => v_hcodesPointsFacettes.Contains(c.p01_hCodeGeog)).ToList();

                //for (int i=0; i< v_pointsLimites.Count;i++)
                //{
                //    v_pointsLimites[i].p21_estPointFacette_vf = true;
                //}
                ////FIN PUSTULE POUR TEST

                List<BeanPoint_internal> v_pointsToTest = p_pointsInclus
                    .Where(c => !c.p21_estPointFacette_vf)
                    .Where(c => !c.p22_estPointInclus_vf).ToList();
                Dictionary<int, double[]> v_coordRef;
                HashSet<int> v_idPointsUtiles;

                //Je commence par le 'point haut'
                //'x'>0 et 'y'>=0 ('arc droit'=>abscisses, 'arc gauche'=>ordonnées)
                v_coordRef = GetCoordonneesDansNewReferentiel2D(v_pointsToTest, p_facette.p01_pointsDeFacette[0].p10_coord, p_facette.p01_pointsDeFacette[1].p10_coord, p_facette.p01_pointsDeFacette[2].p10_coord);
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
        //
        /// <summary>
        /// Transpose les 'points à référencer' dans un repère 2D:
        /// d'origine p_coordPoint0
        /// de premier vecteur directeur i: p_coordPoint0=>p_coordPoint2Abs
        /// de second vecteur directeur j: p_coordPoint0=>p_coordPoint3Ord_orthoSiNull OU d'un vecteur normal à i, 
        ///   de même norme et d'origine p_coordPoint0 si p_coordPoint3Ord_orthoSiNull est null 
        /// </summary>
        /// <param name="p_pointsAReferencer"></param>
        /// <param name="p_coordPoint0"></param>
        /// <param name="p_coordPoint2Abs"></param>
        /// <param name="p_coordPoint3Ord_orthoSiNull"></param>
        /// <returns></returns>
        public Dictionary<int, double[]> GetCoordonneesDansNewReferentiel2D(IEnumerable<BeanPoint_internal> p_pointsAReferencer, double[] p_coordPoint0, double[] p_coordPoint2Abs, double[] p_coordPoint3Ord_orthoSiNull = null)
        {
            Dictionary<int, double[]> v_coords = new Dictionary<int, double[]>();
            try
            {
                ICalculServices_Low v_calcul = new CalculServices_Low();
                bool v_normaliser_vf = true;
                double[,] v_matriceDeConversion;
                v_matriceDeConversion = v_calcul.GetMatriceChangementDeRepereXY(p_coordPoint0, p_coordPoint2Abs, p_coordPoint3Ord_orthoSiNull, v_normaliser_vf);
                //
                double[] v_coord;
                foreach (BeanPoint_internal v_point in p_pointsAReferencer)
                {
                    v_coord = v_calcul.GetCoordDansNewRepereXY(v_matriceDeConversion, p_coordPoint0, v_point.p10_coord);
                    v_coords.Add(v_point.p00_id, v_coord);
                }
            }
            catch (Exception)
            {

                throw;
            }
            return v_coords;
        }
        public double[] GetCoordonneesDansNewReferentiel2D(BeanPoint_internal p_pointAReferencer, double[] p_coordPoint0, double[] p_coordPoint2Abs, double[] p_coordPoint3Ord_orthoSiNull = null)
        {
            double[] v_coord = null;
            Dictionary<int, double[]> v_coords = new Dictionary<int, double[]>();
            try
            {
                ICalculServices_Low v_calcul = new CalculServices_Low();
                bool v_normaliser_vf = true;
                double[,] v_matriceDeConversion;
                v_matriceDeConversion = v_calcul.GetMatriceChangementDeRepereXY(p_coordPoint0, p_coordPoint2Abs, p_coordPoint3Ord_orthoSiNull, v_normaliser_vf);
                v_coord = v_calcul.GetCoordDansNewRepereXY(v_matriceDeConversion, p_coordPoint0, p_pointAReferencer.p10_coord);
            }
            catch (Exception)
            {

                throw;
            }
            return v_coord;
        }

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
        //
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

        public double GetLongueurArcAuCarre(BeanPoint_internal p_point1, BeanPoint_internal p_point2)
        {
            return FLabServices.createCalculLow().GetDistanceEuclidienneCarreeXYZ(p_point1.p10_coord, p_point2.p10_coord);
        }

        public double[] GetVecteurPenteInFacetteFromPoint(BeanFacette_internal p_facette, BeanPoint_internal p_pointFacette,bool p_vecteurSortantSinonEntrant_vf,bool p_nullSiVecteurNonInclus_vf)
        {
            try
            {
                double[] v_vecteurPente;
                bool v_normaliserVecteurPente_vf = true;
                v_vecteurPente=FLabServices.createCalculLow().GetVecteurPenteMaxi(p_facette.p20_normaleDuPlan, v_normaliserVecteurPente_vf);
                double v_pente=FLabServices.createCalculLow().GetPente(v_vecteurPente);

                //




            }
            catch (Exception)
            {

                throw;
            }
        }
        #endregion UTILITAIRES
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
                v_pointsOut.Add(v_pointsOut.First());
            }
            catch (Exception)
            {

                throw;
            }
            return v_pointsOut;
        }
        public Dictionary<string, int> GetEtComptePointsDoublonnes(List<BeanPoint_internal> p_pointsToTest)
        {
            Dictionary<string, int> v_dicoDoublons = new Dictionary<string, int>();

            v_dicoDoublons = p_pointsToTest.GroupBy(c => c.p01_hCodeGeog).ToDictionary(c => c.Key, c => c.Count());
            v_dicoDoublons = v_dicoDoublons.Where(c => c.Value > 1).ToDictionary(c => c.Key, c => c.Value);

            return v_dicoDoublons;
        }
        
    }
}