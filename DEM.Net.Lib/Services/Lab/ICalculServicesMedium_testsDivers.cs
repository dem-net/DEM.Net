using System.Collections.Generic;

namespace DEM.Net.Lib.Services.Lab
{
    public interface ICalculServicesMedium_testDivers
    {
        double[] GetCentroide(IEnumerable<BeanPoint_internal> p_points);
        double[] GetCoordonneesDansNewReferentiel2D(BeanPoint_internal p_pointAReferencer, double[] p_coordPointOrigine, double[] p_coordPoint2Abs, double[] p_coordPoint3Ord_orthoSiNull = null);
        Dictionary<int, double[]> GetCoordonneesDansNewReferentiel2D(IEnumerable<BeanPoint_internal> p_pointsAReferencer, double[] p_coordPointOrigine, double[] p_coordPoint2Abs, double[] p_coordPoint3Ord_orthoSiNull = null);
        Dictionary<string, int> GetEtComptePointsDoublonnes(List<BeanPoint_internal> p_pointsToTest);
        BeanPoint_internal GetIdPointLePlusEloigneDuPointRef(IEnumerable<BeanPoint_internal> p_points, double[] p_pointRef);
        double GetLongueurArcAuCarre(BeanPoint_internal p_point1, BeanPoint_internal p_point2);
    }
}