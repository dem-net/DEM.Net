using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DEM.Net.WebApi.Models
{
	public class ElevationMetricsModel
	{
		public double minElevation { get; internal set; }
		public double maxElevation { get; internal set; }
		public double length { get; internal set; }
		public int numSamples { get; internal set; }
	}
}