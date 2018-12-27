using System.Collections.Generic;
using System.Windows.Media;
using DEM.Net.Lib.Services.Lab;

namespace DEM.Net.Lib.Services.VisualisationServices
{
    public interface IVisualisationSpatialTraceServices
    {
        Dictionary<string, Color> GetTableCouleursDegradees(List<string> p_classesTriees, enumProgressionCouleurs p_progressionCouleur, int p_alpha, bool p_croissantSinonDecroissant);
        Dictionary<int, Color> GetTableCouleursDegradees(int p_nbreClasses, enumProgressionCouleurs p_progressionCouleur, int p_alpha = 120, bool p_croissantSinonDecroissant = true);
        //
        int GetTailleAffichageDuPoint(List<BeanPoint_internal> p_points, double p_ratioTaillePoint);
        int GetTailleAffichageDuPoint(double p_largeurEspaceReference, double p_ratioTaillePoint);
        //
        void GetVisuPointsAlti(Dictionary<string, List<BeanPoint_internal>> p_pointsParClasse,  Dictionary<string, Color> p_tableCouleurs, int p_taillePoint);
        void GetVisuPointsAlti(List<BeanPoint_internal> p_points,  int p_nbreClasses = 10, bool p_croissantSinonDecroissant = true, enumModeSeuillage p_methodeDeSeuillage = enumModeSeuillage.memeNombreDIndividus, enumProgressionCouleurs p_progressionCouleurs = enumProgressionCouleurs.greenVersRed, int p_alpha = 120, int p_taillePointAutoSi0OuMoins = -1);
        //
        void GetVisuPoints2D(List<BeanPoint_internal> p_points, string p_label, int p_taillePointAutoSi0OuMoins = -1);
        void GetVisuPoints2D(List<BeanPoint_internal> p_points, string p_label, Color p_couleurCourante, int p_taillePointAutoSi0OuMoins);
        void GetVisuPoint2D(BeanPoint_internal v_point,  string p_label, Color p_couleurCourante, int p_taillePoint = 10);
        void GetVisuPoint2D(BeanPoint_internal p_point, string p_label, int p_taillePoint);
        //
        void GetVisuArc2D(BeanArc_internal p_arc, string p_label, Color p_couleurCourante);
        //
        void GetVisuVecteur2D(double[] p_vecteur, double[] p_origine, int p_srid, string p_label, Color p_couleurCourante, double p_coeff = 1);
        void GetVisuVecteur2D(double[] p_vecteur, double[] p_origine, int p_srid, string p_label, double p_coeff = 1);
        //
        void GetVisuTopologieFacettes(BeanTopologieFacettes p_topologieFacettes, bool p_visualiserPointsInclus_vf, bool p_afficherMemeSiInvalide_vf);
        void GetVisuFacette(BeanFacette_internal p_facette, Color p_couleurCourante, bool p_visualiserPointsInclus_vf, bool p_afficherMemeSiInvalide_vf);
      
        //
        void AfficheVisu();
        void ClearSpatialTrace();
    }
}