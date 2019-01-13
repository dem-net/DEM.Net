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
        mboSimple,
        pointsProchesDuMbo
    }
    public enum enumModeCalculZ
    {
        alti_0,
        alti_min,
        alti_saisie
     }
    public enum enumOrdonnancementPoints
    {
        horaire,
        antihoraire
    }
    public enum enumTypeArcReseau
    {
        talweg,
        crete,
        autre,
        indetermine
    }
}
