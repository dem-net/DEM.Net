using System.Collections.Generic;
using DEM.Net.Lib.Services.Lab;

namespace DEM.Net.TestWinForm
{
    public interface IServicesApplicatifs
    {
        List<BeanPoint_internal> GetPointsTests(BeanParamGenerationAutoDePointsTests p_paramGenerationPointsTest);
    }
}