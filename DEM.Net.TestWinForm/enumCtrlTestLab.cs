using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.TestWinForm
{
    public enum enumSrid
    {
        Lambert93
    }
    public enum enumMethodeGenerationPtsEnXetY
    {
        repartitionAleatoireUniforme,
        carroyageRegulierParPas
    }
    public enum enumMethodeGenerationValeursEnZ
    {
        altitudeConstante,
        plan,
        paraboloideElliptique,
        paraboloideHyperbolique
    }
    public enum enumCoeffRecalage
    {
        origineX,
        origineY,
        centreX,
        centreY,
        coeffRecalageX,
        coeffRecalageY
    }
   
}
