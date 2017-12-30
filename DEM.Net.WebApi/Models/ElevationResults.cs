using GeoJSON.Net.Feature;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DEM.Net.WebApi.Models
{
	public class ElevationResults
	{
		public ElevationMetricsModel metrics { get; set; }

		public IEnumerable<ElevationResult> results { get; set; }

		public Feature geoJson { get; set; }
	}
}