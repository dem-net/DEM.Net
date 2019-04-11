// enumServices.cs
//
// Author:
//       Frédéric Aubin
//
// Copyright (c) 2019 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

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
   

    public enum enum_qualificationHydro_pointDeFacette
    {
        sortie,
        entree,
        mixte,
        indetermine
    }
    public enum enumSensPenteArc
    {
        montant,
        descendant,
        flat,
        indetermine
    }

    public enum enum_qualificationMorpho_arc
    {
        talweg,
        crete,
        autre,
        indetermine
    }
}
