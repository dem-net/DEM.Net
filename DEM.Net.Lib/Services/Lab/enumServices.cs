using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Services.Lab
{
    public enum enumStatutArc
    {
        arcNONCandidatASuppression,
        arcCandidatASuppression,
        arcExterne
    }
    public enum enumMethodeChoixDuPointCentral
    {
        pointLePlusExcentre,
        centroide
    }
    public enum enumModeDelimitationFrontiere
    {
        convexHull,
        mbo
    }
    public enum enumModeCalculZ
    {
        alti_0,
        alti_min,
        alti_saisie
     }
}
