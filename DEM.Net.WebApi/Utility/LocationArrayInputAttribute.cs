using DEM.Net.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Web.Http.Routing;

namespace DEM.Net.WebApi.Utility
{
	// Good solution for passing arrays as web api parameter
	//
	// source: https://stackoverflow.com/a/17553324
	// 
	// usage : on action route, add [LocationArrayInput("<paramname>", Separator = '|')]
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

				actionContext.ActionArguments[_parameterName] = parameters.Split(Separator).Select(LocationHelper.Parse).ToArray();
			}
		}

		public char Separator { get; set; }
	}

	public class LocationInputAttribute : ActionFilterAttribute
	{
		private readonly string _parameterName;

		public LocationInputAttribute(string parameterName)
		{
			_parameterName = parameterName;
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

				actionContext.ActionArguments[_parameterName] = LocationHelper.Parse(parameters);
			}
		}

	}

	/// <summary>
	/// Enables constraints of type location (not used)
	/// </summary>
	public class LocationConstraint : IHttpRouteConstraint
	{
		public bool Match(HttpRequestMessage request, IHttpRoute route, string parameterName,
			IDictionary<string, object> values, HttpRouteDirection routeDirection)
		{
			object value;
			if (values.TryGetValue(parameterName, out value) && value != null)
			{
				if (value != null)
				{
					Location loc;
					return LocationHelper.TryParse(value.ToString(), out loc);
				}
			}
			return false;
		}
	}

	public class LocationArrayConstraint : IHttpRouteConstraint
	{
		public bool Match(HttpRequestMessage request, IHttpRoute route, string parameterName,
			IDictionary<string, object> values, HttpRouteDirection routeDirection)
		{
			object value;
			if (values.TryGetValue(parameterName, out value) && value != null)
			{
				if (value != null)
				{
					Location[] loc;
					return LocationHelper.TryParseArray(value.ToString(), out loc);
				}
			}
			return false;
		}
	}

	public static class LocationHelper
	{
		public static Location Parse(string latlng)
		{
			// lat, lng, ie: 40.714728,-73.998672
			string[] coords = latlng.Split(',');
			return new Location(double.Parse(coords[0], CultureInfo.InvariantCulture)
				, double.Parse(coords[1], CultureInfo.InvariantCulture));
		}

		public static bool TryParse(string latlng, out Location location)
		{
			bool ok = true;
			location = null;
			try
			{
				location = Parse(latlng);
			}
			catch (Exception)
			{
				ok = false;
			}
			return ok;
		}

		public static bool TryParseArray(string latlngArray, out Location[] location)
		{
			bool ok = true;
			location = null;
			try
			{
				location = latlngArray.Split('|').Select(LocationHelper.Parse).ToArray();
			}
			catch (Exception)
			{
				ok = false;
			}
			return ok;
		}
	}
}