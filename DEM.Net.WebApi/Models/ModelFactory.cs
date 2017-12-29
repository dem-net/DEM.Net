using DEM.Net.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;

namespace DEM.Net.WebApi.Models
{
	public static class ModelFactory
	{
		public static List<GeoPoint> Create(IEnumerable<Location> locations)
		{
			return locations.Select(l => new GeoPoint(l.lat, l.lng)).ToList();
		}

		internal static ElevationResults CreateElevationResults(IEnumerable<GeoPoint> geoPoints, ElevationMetrics metrics)
		{
			return new ElevationResults
			{
				results = geoPoints.Select(pt => new ElevationResult()
				{
					elevation = pt.Elevation.GetValueOrDefault(0),
					location = new Location(pt.Latitude, pt.Longitude),
					distanceFromOrigin = pt.DistanceFromOriginMeters
				}),
				metrics = new ElevationMetricsModel
				{
					length = metrics.Distance,
					maxElevation = metrics.MaxElevation,
					minElevation = metrics.MinElevation,
					numSamples = geoPoints.Count(),
					numPointsUsed = metrics.NumPoints,
					totalClimb = metrics.Climb,
					totalDescent = metrics.Descent,
				}
			};
		}
	}
}