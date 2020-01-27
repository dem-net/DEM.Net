// ElevationMetrics.cs
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
using System.Globalization;

namespace DEM.Net.Core
{
	public class ElevationMetrics
	{
		public double MinElevation { get; internal set; }
		public double MaxElevation { get; internal set; }
		public double Distance { get; internal set; }
		public int NumPoints { get; internal set; }
		public double Climb { get; internal set; }
		public double Descent { get; internal set; }

        public override string ToString()
        {
            return ToString("F2");
        }

        public string ToString(string numberFormat = "F2")
        {
            return $"Min/Max: {MinElevation.ToString(numberFormat, CultureInfo.InvariantCulture)} / {MaxElevation.ToString(numberFormat, CultureInfo.InvariantCulture)}, Distance: {Distance.ToString(numberFormat, CultureInfo.InvariantCulture)} m, Climb/Descent: {Climb.ToString(numberFormat, CultureInfo.InvariantCulture)} / {Descent.ToString(numberFormat, CultureInfo.InvariantCulture)}";
        }
    }

    public class VisibilityMetrics : ElevationMetrics
    {
        public bool Intervisible => Obstacles.Count == 0;

        public List<VisibilityObstacle> Obstacles { get; set; } = new List<VisibilityObstacle>();

        internal void AddObstacle(GeoPoint point, double minObstacleElevation)
        {
            Obstacles.Add(new VisibilityObstacle(point, minObstacleElevation));
        }

        public override string ToString()
        {
            if (Intervisible)
            {
                return string.Concat("(Intervisible) ", base.ToString()); 
            }
            else
            {
                return string.Concat($"{Obstacles.Count} obstacle(s): ",
                    string.Join(", ",Obstacles),
                    Environment.NewLine,
                    base.ToString());
            }
        }
    }

    public class VisibilityObstacle
    {
        public VisibilityObstacle(GeoPoint point, double minObstacleElevation)
        {
            this.GeoPoint = point;
            this.MinObstacleElevation = minObstacleElevation;
        }

        public GeoPoint GeoPoint { get; }
        public double MinObstacleElevation { get; }

        public override string ToString()
        {
            return string.Concat(GeoPoint.ToString(), $", MinObstacleElevation: {MinObstacleElevation:F2}");
        }
    }
}