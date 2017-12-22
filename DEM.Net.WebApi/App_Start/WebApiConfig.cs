using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using Newtonsoft.Json.Serialization;

namespace DEM.Net.WebApi
{
	public static class WebApiConfig
	{
		public static void Register(HttpConfiguration config)
		{
			config.EnableCors();
			// Configuration et services API Web

			// Itinéraires de l'API Web
			config.MapHttpAttributeRoutes();

			//Suppression du formatteur XML pour que les réponses soient en json
			config.Formatters.Remove(config.Formatters.XmlFormatter);

			config.Routes.MapHttpRoute(
				name: "DefaultApi",
				routeTemplate: "api/{controller}/{id}",
				defaults: new { id = RouteParameter.Optional }
			);
		}
	}
}
