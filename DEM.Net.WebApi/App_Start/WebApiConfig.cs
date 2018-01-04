using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;

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
			//var constraintResolver = new DefaultInlineConstraintResolver();
			//constraintResolver.ConstraintMap.Add("location", typeof(LocationConstraint));
			//config.MapHttpAttributeRoutes(constraintResolver);

			//Suppression du formatteur XML pour que les réponses soient en json
			config.Formatters.Remove(config.Formatters.XmlFormatter);

			config.Routes.MapHttpRoute(
				name: "ElevationApi",
				routeTemplate: "api/{controller}" 
			);

			// Add custom action selector to mimic Google rest API (couldn't do it without implementing my own selector)
			config.Services.Replace(typeof(IHttpActionSelector), new ElevationActionSelector(config));
		}
	}
}
