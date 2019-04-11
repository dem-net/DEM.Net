using System.Collections.Generic;
using DEM.Net.Core;
using DEM.Net.Core.Services.Lab;

namespace DEM.Net.TestWinForm
{
    public interface IServicesApplicatifs
    {
        List<BeanPoint_internal> GetPointsTests(BeanParamGenerationAutoDePointsTests p_paramGenerationPointsTest);
        List<BeanPoint_internal> GetPointsTestsByBBox(string p_bbox, DEMDataSet dataSet, int sridCible);
    }
}