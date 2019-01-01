using System.Collections.Generic;

namespace DEM.Net.Lib.Services.Lab
{
    public interface ICalculServicesLow_testsDivers
    {
        double[] GetCoordonneesCercleCirconscritAuTriangle(List<double[]> p_pointsTriangle);
        bool IsPointDDansCercleCirconscritAuTriangleByMatrice(List<double[]> p_pointsTriangle, double[] p_coordPtD);
        bool IsPointDDansCercleCirconscritAuTriangleExplicite(List<double[]> p_pointsTriangle, double[] p_pointToTest);
        List<int> GetOrdonnancement(Dictionary<int, double[]> p_pointsATester, bool p_renvoyerNullSiColineaires_vf, bool p_horaireSinonAntohoraire_vf);
    }
}