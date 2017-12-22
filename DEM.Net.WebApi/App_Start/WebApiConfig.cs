using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace DEM.Net.WebApi
{
	public static class WebApiConfig
	{
		public static void Register(HttpConfiguration config)
		{
			// Configuration et services API Web

			// Itinéraires de l'API Web
			config.MapHttpAttributeRoutes();

			config.Routes.MapHttpRoute(
				name: "DefaultApi",
				routeTemplate: "api/{controller}/{id}",
				defaults: new { id = RouteParameter.Optional }
			);
		}
	}
}
