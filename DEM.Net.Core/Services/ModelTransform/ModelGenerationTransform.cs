// ModelGenerationTransform.cs
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

namespace DEM.Net.Core;

public class ModelGenerationTransform : GeoTransformPipeline
{
    public BoundingBox BoundingBox { get; set; }
 
    private readonly int _outputSrid;
    private readonly bool _centerOnOrigin;
    private readonly float _zFactor;
    private readonly bool _centerOnZOrigin;

    /// <summary>
    /// X and Y scale applied after centring: cos(lat₀) with true scale, 1 otherwise.
    /// </summary>
    public double HorizontalScale { get; }

    /// <summary>
    /// True when X and Y are scaled to true metres at the model centre.
    /// </summary>
    public bool IsTrueScale => HorizontalScale != 1d;

    /// <inheritdoc/>
    public override double ModelUnitsPerMeter { get; }

    /// <param name="bbox">Model extent, in its own SRID</param>
    /// <param name="datasetSrid">SRID of the points given to <see cref="GeoTransformPipeline.TransformPoints"/></param>
    /// <param name="outputSrid">Model SRID</param>
    /// <param name="centerOnOrigin">Translate the model so the bbox centre is at (0, 0)</param>
    /// <param name="zFactor">Vertical exaggeration</param>
    /// <param name="centerOnZOrigin">Translate the height map so its lowest point is at Z = 0</param>
    /// <param name="trueScale">With a Web Mercator output centred on the origin, scale X and Y by cos(lat₀) (bbox centre),
    /// so model units are true metres at the centre and the relief keeps its real proportions.
    /// Ignored otherwise: an uncentred model keeps pure Web Mercator coordinates.</param>
    public ModelGenerationTransform(BoundingBox bbox, int datasetSrid, int outputSrid, bool centerOnOrigin, float zFactor, bool centerOnZOrigin, bool trueScale = false)
    {
        this.BoundingBox = bbox;
        _outputSrid = outputSrid;
        _centerOnOrigin = centerOnOrigin;
        _zFactor = zFactor;
        _centerOnZOrigin = centerOnZOrigin;

        // Web Mercator inflates horizontal distances by 1/cos(lat), Z stays in metres
        double mercatorScale = 1d;
        if (outputSrid == Reprojection.SRID_PROJECTED_MERCATOR && bbox != null)
        {
            var bboxWgs84 = bbox.SRID == Reprojection.SRID_GEODETIC ? bbox : bbox.ReprojectTo(bbox.SRID, Reprojection.SRID_GEODETIC);
            double lat0 = (bboxWgs84.yMin + bboxWgs84.yMax) / 2d;
            mercatorScale = Math.Cos(lat0 * Math.PI / 180d);
        }
        bool applyScale = trueScale && centerOnOrigin && outputSrid == Reprojection.SRID_PROJECTED_MERCATOR && mercatorScale != 1d;
        HorizontalScale = applyScale ? mercatorScale : 1d;
        ModelUnitsPerMeter = applyScale ? 1d : 1d / mercatorScale;

        base.TransformPoints = points =>
        {
            points = points.ReprojectTo(datasetSrid, _outputSrid);
            if (_centerOnOrigin)
            {
                if (BoundingBox == null)
                {
                    throw new ArgumentNullException($"Bouding box must be set when using {nameof(GeoTransformPipeline)}.{nameof(TransformPoints)} with center on origin");
                }
                points = points.CenterOnOrigin(this.BoundingBox.ReprojectTo(this.BoundingBox.SRID, _outputSrid));
            }

            if (IsTrueScale)
            {
                float s = (float)HorizontalScale;
                points = points.Scale(s, s, 1f);
            }
            points = points.ZScale(_zFactor);
            return points;
        };


        base.TransformHeightMap = hMap =>
        {
            hMap = hMap.ReprojectTo(BoundingBox.SRID, _outputSrid);

            if (_centerOnOrigin)
            {
                hMap = hMap.CenterOnOrigin(this.BoundingBox.ReprojectTo(this.BoundingBox.SRID, _outputSrid), _centerOnZOrigin);
            }

            if (IsTrueScale)
            {
                float s = (float)HorizontalScale;
                hMap = hMap.Scale(s, s, 1f);
            }
            hMap = hMap.ZScale(_zFactor);

            return hMap;
        };
    }
}
