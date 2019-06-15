using GeoAPI.Geometries;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace DEM.Net.Core.Voronoi
{
    public class VoronoiServices : IVoronoiServices
    {
        public BeanTopologie GetTopologieVoronoiByDicoPointsEtMbo(Dictionary<int, IGeometry> p_dicoPointsAvecId, double[] p_coodMinMaxXy)
        {
            BeanTopologie v_topologieVoronoi = new BeanTopologie();
            try
            {
                BeanAlimentationVoronoi v_BeanDAlimentation = GetBeanAlimentationVoronoiByDicoPointsEtMbo(p_dicoPointsAvecId, p_coodMinMaxXy);

                if (!v_BeanDAlimentation.contientObjetsInvalidesVf)
                {
                    VoronoiGraph v_voronoiGraph = GetVoronoiGraph(v_BeanDAlimentation);
                    v_topologieVoronoi = GetBeanTopologieByVoronoiGraph(v_voronoiGraph, v_BeanDAlimentation);
                }
            }
            catch (Exception v_ex)
            {
                throw;
            }
            return v_topologieVoronoi;
        }
        public BeanTopologie GetTopologieVoronoiByDicoPointsEtRegroupementDesPointsEtMbo(Dictionary<int, IGeometry> p_dicoPointsDesSurfaces, Dictionary<int, List<int>> p_dicoDesPointsParSurfaces, double[] p_coodMinMaxXy)
        {
            BeanTopologie v_topologieFusion = new BeanTopologie();
            try
            {
                BeanTopologie v_topologieVoronoi = GetTopologieVoronoiByDicoPointsEtMbo(p_dicoPointsDesSurfaces, p_coodMinMaxXy);

                //#if DEBUG
                //				visuIlotsDansDico(v_topologieVoronoi.BT_ListeIlots, "Vor");
                //#endif

                ITopologieService v_topologieService = new TopologieService();
                //Contrôle topologie
                bool v_test = v_topologieService.IsTopologieIlotsOk_vf(v_topologieVoronoi);
                //Contrôle adéquation du p_dicoPointsDesSurfaces
                v_topologieFusion = v_topologieService.GetTopologieDIlotsFusionnes(v_topologieVoronoi, p_dicoDesPointsParSurfaces);

                //#if DEBUG
                //				visuIlotsDansDico(v_topologieFusion.BT_ListeIlots, "Fusion");
                //#endif

                //#if DEBUG
                //				List<IGeometry> v_frequencesTailleSegments = v_topologieService.GetCourbeFrequencesCumuleesByPasDesLongueursDeSegments(v_topologieVoronoi.BT_ListeArcs, 30);
                //#endif
            }
            catch (Exception v_ex)
            {
                throw v_ex;
            }
            return v_topologieFusion;
        }

        public BeanTopologie GetTopologieVoronoiByDicoPoints(Dictionary<int, IGeometry> p_dicoPointsAvecId, enumVoronoiStrategiePointsDupliques p_strategieSiDuplication = enumVoronoiStrategiePointsDupliques.arretTraitement)
        {
            BeanTopologie v_topologieVoronoi = new BeanTopologie();
            try
            {
                BeanAlimentationVoronoi v_BeanDAlimentation = GetBeanAlimentationVoronoiByDicoPoints(p_dicoPointsAvecId);
                v_BeanDAlimentation.parametrage.gestionPointsDupliques = p_strategieSiDuplication;
                if (!v_BeanDAlimentation.contientObjetsInvalidesVf)
                {
                    VoronoiGraph v_voronoiGraph = GetVoronoiGraph(v_BeanDAlimentation);
                    v_topologieVoronoi = GetBeanTopologieByVoronoiGraph(v_voronoiGraph, v_BeanDAlimentation);
                }
            }
            catch (Exception v_ex)
            {
                throw v_ex;
            }
            return v_topologieVoronoi;
        }
        public BeanTopologie GetTopologieVoronoiByDicoPointsEtRegroupementDesPoints(Dictionary<int, IGeometry> p_dicoPointsDesSurfaces, Dictionary<int, List<int>> p_dicoDesPointsParSurfaces)
        {
            BeanTopologie v_topologieFusion = new BeanTopologie();
            try
            {
                BeanTopologie v_topologieVoronoi = GetTopologieVoronoiByDicoPoints(p_dicoPointsDesSurfaces);
                ITopologieService v_topologieService = new TopologieService();
                v_topologieFusion = v_topologieService.GetTopologieDIlotsFusionnes(v_topologieVoronoi, p_dicoDesPointsParSurfaces);
            }
            catch (Exception v_ex)
            {
                throw v_ex;
            }
            return v_topologieFusion;
        }
        //
        public BeanTriangulationDelaunay GetTriangulationDeDelaunayByDicoPoints(Dictionary<int, IGeometry> p_dicoPointsAvecId)
        {
            BeanTriangulationDelaunay v_trOut = null;
            try
            {
                BeanAlimentationVoronoi v_BeanDAlimentation = GetBeanAlimentationVoronoiByDicoPoints(p_dicoPointsAvecId);
                v_BeanDAlimentation.parametrage.gestionPointsDupliques = enumVoronoiStrategiePointsDupliques.deduplicationAleatoire;

                if (!v_BeanDAlimentation.contientObjetsInvalidesVf)
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
        public Dictionary<int, IGeometry> GetArcsDelaunayGeometryByTriangulationDelaunay(BeanTriangulationDelaunay p_triangulationDelaunay)
        {
            Dictionary<int, IGeometry> v_arcs = new Dictionary<int, IGeometry>();
            try
            {
                int v_index = 0;
                foreach (BeanArcDelaunay v_arcDelaunay in p_triangulationDelaunay.p01_arcsDelaunay)
                {
                    v_arcs.Add(v_index, v_arcDelaunay.p30_arcDelaunay);
                    v_index++;
                }
            }
            catch (Exception v_ex)
            {
                throw;
            }
            return v_arcs;
        }




        public BeanAlimentationVoronoi GetBeanAlimentationVoronoiByDicoPoints(Dictionary<int, IGeometry> p_DicoPoints)
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
                v_BeanAlimentationVoronoi = GetBeanAlimentationVoronoiByDicoPointsEtMbo(p_DicoPoints, v_pseudoCoordonneesMinMax);
            }
            catch (Exception v_ex)
            {
                throw v_ex;
            }
            return v_BeanAlimentationVoronoi;
        }
        public BeanAlimentationVoronoi GetBeanAlimentationVoronoiByDicoPointsEtMbo(Dictionary<int, IGeometry> p_DicoPoints, double[] p_coodMinMaxXy)
        {
            BeanAlimentationVoronoi v_BeanAlimentationVoronoi = new BeanAlimentationVoronoi();
            try
            {
                v_BeanAlimentationVoronoi.contientObjetsInvalidesVf = false;
                v_BeanAlimentationVoronoi.contientObjetsSuperposesVf = false;
                v_BeanAlimentationVoronoi.territoireSuperieurA200kmVf = false;

                //v_BeanAlimentationVoronoi.xMin = 1000000000;
                //v_BeanAlimentationVoronoi.yMin = 1000000000;
                //v_BeanAlimentationVoronoi.xMax = -1000000000;
                //v_BeanAlimentationVoronoi.yMax = -1000000000;

                v_BeanAlimentationVoronoi.xMin = p_coodMinMaxXy[0];
                v_BeanAlimentationVoronoi.yMin = p_coodMinMaxXy[1];
                v_BeanAlimentationVoronoi.xMax = p_coodMinMaxXy[2];
                v_BeanAlimentationVoronoi.yMax = p_coodMinMaxXy[3];

                List<Double[]> v_ListeDoublePourVectors = new List<Double[]>();
                string v_codeXYPoint;
                double[] v_DoublePourVector;
                int v_keyDoublon;

                foreach (KeyValuePair<int, IGeometry> v_point in p_DicoPoints)
                {
                    v_BeanAlimentationVoronoi.dicoDesPointsSources.Add(v_point.Key, v_point.Value);
                    if (v_point.Value.OgcGeometryType == OgcGeometryType.Point)
                    {
                        //Je génère le code à partir des coordonnees
                        v_codeXYPoint = GetCodeXY((double)v_point.Value.Coordinate.X, (double)v_point.Value.Coordinate.Y);
                        if (v_BeanAlimentationVoronoi.dicoLienCodeXyKeySource.ContainsKey(v_codeXYPoint))
                        {
                            //j'ajoute dans la liste des doublons l'id du point en cours 
                            v_BeanAlimentationVoronoi.pointsSuperposes.Add(v_point.Key);
                            v_BeanAlimentationVoronoi.contientObjetsSuperposesVf = true;
                            //+ celui déjà listé s'il n'existe pas déjà
                            v_BeanAlimentationVoronoi.dicoLienCodeXyKeySource.TryGetValue(v_codeXYPoint, out v_keyDoublon);
                            if (!v_BeanAlimentationVoronoi.pointsSuperposes.Contains(v_keyDoublon)) { v_BeanAlimentationVoronoi.pointsSuperposes.Add(v_keyDoublon); }
                        }
                        else  //Si l'objet est OK à ce stade...
                        {
                            //J'injecte dans la liste des correspondances
                            v_BeanAlimentationVoronoi.dicoLienCodeXyKeySource.Add(v_codeXYPoint, v_point.Key);
                            //
                            v_DoublePourVector = new double[2];
                            v_DoublePourVector[0] = (double)v_point.Value.Coordinate.X;
                            v_DoublePourVector[1] = (double)v_point.Value.Coordinate.Y;

                            //J'ajoute dans une liste temporaire et pas dans le HS car il semble nécessaire de faire un changement de repère avant d'injecter les données ds le HS<vector>
                            v_ListeDoublePourVectors.Add(v_DoublePourVector);

                            //récupération des points extrêmes
                            if (v_BeanAlimentationVoronoi.xMin > (double)v_point.Value.Coordinate.X) { v_BeanAlimentationVoronoi.xMin = (double)v_point.Value.Coordinate.X; }
                            if (v_BeanAlimentationVoronoi.yMin > (double)v_point.Value.Coordinate.Y) { v_BeanAlimentationVoronoi.yMin = (double)v_point.Value.Coordinate.Y; }
                            if (v_BeanAlimentationVoronoi.xMax < (double)v_point.Value.Coordinate.X) { v_BeanAlimentationVoronoi.xMax = (double)v_point.Value.Coordinate.X; }
                            if (v_BeanAlimentationVoronoi.yMax < (double)v_point.Value.Coordinate.Y) { v_BeanAlimentationVoronoi.yMax = (double)v_point.Value.Coordinate.Y; }
                        }
                    }//Si le IGeometry n'est pas un point (FIN if (v_point.Value.STGeometryType() == OpenGisGeographyType.Point.ToString())
                    else
                    {
                        v_BeanAlimentationVoronoi.pointsInvalidesSaufSuperposes.Add(v_point.Key);
                        v_BeanAlimentationVoronoi.contientObjetsInvalidesVf = true;
                    }
                }//FIN foreach

                //Création des points périphériques artificiels
                Dictionary<int, Double[]> v_dicoPointscadres = GetPointsCadresVoronoi(v_BeanAlimentationVoronoi);

                ////POUR DEBUG
                //ITopologieService v_topologieServices = new TopologieService();
                //IGeometry v_geom;
                //foreach(Double[] v_coordPoint in v_dicoPointscadres.Values)
                //{
                //	v_geom = v_topologieServices.GetUnPointIGeometryByCoordonneesXy(v_coordPoint[0], v_coordPoint[1]);
                //	v_topologieServices.visuGeometry(v_geom, "POINTS CADRES");
                //}
                ////FIN DEBUG


                //Injection dans les listes
                Dictionary<int, IGeometry> v_newListePoint = new Dictionary<int, IGeometry>();

                ITopologieService v_topologieService = new TopologieService();
                foreach (KeyValuePair<int, Double[]> v_pointSup in v_dicoPointscadres)
                {
                    v_codeXYPoint = GetCodeXY(v_pointSup.Value[0], v_pointSup.Value[1]);
                    //PUSTULE FAU
                    if (v_BeanAlimentationVoronoi.dicoLienCodeXyKeySource.ContainsKey(v_codeXYPoint))
                    {
                        continue;
                    }
                    v_BeanAlimentationVoronoi.dicoLienCodeXyKeySource.Add(v_codeXYPoint, v_pointSup.Key);
                    v_BeanAlimentationVoronoi.dicoDesPointsSources.Add(v_pointSup.Key, v_topologieService.GetUnPointIGeometryByCoordonneesXy((float)v_pointSup.Value[0], (float)v_pointSup.Value[1]));
                    v_ListeDoublePourVectors.Add(v_pointSup.Value);
                }

                //Changement d'origine des coordonnées
                //(Indispensable pour pouvoir gérer les calculs sur les coord L93 (>10 puissance 5)) 
                v_BeanAlimentationVoronoi.origineXCorrigee = (int)(Math.Round(v_BeanAlimentationVoronoi.xMin) - 1);
                v_BeanAlimentationVoronoi.origineYCorrigee = (int)(Math.Round(v_BeanAlimentationVoronoi.yMin) - 1);
                if (v_BeanAlimentationVoronoi.parametrage.reductionCoordonneesVf)
                {
                    v_BeanAlimentationVoronoi.xMin = v_BeanAlimentationVoronoi.xMin - v_BeanAlimentationVoronoi.origineXCorrigee;
                    v_BeanAlimentationVoronoi.xMax = v_BeanAlimentationVoronoi.xMax - v_BeanAlimentationVoronoi.origineXCorrigee;
                    v_BeanAlimentationVoronoi.yMin = v_BeanAlimentationVoronoi.yMin - v_BeanAlimentationVoronoi.origineYCorrigee;
                    v_BeanAlimentationVoronoi.yMax = v_BeanAlimentationVoronoi.yMax - v_BeanAlimentationVoronoi.origineYCorrigee;
                }

                foreach (Double[] v_DoubleVectorPourCorrection in v_ListeDoublePourVectors)
                {
                    if (v_BeanAlimentationVoronoi.parametrage.reductionCoordonneesVf)
                    {
                        v_DoubleVectorPourCorrection[0] = v_DoubleVectorPourCorrection[0] - v_BeanAlimentationVoronoi.origineXCorrigee;
                        v_DoubleVectorPourCorrection[1] = v_DoubleVectorPourCorrection[1] - v_BeanAlimentationVoronoi.origineYCorrigee;
                    }
                    v_BeanAlimentationVoronoi.pointsFormatesPourInsertion.Add(new Vector(v_DoubleVectorPourCorrection));
                }
                //
                if (((v_BeanAlimentationVoronoi.xMax - v_BeanAlimentationVoronoi.xMin) > 200000) || ((v_BeanAlimentationVoronoi.yMax - v_BeanAlimentationVoronoi.yMin) > 200000))
                {
                    v_BeanAlimentationVoronoi.territoireSuperieurA200kmVf = true;
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
                if (p_beanAlimentationVoronoi.contientObjetsInvalidesVf)
                {
                    switch (p_beanAlimentationVoronoi.parametrage.gestionObjetsInvalides)
                    {
                        case enumVoronoiStrategieObjetsInvalides.arretTraitement:
                            throw new Exception("GetVoronoiGraph en erreur: des objets ne sont pas des points", new Exception());
                        case enumVoronoiStrategieObjetsInvalides.ignorerCesObjets:
                            break;
                    }
                }
                if (p_beanAlimentationVoronoi.contientObjetsSuperposesVf)
                {
                    switch (p_beanAlimentationVoronoi.parametrage.gestionPointsDupliques)
                    {
                        case enumVoronoiStrategiePointsDupliques.arretTraitement:
                            throw new Exception("GetVoronoiGraph en erreur: des points sont superposés.", new Exception());
                        case enumVoronoiStrategiePointsDupliques.deduplicationAleatoire:
                            break;
                    }
                }


                if (p_beanAlimentationVoronoi.territoireSuperieurA200kmVf)
                {
                    switch (p_beanAlimentationVoronoi.parametrage.gestionDepassementTerritoire)
                    {
                        case enumVoronoiStrategieDistanceTropGrande.arretTraitement:
                            throw new Exception("GetVoronoiGraph en erreur: le territoire total de traitement ne peut pas dépasser 200 000 x 200 000 unités de longueur ");
                        case enumVoronoiStrategieDistanceTropGrande.reductionPrecision:
                            throw new Exception("GetVoronoiGraph en erreur: le territoire total de traitement ne peut pas dépasser 200x200 unités de longueur (reduction précision non implementée)");
                    }
                }

                v_voronoiGraph = Fortune.ComputeVoronoiGraph(p_beanAlimentationVoronoi.pointsFormatesPourInsertion);


            }
            catch (Exception v_ex)
            {
                throw v_ex;
            }
            return v_voronoiGraph;
        }
        public BeanTopologie GetBeanTopologieByVoronoiGraph(VoronoiGraph p_voronoiGraph, BeanAlimentationVoronoi p_beanAlimentationVoronoi)
        {
            BeanTopologie v_topologieVoronoi = new BeanTopologie();
            try
            {
                Dictionary<int, IGeometry> v_DicoLignes = new Dictionary<int, IGeometry>();
                Dictionary<int, int> v_DicoPointSourceGaucheParArc = new Dictionary<int, int>();
                Dictionary<int, int> v_DicoPointSourceDroitParArc = new Dictionary<int, int>();

                //Création du réseau
                BeanTopologie v_topologieLignes = new BeanTopologie();

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
                IGeometry v_line;
                String v_CodeXYPointGauche;
                String v_CodeXYPointDroit;
                int v_KeyPointGauche;
                int v_KeyPointDroit;
                int v_KeyArc = 0;
                HashSet<string> v_testDoublesLignes = new HashSet<string>();
                string v_codeLigne;

                if (p_beanAlimentationVoronoi.parametrage.reductionCoordonneesVf)
                {
                    v_CorrectionX = (double)p_beanAlimentationVoronoi.origineXCorrigee;
                    v_CorrectionY = (double)p_beanAlimentationVoronoi.origineYCorrigee;
                }


                foreach (VoronoiEdge v_Edges in p_voronoiGraph.Edges)
                {
                    if (!v_Edges.IsInfinite && !v_Edges.IsPartlyInfinite)
                    {
                        v_KeyArc++;
                        //Je créé des variables intermédiaires car modifierLaGeometrie directement le vecteur pose graves pb
                        v_vorX_Left = v_Edges.VVertexA[0] + v_CorrectionX;
                        v_vorY_Left = v_Edges.VVertexA[1] + v_CorrectionY;
                        v_vorX_Right = v_Edges.VVertexB[0] + v_CorrectionX;
                        v_vorY_Right = v_Edges.VVertexB[1] + v_CorrectionY;
                        v_OrigX_Left = v_Edges.LeftData[0] + v_CorrectionX;
                        v_OrigY_Left = v_Edges.LeftData[1] + v_CorrectionY;
                        v_OrigX_Right = v_Edges.RightData[0] + v_CorrectionX;
                        v_OrigY_Right = v_Edges.RightData[1] + v_CorrectionY;
                        //

                        v_line = v_topologieService.GetLineStringByCoord(v_vorX_Left, v_vorY_Left, v_vorX_Right, v_vorY_Right);

                        if (v_line.Length > 0)
                        {
                            v_CodeXYPointGauche = GetCodeXY(v_OrigX_Left, v_OrigY_Left);
                            v_CodeXYPointDroit = GetCodeXY(v_OrigX_Right, v_OrigY_Right);
                            //J'ai rencontré des duplications de lignes (dans un sens et dans l'autre)
                            //Ces cas provoquent des boucles sans fin dans la fermeture d'îlots
                            // Je mets un patch : A réintégrer DANS TOPOLOGIE SERVICE  	
                            if (v_OrigX_Left < v_OrigX_Right)
                            {
                                v_codeLigne = v_CodeXYPointGauche + "_" + v_CodeXYPointDroit;
                            }
                            else
                            {
                                if (v_OrigX_Left == v_OrigX_Right)
                                {
                                    if (v_OrigY_Left < v_OrigY_Right)
                                    {
                                        v_codeLigne = v_CodeXYPointGauche + "_" + v_CodeXYPointDroit;
                                    }
                                    else
                                    { v_codeLigne = v_CodeXYPointDroit + "_" + v_CodeXYPointGauche; }
                                }
                                else
                                { v_codeLigne = v_CodeXYPointDroit + "_" + v_CodeXYPointGauche; }
                            }
                            //Si la ligne est déjà contenue, je ne l'introduis pas
                            if (v_testDoublesLignes.Contains(v_codeLigne)) { break; }

                            //
                            v_DicoLignes.Add(v_KeyArc, v_line);

                            v_KeyPointGauche = -1;
                            if (p_beanAlimentationVoronoi.dicoLienCodeXyKeySource.ContainsKey(v_CodeXYPointGauche))
                            {
                                p_beanAlimentationVoronoi.dicoLienCodeXyKeySource.TryGetValue(v_CodeXYPointGauche, out v_KeyPointGauche);
                            }
                            v_DicoPointSourceGaucheParArc.Add(v_KeyArc, v_KeyPointGauche);
                            v_KeyPointDroit = -1;
                            if (p_beanAlimentationVoronoi.dicoLienCodeXyKeySource.ContainsKey(v_CodeXYPointGauche))
                            {
                                p_beanAlimentationVoronoi.dicoLienCodeXyKeySource.TryGetValue(v_CodeXYPointDroit, out v_KeyPointDroit);
                            }
                            v_DicoPointSourceDroitParArc.Add(v_KeyArc, v_KeyPointDroit);
                        }
                    }//Fin if (!v_Edges.IsInfinite && !v_Edges.IsPartlyInfinite)
                }//Fin foreach
                 //Création de la topologie

                v_topologieLignes = v_topologieService.GetTopologie(v_DicoLignes);
                v_topologieVoronoi = v_topologieService.GetTopologieSansImpassesEnrichiesDesIlots(v_topologieLignes);
                v_topologieService.MiseAJourDesIndicateursDeControleTopologieIlot(v_topologieVoronoi);

                ////TO DEBUG
                //visuArcsDansTopologie(v_topologieVoronoi, "Test arcs");
                //visuPointsDeDico(p_beanAlimentationVoronoi.dicoDesPointsSources, "points", 10);
                ////FIN TO DEBUG

                //Renumérotation des îlots
                //p_beanAlimentationVoronoi.correspondance_IdIlot_IdPoint = v_topologieService.GetListeRenumerotationDesIlotsByCotesArc(v_topologieVoronoi, v_DicoPointSourceDroitParArc, v_DicoPointSourceGaucheParArc);
                v_topologieService.UpdateIdIlotsByCotesArcs(v_topologieVoronoi, v_DicoPointSourceDroitParArc, v_DicoPointSourceGaucheParArc);
            }
            catch (Exception v_ex)
            {
                throw v_ex;
            }
            return v_topologieVoronoi;
        }

        public BeanTriangulationDelaunay GetTriangulationDelaunayByVoronoiGraph(VoronoiGraph p_voronoiGraph, BeanAlimentationVoronoi p_beanAlimentationVoronoi)
        {
            BeanTriangulationDelaunay v_triangulationDelaunay = new BeanTriangulationDelaunay();
            try
            {
                //[Je filtre les id négatifs: correspondent au Mbo de construction
                foreach (KeyValuePair<int, IGeometry> v_pointsOriginels in p_beanAlimentationVoronoi.dicoDesPointsSources)
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
                if (p_beanAlimentationVoronoi.parametrage.reductionCoordonneesVf)
                {
                    v_CorrectionX = (double)p_beanAlimentationVoronoi.origineXCorrigee;
                    v_CorrectionY = (double)p_beanAlimentationVoronoi.origineYCorrigee;
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
                        v_CodeXYPointGauche = GetCodeXY(v_OrigX_Left, v_OrigY_Left);
                        v_CodeXYPointDroit = GetCodeXY(v_OrigX_Right, v_OrigY_Right);
                        //
                        if (p_beanAlimentationVoronoi.dicoLienCodeXyKeySource.ContainsKey(v_CodeXYPointGauche) && p_beanAlimentationVoronoi.dicoLienCodeXyKeySource.ContainsKey(v_CodeXYPointDroit))
                        {
                            v_cleGauche = p_beanAlimentationVoronoi.dicoLienCodeXyKeySource[v_CodeXYPointGauche];
                            v_cleDroite = p_beanAlimentationVoronoi.dicoLienCodeXyKeySource[v_CodeXYPointDroit];
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
            string v_out = "";
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

                double p_TxReductionMargeX = p_BeanAlimentationVoronoi.parametrage.txReductionMargeEnX;
                double p_TxReductionMargeY = p_BeanAlimentationVoronoi.parametrage.txReductionMargeEnY;
                int p_nbPointsLargeur = p_BeanAlimentationVoronoi.parametrage.nbPointsLargeur;
                int p_nbPointsHauteur = p_BeanAlimentationVoronoi.parametrage.nbPointsHauteur;

                //Calcul des paramètres secondaires
                double v_encombrementX = (double)p_BeanAlimentationVoronoi.xMax - (double)p_BeanAlimentationVoronoi.xMin;
                double v_encombrementY = (double)p_BeanAlimentationVoronoi.yMax - (double)p_BeanAlimentationVoronoi.yMin;

                //FAU 2018/04/17  PUSTULE: =>Dans certains cas (ex commune très large avec deux petites ug 'calées' dans un coin. ex 30117)
                //une partie de la commune peut ne pas être couverte=>j'étends l'encombrement.
                v_encombrementX = v_encombrementX * 3;
                v_encombrementY = v_encombrementY * 3;
                //FIN PUSTULE

                double v_XOrigine = (p_BeanAlimentationVoronoi.xMin - (v_encombrementX / p_TxReductionMargeX));
                double v_YOrigine = (p_BeanAlimentationVoronoi.yMin - (v_encombrementY / p_TxReductionMargeY));

                double v_encombrementXCorrige = v_encombrementX + (2 * v_encombrementX / p_TxReductionMargeX);
                double v_encombrementYCorrige = v_encombrementY + (2 * v_encombrementY / p_TxReductionMargeY);

                double v_pasX = v_encombrementXCorrige / (p_nbPointsLargeur - 1);
                double v_pasY = v_encombrementYCorrige / (p_nbPointsHauteur - 1);
                //
                int v_cleMin = 0;
                foreach (int v_key in p_BeanAlimentationVoronoi.dicoDesPointsSources.Keys)
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
                double v_encombrementX = (double)p_BeanAlimentationVoronoi.xMax - (double)p_BeanAlimentationVoronoi.xMin;
                double v_encombrementY = (double)p_BeanAlimentationVoronoi.yMax - (double)p_BeanAlimentationVoronoi.yMin;

                double v_XOrigine = (p_BeanAlimentationVoronoi.xMin - (v_encombrementX / p_TxReductionMargeX));
                double v_YOrigine = (p_BeanAlimentationVoronoi.yMin - (v_encombrementY / p_TxReductionMargeY));

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
                    p_BeanAlimentationVoronoi.pointsFormatesPourInsertion.Add(new Vector(v_CoordPourVector));

                    v_CoordPourVector = new double[2];
                    v_CoordPourVector[0] = v_XOrigine + (i * v_pasX);
                    v_CoordPourVector[1] = v_YOrigine + ((p_nbPointsHauteur - 1) * v_pasY);
                    p_BeanAlimentationVoronoi.pointsFormatesPourInsertion.Add(new Vector(v_CoordPourVector));
                }
                if (p_nbPointsHauteur > 2)
                {
                    for (int i = 1; i < p_nbPointsHauteur - 1; i++)
                    {
                        v_CoordPourVector = new double[2];
                        v_CoordPourVector[0] = v_XOrigine;
                        v_CoordPourVector[1] = v_YOrigine + +(i * v_pasY);
                        p_BeanAlimentationVoronoi.pointsFormatesPourInsertion.Add(new Vector(v_CoordPourVector));

                        v_CoordPourVector = new double[2];
                        v_CoordPourVector[0] = v_XOrigine + ((p_nbPointsLargeur - 1) * v_pasX);
                        v_CoordPourVector[1] = v_YOrigine + (i * v_pasY);
                        p_BeanAlimentationVoronoi.pointsFormatesPourInsertion.Add(new Vector(v_CoordPourVector));
                    }
                }

                //
                if (p_MajEncombrement)
                {
                    p_BeanAlimentationVoronoi.xMin = v_XOrigine;
                    p_BeanAlimentationVoronoi.yMin = v_YOrigine;
                    p_BeanAlimentationVoronoi.xMax = v_XOrigine + ((p_nbPointsLargeur - 1) * v_pasX);
                    p_BeanAlimentationVoronoi.yMax = v_YOrigine + ((p_nbPointsHauteur - 1) * v_pasY);
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

        //public IGeometry GetLineStringByVectors(Vector p_point1, Vector p_point2, int p_SRid = 2154)
        //{
        //    IGeometry v_Ligne = new IGeometry();
        //    try
        //    {
        //        ITopologieService v_topologieService = new TopologieService();
        //        v_Ligne = v_topologieService.GetLineStringByCoord(p_point1[0], p_point1[1], p_point2[0], p_point2[1], p_SRid);
        //    }
        //    catch (Exception v_ex)
        //    {
        //        throw v_ex;
        //    }
        //    return v_Ligne;
        //}

        ////POUR DEBUG : A SUPP
        //public void visuArcsDansTopologie(BeanTopologie p_beanTopologie, string p_commentaire)
        //{
        //    SpatialTrace.Enable();

        //    SpatialTrace.TraceText(p_commentaire);
        //    foreach (KeyValuePair<int, BeanTopologieArc> v_topologieArc in p_beanTopologie.BT_ListeArcs)
        //    {
        //        SpatialTrace.TraceGeometry(v_topologieArc.Value.Arc_geometry, "Arc: " + v_topologieArc.Value.Arc_id, "Arc: " + v_topologieArc.Value.Arc_id);
        //    }

        //    //
        //    SpatialTrace.Disable();
        //}
        //public void visuIlotsDansDico(Dictionary<int, BeanTopologieIlot> p_dicoBeanIlot, string p_commentaire)
        //{
        //    SpatialTrace.Enable();

        //    SpatialTrace.TraceText(p_commentaire);
        //    foreach (KeyValuePair<int, BeanTopologieIlot> v_ilot in p_dicoBeanIlot)
        //    {
        //        if (v_ilot.Value.IlotQualificationContours != enumQualiteContoursIlot.IlotAnneau)
        //        {
        //            SpatialTrace.TraceGeometry(v_ilot.Value.IlotGeometry, "Ilot no: " + v_ilot.Value.IlotId);
        //        }
        //    }
        //    SpatialTrace.Disable();
        //}
        //public void visuPointsDeDico(Dictionary<int, IGeometry> p_DicoPoints, string p_commentaire, int p_taillePoints)
        //{
        //    SpatialTrace.Enable();

        //    SpatialTrace.TraceText(p_commentaire);
        //    foreach (KeyValuePair<int, IGeometry> v_point in p_DicoPoints)
        //    {
        //        SpatialTrace.TraceGeometry(v_point.Value.STBuffer(p_taillePoints), "Point: " + v_point.Key);
        //    }

        //    //
        //    SpatialTrace.Disable();
        //}

        //public void visuTriangulationDelaunay(BeanTriangulationDelaunay v_triangulationDelaunay)
        //{
        //    ITopologieService v_topologieService = new TopologieService();
        //    Color v_couleur;
        //    //Points originels
        //    v_couleur = Color.FromArgb(200, 200, 0, 0);
        //    foreach (KeyValuePair<int, IGeometry> v_geom in v_triangulationDelaunay.p00_PointIGeometrySources)
        //    {

        //        v_topologieService.visuGeometry(v_geom.Value, "POINTS SOURCES", "ptSourc: " + v_geom.Key, v_couleur, 10);
        //    }
        //    //Arcs
        //    v_couleur = Color.FromArgb(200, 0, 0, 150);
        //    foreach (BeanArcDelaunay v_arc in v_triangulationDelaunay.p01_arcsDelaunay)
        //    {
        //        v_topologieService.visuGeometry(v_arc.p30_arcDelaunay, "ARCS:", "arc: " + v_arc.p11_idPoint1 + " vers " + v_arc.p12_idPoint2, v_couleur);
        //    }
        //}
    }
}
