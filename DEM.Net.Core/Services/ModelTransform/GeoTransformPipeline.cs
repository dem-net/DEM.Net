// GeoTransformPipeline.cs
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
using System.Linq;

namespace DEM.Net.Core;


public class GeoTransformPipeline
{
    public Func<IEnumerable<GeoPoint>, IEnumerable<GeoPoint>> TransformPoints { get; set; } = null!;
    public Func<HeightMap, HeightMap> TransformHeightMap { get; set; } = null!;

    /// <summary>
    /// Horizontal model units for one true metre at the model centre. Metric sizes added after the transform
    /// (line widths, building heights) are multiplied by it, so they keep their real proportions in the model.
    /// </summary>
    /// <remarks>Elevations are in metres times the z factor, whatever this value: a lift above the terrain
    /// is added before the transform, so it follows the z factor like the terrain does.</remarks>
    public virtual double ModelUnitsPerMeter => 1d;

    public GeoTransformPipeline Clone()
    {
        return (GeoTransformPipeline)this.MemberwiseClone();
    }

    public static GeoTransformPipeline Default => new() { TransformHeightMap = h => h, TransformPoints = pts => pts };
}
