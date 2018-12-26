using DEM.Net.Lib.Services.VisualisationServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace DEM.Net.Lib.Services.Lab
{
    public class CalculServices_Medium : ICalculServices_Medium
    {
        public BeanParametresDuTin GetParametresDuTinParDefaut()
        {
            BeanParametresDuTin v_parametresDuTin = new BeanParametresDuTin();
            try
            {
                v_parametresDuTin.p11_modeChoixDuPointCentral = new BeanParametresChoixDuPointCentral();
                v_parametresDuTin.p11_modeChoixDuPointCentral.p00_methodeChoixDuPointCentral = enumMethodeChoixDuPointCentral.pointLePlusExcentre;
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
                if(p_parametresDuTin==null)
                {
                    p_parametresDuTin = GetParametresDuTinParDefaut();
                }

                //1-Extraction du convexhull de la base du relief:
                List<BeanPoint_internal> v_convexHullDeLaBase = GetConvexHull2D(p_points);
                double v_altitudeMax = p_points.Select(c => c.p10_coord[2]).Max();

                //2-Extraction du point d'altitude maxi
                BeanPoint_internal v_pointDAltitudeMax;
                v_pointDAltitudeMax = p_points.Where(c => c.p10_coord[2] == v_altitudeMax).First();

                //3-Calcul les facettes du convexHull étendu au point d'altitude maxi.
                List<BeanFacette_internal> v_facettesInitiales;
                v_facettesInitiales = GetFacettesInitialesByPolygoneConvexe(v_convexHullDeLaBase, v_pointDAltitudeMax, p_points);

                //4-On injecte dans le bean topologie
                v_topologieFacette = new BeanTopologieFacettes(p_points);
                v_topologieFacette.p13_facettesById = v_facettesInitiales.ToDictionary(c => c.p00_idFacette, c=>c);
                //
                v_topologieFacette.p11_pointsFacettes.AddRange(v_convexHullDeLaBase);
                v_topologieFacette.p11_pointsFacettes.Add(v_pointDAltitudeMax);
                //
                List<BeanArc_internal> v_arcsFacette=v_facettesInitiales.SelectMany(c => c.p02_arcs).Distinct().ToList();
                v_topologieFacette.p12_arcsByCode = v_arcsFacette.ToDictionary(c => c.p01_hcodeArc, c => c);
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
                //[Nécessaire car on ajoute et supprime des facettes au fur et à mesure et donc=>modif de collection
                List<int> v_idFacettesDeDepart = p_topologieFacette.p13_facettesById.Keys.ToList();
                BeanPoint_internal v_meilleurPoint;
                BeanFacette_internal v_facette;
                foreach (int v_idFacette in v_idFacettesDeDepart)
                {
                    v_facette = p_topologieFacette.p13_facettesById[v_idFacette];
                    v_meilleurPoint = GetPointCentralDeLaFacette(v_facette, p_parametresDuTin.p11_modeChoixDuPointCentral);

                    //TODEBUG
                    Color v_couleur = Color.FromScRgb(255, 250, 0, 0);
                    FVisualisationServices.createVisualisationSpatialTraceServices().GetVisuPoint2D(v_meilleurPoint, "PT max", v_couleur, 10);
                    //FINTODEBUG

                    if (v_meilleurPoint == null)
                    {
                        continue;
                    }
                    GenereEtInsertSousfacettesByRef(ref p_topologieFacette, v_facette.p00_idFacette, v_meilleurPoint);
                }     
            }
            catch (Exception)
            {

                throw;
            }
           
        }


        #region UTILITAIRES
        private void GenereEtInsertSousfacettesByRef(ref BeanTopologieFacettes p_topologieCible, int p_idFacetteSource, BeanPoint_internal p_pointCentral)
        {
            try
            {
                BeanFacette_internal v_facetteSource = p_topologieCible.p13_facettesById[p_idFacetteSource];
                List<BeanPoint_internal> v_pointsFacetteSource = v_facetteSource.p01_pointsDeFacette;
                List<BeanArc_internal> v_arcsBases = v_facetteSource.p02_arcs;
                List<BeanPoint_internal> v_pointsInclus = v_facetteSource.p10_pointsInclus;

                //On marque le 'point central' comme 'point facette':
                p_pointCentral.p21_estPointFacette_vf = true;
                p_pointCentral.p22_estPointInclus_vf = true;
                p_topologieCible.p11_pointsFacettes.Add(p_pointCentral);

                //On désaffecte les points inclus
                BeanPoint_internal v_pointInclus;
                for (int v_indicePoint=0; v_indicePoint< v_facetteSource.p10_pointsInclus.Count; v_indicePoint++)
                {
                    v_pointInclus= v_facetteSource.p10_pointsInclus[v_indicePoint];
                    if (!p_pointCentral.p21_estPointFacette_vf)
                    {
                        v_pointInclus.p22_estPointInclus_vf = false;
                    }
                }


                //On initialise les nouveaux arcs:
                BeanArc_internal v_arc;
                List<BeanArc_internal> v_arcsRayonnants = new List<BeanArc_internal>();
                for (int v_indicePointBase = 0; v_indicePointBase < v_pointsFacetteSource.Count - 1; v_indicePointBase++)
                {
                    v_arc = new BeanArc_internal(p_pointCentral, v_pointsFacetteSource[v_indicePointBase]);
                    v_arcsRayonnants.Add(v_arc);
                    p_topologieCible.p12_arcsByCode.Add(v_arc.p01_hcodeArc, v_arc);
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
                    v_arcDescendant.p22_facetteDroite = v_facette;
                    //
                    v_arcCandidatBase = v_arcsBases.Where(c => c.p11_pointDbt.p01_hCodeGeog == v_point2.p01_hCodeGeog && c.p12_pointFin.p01_hCodeGeog == v_point3.p01_hCodeGeog).ToList();
                    if (v_arcCandidatBase.Count > 0)
                    {
                        v_arcBase = v_arcCandidatBase.First();
                        v_arcBase.p21_facetteGauche = v_facette;
                    }
                    else
                    {
                        v_arcCandidatBase = v_arcsBases.Where(c => c.p12_pointFin.p01_hCodeGeog == v_point2.p01_hCodeGeog && c.p11_pointDbt.p01_hCodeGeog == v_point3.p01_hCodeGeog).ToList();
                        v_arcBase = v_arcCandidatBase.First();
                        v_arcBase.p22_facetteDroite = v_facette;
                    }
                    //Récupération des points inclus:
                    RattachePointsToFacette(ref v_pointsInclus, ref v_facette);
                    //
                    p_topologieCible.p13_facettesById.Add(v_facette.p00_idFacette, v_facette);
                }

                //On supprime la facette d'origine
                p_topologieCible.p13_facettesById.Remove(p_idFacetteSource);
            }
            catch (Exception)
            {

                throw;
            }
        }

        private BeanPoint_internal GetPointCentralDeLaFacette(BeanFacette_internal p_facette, BeanParametresChoixDuPointCentral p_paramDeChoixDuPointCentral)
        {
            BeanPoint_internal v_point = null;
            try
            {
                if(p_facette.p10_pointsInclus==null || p_facette.p10_pointsInclus.Count==0)
                {
                    return null;
                }
                //
                Dictionary<int, double[]> v_coordPointsDansReferentielDeLaFacette;
                v_coordPointsDansReferentielDeLaFacette = GetCoordonneesPointsDansLeRefDuPlanXYDeLaFacette(p_facette);
             
                //
                int p_idPointCible;
                switch(p_paramDeChoixDuPointCentral.p00_methodeChoixDuPointCentral)
                {
                    case enumMethodeChoixDuPointCentral.pointLePlusExcentre:
                        double   v_valeurMaxiAbs=Math.Max(v_coordPointsDansReferentielDeLaFacette.Max(c => c.Value[2]), Math.Abs(v_coordPointsDansReferentielDeLaFacette.Min(c => c.Value[2])));
                        p_idPointCible=v_coordPointsDansReferentielDeLaFacette.Where(c => Math.Abs(c.Value[2]) == v_valeurMaxiAbs).Select(c=>c.Key).First();
                        break;
                    default:
                        throw new Exception("Méthode " + p_paramDeChoixDuPointCentral.p00_methodeChoixDuPointCentral + " non implémentée.");
                }
                v_point = p_facette.p10_pointsInclus.Where(c => c.p00_id == p_idPointCible).First();
            }
            catch (Exception)
            {

                throw;
            }
            return v_point;
        }
        private Dictionary<int, double[]> GetCoordonneesPointsDansLeRefDuPlanXYDeLaFacette(BeanFacette_internal p_facette)
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
                //
                double[] v_vector1=FLabServices.createCalculLow().GetVectorBrutFromTwoPoints(v_coordDesPointsFacettes[0], v_coordDesPointsFacettes[1]);
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
                foreach(BeanPoint_internal v_point in p_facette.p10_pointsInclus)
                {
                    v_vector = FLabServices.createCalculLow().GetVectorBrutFromTwoPoints(v_coordDesPointsFacettes[0], v_point.p10_coord);
                    v_coordDansLeRepereDuPlan=FLabServices.createCalculLow().GetProduitMatriceParVector(v_matriceDeRotation, v_vector);
                    v_coords.Add(v_point.p00_id, v_coordDansLeRepereDuPlan);
                }
            }
            catch (Exception)
            {

                throw;
            }
            return v_coords;
        }

       private List<BeanFacette_internal> GetFacettesInitialesByPolygoneConvexe(List<BeanPoint_internal> p_pointsBase,  BeanPoint_internal p_pointCentral, List<BeanPoint_internal> p_tousPointsInclus)
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
                for (int v_indicePointBase = 0; v_indicePointBase < p_pointsBase.Count - 1; v_indicePointBase++)
                {
                    v_arc = new BeanArc_internal(p_pointsBase[v_indicePointBase], p_pointsBase[v_indicePointBase + 1]);
                    v_arcsBases.Add(v_arc);
                    //On en profite pour marquer ces points comme extrêmité de 'facettes'
                    p_pointsBase[v_indicePointBase].p21_estPointFacette_vf = true;
                    p_pointsBase[v_indicePointBase].p22_estPointInclus_vf = true;
                    //(Le dernier point est -normalement!-identique au premier
                    v_arc = new BeanArc_internal(p_pointCentral, p_pointsBase[v_indicePointBase]);
                    v_arcsRayonnants.Add(v_arc);
                }

                //On rajoute privisoirement le 1er arc convergeant à la fin de la liste des arcs convergeants.
                //?Va simplifier l'algo en créant un "anneau".
                v_arcsRayonnants.Add(v_arcsRayonnants.First());

                //On détermine le côté 'intérieur':
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
        /// <param name="p_points"></param>
        /// <param name="p_facette"></param>
        private void RattachePointsToFacette(ref List<BeanPoint_internal> p_points, ref BeanFacette_internal p_facette)
        {
            try
            {
                List<BeanPoint_internal> v_pointsToTest = p_points.Where(c => !c.p22_estPointInclus_vf).ToList();
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
                for(int v_indicePoint=0; v_indicePoint<v_pointsToTest.Count; v_indicePoint++)
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
        private Dictionary<int, double[]> GetCoordonneesDansNewReferentiel2D(IEnumerable<BeanPoint_internal> p_pointsAReferencer, double[] p_coordPoint0, double[] p_coordPoint2Abs, double[] p_coordPoint3Ord_orthoSiNull = null)
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
        private double[] GetCoordonneesDansNewReferentiel2D(BeanPoint_internal p_pointAReferencer, double[] p_coordPoint0, double[] p_coordPoint2Abs, double[] p_coordPoint3Ord_orthoSiNull = null)
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
                if(v_idPositifs.Count==0)
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
                if(v_idPt1==p_arcEnrichiSource.p11_pointDbt.p00_id || v_idPt1 == p_arcEnrichiSource.p12_pointFin.p00_id)
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
        private BeanPoint_internal GetIdPointLePlusEloigneDuPointRef(IEnumerable<BeanPoint_internal> p_points, double[] p_pointRef)
        {
            BeanPoint_internal v_point;
            try
            {
                ICalculServices_Low v_calcul = new CalculServices_Low();
                double v_distanceMax = p_points.Select(c => v_calcul.GetDistanceEuclidienneCarree2D(c.p10_coord, p_pointRef)).Max();
                v_point = p_points.Where(c => v_calcul.GetDistanceEuclidienneCarree2D(c.p10_coord, p_pointRef) == v_distanceMax).First();
            }
            catch (Exception)
            {

                throw;
            }
            return v_point;
        }
        private double[] GetCentroide(IEnumerable<BeanPoint_internal> p_points)
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
        #endregion UTILITAIRES

       
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


        #region Tests divers pour debug=>A purger à terme
        public void TestMatrice(IEnumerable<BeanPoint_internal> p_points, double[] p_vecteurDeDecalage)
        {
            Color v_couleurObjet;
            string v_label;

            //POINTS REF
            double[] v_coordCentroide = GetCentroide(p_points);
            BeanPoint_internal v_centroide = new BeanPoint_internal(v_coordCentroide, 2154);
            //
            double[] p_coordPointX = new double[3];
            p_coordPointX[0] = v_coordCentroide[0] + p_vecteurDeDecalage[0];
            p_coordPointX[1] = v_coordCentroide[1] + p_vecteurDeDecalage[1];
            p_coordPointX[2] = 0;
            BeanPoint_internal p_pointX = new BeanPoint_internal(p_coordPointX, 2154);

            //VISU POINTS REF
            v_couleurObjet = Color.FromScRgb(255, 0, 0, 255);
            FVisualisationServices.createVisualisationSpatialTraceServices().GetVisuPoint2D(v_centroide, "Pt0", v_couleurObjet, 20);
            v_couleurObjet = Color.FromScRgb(255, 0, 255, 0);
            FVisualisationServices.createVisualisationSpatialTraceServices().GetVisuPoint2D(p_pointX, "Pt1", v_couleurObjet, 20);
            //FIN VISU POINTS REF

            Dictionary<int, double[]> v_coordDansRef;
            //
            v_coordDansRef = GetCoordonneesDansNewReferentiel2D(p_points, v_coordCentroide, p_coordPointX, null);
            Dictionary<int, BeanPoint_internal> v_dicoPoints = p_points.ToDictionary(c => c.p00_id, c => c);

            //VISU COORD
            int param_arrondi = 2;
            v_couleurObjet = Color.FromScRgb(255, 0, 0, 255);
            List<int> v_idPointsOrdonnes = v_coordDansRef.OrderBy(c => c.Value[1]).Select(c => c.Key).ToList();
            foreach (int v_id in v_idPointsOrdonnes)
            {
                v_label = "Pt: " + v_id + " => " + Math.Round(v_coordDansRef[v_id][0], param_arrondi) + " / " + Math.Round(v_coordDansRef[v_id][1], param_arrondi);
                FVisualisationServices.createVisualisationSpatialTraceServices().GetVisuPoint2D(v_dicoPoints[v_id], v_label, v_couleurObjet, 10);
            }
            FVisualisationServices.createVisualisationSpatialTraceServices().AfficheVisu();
            //FIN VISU COORD

        }
        #endregion Tests divers pour debug


    }
}
