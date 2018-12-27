using DEM.Net.Lib.Services.Lab;
using SqlServerSpatial.Toolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Types;
using System.Windows.Media;
using System.Data.SqlTypes;

namespace DEM.Net.Lib.Services.VisualisationServices
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
                //
                for (int v_indexClasse = 1; v_indexClasse <= p_nbreClasses; v_indexClasse++)
                {
                    switch (p_progressionCouleur)
                    {
                        case enumProgressionCouleurs.red:
                            v_couleur = Color.FromRgb((byte) v_niveauCouleurRef, (byte)0, (byte)0);
                            break;
                        case enumProgressionCouleurs.green:
                            v_couleur = Color.FromRgb((byte)0, (byte)v_niveauCouleurRef, (byte)0);
                            break;
                        case enumProgressionCouleurs.blue:
                            v_couleur = Color.FromRgb((byte)0, (byte)0, (byte)v_niveauCouleurRef);
                            break;
                        case enumProgressionCouleurs.greenVersRed:
                            v_couleur = Color.FromRgb((byte)v_niveauCouleurRef, (byte)v_niveauCouleurInverseRef, (byte)0);
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

                SpatialTrace.Enable();
                Color v_couleurCourante;
                string v_message;
                SqlGeometry v_pointGeom;
                foreach (KeyValuePair<string, List<BeanPoint_internal>> v_classe in p_pointsParClasse)
                {
                    v_couleurCourante = p_tableCouleurs[v_classe.Key];
                    SpatialTrace.SetFillColor(v_couleurCourante);
                    SpatialTrace.SetLineColor(v_couleurCourante);
                    foreach (BeanPoint_internal v_point in v_classe.Value)
                    {
                        v_message = "Pt " + v_point.p00_id + " (" + v_point.p10_coord[2] + " m)";
                        v_pointGeom = SqlGeometry.Point(v_point.p10_coord[0], v_point.p10_coord[1], v_point.p11_srid).STBuffer(p_taillePoint);
                        SpatialTrace.TraceGeometry(v_pointGeom, v_message, v_message);
                    }
                }
                SpatialTrace.Disable();
            }
            catch (Exception)
            {

                throw;
            }
        }
        public void GetVisuPointsAlti(List<BeanPoint_internal> p_points,  int p_nbreClasses, bool p_croissantSinonDecroissant, enumModeSeuillage p_methodeDeSeuillage, enumProgressionCouleurs p_progressionCouleurs, int p_alpha, int p_taillePointAutoSi0OuMoins)
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
        public void GetVisuPoints2D(List<BeanPoint_internal> p_points,string p_label, Color p_couleurCourante, int p_taillePointAutoSi0OuMoins)
        {
            try
            {
             
                int param_ratioTaillePoint = 100;
                int v_taillePoints = p_taillePointAutoSi0OuMoins;
                if (p_taillePointAutoSi0OuMoins <= 0)
                {
                    v_taillePoints = GetTailleAffichageDuPoint(p_points, param_ratioTaillePoint);
                }

                SpatialTrace.Enable();
                string v_message;
                SqlGeometry v_pointGeom;

                //SpatialTrace.SetFillColor(param_couleurCourante);
                SpatialTrace.SetLineColor(p_couleurCourante);
                foreach (BeanPoint_internal v_point in p_points)
                {
                    v_message = "Pt " + v_point.p00_id + " (" + v_point.p10_coord[2] + " m)";
                    if (p_label!="")
                    {
                        v_message = p_label + "/" + v_message;
                    }
                  
                    v_pointGeom = SqlGeometry.Point(v_point.p10_coord[0], v_point.p10_coord[1], v_point.p11_srid).STBuffer(v_taillePoints);
                    SpatialTrace.TraceGeometry(v_pointGeom, v_message, v_message);
                }

                SpatialTrace.Disable();
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
                Color v_couleur = Color.FromRgb((byte)255, (byte)0, (byte)0);
                GetVisuPoints2D(p_points, p_label, v_couleur, p_taillePointAutoSi0OuMoins);
            }
            catch (Exception)
            {

                throw;
            }
        }
        public void GetVisuPoint2D(BeanPoint_internal p_point, string p_label, Color p_couleurCourante, int p_taillePoint)
        {

            Color param_couleurContour = Color.FromRgb((byte)125, (byte)125, (byte)125);
            if (p_taillePoint<=0)
            {
                p_taillePoint = 1;
            }
            //
            SpatialTrace.Enable();
            string v_message="";
            SqlGeometry v_pointGeom;

            SpatialTrace.SetFillColor(p_couleurCourante);
            SpatialTrace.SetLineColor(param_couleurContour);

            v_message = "Pt " + p_point.p00_id + " (" + p_point.p10_coord[2] + " m)";
            if (p_label != "")
            {
                v_message += " " + p_label;
            }
            v_pointGeom = SqlGeometry.Point(p_point.p10_coord[0], p_point.p10_coord[1], p_point.p11_srid).STBuffer(p_taillePoint);
            SpatialTrace.TraceGeometry(v_pointGeom, v_message, v_message);

            SpatialTrace.Disable();
        }
        public void GetVisuPoint2D(BeanPoint_internal p_point, string p_label,  int p_taillePoint)
        {
            Color p_couleur = Color.FromRgb((byte)125, (byte)125, (byte)125);
            GetVisuPoint2D(p_point, p_label, p_couleur, p_taillePoint);
        }
        //
        public void GetVisuArc2D(BeanArc_internal p_arc, string p_label, Color p_couleurCourante)
        {
            SpatialTrace.Enable();
            string v_message;

            SpatialTrace.SetFillColor(p_couleurCourante);
            SpatialTrace.SetLineColor(p_couleurCourante);

            v_message = "arc " + p_arc.p00_idArc+" ("+p_arc.p01_hcodeArc+")";
            if (p_label != "")
            {
                v_message += " " + p_label;
            }
            bool v_generePointSiConfondus_vf = true;
            SqlGeometry v_lineGeom;
            v_lineGeom = FLabServices.createUtilitaires().GetGeometryArc(p_arc, v_generePointSiConfondus_vf);
            if(v_lineGeom.STGeometryType().ToString()!=OpenGisGeometryType.LineString.ToString())
            {
                v_message += " PB GEOM (Arc est: " + v_lineGeom.STGeometryType().ToString() + ")";
            }
            SpatialTrace.TraceGeometry(v_lineGeom, v_message, v_message);
            //
            GetVisuPoint2D(p_arc.p11_pointDbt, " =>Dbt>", p_couleurCourante, 10);
            GetVisuPoint2D(p_arc.p12_pointFin, " =>Fin>", p_couleurCourante, 10);
            SpatialTrace.Disable();
        }
        //
        public void GetVisuVecteur2D(double[] p_vecteur, double[] p_origine,int p_srid, string p_label, Color p_couleurCourante, double p_coeff = 1)
        {
            try
            {
                string v_message = p_label;
                double[] v_coordPoint2 = new double[2] { p_origine[0], p_origine[1] };
                v_coordPoint2[0] += (p_vecteur[0] * p_coeff);
                v_coordPoint2[1] += (p_vecteur[1] * p_coeff);
                bool ifPt1AndPt2IqualReturnPointElseNull = true;
                SqlGeometry v_lineGeom = FLabServices.createUtilitaires().GetGeometryLine(p_origine, v_coordPoint2, p_srid, ifPt1AndPt2IqualReturnPointElseNull);
                if (v_lineGeom.STGeometryType().ToString() != OpenGisGeometryType.LineString.ToString())
                {
                    v_message += " PB GEOM (Arc est: " + v_lineGeom.STGeometryType().ToString() + ")";
                }
                //
                SpatialTrace.Enable();
                //SpatialTrace.SetFillColor(p_couleurCourante);
                SpatialTrace.SetLineColor(p_couleurCourante);

                SpatialTrace.TraceGeometry(v_lineGeom, p_label, p_label);
                SpatialTrace.Disable();
            }
            catch (Exception)
            {

                throw;
            }
        }
        public void GetVisuVecteur2D(double[] p_vecteur, double[] p_origine, int p_srid, string p_label, double p_coeff = 1)
        {
            Color v_couleur = Color.FromRgb((byte)255, (byte)0, (byte)0);
            GetVisuVecteur2D(p_vecteur, p_origine, p_srid, p_label, v_couleur, p_coeff);
        }
        //
        public void GetVisuTopologieFacettes(BeanTopologieFacettes p_topologieFacettes, bool p_visupointsInclus_vf)
        {
            Color v_couleur;
            Random v_randomisateur = new Random(2);
            foreach(BeanFacette_internal v_facette in p_topologieFacettes.p13_facettesById.Values)
            {
                v_couleur = Color.FromRgb((byte) v_randomisateur.Next(1, 254), (byte)v_randomisateur.Next(1, 254), (byte)v_randomisateur.Next(1, 254));
                GetVisuFacette(v_facette,v_couleur, p_visupointsInclus_vf);
            }
        }
        public void GetVisuFacette(BeanFacette_internal p_facette, Color p_couleurCourante, bool p_visualiserPointsInclus_vf)
        {
            Color param_couleurContour = Color.FromRgb((byte)125, (byte)125, (byte)125);
            Color param_couleurPoint = Color.FromRgb((byte)200, (byte)200, (byte)200);

            SpatialTrace.Enable();

            SpatialTrace.SetFillColor(p_couleurCourante);
            SpatialTrace.SetLineColor(param_couleurContour);
            //
            List<double[]> v_coord = p_facette.p01_pointsDeFacette.Select(c => c.p10_coord).ToList();
            SqlGeometry v_polygone;
            v_polygone = FLabServices.createUtilitaires().GetGeometryPolygon(v_coord, p_facette.p01_pointsDeFacette.First().p11_srid);
            SpatialTrace.TraceGeometry(v_polygone, "fac: " + p_facette.p00_idFacette);
            //
            
            if (p_visualiserPointsInclus_vf)
            {
                foreach (BeanPoint_internal v_point in p_facette.p10_pointsInclus)
                {
                    GetVisuPoint2D(v_point, "=>Fac " + p_facette.p00_idFacette, param_couleurPoint, 5);
                }
            }

            SpatialTrace.Disable();
        }

        //
        public void AfficheVisu()
        {
            SpatialTrace.Enable();
            SpatialTrace.ShowDialog();
            SpatialTrace.Disable();
        }
        public void ClearSpatialTrace()
        {
            SpatialTrace.Enable();
            SpatialTrace.Clear();
            SpatialTrace.Disable();
        }
    }
}
