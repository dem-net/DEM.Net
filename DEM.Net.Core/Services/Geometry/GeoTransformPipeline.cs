// IGeoTransformPipeline.cs
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
    public interface IGeoTransformPipeline
    {

        IEnumerable<GeoPoint> TransformPoints(IEnumerable<GeoPoint> points);

        HeightMap TransformHeightMap(HeightMap hmap);

    }

    public class ModelGenerationTransform : IGeoTransformPipeline
    {
        public BoundingBox BoundingBox { get; set; }
        private readonly int _outputSrid;
        private readonly bool _centerOnOrigin;
        private readonly float _zFactor;

        public ModelGenerationTransform(int outputSrid, bool centerOnOrigin, float zFactor) : this(null, outputSrid, centerOnOrigin, zFactor)
        {
        }
        public ModelGenerationTransform(BoundingBox bbox, int outputSrid, bool centerOnOrigin, float zFactor)
        {
            this.BoundingBox = bbox;
            _outputSrid = outputSrid;
            _centerOnOrigin = centerOnOrigin;
            _zFactor = zFactor;
        }
        public HeightMap TransformHeightMap(HeightMap hMap)
        {
            hMap = hMap.ReprojectTo(4326, _outputSrid);

            if (_centerOnOrigin)
            {
                hMap = hMap.CenterOnOrigin();
            }

            hMap = hMap.ZScale(_zFactor);

            return hMap;
        }

        public IEnumerable<GeoPoint> TransformPoints(IEnumerable<GeoPoint> points)
        {

            points = points.ReprojectTo(4326, _outputSrid);

            if (_centerOnOrigin)
            {
                if (BoundingBox == null)
                {
                    throw new ArgumentNullException($"Bouding box must be set when using {nameof(IGeoTransformPipeline)}.{nameof(TransformPoints)} with center on origin");
                }

                points = points.CenterOnOrigin(this.BoundingBox.ReprojectTo(this.BoundingBox.SRID, _outputSrid));
            }

            points = points.ZScale(_zFactor);

            return points;


        }
    }
}
