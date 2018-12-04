using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Services.Lab
{
    public class CalculServices_Medium : ICalculServices_Medium
    {

        public BeanTopologieFacettes GetInitialisationTin(List<BeanPoint_internal> p_points)
        {
            BeanTopologieFacettes v_topologieFacette = new BeanTopologieFacettes(p_points);
            try
            {
                //1-Extraction du convexhull de la base du relief:
                List<BeanPoint_internal> v_convexHullDeLaBase = GetConvexHull2D(p_points);
                double v_altitudeMax = p_points.Select(c => c.p10_coord[2]).Max();
                //2-Extraction du point d'altitude maxi
                BeanPoint_internal v_pointDAltitudeMax;
                v_pointDAltitudeMax = p_points.Where(c => c.p10_coord[2] == v_altitudeMax).First();
                //On le marque comme 'point facette':
                v_pointDAltitudeMax.p21_estPointFacette_vf = true;
                v_pointDAltitudeMax.p22_estPointInclus_vf = true;

                //Le point max appartient-il au convexHull?
                int v_nbreOccurrencesPointMaxInConvexHull = v_convexHullDeLaBase.Where(c => c.p00_id == v_pointDAltitudeMax.p00_id).Count();
                if (v_nbreOccurrencesPointMaxInConvexHull>=1)
                {
                    v_convexHullDeLaBase = v_convexHullDeLaBase.Where(c => c.p00_id != v_pointDAltitudeMax.p00_id).ToList();
                }
                //3-facettage initial:
                //On créé 2 listes:
                //1 pour les "arcs bases" (=ceux composant le concexhull),
                //1 pour les arcs convergeant au sommet

                BeanArc_internal v_arc;
                List<BeanArc_internal> v_arcsBases = new List<BeanArc_internal>();
                List<BeanArc_internal> v_arcsConvergeants = new List<BeanArc_internal>();
                for (int v_indicePointBase=0; v_indicePointBase < v_convexHullDeLaBase.Count-1; v_indicePointBase++)
                {
                    v_arc = new BeanArc_internal(v_convexHullDeLaBase[v_indicePointBase], v_convexHullDeLaBase[v_indicePointBase + 1]);
                    v_arcsBases.Add(v_arc);
                    v_arc = new BeanArc_internal(v_pointDAltitudeMax, v_convexHullDeLaBase[v_indicePointBase]);
                    v_arcsConvergeants.Add(v_arc);
                    //On en profite pour marquer ces points comme extrêmité de 'facettes'
                    v_convexHullDeLaBase[v_indicePointBase].p21_estPointFacette_vf = true;
                    v_convexHullDeLaBase[v_indicePointBase].p22_estPointInclus_vf = true;
                    //(Le dernier point est -normalement!-identique au premier
                }
               
                //Je rajoute privisoirement le 1er arc convergeant à la fin de la liste des arcs convergeants.
                //?Va simplifier l'algo en créant un "anneau".
                v_arcsConvergeants.Add(v_arcsConvergeants.First());

                //On détermine le côté intérieur:
                bool v_interieurEstGauche_vf = true;
                double[] v_coordDUnPointInterieur = v_pointDAltitudeMax.p10_coord;
                if (v_nbreOccurrencesPointMaxInConvexHull==0)
                {
                    v_coordDUnPointInterieur = GetCentroide(v_convexHullDeLaBase);
                }
                double[] v_coordDUnPointInterieurRef;
                v_coordDUnPointInterieurRef = FServices.createCalculLow().GetCoordDansNewRepere2D(v_coordDUnPointInterieur, v_convexHullDeLaBase[0].p10_coord, v_convexHullDeLaBase[1].p10_coord);
                if (v_coordDUnPointInterieurRef[1] < 0)
                {
                    v_interieurEstGauche_vf = false;
                }

                //On peut, maintenant, créer les facettes
                BeanFacette_internal v_facette;
                BeanArc_internal v_arcDescendant;
                BeanArc_internal v_arcMontant;
                for (int v_indiceArcBase=0; v_indiceArcBase< v_arcsBases.Count; v_indiceArcBase++)
                {
                    v_facette = new BeanFacette_internal();
                    //
                    v_facette.p01_points.Add(v_pointDAltitudeMax);
                    v_facette.p01_points.Add(v_arcsBases[v_indiceArcBase].p11_pointDbt);
                    v_facette.p01_points.Add(v_arcsBases[v_indiceArcBase].p12_pointFin);
                    //
                    v_arcDescendant = v_arcsConvergeants[v_indiceArcBase];
                    v_arcMontant= v_arcsConvergeants[v_indiceArcBase+1];
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
                    //[Les points 
                    RattachePointsToFacette(ref p_points, ref v_facette);
                    
                    //Injection de la facette
                    v_topologieFacette.p13_facettes.Add(v_facette);
                }
                //On supprime l'arc supplémentaire rajouté
                v_arcsConvergeants.RemoveAt(v_arcsConvergeants.Count - 1);

                //On supprime de la liste le dernier point contenu dans le convexhull
                //(Sauf si le point maxi était aussi le premier -et donc le dernier- point du convexhull!)
                if (v_nbreOccurrencesPointMaxInConvexHull<2)
                {
                    v_convexHullDeLaBase.RemoveAt(v_convexHullDeLaBase.Count - 1);
                }
                //
                v_topologieFacette.p11_pointsFacettes.AddRange(v_convexHullDeLaBase);
                v_topologieFacette.p11_pointsFacettes.Add(v_pointDAltitudeMax);
                //
                v_topologieFacette.p12_arcs.AddRange(v_arcsBases);
                v_topologieFacette.p12_arcs.AddRange(v_arcsConvergeants);  
            }
            catch (Exception)
            {

                throw;
            }
            return v_topologieFacette;
        }
        public List<BeanPoint_internal> GetConvexHull2D(IEnumerable<BeanPoint_internal> p_points)
        {
            List<BeanPoint_internal> p_pointsOrdonnesConvexHull = new List<BeanPoint_internal>();
            try
            {
                double[] v_centroide = GetCentroide(p_points);
                //
                BeanPoint_internal p_point0;
                p_point0 = GetIdPointLePlusEloigneDuPointRef(p_points, v_centroide);
                BeanPoint_internal p_point0_Oppose;
                p_point0_Oppose = GetIdPointLePlusEloigneDuPointRef(p_points, p_point0.p10_coord);
                //
                ICalculServices_Low v_calcul = new CalculServices_Low();
                Dictionary<int, double[]> v_coordDansRef;

                v_coordDansRef = GetCoordonneesDansNewReferentiel2D(p_points, p_point0.p10_coord, p_point0_Oppose.p10_coord, null);
                HashSet<int> v_idPositifs = new HashSet<int>(v_coordDansRef.Where(c => c.Value[1] > 0).Select(c => c.Key).ToList());
                //(Les points '0' ne nous intéressent pas: ils ne peuvent pas appartenir au CH (sauf les 2 extrêmes déjà identifiés)
                List<BeanPoint_internal> v_pointsPositifs = p_points.Where(c => v_idPositifs.Contains(c.p00_id)).ToList();
                List<BeanPoint_internal> v_pointsNegatifs = p_points.Where(c => !v_idPositifs.Contains(c.p00_id)).ToList();
                //
                p_pointsOrdonnesConvexHull.Add(p_point0);
                Stack<BeanArc_internal> v_pileLifo = new Stack<BeanArc_internal>();
                BeanArc_internal v_arcDescendant = new BeanArc_internal(p_point0_Oppose, p_point0, v_pointsNegatifs);
                v_pileLifo.Push(v_arcDescendant);
                BeanArc_internal v_arcMontant = new BeanArc_internal(p_point0, p_point0_Oppose, v_pointsPositifs);
                v_pileLifo.Push(v_arcMontant);
                //
                BeanArc_internal v_arcToExplore;
                List<BeanArc_internal> v_arcsResultants;
                while (v_pileLifo.Count > 0)
                {
                    v_arcToExplore = v_pileLifo.Pop();
                    v_arcsResultants = GetPointConvexGauche(v_arcToExplore);
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

        private void RattachePointsToFacette(ref List<BeanPoint_internal> p_points, ref BeanFacette_internal p_facette)
        {
            try
            {
                List<BeanPoint_internal> v_pointsToTest = p_points.Where(c => !c.p22_estPointInclus_vf).ToList();
                Dictionary<int, double[]> v_coordRef;
                HashSet<int> v_idPointsUtiles;

                //Je commence par le 'point haut' (mais...peu importe!)
                //'x'>0 et 'y'>=0 ('arc droit'=>abscisses, 'arc gauche'=>ordonnées)
                v_coordRef = GetCoordonneesDansNewReferentiel2D(v_pointsToTest, p_facette.p01_points[0].p10_coord, p_facette.p01_points[1].p10_coord, p_facette.p01_points[2].p10_coord);
                v_idPointsUtiles=new HashSet<int>(v_coordRef.Where(c => c.Value[0] > 0 && c.Value[1] >= 0).Select(c=>c.Key).ToList());
                v_pointsToTest=v_pointsToTest.Where(c => v_idPointsUtiles.Contains(c.p00_id)).ToList();
                //Puis...
                v_coordRef = GetCoordonneesDansNewReferentiel2D(v_pointsToTest, p_facette.p01_points[1].p10_coord, p_facette.p01_points[0].p10_coord, p_facette.p01_points[2].p10_coord);
                //'x'>=0 et 'y'>=0 ('arc gauchet'=>abscisses, 'arc base'=>ordonnées)
                v_idPointsUtiles = new HashSet<int>(v_coordRef.Where(c => c.Value[0] >= 0 && c.Value[1] >= 0).Select(c => c.Key).ToList());
                v_pointsToTest = v_pointsToTest.Where(c => v_idPointsUtiles.Contains(c.p00_id)).ToList();
                foreach(BeanPoint_internal v_point in v_pointsToTest)
                {
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


        private Dictionary<int, double[]> GetCoordonneesDansNewReferentiel2D(IEnumerable<BeanPoint_internal> p_pointsAReferencer, double[] p_coordPoint0, double[] p_coordPoint2Abs, double[] p_coordPoint3Ord_orthoSiNull = null)
        {
            Dictionary<int, double[]> v_coords = new Dictionary<int, double[]>();
            try
            {
                ICalculServices_Low v_calcul = new CalculServices_Low();
                bool v_normaliser_vf = true;
                double[,] v_matriceDeConversion;
                v_matriceDeConversion = v_calcul.GetMatriceChangementDeRepere2D(p_coordPoint0, p_coordPoint2Abs, p_coordPoint3Ord_orthoSiNull, v_normaliser_vf);
                //
                double[] v_coord;
                foreach (BeanPoint_internal v_point in p_pointsAReferencer)
                {
                    v_coord = v_calcul.GetCoordDansNewRepere2D(v_matriceDeConversion, v_point.p10_coord);
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
                v_matriceDeConversion = v_calcul.GetMatriceChangementDeRepere2D(p_coordPoint0, p_coordPoint2Abs, p_coordPoint3Ord_orthoSiNull, v_normaliser_vf);
                //
               
                v_coord = v_calcul.GetCoordDansNewRepere2D(v_matriceDeConversion, p_pointAReferencer.p10_coord);
              
            }
            catch (Exception)
            {

                throw;
            }
            return v_coord;
        }
        private List<BeanArc_internal> GetPointConvexGauche(BeanArc_internal p_arcEnrichiSource)
        {
            List<BeanArc_internal> v_pointConvex = null;
            try
            {
                Dictionary<int, double[]> v_coordDansRef;
                v_coordDansRef = GetCoordonneesDansNewReferentiel2D(p_arcEnrichiSource.p31_pointsAssocies, p_arcEnrichiSource.p11_pointDbt.p10_coord, p_arcEnrichiSource.p12_pointFin.p10_coord, null);
                //
                HashSet<int> v_idPositifs = new HashSet<int>(v_coordDansRef.Where(c => c.Value[1] > 0).Select(c => c.Key).ToList());
                //(Les points '0' ne nous intéressent pas: ils ne peuvent pas appartenir au CH (sauf les 2 extrêmes déjà identifiés)
                List<BeanPoint_internal> v_pointsPositifs = p_arcEnrichiSource.p31_pointsAssocies.Where(c => v_idPositifs.Contains(c.p00_id)).ToList();
                //
                int v_idPt1 = -1;
                double v_ecartMax = v_coordDansRef.Select(c => c.Value[1]).Max();
                if (v_ecartMax == 0)
                {
                    return null;
                }
                v_idPt1 = v_coordDansRef.Where(c => c.Value[1] == v_ecartMax).First().Key;
                BeanPoint_internal v_points1 = v_pointsPositifs.Where(c => c.p00_id == v_idPt1).ToList().First();

                BeanArc_internal v_arc1 = new BeanArc_internal(p_arcEnrichiSource.p11_pointDbt, v_points1, v_pointsPositifs);
                BeanArc_internal v_arc2 = new BeanArc_internal(v_points1, p_arcEnrichiSource.p12_pointFin, v_pointsPositifs);
            }
            catch (Exception)
            {
                throw;
            }
            return v_pointConvex;
        }

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
    }
}
