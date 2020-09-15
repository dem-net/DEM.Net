using System;
using System.Collections.Generic;
using System.Text;

namespace DEM.Net.Core
{
    public static class PointsExtensions
    {
        /// <summary>
        /// Get usual stats from an elevated point list.
        /// Min/Max altitudes, length, climb/descent
        /// Assumes that the provided list is a "line string"
        /// </summary>
        /// <param name="points"></param>
        /// <param name="noDataValue"></param>
        /// <returns></returns>
        public static ElevationMetrics ComputeMetrics(this IList<GeoPoint> points, double? noDataValue = null)
        {
            return GeometryService.ComputeMetrics(points, noDataValue);
        }

        /// <summary>
        /// Get <see cref="ComputeMetrics(IList{GeoPoint})"/> metrics and visiblity report if any obstacles are found
        /// </summary>
        /// <param name="points"></param>
        /// <param name="sourceVerticalOffset">Vertical elevation offset at source point. The line of sight will be calculated from this point (set to 1.8 for simulate a human eye height)</param>
        /// <param name="targetVerticalOffset"></param>
        /// <returns></returns>
        public static IntervisibilityMetrics ComputeVisibilityMetrics(this IList<GeoPoint> linePoints
            , double sourceVerticalOffset = 0d
            , double targetVerticalOffset = 0d
            , double? noDataValue = null)
        {
            return GeometryService.ComputeVisibilityMetrics(linePoints, visibilityCheck: true, sourceVerticalOffset: sourceVerticalOffset, targetVerticalOffset: targetVerticalOffset, noDataValue);
        }

        /// <summary>
        /// Reduces the point list using a Douglas Peucker Reduction
        /// All details below toleranceMeters will usually be skipped
        /// </summary>
        /// <param name="points"></param>
        /// <param name="toleranceMeters"></param>
        /// <returns></returns>
        public static List<GeoPoint> Simplify(this IReadOnlyList<GeoPoint> points, double toleranceMeters)
        {
            return DouglasPeucker.DouglasPeuckerReduction(points, toleranceMeters);
        }

        public static IEnumerable<GeoPoint> FitInto(this IEnumerable<GeoPoint> points, BoundingBox bboxUsedForGeneration, float maxSize)
        {
            float scale = 1f;
            if (bboxUsedForGeneration.Width > bboxUsedForGeneration.Height)
            {
                scale = (float)(maxSize / bboxUsedForGeneration.Width);
            }
            else
            {
                scale = (float)(maxSize / bboxUsedForGeneration.Height);
            }

            return points.Scale(scale, scale, scale);
        }
    }
}
