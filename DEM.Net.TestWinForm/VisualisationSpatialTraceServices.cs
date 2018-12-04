using DEM.Net.Lib.Services.Lab;
using SqlServerSpatial.Toolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Drawing;
using System.Windows.Media;
using System.Threading.Tasks;
using Microsoft.SqlServer.Types;

namespace DEM.Net.TestWinForm
{
    public class VisualisationSpatialTraceServices : IVisualisationSpatialTraceServices
    {
        //
        public Dictionary<string, Color> GetTableCouleursDegradees(List<string> p_classesTriees, enumProgressionCouleurs p_progressionCouleur, int p_alpha, bool p_croissantSinonDecroissant)
        {
            Dictionary<string, Color> v_tableCouleurs = new Dictionary<string, Color>();
            try
            {
                Dictionary<int, Color> v_tab= GetTableCouleursDegradees(p_classesTriees.Count, p_progressionCouleur, p_alpha, p_croissantSinonDecroissant);
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

        public Dictionary<int, Color> GetTableCouleursDegradees(int p_nbreClasses, enumProgressionCouleurs p_progressionCouleur,int p_alpha = 120, bool p_croissantSinonDecroissant = true)
        {
            Dictionary<int, Color> v_tableCouleurs = new Dictionary<int, Color>();
            try
            {
                Color v_couleur;
                int v_pasDIncremetation = Convert.ToInt16(Math.Round((double) 255 / p_nbreClasses));
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
                for (int v_indexClasse=1; v_indexClasse<= p_nbreClasses; v_indexClasse++)
                {
                  switch(p_progressionCouleur)
                    {
                        case enumProgressionCouleurs.red:
                            v_couleur = Color.FromScRgb(p_alpha, v_niveauCouleurRef, 0, 0);
                            break;
                        case enumProgressionCouleurs.green:
                            v_couleur = Color.FromScRgb(p_alpha, 0, v_niveauCouleurRef, 0);
                            break;
                        case enumProgressionCouleurs.blue:
                            v_couleur = Color.FromScRgb(p_alpha, 0, 0, v_niveauCouleurRef);
                            break;
                        case enumProgressionCouleurs.greenVersRed:
                            v_couleur = Color.FromScRgb(p_alpha, v_niveauCouleurRef, v_niveauCouleurInverseRef, 0);
                            break;
                        default:
                            throw new Exception("Méthode " + p_progressionCouleur + " non implémentée.");
                    }
                    v_tableCouleurs.Add(v_indexClasse, v_couleur);
                    if (p_croissantSinonDecroissant)
                    {
                        v_niveauCouleurRef += v_pasDIncremetation;
                        v_niveauCouleurRef -= v_pasDIncremetation;
                    }
                    else
                    {
                        v_niveauCouleurRef -= v_pasDIncremetation;
                        v_niveauCouleurRef += v_pasDIncremetation;
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
            return v_tableCouleurs;
        }
     
        public void VisuPoints(Dictionary<string, List<BeanPoint_internal>> p_pointsParClasse, Dictionary<string, Color> p_tableCouleurs)
        {
            try
            {
                int param_taillePoint = 2;
                SpatialTrace.Enable();
                Color v_couleurCourante;
                string v_message;
                SqlGeometry v_pointGeom;
               foreach(KeyValuePair<string, List<BeanPoint_internal>> v_classe in p_pointsParClasse)
                {
                    v_couleurCourante = p_tableCouleurs[v_classe.Key];
                    SpatialTrace.SetFillColor(v_couleurCourante);
                    foreach(BeanPoint_internal v_point in v_classe.Value)
                    {
                        v_message = "Pt " + v_point.p00_id + " (" + v_point.p10_coord[2] + " m)";
                        v_pointGeom = SqlGeometry.Point(v_point.p10_coord[0], v_point.p10_coord[1], v_point.p11_srid).STBuffer(param_taillePoint);
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
    }
}
