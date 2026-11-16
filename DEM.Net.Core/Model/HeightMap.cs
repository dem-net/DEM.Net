// HeightMap.cs
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
    /// A grid of points (rows north to south, columns west to east), as read from a raster
    /// </summary>
    /// <remarks>
    /// Since v11 the coordinates are an array, and the transforms (<see cref="HeightMapExtensions"/>, <see cref="Reprojection"/>)
    /// change them in place: before, each transform added a step to a lazy pipeline that ran again on every enumeration.
    /// Use <see cref="Clone"/> to keep the original points.
    /// </remarks>
    [Serializable()]
    public class HeightMap
    {
        public HeightMap(int width, int height)
        {
            Width = width;
            Height = height;
            Count = width * height;
            Minimum = 15000;
            Maximum = -15000;
        }

        /// <summary>
        /// Bounds of the points, computed from the coordinates when not set
        /// </summary>
        public BoundingBox BoundingBox
        {
            get => field ??= Coordinates.GetBoundingBox();
            set;
        }

        /// <summary>
        /// Points of the grid, row by row (north to south), west to east in each row
        /// </summary>
        public GeoPoint[] Coordinates { get; set; } = [];

        /// <summary>
        /// Coordinate count
        /// </summary>
        public int Count { get; set; }

        public float Minimum { get; set; }
        public float Maximum { get; set; }
        public float Range => Maximum - Minimum;

        public int Width { get; private set; }
        public int Height { get; private set; }

        /// <summary>
        /// Copy of the height map, with its own coordinates (the transforms of the copy leave this height map unchanged)
        /// </summary>
        public HeightMap Clone()
        {
            var clone = (HeightMap)MemberwiseClone();
            clone.Coordinates = (GeoPoint[])Coordinates.Clone();
            return clone;
        }
    }
}
