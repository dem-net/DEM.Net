using System.Collections.Generic;
using DEM.Net.Lib.Services.Lab;

namespace DEM.Net.TestWinForm
{
    public interface ITestsDiversServices
    {
        void TestCercleCirconscritAuTriangle();
        void TestIsInCercleCirconscrit();
        void TestChangementReferentiel2D(IEnumerable<BeanPoint_internal> p_points, double[] p_vecteurDeDecalage);
        void TestOrdonnancement();
    }
}