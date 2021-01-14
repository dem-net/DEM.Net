using DEM.Net.Core.Services.Lab;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Point = NetTopologySuite.Geometries.Point;

namespace DEM.Net.Core.Voronoi
{
    public class VoronoiServices : IVoronoiServices
    {
        public BeanTopologieFacettes GetTopologieVoronoiByDicoPointsEtMbo(Dictionary<int, Point> p_dicoPointsAvecId, double[] p_coodMinMaxXy, int p_srid)
        {
            BeanTopologieFacettes v_topol = new BeanTopologieFacettes();
            try
            {
                BeanAlimentationVoronoi v_BeanDAlimentation = GetBeanAlimentationVoronoiByDicoPointsEtMbo(p_dicoPointsAvecId, p_coodMinMaxXy, p_srid);

                if (!v_BeanDAlimentation.p22_contientObjetsInvalidesVf)
                {
                    VoronoiGraph v_voronoiGraph = GetVoronoiGraph(v_BeanDAlimentation);
                    v_topol = GetBeanTopologieByVoronoiGraph(v_voronoiGraph, v_BeanDAlimentation);
                }
            }
            catch (Exception)
            {
                throw;
            }
            return v_topol;
        }
    
        public BeanTopologieFacettes GetTopologieVoronoiByDicoPoints(Dictionary<int, Point> p_dicoPointsAvecId, int p_srid, enumVoronoiStrategiePointsDupliques p_strategieSiDuplication = enumVoronoiStrategiePointsDupliques.arretTraitement)
        {
            BeanTopologieFacettes v_topol = new BeanTopologieFacettes();
            try
            {
                BeanAlimentationVoronoi v_BeanDAlimentation = GetBeanAlimentationVoronoiByDicoPoints(p_dicoPointsAvecId, p_srid);
                v_BeanDAlimentation.p12_parametrage.gestionPointsDupliques = p_strategieSiDuplication;
                if (!v_BeanDAlimentation.p22_contientObjetsInvalidesVf)
                {
                    VoronoiGraph v_voronoiGraph = GetVoronoiGraph(v_BeanDAlimentation);
                    v_topol = GetBeanTopologieByVoronoiGraph(v_voronoiGraph, v_BeanDAlimentation);
                }
            }
            catch (Exception v_ex)
            {
                throw v_ex;
            }
            return v_topol;
        }
       
        //
        public BeanTriangulationDelaunay GetTriangulationDeDelaunayByDicoPoints(Dictionary<int, Point> p_dicoPointsAvecId, int p_srid)
        {
            BeanTriangulationDelaunay v_trOut = null;
            try
            {
                BeanAlimentationVoronoi v_BeanDAlimentation = GetBeanAlimentationVoronoiByDicoPoints(p_dicoPointsAvecId,p_srid);
                v_BeanDAlimentation.p12_parametrage.gestionPointsDupliques = enumVoronoiStrategiePointsDupliques.deduplicationAleatoire;

                if (!v_BeanDAlimentation.p22_contientObjetsInvalidesVf)
                {
                    VoronoiGraph v_voronoiGraph = GetVoronoiGraph(v_BeanDAlimentation);
                    v_trOut = GetTriangulationDelaunayByVoronoiGraph(v_voronoiGraph, v_BeanDAlimentation);
                    //Je remets la source d'origine, par "sécurité"
                    v_trOut.p00_PointIGeometrySources = p_dicoPointsAvecId;
                    //
                }
            }
            catch (Exception v_ex)
            {
                throw v_ex;
            }
            return v_trOut;
        }
        public Dictionary<int, Geometry> GetArcsDelaunayGeometryByTriangulationDelaunay(BeanTriangulationDelaunay p_triangulationDelaunay)
        {
            Dictionary<int, Geometry> v_arcs = new Dictionary<int, Geometry>();
            try
            {
                int v_index = 0;
                foreach (BeanArcDelaunay v_arcDelaunay in p_triangulationDelaunay.p01_arcsDelaunay)
                {
                    v_arcs.Add(v_index, v_arcDelaunay.p30_arcDelaunay);
                    v_index++;
                }
            }
            catch (Exception)
            {
                throw;
            }
            return v_arcs;
        }




        public BeanAlimentationVoronoi GetBeanAlimentationVoronoiByDicoPoints(Dictionary<int, Point> p_DicoPoints, int p_srid)
        {
            BeanAlimentationVoronoi v_BeanAlimentationVoronoi = new BeanAlimentationVoronoi();
            try
            {
                double[] v_pseudoCoordonneesMinMax = new double[4];
                v_pseudoCoordonneesMinMax[0] = 1000000000;
                v_pseudoCoordonneesMinMax[1] = 1000000000;
                v_pseudoCoordonneesMinMax[2] = -1000000000;
                v_pseudoCoordonneesMinMax[3] = -1000000000;

                //
                v_BeanAlimentationVoronoi = GetBeanAlimentationVoronoiByDicoPointsEtMbo(p_DicoPoints, v_pseudoCoordonneesMinMax, p_srid);
            }
            catch (Exception v_ex)
            {
                throw v_ex;
            }
            return v_BeanAlimentationVoronoi;
        }
        public BeanAlimentationVoronoi GetBeanAlimentationVoronoiByDicoPointsEtMbo(Dictionary<int, Point> p_DicoPoints, double[] p_coodMinMaxXy,int p_srid)
        {
            BeanAlimentationVoronoi v_BeanAlimentationVoronoi = new BeanAlimentationVoronoi();
            try
            {

                v_BeanAlimentationVoronoi.p11_srid = p_srid;
                v_BeanAlimentationVoronoi.p22_contientObjetsInvalidesVf = false;
                v_BeanAlimentationVoronoi.p23_contientObjetsSuperposesVf = false;
                v_BeanAlimentationVoronoi.p21_territoireSuperieurA200kmVf = false;

                v_BeanAlimentationVoronoi.p51_xMin = p_coodMinMaxXy[0];
                v_BeanAlimentationVoronoi.p52_yMin = p_coodMinMaxXy[1];
                v_BeanAlimentationVoronoi.p53_xMax = p_coodMinMaxXy[2];
                v_BeanAlimentationVoronoi.p54_yMax = p_coodMinMaxXy[3];

                List<Double[]> v_ListeDoublePourVectors = new List<Double[]>();
                string v_codeXYPoint;
                double[] v_DoublePourVector;
                int v_keyDoublon;

                foreach (KeyValuePair<int, Point> v_point in p_DicoPoints)
                {
                    v_BeanAlimentationVoronoi.p10_dicoDesPointsSources.Add(v_point.Key, v_point.Value);
                    if (v_point.Value.OgcGeometryType == OgcGeometryType.Point)
                    {
                        //On génère le code à partir des coordonnees
                        //v_codeXYPoint = GetCodeXY((double)v_point.Value.Coordinate.X, (double)v_point.Value.Coordinate.Y);
                        v_codeXYPoint=FLabServices.createUtilitaires().GetHCodeGeogPoint(new double[2] { (double)v_point.Value.Coordinate.X, (double)v_point.Value.Coordinate.Y });

                        if (v_BeanAlimentationVoronoi.p31_correspondanceHCodePointvsIdPoint.ContainsKey(v_codeXYPoint))
                        {
                            //j'ajoute dans la liste des doublons l'id du point en cours 
                            v_BeanAlimentationVoronoi.p25_pointsSuperposes.Add(v_point.Key);
                            v_BeanAlimentationVoronoi.p23_contientObjetsSuperposesVf = true;
                            //+ celui déjà listé s'il n'existe pas déjà
                            v_BeanAlimentationVoronoi.p31_correspondanceHCodePointvsIdPoint.TryGetValue(v_codeXYPoint, out v_keyDoublon);
                            if (!v_BeanAlimentationVoronoi.p25_pointsSuperposes.Contains(v_keyDoublon)) { v_BeanAlimentationVoronoi.p25_pointsSuperposes.Add(v_keyDoublon); }
                        }
                        else  //Si l'objet est OK à ce stade...
                        {
                            //J'injecte dans la liste des correspondances
                            v_BeanAlimentationVoronoi.p31_correspondanceHCodePointvsIdPoint.Add(v_codeXYPoint, v_point.Key);
                            //
                            v_DoublePourVector = new double[2];
                            v_DoublePourVector[0] = (double)v_point.Value.Coordinate.X;
                            v_DoublePourVector[1] = (double)v_point.Value.Coordinate.Y;

                            //J'ajoute dans une liste temporaire et pas dans le HS car il semble nécessaire de faire un changement de repère avant d'injecter les données ds le HS<vector>
                            v_ListeDoublePourVectors.Add(v_DoublePourVector);

                            //récupération des points extrêmes
                            if (v_BeanAlimentationVoronoi.p51_xMin > (double)v_point.Value.Coordinate.X) { v_BeanAlimentationVoronoi.p51_xMin = (double)v_point.Value.Coordinate.X; }
                            if (v_BeanAlimentationVoronoi.p52_yMin > (double)v_point.Value.Coordinate.Y) { v_BeanAlimentationVoronoi.p52_yMin = (double)v_point.Value.Coordinate.Y; }
                            if (v_BeanAlimentationVoronoi.p53_xMax < (double)v_point.Value.Coordinate.X) { v_BeanAlimentationVoronoi.p53_xMax = (double)v_point.Value.Coordinate.X; }
                            if (v_BeanAlimentationVoronoi.p54_yMax < (double)v_point.Value.Coordinate.Y) { v_BeanAlimentationVoronoi.p54_yMax = (double)v_point.Value.Coordinate.Y; }
                        }
                    }//Si le Geometry n'est pas un point (FIN if (v_point.Value.STGeometryType() == OpenGisGeographyType.Point.ToString())
                    else
                    {
                        v_BeanAlimentationVoronoi.p24_pointsInvalidesSaufSuperposes.Add(v_point.Key);
                        v_BeanAlimentationVoronoi.p22_contientObjetsInvalidesVf = true;
                    }
                }//FIN foreach

                //Création des points périphériques artificiels
                Dictionary<int, Double[]> v_dicoPointscadres = GetPointsCadresVoronoi(v_BeanAlimentationVoronoi);

           
                //Injection dans les listes
                Dictionary<int, Geometry> v_newListePoint = new Dictionary<int, Geometry>();

                ITopologieService v_topologieService = new TopologieService();
                foreach (KeyValuePair<int, Double[]> v_pointSup in v_dicoPointscadres)
                {
                    //v_codeXYPoint = GetCodeXY(v_pointSup.Value[0], v_pointSup.Value[1]);
                    v_codeXYPoint = FLabServices.createUtilitaires().GetHCodeGeogPoint(new double[2] { v_pointSup.Value[0], v_pointSup.Value[1] });
                    //PUSTULE FAU
                    if (v_BeanAlimentationVoronoi.p31_correspondanceHCodePointvsIdPoint.ContainsKey(v_codeXYPoint))
                    {
                        continue;
                    }
                    v_BeanAlimentationVoronoi.p31_correspondanceHCodePointvsIdPoint.Add(v_codeXYPoint, v_pointSup.Key);
                    v_BeanAlimentationVoronoi.p10_dicoDesPointsSources.Add(v_pointSup.Key, FLabServices.createUtilitaires().ConstructPoint(v_pointSup.Value[0], v_pointSup.Value[1], p_srid));
                        
                   
                    v_ListeDoublePourVectors.Add(v_pointSup.Value);
                }

                //Changement d'origine des coordonnées
                //(Indispensable pour pouvoir gérer les calculs sur les coord L93 (>10 puissance 5)) 
                v_BeanAlimentationVoronoi.p55_origineXCorrigee = (int)(Math.Round(v_BeanAlimentationVoronoi.p51_xMin) - 1);
                v_BeanAlimentationVoronoi.p56_origineYCorrigee = (int)(Math.Round(v_BeanAlimentationVoronoi.p52_yMin) - 1);
                if (v_BeanAlimentationVoronoi.p12_parametrage.reductionCoordonneesVf)
                {
                    v_BeanAlimentationVoronoi.p51_xMin = v_BeanAlimentationVoronoi.p51_xMin - v_BeanAlimentationVoronoi.p55_origineXCorrigee;
                    v_BeanAlimentationVoronoi.p53_xMax = v_BeanAlimentationVoronoi.p53_xMax - v_BeanAlimentationVoronoi.p55_origineXCorrigee;
                    v_BeanAlimentationVoronoi.p52_yMin = v_BeanAlimentationVoronoi.p52_yMin - v_BeanAlimentationVoronoi.p56_origineYCorrigee;
                    v_BeanAlimentationVoronoi.p54_yMax = v_BeanAlimentationVoronoi.p54_yMax - v_BeanAlimentationVoronoi.p56_origineYCorrigee;
                }

                foreach (Double[] v_DoubleVectorPourCorrection in v_ListeDoublePourVectors)
                {
                    if (v_BeanAlimentationVoronoi.p12_parametrage.reductionCoordonneesVf)
                    {
                        v_DoubleVectorPourCorrection[0] = v_DoubleVectorPourCorrection[0] - v_BeanAlimentationVoronoi.p55_origineXCorrigee;
                        v_DoubleVectorPourCorrection[1] = v_DoubleVectorPourCorrection[1] - v_BeanAlimentationVoronoi.p56_origineYCorrigee;
                    }
                    v_BeanAlimentationVoronoi.p50_pointsFormatesPourInsertion.Add(new Vector(v_DoubleVectorPourCorrection));
                }
                //
                if (((v_BeanAlimentationVoronoi.p53_xMax - v_BeanAlimentationVoronoi.p51_xMin) > 200000) || ((v_BeanAlimentationVoronoi.p54_yMax - v_BeanAlimentationVoronoi.p52_yMin) > 200000))
                {
                    v_BeanAlimentationVoronoi.p21_territoireSuperieurA200kmVf = true;
                }
                //Ajout des points périphériques	
                //AjoutPointsFrontiere(ref v_BeanAlimentationVoronoi);		
            }
            catch (Exception v_ex)
            {
                throw v_ex;
            }
            return v_BeanAlimentationVoronoi;
        }
        public VoronoiGraph GetVoronoiGraph(BeanAlimentationVoronoi p_beanAlimentationVoronoi)
        {
            VoronoiGraph v_voronoiGraph = new VoronoiGraph();

            try
            {
                if (p_beanAlimentationVoronoi.p22_contientObjetsInvalidesVf)
                {
                    switch (p_beanAlimentationVoronoi.p12_parametrage.gestionObjetsInvalides)
                    {
                        case enumVoronoiStrategieObjetsInvalides.arretTraitement:
                            throw new Exception("GetVoronoiGraph en erreur: des objets ne sont pas des points", new Exception());
                        case enumVoronoiStrategieObjetsInvalides.ignorerCesObjets:
                            break;
                    }
                }
                if (p_beanAlimentationVoronoi.p23_contientObjetsSuperposesVf)
                {
                    switch (p_beanAlimentationVoronoi.p12_parametrage.gestionPointsDupliques)
                    {
                        case enumVoronoiStrategiePointsDupliques.arretTraitement:
                            throw new Exception("GetVoronoiGraph en erreur: des points sont superposés.", new Exception());
                        case enumVoronoiStrategiePointsDupliques.deduplicationAleatoire:
                            break;
                    }
                }


                if (p_beanAlimentationVoronoi.p21_territoireSuperieurA200kmVf)
                {
                    switch (p_beanAlimentationVoronoi.p12_parametrage.gestionDepassementTerritoire)
                    {
                        case enumVoronoiStrategieDistanceTropGrande.arretTraitement:
                            throw new Exception("GetVoronoiGraph en erreur: le territoire total de traitement ne peut pas dépasser 200 000 x 200 000 unités de longueur ");
                        case enumVoronoiStrategieDistanceTropGrande.reductionPrecision:
                            throw new Exception("GetVoronoiGraph en erreur: le territoire total de traitement ne peut pas dépasser 200x200 unités de longueur (reduction précision non implementée)");
                    }
                }

                v_voronoiGraph = Fortune.ComputeVoronoiGraph(p_beanAlimentationVoronoi.p50_pointsFormatesPourInsertion);


            }
            catch (Exception v_ex)
            {
                throw v_ex;
            }
            return v_voronoiGraph;
        }

        public BeanTopologieFacettes GetBeanTopologieByVoronoiGraph(VoronoiGraph p_voronoiGraph, BeanAlimentationVoronoi p_beanAlimentationVoronoi)
        {
            BeanTopologieFacettes v_topologie = new BeanTopologieFacettes();
            try
            {
                List<BeanArc_internal> v_arcsVoronoi = new List<BeanArc_internal>();
                Dictionary<string, BeanArc_internal> v_arcsParHCode = new Dictionary<string, BeanArc_internal>();
                int v_srid = p_beanAlimentationVoronoi.p11_srid;
                //
                Dictionary<string, BeanPoint_internal> v_pointsDesArcsByCode = new Dictionary<string, BeanPoint_internal>();
                Dictionary<int, BeanArc_internal> v_arcs = new Dictionary<int, BeanArc_internal>();
                Dictionary<int, int> v_DicoPointSourceGaucheParArc = new Dictionary<int, int>();
                Dictionary<int, int> v_DicoPointSourceDroitParArc = new Dictionary<int, int>();
                //

                //Création du réseau
                double v_vorX_Left;
                double v_vorY_Left;
                double v_vorX_Right;
                double v_vorY_Right;
                double v_OrigX_Left;
                double v_OrigY_Left;
                double v_OrigX_Right;
                double v_OrigY_Right;
                double v_CorrectionX = 0;
                double v_CorrectionY = 0;

                ITopologieService v_topologieService = new TopologieService();
                int v_KeyArc = 0;
                HashSet<string> v_testDoublesLignes = new HashSet<string>();
               
                if (p_beanAlimentationVoronoi.p12_parametrage.reductionCoordonneesVf)
                {
                    v_CorrectionX = (double)p_beanAlimentationVoronoi.p55_origineXCorrigee;
                    v_CorrectionY = (double)p_beanAlimentationVoronoi.p56_origineYCorrigee;
                }

                BeanPoint_internal v_pt1;
                BeanPoint_internal v_pt2;
                BeanArc_internal v_arc;

                //On créé une facette/ilot pour chaque point;
                BeanFacette_internal v_facette;
                Dictionary<int, BeanFacette_internal> v_toutesFacettes;

                v_toutesFacettes = p_beanAlimentationVoronoi.p10_dicoDesPointsSources.ToDictionary(c => c.Key, c => new BeanFacette_internal());
                   //[ATTENTION! On doit mettre à jour l'id 
                foreach (KeyValuePair<int, BeanFacette_internal> v_fac in v_toutesFacettes)
                {
                    v_fac.Value.p00_idFacette = v_fac.Key;
                }

                //On parcourt les arcs:
                string v_hcodePt1;
                string v_hcodePt2;
                string v_hcodeGraineVoronoiAGauche;
                string v_hcodeGraineVoronoiADroite;
                int v_idIlotAGauche;
                int v_idIlotADroite;
                foreach (VoronoiEdge v_Edges in p_voronoiGraph.Edges)
                {
                    if (!v_Edges.IsInfinite && !v_Edges.IsPartlyInfinite)
                    {
                        v_KeyArc++;
                       
                        //L'arc voronoi
                        v_vorX_Left = v_Edges.VVertexA[0] + v_CorrectionX;
                        v_vorY_Left = v_Edges.VVertexA[1] + v_CorrectionY;
                        v_vorX_Right = v_Edges.VVertexB[0] + v_CorrectionX;
                        v_vorY_Right = v_Edges.VVertexB[1] + v_CorrectionY;
                        //
                        v_hcodePt1 = FLabServices.createUtilitaires().GetHCodeGeogPoint(new double[2] { v_vorX_Left, v_vorY_Left });
                        if(v_pointsDesArcsByCode.ContainsKey(v_hcodePt1))
                        {
                            v_pt1 = v_pointsDesArcsByCode[v_hcodePt1];
                        }
                        else
                        {
                            v_pt1 = new BeanPoint_internal(v_vorX_Left, v_vorY_Left, 0, v_srid);
                            v_pointsDesArcsByCode.Add(v_hcodePt1, v_pt1);
                        }
                        //
                        v_hcodePt2 = FLabServices.createUtilitaires().GetHCodeGeogPoint(new double[2] { v_vorX_Right, v_vorY_Right });
                        if (v_pointsDesArcsByCode.ContainsKey(v_hcodePt2))
                        {
                            v_pt2 = v_pointsDesArcsByCode[v_hcodePt2];
                        }
                        else
                        {
                            v_pt2 = new BeanPoint_internal(v_vorX_Right, v_vorY_Right, 0, v_srid);
                            v_pointsDesArcsByCode.Add(v_hcodePt2, v_pt2);
                        }
                      
                        //
                        v_arc = new BeanArc_internal(v_pt1, v_pt2);
                        //
                        if(!v_arcsParHCode.ContainsKey(v_arc.p01_hcodeArc))
                        {
                            v_arcsParHCode.Add(v_arc.p01_hcodeArc, v_arc);
                        }

                        //On référence les points 'graines' voronoi à droite et à gauche 
                        v_OrigX_Left = v_Edges.LeftData[0] + v_CorrectionX;
                        v_OrigY_Left = v_Edges.LeftData[1] + v_CorrectionY;
                        v_OrigX_Right = v_Edges.RightData[0] + v_CorrectionX;
                        v_OrigY_Right = v_Edges.RightData[1] + v_CorrectionY;
                        //
                        v_hcodeGraineVoronoiAGauche = FLabServices.createUtilitaires().GetHCodeGeogPoint(new double[2] { v_OrigX_Left, v_OrigY_Left });
                        v_hcodeGraineVoronoiADroite = FLabServices.createUtilitaires().GetHCodeGeogPoint(new double[2] { v_OrigX_Right, v_OrigY_Right });
                        //
                        v_facette = null;
                        if (p_beanAlimentationVoronoi.p31_correspondanceHCodePointvsIdPoint.ContainsKey(v_hcodeGraineVoronoiAGauche))
                        {
                            v_idIlotAGauche = p_beanAlimentationVoronoi.p31_correspondanceHCodePointvsIdPoint[v_hcodeGraineVoronoiAGauche];
                            v_facette = v_toutesFacettes[v_idIlotAGauche];
                            v_facette.p02_arcs.Add(v_arc);
                            v_arc.p21_facetteGauche = v_facette;
                        }
                        else
                        {
                            v_arc.p20_statutArc = enumStatutArc.arcExterne;
                        }
                        v_facette = null;
                        if (p_beanAlimentationVoronoi.p31_correspondanceHCodePointvsIdPoint.ContainsKey(v_hcodeGraineVoronoiADroite))
                        {
                            v_idIlotADroite = p_beanAlimentationVoronoi.p31_correspondanceHCodePointvsIdPoint[v_hcodeGraineVoronoiADroite];
                            v_facette = v_toutesFacettes[v_idIlotADroite];
                            v_facette.p02_arcs.Add(v_arc);
                            v_arc.p21_facetteGauche = v_facette;
                        }
                        else
                        {
                            v_arc.p20_statutArc = enumStatutArc.arcExterne;
                        }

                    }//Fin if (!v_Edges.IsInfinite && !v_Edges.IsPartlyInfinite)
                }//Fin foreach
                 //Création de la topologie
                v_arcsVoronoi = v_arcsParHCode.Values.OrderBy(c => c.p01_hcodeArc).ToList();

                //On actualise les points des facettes:
                foreach(KeyValuePair<int, BeanFacette_internal> v_fac in v_toutesFacettes)
                {
                    List<BeanPoint_internal> v_points = v_fac.Value.p02_arcs.Select(c => c.p11_pointDbt).ToList();
                    v_points.AddRange(v_fac.Value.p02_arcs.Select(c => c.p12_pointFin).ToList());
                    HashSet<string> v_pointsDeja = new HashSet<string>();
                    foreach(BeanPoint_internal v_pt in v_points)
                    {
                        if(!v_pointsDeja.Contains(v_pt.p01_hCodeGeog))
                        {
                            v_fac.Value.p01_pointsDeFacette.Add(v_pt);
                            v_pointsDeja.Add(v_pt.p01_hCodeGeog);
                        }
                    }
                }
          
                //
                v_topologie = new BeanTopologieFacettes(v_arcsVoronoi);
                v_topologie.p13_facettesById = v_toutesFacettes;
                //On supprime les éventuelles facettes sans dimension
                List<int> v_idFacettesASupprimer;
                v_idFacettesASupprimer = v_toutesFacettes.Where(c => c.Value.p02_arcs.Count < 3).Select(c=>c.Key).ToList();
                BeanFacette_internal v_facetteASupprimer;
                foreach(int v_idFacASuppr in v_idFacettesASupprimer)
                {
                    v_facetteASupprimer = v_topologie.p13_facettesById[v_idFacASuppr];
                    v_topologie.FacetteSupprimer(v_facetteASupprimer);
                }
              

                //Géométrie:
                List<int> v_idFacettes = v_topologie.p13_facettesById.Keys.ToList();
                Geometry v_geomFacette;
                BeanFacette_internal v_facettePourGeom;
                foreach(int v_idFac in v_idFacettes)
                {
                    v_facettePourGeom = v_topologie.p13_facettesById[v_idFac];
                    v_geomFacette = FLabServices.createCalculMedium().GetGeometryPolygoneFacetteEtOrdonnePointsFacette(ref v_facettePourGeom, ref v_topologie);
                    v_facettePourGeom.p04_geomFacette = v_geomFacette;
                }
            }
            catch (Exception v_ex)
            {
                throw v_ex;
            }
            return v_topologie;
        }




        //public BeanTopologie GetBeanTopologieByVoronoiGraph(VoronoiGraph p_voronoiGraph, BeanAlimentationVoronoi p_beanAlimentationVoronoi)
        //{
        //    BeanTopologie v_topologieVoronoi = new BeanTopologie();
        //    try
        //    {
        //        Dictionary<int, BeanArc_internal> v_arcs = new Dictionary<int, BeanArc_internal>();
        //        Dictionary<int, int> v_DicoPointSourceGaucheParArc = new Dictionary<int, int>();
        //        Dictionary<int, int> v_DicoPointSourceDroitParArc = new Dictionary<int, int>();

        //        //Création du réseau
        //        BeanTopologie v_topologieLignes = new BeanTopologie();

        //        double v_vorX_Left;
        //        double v_vorY_Left;
        //        double v_vorX_Right;
        //        double v_vorY_Right;
        //        double v_OrigX_Left;
        //        double v_OrigY_Left;
        //        double v_OrigX_Right;
        //        double v_OrigY_Right;
        //        double v_CorrectionX = 0;
        //        double v_CorrectionY = 0;

        //        ITopologieService v_topologieService = new TopologieService();
        //        Geometry v_line;
        //        String v_CodeXYPointGauche;
        //        String v_CodeXYPointDroit;
        //        int v_KeyPointGauche;
        //        int v_KeyPointDroit;
        //        int v_KeyArc = 0;
        //        HashSet<string> v_testDoublesLignes = new HashSet<string>();
        //        string v_codeLigne;

        //        if (p_beanAlimentationVoronoi.parametrage.reductionCoordonneesVf)
        //        {
        //            v_CorrectionX = (double)p_beanAlimentationVoronoi.origineXCorrigee;
        //            v_CorrectionY = (double)p_beanAlimentationVoronoi.origineYCorrigee;
        //        }


        //        foreach (VoronoiEdge v_Edges in p_voronoiGraph.Edges)
        //        {
        //            if (!v_Edges.IsInfinite && !v_Edges.IsPartlyInfinite)
        //            {
        //                v_KeyArc++;
        //                //Je créé des variables intermédiaires car modifierLaGeometrie directement le vecteur pose graves pb
        //                v_vorX_Left = v_Edges.VVertexA[0] + v_CorrectionX;
        //                v_vorY_Left = v_Edges.VVertexA[1] + v_CorrectionY;
        //                v_vorX_Right = v_Edges.VVertexB[0] + v_CorrectionX;
        //                v_vorY_Right = v_Edges.VVertexB[1] + v_CorrectionY;
        //                v_OrigX_Left = v_Edges.LeftData[0] + v_CorrectionX;
        //                v_OrigY_Left = v_Edges.LeftData[1] + v_CorrectionY;
        //                v_OrigX_Right = v_Edges.RightData[0] + v_CorrectionX;
        //                v_OrigY_Right = v_Edges.RightData[1] + v_CorrectionY;
        //                //

        //                v_line = v_topologieService.GetLineStringByCoord(v_vorX_Left, v_vorY_Left, v_vorX_Right, v_vorY_Right);

        //                if (v_line.Length > 0)
        //                {
        //                    v_CodeXYPointGauche = GetCodeXY(v_OrigX_Left, v_OrigY_Left);
        //                    v_CodeXYPointDroit = GetCodeXY(v_OrigX_Right, v_OrigY_Right);
        //                    //J'ai rencontré des duplications de lignes (dans un sens et dans l'autre)
        //                    //Ces cas provoquent des boucles sans fin dans la fermeture d'îlots
        //                    // Je mets un patch : A réintégrer DANS TOPOLOGIE SERVICE  	
        //                    if (v_OrigX_Left < v_OrigX_Right)
        //                    {
        //                        v_codeLigne = v_CodeXYPointGauche + "_" + v_CodeXYPointDroit;
        //                    }
        //                    else
        //                    {
        //                        if (v_OrigX_Left == v_OrigX_Right)
        //                        {
        //                            if (v_OrigY_Left < v_OrigY_Right)
        //                            {
        //                                v_codeLigne = v_CodeXYPointGauche + "_" + v_CodeXYPointDroit;
        //                            }
        //                            else
        //                            { v_codeLigne = v_CodeXYPointDroit + "_" + v_CodeXYPointGauche; }
        //                        }
        //                        else
        //                        { v_codeLigne = v_CodeXYPointDroit + "_" + v_CodeXYPointGauche; }
        //                    }
        //                    //Si la ligne est déjà contenue, je ne l'introduis pas
        //                    if (v_testDoublesLignes.Contains(v_codeLigne)) { break; }

        //                    //
        //                    v_arcs.Add(v_KeyArc, v_line);

        //                    v_KeyPointGauche = -1;
        //                    if (p_beanAlimentationVoronoi.dicoLienCodeXyKeySource.ContainsKey(v_CodeXYPointGauche))
        //                    {
        //                        p_beanAlimentationVoronoi.dicoLienCodeXyKeySource.TryGetValue(v_CodeXYPointGauche, out v_KeyPointGauche);
        //                    }
        //                    v_DicoPointSourceGaucheParArc.Add(v_KeyArc, v_KeyPointGauche);
        //                    v_KeyPointDroit = -1;
        //                    if (p_beanAlimentationVoronoi.dicoLienCodeXyKeySource.ContainsKey(v_CodeXYPointGauche))
        //                    {
        //                        p_beanAlimentationVoronoi.dicoLienCodeXyKeySource.TryGetValue(v_CodeXYPointDroit, out v_KeyPointDroit);
        //                    }
        //                    v_DicoPointSourceDroitParArc.Add(v_KeyArc, v_KeyPointDroit);
        //                }
        //            }//Fin if (!v_Edges.IsInfinite && !v_Edges.IsPartlyInfinite)
        //        }//Fin foreach
        //         //Création de la topologie

        //        v_topologieLignes = v_topologieService.GetTopologie(v_arcs);

        //        ////STRUCTURATION en ILOTS:
        //        //v_topologieVoronoi = v_topologieService.GetTopologieSansImpassesEnrichiesDesIlots(v_topologieLignes);
        //        //v_topologieService.MiseAJourDesIndicateursDeControleTopologieIlot(v_topologieVoronoi);
        //        ////Renumérotation des îlots
        //        //v_topologieService.UpdateIdIlotsByCotesArcs(v_topologieVoronoi, v_DicoPointSourceDroitParArc, v_DicoPointSourceGaucheParArc);
        //        ////FIN STRUCTURATION en ILOTS:
        //    }
        //    catch (Exception v_ex)
        //    {
        //        throw v_ex;
        //    }
        //    return v_topologieVoronoi;
        //}

        public BeanTriangulationDelaunay GetTriangulationDelaunayByVoronoiGraph(VoronoiGraph p_voronoiGraph, BeanAlimentationVoronoi p_beanAlimentationVoronoi)
        {
            BeanTriangulationDelaunay v_triangulationDelaunay = new BeanTriangulationDelaunay();
            try
            {
                //[Je filtre les id négatifs: correspondent au Mbo de construction
                foreach (KeyValuePair<int, Point> v_pointsOriginels in p_beanAlimentationVoronoi.p10_dicoDesPointsSources)
                {
                    if (v_pointsOriginels.Key >= 0)
                    {
                        v_triangulationDelaunay.p00_PointIGeometrySources.Add(v_pointsOriginels.Key, v_pointsOriginels.Value);
                    }
                }
                //
                double v_OrigX_Left;
                double v_OrigY_Left;
                double v_OrigX_Right;
                double v_OrigY_Right;
                double v_CorrectionX = 0;
                double v_CorrectionY = 0;
                //
                if (p_beanAlimentationVoronoi.p12_parametrage.reductionCoordonneesVf)
                {
                    v_CorrectionX = (double)p_beanAlimentationVoronoi.p55_origineXCorrigee;
                    v_CorrectionY = (double)p_beanAlimentationVoronoi.p56_origineYCorrigee;
                }

                BeanArcDelaunay v_arcDelaunay;
                string v_CodeXYPointGauche;
                string v_CodeXYPointDroit;
                ITopologieService v_topologieService = new TopologieService();
                int v_cleDroite;
                int v_cleGauche;
                int v_KeyArc = 0;
                foreach (VoronoiEdge v_Edges in p_voronoiGraph.Edges)
                {
                    if (!v_Edges.IsInfinite && !v_Edges.IsPartlyInfinite)
                    {
                        v_KeyArc++;
                        v_OrigX_Left = v_Edges.LeftData[0] + v_CorrectionX;
                        v_OrigY_Left = v_Edges.LeftData[1] + v_CorrectionY;
                        v_OrigX_Right = v_Edges.RightData[0] + v_CorrectionX;
                        v_OrigY_Right = v_Edges.RightData[1] + v_CorrectionY;
                        //
                        //v_CodeXYPointGauche = GetCodeXY(v_OrigX_Left, v_OrigY_Left);
                        v_CodeXYPointGauche = FLabServices.createUtilitaires().GetHCodeGeogPoint(new double[2] { v_OrigX_Left, v_OrigY_Left });
                        //v_CodeXYPointDroit = GetCodeXY(v_OrigX_Right, v_OrigY_Right);
                        v_CodeXYPointDroit = FLabServices.createUtilitaires().GetHCodeGeogPoint(new double[2] { v_OrigX_Right, v_OrigY_Right });
                        //
                        if (p_beanAlimentationVoronoi.p31_correspondanceHCodePointvsIdPoint.ContainsKey(v_CodeXYPointGauche) && p_beanAlimentationVoronoi.p31_correspondanceHCodePointvsIdPoint.ContainsKey(v_CodeXYPointDroit))
                        {
                            v_cleGauche = p_beanAlimentationVoronoi.p31_correspondanceHCodePointvsIdPoint[v_CodeXYPointGauche];
                            v_cleDroite = p_beanAlimentationVoronoi.p31_correspondanceHCodePointvsIdPoint[v_CodeXYPointDroit];
                            if (v_cleDroite >= 0 && v_cleGauche >= 0)
                            {
                                v_arcDelaunay = new BeanArcDelaunay();
                                v_arcDelaunay.p11_idPoint1 = v_cleGauche;
                                v_arcDelaunay.p21_coordPoint1[0] = v_OrigX_Left;
                                v_arcDelaunay.p21_coordPoint1[1] = v_OrigY_Left;
                                //
                                v_arcDelaunay.p12_idPoint2 = v_cleDroite;
                                v_arcDelaunay.p22_coordPoint2[0] = v_OrigX_Right;
                                v_arcDelaunay.p22_coordPoint2[1] = v_OrigY_Right;
                                //
                                v_arcDelaunay.p00_codeArcDelaunay = GetHCodeArcDelaunay(v_cleGauche, v_cleDroite, true);
                                //
                                v_arcDelaunay.p30_arcDelaunay = v_topologieService.GetLineStringByCoord(v_OrigX_Left, v_OrigY_Left, v_OrigX_Right, v_OrigY_Right);
                                v_triangulationDelaunay.p01_arcsDelaunay.Add(v_arcDelaunay);
                            }
                        }

                    }
                }
            }
            catch (Exception v_ex)
            {
                throw v_ex;
            }
            return v_triangulationDelaunay;
        }

        public string GetHCodeArcDelaunay(int p_id1, int p_id2, bool p_nonOrdonnance_vf, char p_separateur = '_')
        {
            if (p_nonOrdonnance_vf)
            {
                return p_id1.ToString() + p_separateur + p_id2.ToString();
            }
            else
            {
                return Math.Min(p_id1, p_id2).ToString() + p_separateur + Math.Max(p_id1, p_id2).ToString();
            }
        }
        public int[] GetIdPointsDelaunayByHCodeArc(string p_codeArcDelaunay, char p_separateur = '_')
        {
            int[] v_idPoints = new int[2];
            string[] v_idPointSTXt = p_codeArcDelaunay.Split(p_separateur);
            v_idPoints[0] = Convert.ToInt32(v_idPointSTXt[0]);
            v_idPoints[1] = Convert.ToInt32(v_idPointSTXt[1]);
            return v_idPoints;
        }
        //EN DEV
        private Dictionary<int, Double[]> GetPointsCadresVoronoi(BeanAlimentationVoronoi p_BeanAlimentationVoronoi, enumVoronoiTypeAjoutPointsFrontieres p_methode = enumVoronoiTypeAjoutPointsFrontieres.standard)
        {
            Dictionary<int, Double[]> v_dicoPointsCadres = new Dictionary<int, double[]>();
            try
            {
                switch (p_methode)
                {
                    case enumVoronoiTypeAjoutPointsFrontieres.standard:
                        v_dicoPointsCadres = GetPointsCadresVoronoi_Standard(p_BeanAlimentationVoronoi);
                        break;
                }
            }
            catch (Exception v_ex)
            {
                throw v_ex;
            }
            return v_dicoPointsCadres;
        }
        private Dictionary<int, Double[]> GetPointsCadresVoronoi_Standard(BeanAlimentationVoronoi p_BeanAlimentationVoronoi)
        {
            Dictionary<int, Double[]> v_dicoPointsCadres = new Dictionary<int, double[]>();
            try
            {
                //Récup des paramètres de calcul primaire

                double p_TxReductionMargeX = p_BeanAlimentationVoronoi.p12_parametrage.txReductionMargeEnX;
                double p_TxReductionMargeY = p_BeanAlimentationVoronoi.p12_parametrage.txReductionMargeEnY;
                int p_nbPointsLargeur = p_BeanAlimentationVoronoi.p12_parametrage.nbPointsLargeur;
                int p_nbPointsHauteur = p_BeanAlimentationVoronoi.p12_parametrage.nbPointsHauteur;

                //Calcul des paramètres secondaires
                double v_encombrementX = (double)p_BeanAlimentationVoronoi.p53_xMax - (double)p_BeanAlimentationVoronoi.p51_xMin;
                double v_encombrementY = (double)p_BeanAlimentationVoronoi.p54_yMax - (double)p_BeanAlimentationVoronoi.p52_yMin;

                //FAU 2018/04/17  PUSTULE: =>Dans certains cas (ex commune très large avec deux petites ug 'calées' dans un coin. ex 30117)
                //une partie de la commune peut ne pas être couverte=>j'étends l'encombrement.
                v_encombrementX = v_encombrementX * 3;
                v_encombrementY = v_encombrementY * 3;
                //FIN PUSTULE

                double v_XOrigine = (p_BeanAlimentationVoronoi.p51_xMin - (v_encombrementX / p_TxReductionMargeX));
                double v_YOrigine = (p_BeanAlimentationVoronoi.p52_yMin - (v_encombrementY / p_TxReductionMargeY));

                double v_encombrementXCorrige = v_encombrementX + (2 * v_encombrementX / p_TxReductionMargeX);
                double v_encombrementYCorrige = v_encombrementY + (2 * v_encombrementY / p_TxReductionMargeY);

                double v_pasX = v_encombrementXCorrige / (p_nbPointsLargeur - 1);
                double v_pasY = v_encombrementYCorrige / (p_nbPointsHauteur - 1);
                //
                int v_cleMin = 0;
                foreach (int v_key in p_BeanAlimentationVoronoi.p10_dicoDesPointsSources.Keys)
                {
                    if (v_key < v_cleMin) { v_cleMin = v_key; }
                }
                int p_debutNumerotationNegative = v_cleMin - 1;
                v_dicoPointsCadres = CalculPointsCadres(v_XOrigine, v_YOrigine, p_nbPointsLargeur, v_pasX, p_nbPointsHauteur, v_pasY, p_debutNumerotationNegative);
            }
            catch (Exception v_ex)
            {
                throw v_ex;
            }
            return v_dicoPointsCadres;
        }
        private Dictionary<int, Double[]> CalculPointsCadres(double p_XOrigine, double p_YOrigine, int p_nbPointsX, double p_pasX, int p_nbPointsY, double p_pasY, int p_debutNumerotationNegative)
        {
            Dictionary<int, Double[]> v_dicoPointsCadres = new Dictionary<int, Double[]>();
            try
            {
                double[] v_doublePoint;
                int v_no_point = p_debutNumerotationNegative + 1;
                ITopologieService v_topologieService = new TopologieService();

                for (int i = 0; i < p_nbPointsX; i++)
                {
                    v_doublePoint = new double[2];

                    v_doublePoint[0] = p_XOrigine + (i * p_pasX);
                    v_doublePoint[1] = p_YOrigine;
                    v_no_point--;
                    v_dicoPointsCadres.Add(v_no_point, v_doublePoint);

                    v_doublePoint = new double[2];
                    v_doublePoint[0] = p_XOrigine + (i * p_pasX);
                    v_doublePoint[1] = p_YOrigine + ((p_nbPointsY - 1) * p_pasY);
                    v_no_point--;
                    v_dicoPointsCadres.Add(v_no_point, v_doublePoint);
                }
                if (p_nbPointsY > 2)
                {
                    for (int i = 1; i < p_nbPointsY - 1; i++)
                    {
                        v_doublePoint = new double[2];
                        v_doublePoint[0] = p_XOrigine;
                        v_doublePoint[1] = p_YOrigine + +(i * p_pasY);
                        v_no_point--;
                        v_no_point--;
                        v_dicoPointsCadres.Add(v_no_point, v_doublePoint);


                        v_doublePoint = new double[2];
                        v_doublePoint[0] = p_XOrigine + ((p_nbPointsY - 1) * p_pasX);
                        v_doublePoint[1] = p_YOrigine + (i * p_pasY);
                        v_no_point--;
                        v_no_point--;
                        v_dicoPointsCadres.Add(v_no_point, v_doublePoint);
                    }
                }
            }
            catch (Exception v_ex)
            {
                throw v_ex;
            }
            return v_dicoPointsCadres;
        }

        //OBSOLETE
        public void AjoutPointsFrontiere(ref BeanAlimentationVoronoi p_BeanAlimentationVoronoi, enumVoronoiTypeAjoutPointsFrontieres p_methode = enumVoronoiTypeAjoutPointsFrontieres.standard)
        {
            try
            {
                switch (p_methode)
                {
                    case enumVoronoiTypeAjoutPointsFrontieres.standard:
                        AjoutPointsFrontiereStandard(ref p_BeanAlimentationVoronoi);
                        break;
                }
            }
            catch (Exception v_ex)
            {
                throw v_ex;
            }
        }
        private void AjoutPointsFrontiereStandard(ref BeanAlimentationVoronoi p_BeanAlimentationVoronoi, int p_nbPointsLargeur = 3, int p_nbPointsHauteur = 3, double p_TxReductionMargeX = 1, double p_TxReductionMargeY = 1, bool p_MajEncombrement = true)
        {
            try
            {
                double v_encombrementX = (double)p_BeanAlimentationVoronoi.p53_xMax - (double)p_BeanAlimentationVoronoi.p51_xMin;
                double v_encombrementY = (double)p_BeanAlimentationVoronoi.p54_yMax - (double)p_BeanAlimentationVoronoi.p52_yMin;

                double v_XOrigine = (p_BeanAlimentationVoronoi.p51_xMin - (v_encombrementX / p_TxReductionMargeX));
                double v_YOrigine = (p_BeanAlimentationVoronoi.p52_yMin - (v_encombrementY / p_TxReductionMargeY));

                double v_encombrementXCorrige = v_encombrementX + (2 * v_encombrementX / p_TxReductionMargeX);
                double v_encombrementYCorrige = v_encombrementY + (2 * v_encombrementY / p_TxReductionMargeY);

                double v_pasX = v_encombrementXCorrige / (p_nbPointsLargeur - 1);
                double v_pasY = v_encombrementYCorrige / (p_nbPointsHauteur - 1);

                double[] v_CoordPourVector;

                for (int i = 0; i < p_nbPointsLargeur; i++)
                {
                    v_CoordPourVector = new double[2];
                    v_CoordPourVector[0] = v_XOrigine + (i * v_pasX);
                    v_CoordPourVector[1] = v_YOrigine;
                    p_BeanAlimentationVoronoi.p50_pointsFormatesPourInsertion.Add(new Vector(v_CoordPourVector));

                    v_CoordPourVector = new double[2];
                    v_CoordPourVector[0] = v_XOrigine + (i * v_pasX);
                    v_CoordPourVector[1] = v_YOrigine + ((p_nbPointsHauteur - 1) * v_pasY);
                    p_BeanAlimentationVoronoi.p50_pointsFormatesPourInsertion.Add(new Vector(v_CoordPourVector));
                }
                if (p_nbPointsHauteur > 2)
                {
                    for (int i = 1; i < p_nbPointsHauteur - 1; i++)
                    {
                        v_CoordPourVector = new double[2];
                        v_CoordPourVector[0] = v_XOrigine;
                        v_CoordPourVector[1] = v_YOrigine + +(i * v_pasY);
                        p_BeanAlimentationVoronoi.p50_pointsFormatesPourInsertion.Add(new Vector(v_CoordPourVector));

                        v_CoordPourVector = new double[2];
                        v_CoordPourVector[0] = v_XOrigine + ((p_nbPointsLargeur - 1) * v_pasX);
                        v_CoordPourVector[1] = v_YOrigine + (i * v_pasY);
                        p_BeanAlimentationVoronoi.p50_pointsFormatesPourInsertion.Add(new Vector(v_CoordPourVector));
                    }
                }

                //
                if (p_MajEncombrement)
                {
                    p_BeanAlimentationVoronoi.p51_xMin = v_XOrigine;
                    p_BeanAlimentationVoronoi.p52_yMin = v_YOrigine;
                    p_BeanAlimentationVoronoi.p53_xMax = v_XOrigine + ((p_nbPointsLargeur - 1) * v_pasX);
                    p_BeanAlimentationVoronoi.p54_yMax = v_YOrigine + ((p_nbPointsHauteur - 1) * v_pasY);
                }
            }
            catch (Exception v_ex)
            {
                throw v_ex;
            }
        }

        public string GetCodeXY(double p_X, double p_Y, int p_nbDecimales = 0)
        {
            string v_code = "";
            try
            {
                v_code = Math.Round(p_X, p_nbDecimales, MidpointRounding.AwayFromZero).ToString() + "/" + Math.Round(p_Y, p_nbDecimales, MidpointRounding.AwayFromZero).ToString();
            }
            catch (Exception v_ex)
            {
                throw v_ex;
            }
            return v_code;
        }

     
    }
}
