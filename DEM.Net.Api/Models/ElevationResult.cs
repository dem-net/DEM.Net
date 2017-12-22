using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DEM.Net.Api.Models
{
	public class ElevationResult
	{
		public Location location { get; set; }
		public double elevation { get; set; }
	}
}