// IInterpolator.cs
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
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core
{
    public interface IInterpolator
    {
        /// <summary>
        /// 
        /// 
        /// The concept of linear interpolation between two points can be extended to bilinear interpolation within 
        /// the grid cell. The function is said to be linear in each variable when the other is held fixed. 
        /// 
        /// For example, to determine the height hi at x, y in Figure 5, the elevations at y on the vertical 
        /// boundaries of the grid cell can be linearly interpolated between h1 and h3 at ha, and h2 and h4 at hb.
        /// Finally, the required elevation at x can be linearly interpolated between ha and hb. 
        /// 
        /// Source : http://www.geocomputation.org/1999/082/gc_082.htm
        /// </summary>
        /// <param name="h1"></param>
        /// <param name="h2"></param>
        /// <param name="h3"></param>
        /// <param name="h4"></param>
        /// <returns></returns>
        double Interpolate(double southWestHeight, double southEastHeight, double northWestHeight, double northEastHeight, double x, double y);

    }
}
