// VisibilityReport.cs
//
// Author:
//       Xavier Fischer
//
// Copyright (c) 2020 Xavier Fischer
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the right
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

namespace DEM.Net.Core
{
    /// <summary>
    /// Reports ray casting from an origin to a target point
    /// If there is anything standing between the points, it will be listed in obstacles
    /// </summary>
    public class IntervisibilityReport
    {
        public GeoPoint Origin { get; set; }
        public GeoPoint Target { get; set; }
        public bool HasObstacles => Metrics?.Intervisible == false;
        public int ObstacleCount => Metrics?.Obstacles?.Count ?? 0;
        //public List<GeoPoint> GeoPoints { get; set; }
        public IntervisibilityMetrics Metrics { get; set; }

        public IntervisibilityReport(GeoPoint origin, GeoPoint target, IntervisibilityMetrics visibilityMetrics)
        {
            this.Origin = origin;
            this.Target = target;
            this.Metrics = visibilityMetrics;
        }
    }
}
