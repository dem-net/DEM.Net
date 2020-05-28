// BilinearInterpolator.cs
//
// Author:
//       Xavier Fischer 
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
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core.Interpolation
{
    /// <summary>
    /// Standard interpolation
    /// 
    /// For a point lying between 4 nearest grid coordinates, this interpolator will do a linear interpolation on X axis and then on Y axis.
    /// 
    ///
    /// 
    /// "The concept of linear interpolation between two points can be extended to bilinear interpolation within 
    /// the grid cell. The function is said to be linear in each variable when the other is held fixed. 
    /// 
    /// For example, to determine the height hi at x, y in Figure 5, the elevations at y on the vertical 
    /// boundaries of the grid cell can be linearly interpolated between h1 and h3 at ha, and h2 and h4 at hb.
    /// Finally, the required elevation at x can be linearly interpolated between ha and hb."
    /// 
    /// Source : http://www.geocomputation.org/1999/082/gc_082.htm
    /// </summary>
    public class BilinearInterpolator : IInterpolator
    {
        public double Interpolate(double southWestHeight, double southEastHeight, double northWestHeight, double northEastHeight, double x, double y)
        {
            double sw = southWestHeight, se = southEastHeight, nw = northWestHeight, ne = northEastHeight;
            // bilinear
            double ha = MathHelper.Lerp(nw, sw, y);
            double hb = MathHelper.Lerp(ne, se, y);
            double hi_linear = MathHelper.Lerp(MathHelper.Lerp(nw, sw, y), MathHelper.Lerp(ne, se, y), x);

            return hi_linear;
        }
    }
}
