// BeanResultatConversions_internal.cs
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
    public class BeanResultatConversions_internal
    {
        public bool p00_modif_vf { get; set; }
        public List<int> p01_idFacettesSupprimees { get; set; }
        public List<BeanFacette_internal> p02_newFacettes { get; set; }
        public List<BeanArc_internal> p03_arcsCandidatsOut { get; set; }
        public List<BeanArc_internal> p04_arcsAExclureOut { get; set; }

        public BeanResultatConversions_internal()
        {
            p01_idFacettesSupprimees = new List<int>();
            p02_newFacettes = new List<BeanFacette_internal>();
            p03_arcsCandidatsOut = new List<BeanArc_internal>();
            p04_arcsAExclureOut = new List<BeanArc_internal>();
        }
    }
}
