using DEM.Net.Api.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace DEM.Net.Api.Utility
{
	// Good solution for passing arrays as web api parameter
	//
	// source: https://stackoverflow.com/a/17553324
	//
	public class LocationArrayInputAttribute : ActionFilterAttribute
	{
		private readonly string _parameterName;

		public LocationArrayInputAttribute(string parameterName)
		{
			_parameterName = parameterName;
			Separator = '|';
		}

		public override void OnActionExecuting(HttpActionContext actionContext)
		{
			if (actionContext.ActionArguments.ContainsKey(_parameterName))
			{
				string parameters = string.Empty;
				if (actionContext.ControllerContext.RouteData.Values.ContainsKey(_parameterName))
					parameters = (string)actionContext.ControllerContext.RouteData.Values[_parameterName];
				else if (actionContext.ControllerContext.Request.RequestUri.ParseQueryString()[_parameterName] != null)
					parameters = actionContext.ControllerContext.Request.RequestUri.ParseQueryString()[_parameterName];

				actionContext.ActionArguments[_parameterName] = parameters.Split(Separator).Select(Parse).ToArray();
			}
		}

		private Location Parse(string latlng)
		{
			// lat, lng, ie: 40.714728,-73.998672
			string[] coords = latlng.Split(',');
			return new Location(double.Parse(coords[0], CultureInfo.InvariantCulture)
				, double.Parse(coords[1], CultureInfo.InvariantCulture));
		}

		public char Separator { get; set; }
	}
}