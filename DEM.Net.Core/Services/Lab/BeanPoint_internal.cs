// BeanPoint_internal.cs
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

namespace DEM.Net.Core.Services.Lab
{
    public class BeanPoint_internal
    {
        private static int _dernierId = 0;

        public int p00_id { get; set; }
        //
        private string p01_hCodeGeogP;
        public string p01_hCodeGeog
        {
            set
            {
                p01_hCodeGeogP=FLabServices.createUtilitaires().GetHCodeGeogPoint(p10_coord);
            }
            get
            {
                if(p01_hCodeGeogP=="")
                {
                    p01_hCodeGeogP = FLabServices.createUtilitaires().GetHCodeGeogPoint(p10_coord);
                }
                return p01_hCodeGeogP;
            }
        }
        //
        public double[] p10_coord { get; set; }
        public int p11_srid { get; set; }
        //
        public bool p21_estPointFacette_vf { get; set; }

        //[Pour construction TIN
        public bool p22_estPointInclus_vf { get; set; }
        public double p31_ecartAbsAuPlanCourant { get; set; }
        
        //[Pour traitements morphologiques
        public Dictionary<string, BeanArc_internal> p41_arcsAssocies { get; set; }
        public List<string> p42_ordonnancementHorairesArcs { get; set; }
        public bool p43_ordonnancementOK_vf { get; set; }
        //
        public BeanPoint_internal(double[] p_coord, int p_srid):this(p_coord[0], p_coord[1], p_coord[2], p_srid)
        {
          
        }
        public BeanPoint_internal(double p_x, double p_y, double p_z, int p_srid)
        {
            p00_id = _dernierId++;
            p10_coord=new double[3] { p_x, p_y, p_z };
            p01_hCodeGeog = FLabServices.createUtilitaires().GetHCodeGeogPoint(p10_coord);
            p11_srid = p_srid;
            p41_arcsAssocies = new Dictionary<string, BeanArc_internal>();
            p42_ordonnancementHorairesArcs = new List<string>();
            p43_ordonnancementOK_vf = false;
        }

    }
}
