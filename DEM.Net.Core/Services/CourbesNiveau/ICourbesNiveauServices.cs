using DEM.Net.Core.Services.Lab;
using System.Collections.Generic;

namespace Spir.Commun.Service.Technical.Cartographie.Service.CourbesNiveau
{
	public interface ICourbesNiveauServices
	{
        /// <summary>
        /// Calcul des courbes de niveau.
        /// Une seule méthode développée à ce stade (interpol linéaire sur triangulation) mais permet de traiter les champs non réguliers.
        /// Les 'points alti' contiennent des objets très simples avec un Id, une géométrie de point, une valeur numérique décrivant une "élévation" 
        /// (de quelque nature quelle soit: altitude, population, coût,...)
        /// A noter:
        /// - la méthode prévoit une déduplication préalable des points (méthode utilisée par défaut: arrondi des valeurs en x et en y).
        /// - l'écart entre courbes doit être constant.
        /// - cette méthode permet de générer des courbes pas des polygones.
        /// - Les calculs sont prévus aujourd'hui pour des données projetées (pas sur que cela fonctionne en Lat/long)
        /// </summary>
        /// <param name="p_pointsAlti"></param>
        /// <param name="p_parametresCalculDesCourbes"></param>
        /// <returns></returns>
        BeanCourbes GetCourbesNiveau_interpolationLineaireSurTriangulation(BeanTopologieFacettes p_triangulation, BeanParametresCalculCourbesNiveau p_parametresCalculDesCourbes);

        /// <summary>
        /// Permet de paramétrer le calcul de courbes de niveau.
        /// (Notamment l'écart entre courbes, le niveau '0' (l' "altitude" par laquelle doit passer la courbe de référence à partir de laquelle on calcule les autres courbes)
        /// et la distance en-deça de laquelle 2 points sont considérés comme confondus)
        /// et renvoie des paramètres par défaut
        /// Ces paramètres peuvent être modifiés par les setteurs sauf le pas de duplication qui doit être mis à jour par la méthode Ad hoc. 
        /// </summary>
        /// <param name="p_ecartEntreCourbes"></param>
        /// <param name="p_pasDeDeduplicationEnM"></param>
        /// <returns></returns>
        BeanParametresCalculCourbesNiveau GetParametresCalculCourbesNiveauParDefaut(double p_valeurCourbe0, double p_ecartEntreCourbes, double p_pasDeDeduplicationEnM = 10);
		void GetParametresCalculCourbesNiveau_majPasDeDuplicationByRef(ref BeanParametresCalculCourbesNiveau p_parametresAModifier, double p_pasDeDeduplicationEnM);
		//
	}
}