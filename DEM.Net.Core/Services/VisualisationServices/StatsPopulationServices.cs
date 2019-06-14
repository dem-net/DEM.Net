// StatsPopulationServices.cs
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

namespace DEM.Net.Core.Services.VisualisationServices
{
    public class StatsPopulationServices : IStatsPopulationServices
    {
        //Traitements points
        public Dictionary<string, List<BeanPoint_internal>> GetPointsParClasseOrdonnees(List<BeanPoint_internal> p_points, int p_nbreClasses, enumModeSeuillage p_modeSeuillage)
        {
            Dictionary<string, List<BeanPoint_internal>> v_classes = new Dictionary<string, List<BeanPoint_internal>>();
            try
            {
                if(p_modeSeuillage==enumModeSeuillage.memeNombreDIndividus)
                {
                    v_classes = GetClassesOrdonneesPoints_parIsoQuantite(p_points, p_nbreClasses);
                }
                else
                {
                    Dictionary<int, double> v_seuilsBas;
                    v_seuilsBas = GetSeuilBasClassesForPoints(p_points, p_nbreClasses, p_modeSeuillage);
                    v_classes=GetClassesOrdonneesPoints_parSeuilsDeValeur(p_points, v_seuilsBas);
                }
            }
            catch (Exception)
            {
                throw;
            }
            return v_classes;
        }
        private Dictionary<string, List<BeanPoint_internal>> GetClassesOrdonneesPoints_parIsoQuantite(List<BeanPoint_internal> p_points, int p_nbreClasses)
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
        private Dictionary<string, List<BeanPoint_internal>> GetClassesOrdonneesPoints_parSeuilsDeValeur(List<BeanPoint_internal> p_points, Dictionary<int, double> p_seuilsBas)
        {
            Dictionary<string, List<BeanPoint_internal>> v_classes = new Dictionary<string, List<BeanPoint_internal>>();
            try
            {
                Dictionary<int, double> v_altiParIdPoint = p_points.ToDictionary(c => c.p00_id, c => c.p10_coord[2]);
                Dictionary<string, List<int>> v_classeDId = GetClassesOrdonnees_parSeuilsDeValeur(v_altiParIdPoint, p_seuilsBas);
                Dictionary<int, BeanPoint_internal> v_dicoPoints= p_points.ToDictionary(c => c.p00_id, c => c);
                foreach(KeyValuePair<string, List<int>> v_classe in  v_classeDId)
                {
                    v_classes.Add(v_classe.Key, new List<BeanPoint_internal>());
                    foreach(int v_id in v_classe.Value)
                    {
                        v_classes[v_classe.Key].Add(v_dicoPoints[v_id]);
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
            return v_classes;
        }
        private Dictionary<int, double> GetSeuilBasClassesForPoints(List<BeanPoint_internal> p_points, int p_nbreClasses, enumModeSeuillage p_modeSeuillage)
        {
            Dictionary<int, double> v_seuils = new Dictionary<int, double>();
            try
            {
                switch (p_modeSeuillage)
                {
                    case enumModeSeuillage.memeEspaceInterclasse:
                        double v_min = p_points.Min(c => c.p10_coord[2]);
                        double v_max = p_points.Max(c => c.p10_coord[2]);
                        v_seuils = GetSeuilBasClasses_memeEspaceInterclasse(p_nbreClasses, v_min, v_max);
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

        //Traitements génériques
        public Dictionary<string, List<int>> GetIdParClassesOrdonnees_parIsoQuantite(Dictionary<int, double> p_valeurParObjet, int p_nbreClasses)
        {
            Dictionary<string, List<int>> v_classes = new Dictionary<string, List<int>>();
            try
            {
                int param_NbreDedicimalesArrondiesPourLabel = 2;
                int v_nbreDInvidusParClasse = Convert.ToInt32(Math.Round((double)p_valeurParObjet.Count / p_nbreClasses));
                List<int> v_clesTriees;
                v_clesTriees = p_valeurParObjet.OrderBy(c => c.Value).Select(c => c.Key).ToList();
             
                int v_indexBas = 0;
                string v_label = "";
                int v_indexHaut = v_nbreDInvidusParClasse;
                int v_indexClasse = 0;
                for (v_indexClasse = 1; v_indexClasse < p_nbreClasses; v_indexClasse++)
                {
                    v_label = "De_" + Math.Round(p_valeurParObjet[v_clesTriees[v_indexBas]], param_NbreDedicimalesArrondiesPourLabel) + "_a_" + Math.Round(p_valeurParObjet[v_clesTriees[v_indexHaut]], param_NbreDedicimalesArrondiesPourLabel);
                    v_classes.Add(v_label, v_clesTriees.GetRange(v_indexBas, v_nbreDInvidusParClasse));
                    v_indexBas = v_indexHaut;
                    v_indexHaut += v_nbreDInvidusParClasse;

                }
                v_label = "De_" + Math.Round(p_valeurParObjet[v_clesTriees[v_indexBas]], param_NbreDedicimalesArrondiesPourLabel) + "_a_" + Math.Round(p_valeurParObjet[v_clesTriees.Last()], param_NbreDedicimalesArrondiesPourLabel);
                v_classes.Add(v_label, v_clesTriees.Skip(v_indexBas).ToList());
            }
            catch (Exception)
            {

                throw;
            }
            return v_classes;
        }
        public Dictionary<string, List<int>> GetClassesOrdonnees_parSeuilsDeValeur(Dictionary<int,double> p_valeurParObjet, Dictionary<int, double> p_seuilsBas)
        {
            Dictionary<string, List<int>> v_classes = new Dictionary<string, List<int>>();
            try
            {
                Dictionary<int, string> v_labels = GetLabelsClasses(p_seuilsBas);
                Dictionary<int, double> p_objetsTries;
                p_objetsTries = p_valeurParObjet.OrderBy(c => c.Value).ToDictionary(c=>c.Key,c=>c.Value);
                //   
                int v_classeCourante = v_labels.First().Key;
                string v_label = v_labels[v_classeCourante];
                v_classes.Add(v_label, new List<int>());
                //
                double v_seuilBasSuivant = p_seuilsBas[v_classeCourante + 1];
                bool v_derniereClasse_vf = false;
                foreach (KeyValuePair<int, double> v_obj in p_objetsTries)
                {
                    if (!v_derniereClasse_vf && v_obj.Value >= v_seuilBasSuivant)
                    {
                        v_classeCourante++;
                        v_label = v_labels[v_classeCourante];
                        v_classes.Add(v_label, new List<int>());
                        if(p_seuilsBas.ContainsKey(v_classeCourante + 1))
                        {
                            v_seuilBasSuivant = p_seuilsBas[v_classeCourante + 1];
                        }
                        else
                        {
                            v_derniereClasse_vf = true;
                        }
                    }
                    v_classes[v_label].Add(v_obj.Key);
                }
            }
            catch (Exception)
            {

                throw;
            }
            return v_classes;
        }
        //
        public Dictionary<int, double> GetSeuilBasClasses_memeEspaceInterclasse(int p_nbreClasses, double p_valeurMin, double p_valeurMax)
        {
            Dictionary<int, double> v_seuilsBas = new Dictionary<int, double>();
            try
            {
                double v_ecartTotal = p_valeurMax - p_valeurMin;
                double v_espaceInterClasse = v_ecartTotal / p_nbreClasses;
                //
                double v_seuil = p_valeurMin;
                for (int v_indexClasse = 1; v_indexClasse <= p_nbreClasses; v_indexClasse++)
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
        public Dictionary<int, double> GetSeuilBasClasses_parIsoQuantite(Dictionary<int, double> p_valeurParObjet, int p_nbreClasses)
        {
            Dictionary<int, double> v_seuilsBas = new Dictionary<int, double>();
            try
            {
                int v_nbreDInvidusParClasse = Convert.ToInt32(Math.Round((double)p_valeurParObjet.Count / p_nbreClasses));
                List<int> v_clesTriees;
                v_clesTriees = p_valeurParObjet.OrderBy(c => c.Value).Select(c => c.Key).ToList();

                int v_indexBas = 0;
                int v_indexHaut = v_nbreDInvidusParClasse;
                int v_indexClasse = 0;
                for (v_indexClasse = 1; v_indexClasse < p_nbreClasses+1; v_indexClasse++)
                {
                    v_seuilsBas.Add(v_indexClasse, p_valeurParObjet[v_clesTriees[v_indexBas]]);
                    v_indexBas = v_indexHaut;
                    v_indexHaut += v_nbreDInvidusParClasse;
                }   
            }
            catch (Exception)
            {
                throw;
            }
            return v_seuilsBas;
        }


        public Dictionary<int, string> GetLabelsClasses(Dictionary<int, double> p_seuilsBas, int p_arrondi=3)
        {
            Dictionary<int, string> v_classes = new Dictionary<int, string>();
            try
            {
                string v_label;
                for (int v_index = p_seuilsBas.First().Key; v_index < p_seuilsBas.Max(c => c.Key); v_index++)
                {
                    v_label = Math.Round(p_seuilsBas[v_index], p_arrondi) + "_" + Math.Round(p_seuilsBas[v_index + 1], p_arrondi);
                    v_classes.Add(v_index, v_label);
                }
                v_label = "Plus_de_" + Math.Round(p_seuilsBas.Last().Value, p_arrondi);
                v_classes.Add(p_seuilsBas.Max(c => c.Key), v_label);
            }
            catch (Exception)
            {

                throw;
            }
            return v_classes;
        }
    }
   
}
