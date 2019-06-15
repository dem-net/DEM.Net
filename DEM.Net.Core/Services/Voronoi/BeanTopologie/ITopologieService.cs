using System.Collections.Generic;
using GeoAPI.Geometries;

namespace DEM.Net.Core.FortuneVoronoi
{
    public interface ITopologieService
    {
        string GetHashCodeGeometriePoint(double v_coordxPoint1, double v_coordyPoint1);
        BeanTopologie GetTopologie(Dictionary<int, IGeometry> v_DicoLignes);
        BeanTopologie GetTopologieSansImpassesEnrichiesDesIlots(BeanTopologie v_topologieLignes);
        void MiseAJourDesIndicateursDeControleTopologieIlot(BeanTopologie v_topologieVoronoi);
        void UpdateIdIlotsByCotesArcs(BeanTopologie v_topologieVoronoi, Dictionary<int, int> v_DicoPointSourceDroitParArc, Dictionary<int, int> v_DicoPointSourceGaucheParArc);
        IGeometry GetLineStringByCoord(double v_OrigX_Left, double v_OrigY_Left, double v_OrigX_Right, double v_OrigY_Right);
        IGeometry GetUnPointIGeometryByCoordonneesXy(float v1, float v2);
        BeanTopologie GetTopologieDIlotsFusionnes(BeanTopologie v_topologieVoronoi, Dictionary<int, List<int>> p_dicoDesPointsParSurfaces);
        bool IsTopologieIlotsOk_vf(BeanTopologie v_topologieVoronoi);
    }
}