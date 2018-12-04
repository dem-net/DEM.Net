using DEM.Net.Lib.Services.Lab;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.TestWinForm
{
    public class StatsPopulationServices : IStatsPopulationServices
    {
        public Dictionary<string, List<BeanPoint_internal>> GetPointsParClasseOrdonnees(List<BeanPoint_internal> p_points, int p_nbreClasses, enumModeSeuillage p_modeSeuillage)
        {
            Dictionary<string, List<BeanPoint_internal>> v_classes = new Dictionary<string, List<BeanPoint_internal>>();
            try
            {
                if(p_modeSeuillage==enumModeSeuillage.memeNombreDIndividus)
                {
                    v_classes = GetClassesOrdonnees_parIsoQuantite(p_points, p_nbreClasses);
                }
                else
                {
                    Dictionary<int, double> v_seuilsBas;
                    v_seuilsBas = GetSeuilBasClasses(p_points, p_nbreClasses, p_modeSeuillage);
                    v_classes=GetClassesOrdonnees_parSeuilsDeValeur(p_points, v_seuilsBas);
                }
            }
            catch (Exception)
            {
                throw;
            }
            return v_classes;
        }
        //
        private Dictionary<string, List<BeanPoint_internal>> GetClassesOrdonnees_parIsoQuantite(List<BeanPoint_internal> p_points, int p_nbreClasses)
        {
            Dictionary<string, List<BeanPoint_internal>> v_classes = new Dictionary<string, List<BeanPoint_internal>>();
            try
            {
                int param_NbreDedicimalesArrondiesPourLabel = 2;
                int v_nbreDInvidusParClasse = Convert.ToInt32(Math.Round((double)p_points.Count / p_nbreClasses));
                List<BeanPoint_internal> p_pointsTries = p_points.OrderBy(c => c.p10_coord[2]).ToList();

                int v_indexBas = 0;
                string v_label="";
                int v_indexHaut = v_nbreDInvidusParClasse;
                int v_indexClasse = 0;
                for (v_indexClasse = 1; v_indexClasse < p_nbreClasses; v_indexClasse++)
                {
                    v_label = "De_" + Math.Round(p_pointsTries[v_indexBas].p10_coord[2], param_NbreDedicimalesArrondiesPourLabel) + "_a_" + Math.Round(p_pointsTries[v_indexHaut].p10_coord[2], param_NbreDedicimalesArrondiesPourLabel);
                    v_classes.Add(v_label, p_pointsTries.GetRange(v_indexBas, v_nbreDInvidusParClasse));
                    v_indexBas = v_indexHaut;
                    v_indexHaut += v_nbreDInvidusParClasse;
                   
                }
                v_label = "De_" + Math.Round(p_pointsTries[v_indexBas].p10_coord[2], param_NbreDedicimalesArrondiesPourLabel) + "_a_" + Math.Round(p_pointsTries.Last().p10_coord[2], param_NbreDedicimalesArrondiesPourLabel);
                v_classes.Add(v_label, p_pointsTries.Skip(v_indexBas).ToList());
            }
            catch (Exception)
            {

                throw;
            }
            return v_classes;
        }
        //
        private Dictionary<string, List<BeanPoint_internal>> GetClassesOrdonnees_parSeuilsDeValeur(List<BeanPoint_internal> p_points, Dictionary<int, double> p_seuilsBas)
        {
            Dictionary<string, List<BeanPoint_internal>> v_classes = new Dictionary<string, List<BeanPoint_internal>>();
            try
            {
                Dictionary<int, string> v_labels = GetLabelsClasses(p_seuilsBas);
                List<BeanPoint_internal> p_pointsTries;
                p_pointsTries=p_points.OrderBy(c => c.p10_coord[0]).ToList();
                //   
                int v_classeCourante = 1;
                string v_label = v_labels[v_classeCourante];
                v_classes.Add(v_label, new List<BeanPoint_internal>());
                //
               double v_seuilBas = p_seuilsBas[v_classeCourante+1];
               
                foreach (BeanPoint_internal v_point in p_pointsTries)
                {
                    if(v_point.p10_coord[2]>= v_seuilBas)
                    {
                        v_classeCourante++;
                        v_label = v_labels[v_classeCourante];
                        v_classes.Add(v_label, new List<BeanPoint_internal>());
                        v_seuilBas = p_seuilsBas[v_classeCourante + 1];
                    }
                    v_classes[v_label].Add(v_point);
                }
            }
            catch (Exception)
            {

                throw;
            }
            return v_classes;
        }
        private Dictionary<int, double> GetSeuilBasClasses(List<BeanPoint_internal> p_points, int p_nbreClasses, enumModeSeuillage p_modeSeuillage)
        {
            Dictionary<int, double> v_seuils = new Dictionary<int, double>();
            try
            {
                switch (p_modeSeuillage)
                {
                    case enumModeSeuillage.memeEspaceInterclasse:
                        v_seuils=GetSeuilBasClasses_memeEspaceInterclasse(p_points, p_nbreClasses);
                        break;
                    default:
                        throw new Exception("Méthode " + p_modeSeuillage + " non implémentée.");
                }
            }
            catch (Exception)
            {

                throw;
            }
            return v_seuils;
        }
        private Dictionary<int, double> GetSeuilBasClasses_memeEspaceInterclasse(List<BeanPoint_internal> p_points, int p_nbreClasses)
        {
            Dictionary<int, double> v_seuilsBas = new Dictionary<int, double>();
            try
            {
                double v_min = p_points.Min(c => c.p10_coord[2]);
                double v_max=p_points.Max(c => c.p10_coord[2]);
                //
                double v_ecartTotal = v_max - v_min;
                double v_espaceInterClasse = v_ecartTotal / p_nbreClasses;
                //
                double v_seuil = v_min;
                for (int v_indexClasse=1; v_indexClasse<= p_nbreClasses; v_indexClasse++)
                {
                    v_seuilsBas.Add(v_indexClasse, v_seuil);
                    v_seuil += v_espaceInterClasse;
                }
            }
            catch (Exception)
            {

                throw;
            }
            return v_seuilsBas;
        }
        private Dictionary<int, string> GetLabelsClasses(Dictionary<int, double> p_seuilsBas)
        {
            Dictionary<int, string> v_classes = new Dictionary<int, string>();
            try
            {
                string v_label;
                for (int v_index = 1; v_index < p_seuilsBas.Max(c => c.Key); v_index++)
                {
                    v_label = p_seuilsBas[v_index] + "_" + p_seuilsBas[v_index + 1];
                    v_classes.Add(v_index, v_label);
                }
                v_label = "Plus_de_" + p_seuilsBas.Last().Value;
            }
            catch (Exception)
            {

                throw;
            }
            return v_classes;
        }
    }
   
}
