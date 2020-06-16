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
using System.Linq;
using System.Collections.Generic;

namespace DEM.Net.Core
{
    public interface IGeoTransformPipeline
    {

        IEnumerable<GeoPoint> TransformPoints(IEnumerable<GeoPoint> points);

        HeightMap TransformHeightMap(HeightMap hmap);

    }

    public class GeoTransformPipelineFacade : IGeoTransformPipeline
    {
        private IGeoTransformPipeline pipeline;
        private Func<HeightMap, HeightMap> transformHmap;
        private Func<IEnumerable<GeoPoint>, IEnumerable<GeoPoint>> transformPoints;

        public GeoTransformPipelineFacade(IGeoTransformPipeline pipeline)
        {
            this.transformHmap = pipeline.TransformHeightMap;
        }

        public IGeoTransformPipeline AddPreTransformHeightMap(Func<HeightMap, HeightMap> transform)
        {
            this.transformHmap = h => transformHmap(transform(h));
            return this;
        }
        public IGeoTransformPipeline AddPostTransformHeightMap(Func<HeightMap, HeightMap> transform)
        {
            this.transformHmap = h => transform(transformHmap(h));
            return this;
        }

        public HeightMap TransformHeightMap(HeightMap hmap)
        {
            return transformHmap(hmap);
        }

        public IGeoTransformPipeline AddPreTransformPoints(Func<IEnumerable<GeoPoint>, IEnumerable<GeoPoint>> transform)
        {
            this.transformPoints = h => transformPoints(transform(h));
            return this;
        }
        public IGeoTransformPipeline AddPostTransformPoints(Func<IEnumerable<GeoPoint>, IEnumerable<GeoPoint>> transform)
        {
            this.transformPoints = h => transform(transformPoints(h));
            return this;
        }

        public IEnumerable<GeoPoint> TransformPoints(IEnumerable<GeoPoint> points)
        {
            return transformPoints(points);
        }
    }

    public class ModelGenerationTransform : IGeoTransformPipeline
    {
        public BoundingBox BoundingBox { get; set; }
        private readonly int _outputSrid;
        private readonly bool _centerOnOrigin;
        private readonly float _zFactor;
        private BoundingBox bbox;
        private int sRID_PROJECTED_MERCATOR;
        private bool centerOnOrigin;
        private float zScale;
        private readonly bool _centerOnZOrigin;

        public ModelGenerationTransform(int outputSrid, bool centerOnOrigin, float zFactor) : this(null, outputSrid, centerOnOrigin, zFactor, true)
        {
        }
        public ModelGenerationTransform(BoundingBox bbox, int outputSrid, bool centerOnOrigin, float zFactor, bool centerOnZOrigin)
        {
            this.BoundingBox = bbox;
            _outputSrid = outputSrid;
            _centerOnOrigin = centerOnOrigin;
            _zFactor = zFactor;
            _centerOnZOrigin = centerOnZOrigin;
        }

        public HeightMap TransformHeightMap(HeightMap hMap)
        {
            hMap = hMap.ReprojectTo(4326, _outputSrid);

            if (_centerOnOrigin)
            {
                hMap = hMap.CenterOnOrigin(this.BoundingBox.ReprojectTo(this.BoundingBox.SRID, _outputSrid), _centerOnZOrigin);
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
