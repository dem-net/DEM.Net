using DEM.Net.Lib;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
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

		internal static ElevationResults CreateElevationResults(List<GeoPoint> geoPoints, ElevationMetricsModel metricsModel)
		{

			var result = new ElevationResults
			{
				metrics = metricsModel,
				results = geoPoints.Select(pt => new ElevationResult()
				{
					elevation = pt.Elevation.GetValueOrDefault(0),
					location = new Location(pt.Latitude, pt.Longitude),
					distanceFromOrigin = pt.DistanceFromOriginMeters
				})
			};

			return result;

		}

		internal static ElevationMetricsModel CreateElevationMetricsModel(List<GeoPoint> geoPoints, ElevationMetrics metrics)
		{
			return new ElevationMetricsModel
			{
				length = metrics.Distance,
				maxElevation = metrics.MaxElevation,
				minElevation = metrics.MinElevation,
				numSamples = geoPoints.Count,
				numPointsUsed = metrics.NumPoints,
				totalClimb = metrics.Climb,
				totalDescent = metrics.Descent
			};
		}

		internal static Feature CreateFeature(IEnumerable<GeoPoint> geoPoints, ElevationMetricsModel metrics)
		{
			
			var points = geoPoints.Select(pt => new Position(pt.Latitude,pt.Longitude,pt.Elevation)).ToList();
			LineString line = new LineString(points);
			return new Feature(line, metrics);
			
		}

		
	}
}