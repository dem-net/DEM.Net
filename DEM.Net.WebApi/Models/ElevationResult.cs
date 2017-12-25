using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DEM.Net.WebApi.Models
{
	public class ElevationResult
	{
		public Location location { get; set; }
		public double elevation { get; set; }
		public double distanceFromOrigin { get; set; }
	}
}