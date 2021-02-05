using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace DEM.Net.Core.Voronoi
{
    public interface ITopologieService
    {
        string GetHashCodeGeometriePoint(double v_coordxPoint1, double v_coordyPoint1);
        BeanTopologie GetTopologie(Dictionary<int, Geometry> v_DicoLignes);
        BeanTopologie GetTopologieSansImpassesEnrichiesDesIlots(BeanTopologie v_topologieLignes);
        void MiseAJourDesIndicateursDeControleTopologieIlot(BeanTopologie v_topologieVoronoi);
        void UpdateIdIlotsByCotesArcs(BeanTopologie v_topologieVoronoi, Dictionary<int, int> v_DicoPointSourceDroitParArc, Dictionary<int, int> v_DicoPointSourceGaucheParArc);
        Geometry GetLineStringByCoord(double v_OrigX_Left, double v_OrigY_Left, double v_OrigX_Right, double v_OrigY_Right);
        Geometry GetUnPointIGeometryByCoordonneesXy(float v1, float v2);
        BeanTopologie GetTopologieDIlotsFusionnes(BeanTopologie v_topologieVoronoi, Dictionary<int, List<int>> p_dicoDesPointsParSurfaces);
        bool IsTopologieIlotsOk_vf(BeanTopologie v_topologieVoronoi);
    }
}