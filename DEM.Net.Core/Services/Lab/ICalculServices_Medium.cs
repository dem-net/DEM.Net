// ICalculServices_Medium.cs
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

using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries;
using System.Collections.Generic;

namespace DEM.Net.Core.Services.Lab
{
    public interface ICalculServices_Medium
    {
        BeanParametresDuTin GetParametresDuTinParDefaut();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="p_points"></param>
        /// <returns></returns>
        BeanTopologieFacettes GetInitialisationTin(List<BeanPoint_internal> p_points, BeanParametresDuTin p_parametresDuTin);
        void AugmenteDetailsTinByRef(ref BeanTopologieFacettes p_topologieFacette, BeanParametresDuTin p_parametresDuTin);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p_points"></param>
        /// <returns></returns>
        List<BeanPoint_internal> GetConvexHull2D(IEnumerable<BeanPoint_internal> p_points);
        //
        Dictionary<string, int> GetEtComptePointsDoublonnes(List<BeanPoint_internal> p_pointsToTest);
        List<BeanPoint_internal> GetOrdonnancementPointsFacette(List<BeanPoint_internal> p_pointsFacettes, bool p_renvoyerNullSiColineaires_vf, bool p_sensHoraireSinonAntiHoraire_vf);
        List<string> GetOrdonnancementArcsAutourPointFacette(BeanPoint_internal p_pointFacette, int p_idPremierArc, bool p_sensHoraireSinonAntihoraire_vf);
        void RecalculFacettes(ref BeanTopologieFacettes p_topol);
        bool SupprimerUneFacette(ref BeanTopologieFacettes p_topologieFacette, ref BeanFacette_internal p_facetteASupprimer, bool p_seulementSiFacetteExterne_vf);
        Geometry GetGeometryPolygoneFacetteEtOrdonnePointsFacette(ref BeanFacette_internal p_facette, ref BeanTopologieFacettes p_topologieFacette);
    }
}