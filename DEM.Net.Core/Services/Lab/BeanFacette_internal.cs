// BeanFacette_internal.cs
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core.Services.Lab
{
    public class BeanFacette_internal
    {
        private static int _dernierId = 0;
        //
        public int p00_idFacette { get; set; }
        //
        public List<BeanPoint_internal> p01_pointsDeFacette { get; set; }
        public List<BeanArc_internal> p02_arcs { get; set; }
        public bool p03_estVerticale_vf { get; set; }
        public Geometry p04_geomFacette { get; set; }
        //
        public List<BeanPoint_internal> p10_pointsInclus { get; set; }
        //  
        public double p21_plusGrandEcartAbsolu { get; set; }
        public BeanPoint_internal p22_pointPlusGrandEcart { get; set; }
        //
        public BeanFacette_internal p23_facetteEcartSup { get; set; }
        public BeanFacette_internal p24_facetteEcartInf { get; set; }

        #region PROPRIETES PRIVEES
        private double[] p31_normaleDuPlan;
        private double[] p32_penteVecteur;
        private double? p33_penteValeur;
        #endregion PROPRIETES PRIVEES

        #region METHODES
        public double[] getNormaleDuPlan()
        {
           if(p31_normaleDuPlan==null)
            {
                bool p_exceptionSiNotAPlanElseVecteur000_vf = false;
                p31_normaleDuPlan= FLabServices.createCalculLow().GetNormaleDuPlan(p01_pointsDeFacette[0].p10_coord, p01_pointsDeFacette[2].p10_coord, p01_pointsDeFacette[1].p10_coord, p_exceptionSiNotAPlanElseVecteur000_vf);
            }
            return p31_normaleDuPlan;
        }
        public double[] getVecteurPenteMaxi()
        {
            if(p32_penteVecteur==null)
            {
                bool v_normaliser_vf = true;
                p32_penteVecteur=FLabServices.createCalculLow().GetVecteurPenteMaxi(getNormaleDuPlan(), v_normaliser_vf);
            }
            return p32_penteVecteur;
        }
        public double getPente()
        {
            if(p33_penteValeur==null)
            {
                p33_penteValeur=FLabServices.createCalculLow().GetPente(getVecteurPenteMaxi());
            }
            return (double) p33_penteValeur;
        }
        #endregion METHODES

        #region CONSTRUCTEURS
        public BeanFacette_internal()
        {
            p00_idFacette= _dernierId++;
            p01_pointsDeFacette = new List<BeanPoint_internal>();
            p02_arcs = new List<BeanArc_internal>();
            p10_pointsInclus = new List<BeanPoint_internal>();
            //
            p31_normaleDuPlan = null;
            p32_penteVecteur = null;
        }
        #endregion CONSTRUCTEURS
    }
}
