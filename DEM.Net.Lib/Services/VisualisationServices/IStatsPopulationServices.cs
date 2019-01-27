using System.Collections.Generic;
using DEM.Net.Lib.Services.Lab;

namespace DEM.Net.Lib.Services.VisualisationServices
{
    public interface IStatsPopulationServices
    {
        /// <summary>
        /// Permet de faire des regroupements des points dans des classes en fonction de leur valeur d'élévation.
        /// </summary>
        /// <param name="p_points"></param>
        /// <param name="p_nbreClasses"></param>
        /// <param name="p_modeSeuillage"></param>
        /// <returns></returns>
        Dictionary<string, List<BeanPoint_internal>> GetPointsParClasseOrdonnees(List<BeanPoint_internal> p_points, int p_nbreClasses, enumModeSeuillage p_modeSeuillage);


        //Services génériques
        Dictionary<string, List<int>> GetClassesOrdonnees_parSeuilsDeValeur(Dictionary<int, double> p_valeurParObjet, Dictionary<int, double> p_seuilsBas);
        Dictionary<string, List<int>> GetIdParClassesOrdonnees_parIsoQuantite(Dictionary<int, double> p_valeurParObjet, int p_nbreClasses);
        Dictionary<int, string> GetLabelsClasses(Dictionary<int, double> p_seuilsBas, int p_arrondi = 3);
        Dictionary<int, double> GetSeuilBasClasses_memeEspaceInterclasse(int p_nbreClasses, double p_valeurMin, double p_valeurMax);
        Dictionary<int, double> GetSeuilBasClasses_parIsoQuantite(Dictionary<int, double> p_valeurParObjet, int p_nbreClasses);
    }
}