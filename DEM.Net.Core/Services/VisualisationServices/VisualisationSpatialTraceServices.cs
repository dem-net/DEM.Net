// VisualisationSpatialTraceServices.cs
//
// Author:
//       Frédéric Aubin
//
// Copyright (c) 2019 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the right
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

using DEM.Net.Core.Services.Lab;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlTypes;
using NetTopologySuite.Geometries;
using System.Drawing;

namespace DEM.Net.Core.Services.VisualisationServices
{
    public class VisualisationSpatialTraceServices : IVisualisationSpatialTraceServices
    {
        public Dictionary<string, Color> GetTableCouleursDegradees(List<string> p_classesTriees, enumProgressionCouleurs p_progressionCouleur, int p_alpha, bool p_croissantSinonDecroissant)
        {
            Dictionary<string, Color> v_tableCouleurs = new Dictionary<string, Color>();
            try
            {
                Dictionary<int, Color> v_tab = GetTableCouleursDegradees(p_classesTriees.Count, p_progressionCouleur, p_alpha, p_croissantSinonDecroissant);
                int v_index = 0;
                foreach (KeyValuePair<int, Color> v_col in v_tab)
                {
                    v_tableCouleurs.Add(p_classesTriees[v_index], v_col.Value);
                    v_index++;
                }
            }
            catch (Exception)
            {

                throw;
            }
            return v_tableCouleurs;
        }
        public Dictionary<int, Color> GetTableCouleursDegradees(int p_nbreClasses, enumProgressionCouleurs p_progressionCouleur, int p_alpha = 120, bool p_croissantSinonDecroissant = true)
        {
            Dictionary<int, Color> v_tableCouleurs = new Dictionary<int, Color>();
            try
            {
                Color v_couleur;
                int v_pasDIncremetation = Convert.ToInt16(Math.Round((double)255 / p_nbreClasses));
                int v_niveauCouleurRef;
                int v_niveauCouleurInverseRef;
                if (p_croissantSinonDecroissant)
                {
                    v_niveauCouleurRef = v_pasDIncremetation;
                    v_niveauCouleurInverseRef = 255;
                }
                else
                {
                    v_niveauCouleurRef = 255;
                    v_niveauCouleurInverseRef = v_pasDIncremetation;
                }
                v_niveauCouleurRef = MathHelper.Clamp(v_niveauCouleurRef, 0, 255);
                //
                for (int v_indexClasse = 1; v_indexClasse <= p_nbreClasses; v_indexClasse++)
                {
                    switch (p_progressionCouleur)
                    {
                        case enumProgressionCouleurs.red:
                            v_couleur = Color.FromArgb(p_alpha,v_niveauCouleurRef, 0, 0);
                            break;
                        case enumProgressionCouleurs.green:
                            v_couleur = Color.FromArgb(p_alpha,0, v_niveauCouleurRef, 0);
                            break;
                        case enumProgressionCouleurs.blue:
                            v_couleur = Color.FromArgb(p_alpha, 0, 0, v_niveauCouleurRef);
                            break;
                        case enumProgressionCouleurs.greenVersRed:
                            v_couleur = Color.FromArgb(p_alpha,v_niveauCouleurRef, v_niveauCouleurInverseRef, 0);
                            break;
                        default:
                            throw new Exception("Méthode " + p_progressionCouleur + " non implémentée.");
                    }
                    v_tableCouleurs.Add(v_indexClasse, v_couleur);
                    if (p_croissantSinonDecroissant)
                    {
                        v_niveauCouleurRef += v_pasDIncremetation;
                        v_niveauCouleurInverseRef -= v_pasDIncremetation;
                    }
                    else
                    {
                        v_niveauCouleurRef -= v_pasDIncremetation;
                        v_niveauCouleurInverseRef += v_pasDIncremetation;
                    }
                    v_niveauCouleurRef = MathHelper.Clamp(v_niveauCouleurRef, 0, 255);
                }
            }
            catch (Exception)
            {

                throw;
            }
            return v_tableCouleurs;
        }

        public int GetTailleAffichageDuPoint(double p_largeurEspaceReference, double p_ratioTaillePoint)
        {
            int p_tailleDePoint = 1;
            if (p_ratioTaillePoint <= 0)
            {
                return 1;
            }
            p_tailleDePoint = (int)Math.Round(p_largeurEspaceReference / p_ratioTaillePoint, 0);
            if (p_tailleDePoint == 0)
            {
                p_tailleDePoint = 1;
            }
            return p_tailleDePoint;
        }
        public int GetTailleAffichageDuPoint(List<BeanPoint_internal> p_points, double p_ratioTaillePoint)
        {
            int p_tailleDePoint = 1;
            try
            {
                double v_largeur = p_points.Select(c => c.p10_coord[0]).Max() - p_points.Select(c => c.p10_coord[0]).Min();
                double v_hauteur = p_points.Select(c => c.p10_coord[1]).Max() - p_points.Select(c => c.p10_coord[1]).Min();

                double v_extension = Math.Max(v_largeur, v_hauteur);
                p_tailleDePoint = GetTailleAffichageDuPoint(v_extension, p_ratioTaillePoint);
            }
            catch (Exception)
            {

                throw;
            }
            return p_tailleDePoint;
        }

        public void GetVisuPointsAlti(Dictionary<string, List<BeanPoint_internal>> p_pointsParClasse, Dictionary<string, Color> p_tableCouleurs, int p_taillePoint)
        {
            try
            {
                //SpatialTrace.Enable();
                Color v_couleurCourante;
                string v_message;
                Geometry v_pointGeom;
                foreach (KeyValuePair<string, List<BeanPoint_internal>> v_classe in p_pointsParClasse)
                {
                    v_couleurCourante = p_tableCouleurs[v_classe.Key];
                    //SpatialTrace.SetFillColor(v_couleurCourante);
                    //SpatialTrace.SetLineColor(v_couleurCourante);
                    foreach (BeanPoint_internal v_point in v_classe.Value)
                    {
                        v_message = "Pt " + v_point.p00_id + " (" + v_point.p10_coord[2] + " m)";
                        v_pointGeom =FLabServices.createUtilitaires().ConstructPoint(v_point.p10_coord[0], v_point.p10_coord[1], v_point.p11_srid).Buffer(p_taillePoint);
                        //SpatialTrace.TraceGeometry(v_pointGeom, v_message, v_message);
                    }
                }
                //SpatialTrace.Disable();
            }
            catch (Exception)
            {

                throw;
            }
        }
        public void GetVisuPointsAlti(List<BeanPoint_internal> p_points, int p_nbreClasses, bool p_croissantSinonDecroissant, enumModeSeuillage p_methodeDeSeuillage, enumProgressionCouleurs p_progressionCouleurs, int p_alpha, int p_taillePointAutoSi0OuMoins)
        {
            try
            {
                int param_ratioTaillePoint = 100;
                int v_taillePoints = p_taillePointAutoSi0OuMoins;
                if (p_taillePointAutoSi0OuMoins <= 0)
                {
                    v_taillePoints = GetTailleAffichageDuPoint(p_points, param_ratioTaillePoint);
                }
                //
                Dictionary<string, List<BeanPoint_internal>> v_classifDesPoints;
                v_classifDesPoints = FVisualisationServices.createSeuillageServices().GetPointsParClasseOrdonnees(p_points, p_nbreClasses, p_methodeDeSeuillage);
                Dictionary<string, Color> v_tableCouleurs;
                v_tableCouleurs = FVisualisationServices.createVisualisationSpatialTraceServices().GetTableCouleursDegradees(v_classifDesPoints.Keys.ToList(), p_progressionCouleurs, p_alpha, p_croissantSinonDecroissant);
                //
                GetVisuPointsAlti(v_classifDesPoints, v_tableCouleurs, v_taillePoints);
            }
            catch (Exception)
            {

                throw;
            }
        }
        //
        public void GetVisuPoints2D(List<BeanPoint_internal> p_points, string p_label, Color p_couleurCourante, int p_taillePointAutoSi0OuMoins)
        {
            try
            {

                int param_ratioTaillePoint = 100;
                int v_taillePoints = p_taillePointAutoSi0OuMoins;
                if (p_taillePointAutoSi0OuMoins <= 0)
                {
                    v_taillePoints = GetTailleAffichageDuPoint(p_points, param_ratioTaillePoint);
                }

                //SpatialTrace.Enable();
                string v_message;
                Geometry v_pointGeom;

                ////SpatialTrace.SetFillColor(param_couleurCourante);
                //SpatialTrace.SetLineColor(p_couleurCourante);
                foreach (BeanPoint_internal v_point in p_points)
                {
                    v_message = "Pt " + v_point.p00_id + " (" + v_point.p10_coord[2] + " m)";
                    if (p_label != "")
                    {
                        v_message = p_label + "/" + v_message;
                    }

                    v_pointGeom = FLabServices.createUtilitaires().ConstructPoint(v_point.p10_coord[0], v_point.p10_coord[1], v_point.p11_srid).Buffer(v_taillePoints);
                    //SpatialTrace.TraceGeometry(v_pointGeom, v_message, v_message);
                }

                //SpatialTrace.Disable();
            }
            catch (Exception)
            {

                throw;
            }
        }

        public void GetVisuPoints2D(List<BeanPoint_internal> p_points, string p_label, int p_taillePointAutoSi0OuMoins)
        {
            try
            {
                Color v_couleur = Color.Red;
                GetVisuPoints2D(p_points, p_label, v_couleur, p_taillePointAutoSi0OuMoins);
            }
            catch (Exception)
            {

                throw;
            }
        }
        public void GetVisuPoint2D(BeanPoint_internal p_point, string p_label, Color p_couleurCourante, int p_taillePoint)
        {

            Color param_couleurContour = Color.FromArgb(125, 125, 125);
            if (p_taillePoint <= 0)
            {
                p_taillePoint = 1;
            }
            //
            //SpatialTrace.Enable();
            string v_message = "";
            Geometry v_pointGeom;

            //SpatialTrace.SetFillColor(p_couleurCourante);
            //SpatialTrace.SetLineColor(param_couleurContour);

            v_message = "Pt " + p_point.p00_id + " (" + p_point.p10_coord[2] + " m)";
            if (p_label != "")
            {
                v_message += " " + p_label;
            }
            v_pointGeom = FLabServices.createUtilitaires().ConstructPoint(p_point.p10_coord[0], p_point.p10_coord[1], p_point.p11_srid).Buffer(p_taillePoint);
            //SpatialTrace.TraceGeometry(v_pointGeom, v_message, v_message);

            //SpatialTrace.Disable();
        }
        public void GetVisuPoint2D(BeanPoint_internal p_point, string p_label, int p_taillePoint)
        {
            Color p_couleur = Color.FromArgb(125, 125, 125);
            GetVisuPoint2D(p_point, p_label, p_couleur, p_taillePoint);
        }
        //
        public void GetVisuArc2D(BeanArc_internal p_arc, string p_label, Color p_couleurCourante)
        {
            //SpatialTrace.Enable();
            string v_message;

            //SpatialTrace.SetFillColor(p_couleurCourante);
            //SpatialTrace.SetLineColor(p_couleurCourante);

            v_message = "arc " + p_arc.p00_idArc + " (" + p_arc.p01_hcodeArc + ")";
            if (p_label != "")
            {
                v_message += " " + p_label;
            }
            bool v_generePointSiConfondus_vf = true;
            Geometry v_lineGeom;
            v_lineGeom = FLabServices.createUtilitaires().GetGeometryArc(p_arc, v_generePointSiConfondus_vf);
            if (v_lineGeom.OgcGeometryType != OgcGeometryType.LineString)
            {
                v_message += " PB GEOM (Arc est: " + v_lineGeom.OgcGeometryType + ")";
            }
            //SpatialTrace.TraceGeometry(v_lineGeom, v_message, v_message);
            //
            //GetVisuPoint2D(p_arc.p11_pointDbt, " =>Dbt>", p_couleurCourante, 10);
            //GetVisuPoint2D(p_arc.p12_pointFin, " =>Fin>", p_couleurCourante, 10);
            //SpatialTrace.Disable();
        }
        //
        public void GetVisuVecteur2D(double[] p_vecteur, double[] p_origine, int p_srid, string p_label, Color p_couleurCourante, double p_coeff = 1)
        {
            try
            {
                string v_message = p_label;
                double[] v_coordPoint2 = new double[2] { p_origine[0], p_origine[1] };
                v_coordPoint2[0] += (p_vecteur[0] * p_coeff);
                v_coordPoint2[1] += (p_vecteur[1] * p_coeff);
                bool ifPt1AndPt2IqualReturnPointElseNull = true;
                Geometry v_lineGeom = FLabServices.createUtilitaires().GetGeometryLine(p_origine, v_coordPoint2, p_srid, ifPt1AndPt2IqualReturnPointElseNull);
                if (v_lineGeom.OgcGeometryType != OgcGeometryType.LineString)
                {
                    v_message += " PB GEOM (Arc est: " + v_lineGeom.OgcGeometryType + ")";
                }
                //
                //SpatialTrace.Enable();
                ////SpatialTrace.SetFillColor(p_couleurCourante);
                //SpatialTrace.SetLineColor(p_couleurCourante);

                //SpatialTrace.TraceGeometry(v_lineGeom, p_label, p_label);
                //SpatialTrace.Disable();
            }
            catch (Exception)
            {

                throw;
            }
        }
        public void GetVisuVecteur2D(double[] p_vecteur, double[] p_origine, int p_srid, string p_label, double p_coeff = 1)
        {
            Color v_couleur = Color.FromArgb(255, 0, 0);
            GetVisuVecteur2D(p_vecteur, p_origine, p_srid, p_label, v_couleur, p_coeff);
        }
        //
        public void GetVisuTopologieFacettes(BeanTopologieFacettes p_topologieFacettes, bool p_visupointsInclus_vf, bool p_afficherMemeSiInvalide_vf)
        {
            Color v_couleur;
            Random v_randomisateur = new Random(2);
            foreach (BeanFacette_internal v_facette in p_topologieFacettes.p13_facettesById.Values)
            {
                v_couleur = Color.FromArgb(v_randomisateur.Next(1, 254), v_randomisateur.Next(1, 254), v_randomisateur.Next(1, 254));
                GetVisuFacette(v_facette,"", v_couleur, p_visupointsInclus_vf, p_afficherMemeSiInvalide_vf);
            }
        }

        public void GetVisuPentesFacettes(BeanTopologieFacettes p_topologieFacettes, int p_nbreClasses)
        {
            bool param_sensCouleursCroissant = true;
            bool param_visupointsInclus_vf = false;
            bool param_afficherMemeSiInvalide_vf = false;
            Dictionary<int, double> v_penteDesfacettes = p_topologieFacettes.p13_facettesById.ToDictionary(c => c.Key, c => c.Value.getPente());
            Dictionary<string, List<int>> v_facettesParClasse;

            //v_facettesParClasse = FVisualisationServices.createSeuillageServices().GetIdParClassesOrdonnees_parIsoQuantite(v_penteDesfacettes, p_nbreClasses);

            //Pentes suspectes?
            double param_seuilMaxi = (Math.PI / 2) - 0.01;

            double v_min = v_penteDesfacettes.Values.Min();
            double v_max = v_penteDesfacettes.Values.Max();
            double v_minRecale = Math.Max(v_min, 0);
            double v_maxRecale = Math.Min(v_max, param_seuilMaxi);
            int param_alpha = 120;
            Dictionary<int, double> v_seuilsBas;
            //On effectue une classification d'abord de la zone 'vraisemblable' (0 à approx 90)
            //v_seuilsBas=FVisualisationServices.createSeuillageServices().GetSeuilBasClasses_memeEspaceInterclasse(p_nbreClasses, v_minRecale, v_maxRecale);
            v_seuilsBas = FVisualisationServices.createSeuillageServices().GetSeuilBasClasses_parIsoQuantite(v_penteDesfacettes, p_nbreClasses);
            //On identifie les éventuels cas à PB
            if (v_min < 0)
            {
                v_seuilsBas.Add(0, v_min);
            }
            if (v_max > param_seuilMaxi)
            {
                v_seuilsBas.Add(p_nbreClasses + 1, v_maxRecale);
            }
            v_seuilsBas = v_seuilsBas.OrderBy(c => c.Key).ToDictionary(c => c.Key, c => c.Value);
            //

            v_facettesParClasse = FVisualisationServices.createSeuillageServices().GetClassesOrdonnees_parSeuilsDeValeur(v_penteDesfacettes, v_seuilsBas); Dictionary<string, Color> v_tableCouleurs;
            v_tableCouleurs = GetTableCouleursDegradees(v_facettesParClasse.Keys.ToList(), enumProgressionCouleurs.greenVersRed, param_alpha, param_sensCouleursCroissant);

            if (v_min < 0)
            {
                v_tableCouleurs[v_tableCouleurs.First().Key] = Color.Yellow;
            }
            if (v_max > param_seuilMaxi)
            {
                v_tableCouleurs[v_tableCouleurs.Last().Key] = Color.Purple;
            }

            Color v_couleur;
            string v_label;
            double v_convertToDegree = 180 / Math.PI;
            foreach (KeyValuePair<string, List<int>> v_classe in v_facettesParClasse)
            {
                v_couleur = v_tableCouleurs[v_classe.Key];
                foreach (int v_idFacette in v_classe.Value)
                {
                    v_label = v_classe.Key + " (" + Math.Round(v_penteDesfacettes[v_idFacette] * v_convertToDegree, 3) + " deg)";
                    GetVisuFacette(p_topologieFacettes.p13_facettesById[v_idFacette], v_label, v_couleur, param_visupointsInclus_vf, param_afficherMemeSiInvalide_vf);
                }
            }
        }

   
        public void GetVisuArcsTopologie(BeanTopologieFacettes p_topologie, Color p_couleur, string p_prefixeLabel)
        {
            if(p_topologie.p12_arcsByCode==null || p_topologie.p12_arcsByCode.Count==0)
            {
                return;
            }
            Geometry v_arcGeom;
            string v_label;
            int v_srid=p_topologie.p11_pointsFacettesByIdPoint.First().Value.p11_srid;
            //SpatialTrace.Enable();
            //SpatialTrace.SetLineColor(p_couleur);
            foreach (KeyValuePair<string, BeanArc_internal> v_arc in p_topologie.p12_arcsByCode)
            {
                v_label = p_prefixeLabel + " " + v_arc.Value.p00_idArc + "\\" + v_arc.Value.p01_hcodeArc;
                v_arcGeom =FLabServices.createUtilitaires().GetGeometryLine(v_arc.Value.p11_pointDbt.p10_coord, v_arc.Value.p12_pointFin.p10_coord, v_srid, false);
                //SpatialTrace.TraceGeometry(v_arcGeom, v_label, v_label);
            }
            //SpatialTrace.Disable();
        }
        public void GetVisuIlots(BeanTopologieFacettes p_topologie, Color p_couleur, string p_prefixeLabel)
        {
            //SpatialTrace.Enable();
            //SpatialTrace.SetFillColor(p_couleur);
            //SpatialTrace.SetLineColor(Color.Blue);
            //SpatialTrace.SetLineWidth(1);

            string v_label;
            foreach(KeyValuePair<int,BeanFacette_internal> v_facette in p_topologie.p13_facettesById)
            {
                if (v_facette.Value.p04_geomFacette!=null)
                {
                    v_label = p_prefixeLabel+ " Fac " + v_facette.Key;
                    //SpatialTrace.TraceGeometry(v_facette.Value.p04_geomFacette, v_label, v_label);
                }
                else
                {
                    if(v_facette.Value.p02_arcs!=null && v_facette.Value.p02_arcs.Count>0)
                    {
                        //SpatialTrace.SetLineColor(Color.Red);
                        //SpatialTrace.SetLineWidth(3);
                        Geometry v_arcGeom;
                        foreach (BeanArc_internal v_arcPolyg in v_facette.Value.p02_arcs)
                        {
                            v_label = "PB FAC "+ v_facette.Key +"_"+ p_prefixeLabel + "=> Arc: " + v_arcPolyg.p00_idArc + "\\" + v_arcPolyg.p01_hcodeArc;
                            int v_srid = v_arcPolyg.p11_pointDbt.p11_srid;
                            v_arcGeom = FLabServices.createUtilitaires().GetGeometryLine(v_arcPolyg.p11_pointDbt.p10_coord, v_arcPolyg.p12_pointFin.p10_coord, v_srid, false);
                            //SpatialTrace.TraceGeometry(v_arcGeom, v_label, v_label);
                        }
                        //SpatialTrace.SetLineWidth(1);
                        //SpatialTrace.SetLineColor(Color.Blue);
                    }
                }
              
            }
            //SpatialTrace.Disable();
        }

        public void GetVisuPoints(List<BeanPoint_internal> p_points, Color p_couleur,int p_taillePoint, string p_prefixeLabel)
        {
            if(p_points==null  && p_points.Count==0)
            {
                return;
            }
            Geometry v_pointGeom;
            string v_label;
            int v_srid = p_points.First().p11_srid;
            //SpatialTrace.Enable();
            //SpatialTrace.SetLineColor(p_couleur);
            foreach (BeanPoint_internal v_point in p_points)
            {
                v_label = p_prefixeLabel + " " + v_point.p00_id + "\\" + v_point.p00_id;
                v_pointGeom = FLabServices.createUtilitaires().ConstructPoint(v_point.p10_coord[0], v_point.p10_coord[1], v_srid);
                //SpatialTrace.TraceGeometry(v_pointGeom, v_label, v_label);
            }
            //SpatialTrace.Disable();
        }

        public void GetVisuFacette(BeanFacette_internal p_facette,string p_label, Color p_couleurCourante, bool p_visualiserPointsInclus_vf, bool p_afficherMemeSiInvalide_vf)
        {
            Color param_couleurContour = Color.FromArgb(125, 125, 125);
            Color param_couleurPoint = Color.FromArgb(200, 200, 200);

            //SpatialTrace.Enable();

            //SpatialTrace.SetFillColor(p_couleurCourante);
            //SpatialTrace.SetLineColor(param_couleurContour);
            //
            List<double[]> v_coord = p_facette.p01_pointsDeFacette.Select(c => c.p10_coord).ToList();
            Geometry v_polygone;
            v_polygone = FLabServices.createUtilitaires().GetGeometryPolygon(v_coord, p_facette.p01_pointsDeFacette.First().p11_srid);
            if (!p_afficherMemeSiInvalide_vf && !v_polygone.IsValid)
            {
                return;
            }
            string v_label = p_label + " fac: " + p_facette.p00_idFacette;
            //SpatialTrace.TraceGeometry(v_polygone, v_label, v_label);
            //

            if (p_visualiserPointsInclus_vf)
            {
                foreach (BeanPoint_internal v_point in p_facette.p10_pointsInclus)
                {
                    GetVisuPoint2D(v_point, "=>Fac " + p_facette.p00_idFacette, param_couleurPoint, 5);
                }
            }

            //SpatialTrace.Disable();
        }

        public void GetVisuCreteEtTalweg(BeanTopologieFacettes p_topologieFacettes, HashSet<enum_qualificationMorpho_arc> p_nePasAfficher = null)
        {
            Color v_couleur;
            //SpatialTrace.Enable();

            // GetVisuTopologieFacettes(p_topologieFacettes,  false, false);

            //On actualise les arcs, pour contrôle
            foreach (string v_cleArc in  p_topologieFacettes.p12_arcsByCode.Keys)
            {
                p_topologieFacettes.p12_arcsByCode[v_cleArc].getQualifMorphoDeLArc();
                //FLabServices.createGeomorphoServices().SetLignesCretesEtTalwegByRefByArc(p_topologieFacettes, v_cleArc);
            }

            //
            if (!p_nePasAfficher.Contains(enum_qualificationMorpho_arc.crete))
            {
                //SpatialTrace.Indent("Cretes");
                List<BeanArc_internal> v_arcsCretes;
                v_arcsCretes = p_topologieFacettes.p12_arcsByCode.Values.Where(c => c.getQualifMorphoDeLArc() == enum_qualificationMorpho_arc.crete).ToList();
                v_couleur = Color.Red;
                foreach (BeanArc_internal v_arc in v_arcsCretes)
                {
                    GetVisuArc2D(v_arc, "Crete", v_couleur);
                }
            }


            if (!p_nePasAfficher.Contains(enum_qualificationMorpho_arc.talweg))
            {
                //SpatialTrace.Unindent();
                //SpatialTrace.Indent("Talwegs");
                List<BeanArc_internal> v_arcsTalweg;
                v_arcsTalweg = p_topologieFacettes.p12_arcsByCode.Values.Where(c => c.getQualifMorphoDeLArc() == enum_qualificationMorpho_arc.talweg).ToList();
                v_couleur = Color.Blue;
                foreach (BeanArc_internal v_arc in v_arcsTalweg)
                {
                    GetVisuArc2D(v_arc, "Talweg", v_couleur);
                }
            }


            if (!p_nePasAfficher.Contains(enum_qualificationMorpho_arc.autre))
            {
                //SpatialTrace.Unindent();
                //SpatialTrace.Indent("Autres");
                List<BeanArc_internal> v_arcsAutres;
                v_arcsAutres = p_topologieFacettes.p12_arcsByCode.Values.Where(c => c.getQualifMorphoDeLArc() == enum_qualificationMorpho_arc.autre).ToList();
                v_couleur = Color.LightGray;
                foreach (BeanArc_internal v_arc in v_arcsAutres)
                {
                    GetVisuArc2D(v_arc, "Autre", v_couleur);
                }
            }
            //SpatialTrace.Unindent();
            //SpatialTrace.Disable();
        }
        //
        public void AfficheVisu()
        {
            //SpatialTrace.Enable();
            ////SpatialTrace.ShowDialog();
            //SpatialTrace.Disable();
        }
        public void ClearSpatialTrace()
        {
            //SpatialTrace.Enable();
            //SpatialTrace.Clear();
            //SpatialTrace.Disable();
        }
    }
}
