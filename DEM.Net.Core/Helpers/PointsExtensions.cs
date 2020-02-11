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
        /// <returns></returns>
        public static IntervisibilityMetrics ComputeVisibilityMetrics(this IList<GeoPoint> linePoints
            , double sourceVerticalOffset = 0d, double? noDataValue = null)
        {
            return GeometryService.ComputeVisibilityMetrics(linePoints, visibilityCheck: true, sourceVerticalOffset: sourceVerticalOffset, noDataValue);
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
    }
}
