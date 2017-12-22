using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DEM.Net.Api.Models
{
	public class Location
	{
		public Location(double Latitude, double Longitude)
		{
			lat = Latitude;
			lng = Longitude;
		}
		public double lat { get; set; }
		public double lng { get; set; }
	}
}