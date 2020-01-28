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

namespace DEM.Net.Core
{
    /// <summary>
    /// Defines an obstacle between two non-intervisible points
    /// A ray is casted from Origin to a Target. If any relief is hit, an obstacle will be created
    /// </summary>
    public class IntervisibilityObstacle
    {
        public IntervisibilityObstacle(GeoPoint entryPoint, double visibilityElevationThreshold)
        {
            this.EntryPoint = entryPoint;
            this.VisibilityElevationThreshold = visibilityElevationThreshold;
        }

        /// <summary>
        /// First location where the relief is being hit
        /// </summary>
        public GeoPoint EntryPoint { get; internal set; }
        /// <summary>
        /// Highest point on the relief above the entry point
        /// </summary>
        public GeoPoint PeakPoint { get; internal set; }
        /// <summary>
        /// Location where the ray exists the obstacle
        /// </summary>
        public GeoPoint ExitPoint { get; internal set; }
        /// <summary>
        /// Elevation threshold. If the relief was below this altitude, the ray will
        /// have passed above.
        /// </summary>
        public double VisibilityElevationThreshold { get; }

        public override string ToString()
        {
            var msg = string.Concat(PointAsString("Entry", EntryPoint), Environment.NewLine
                                    , PointAsString("Peak", PeakPoint), Environment.NewLine
                                    , PointAsString("Exit", ExitPoint), Environment.NewLine);
            return msg;
        }

        private string PointAsString(string label, GeoPoint pt)
        {
            if (pt == null)
                return $"No {label}";
            else
                return $"{label}: dist={pt.DistanceFromOriginMeters ?? 0:F2}, Elevation={pt.Elevation ?? 0:F2}";
        }
    }
}