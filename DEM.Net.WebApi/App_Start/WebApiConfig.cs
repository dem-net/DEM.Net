using DEM.Net.WebApi.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Routing;

namespace DEM.Net.WebApi
{
	public static class WebApiConfig
	{
		public static void Register(HttpConfiguration config)
		{
			config.EnableCors();
			// Configuration et services API Web

			// Itinéraires de l'API Web
			//config.MapHttpAttributeRoutes();

			// Init custom constraints
			var constraintResolver = new DefaultInlineConstraintResolver();
			constraintResolver.ConstraintMap.Add("location", typeof(LocationConstraint));
			constraintResolver.ConstraintMap.Add("locations", typeof(LocationArrayConstraint));
			constraintResolver.ConstraintMap.Add("enum", typeof(EnumConstraint));
			config.MapHttpAttributeRoutes(constraintResolver);

			//Suppression du formatteur XML pour que les réponses soient en json
			config.Formatters.Remove(config.Formatters.XmlFormatter);

			config.Routes.MapHttpRoute(
				name: "DefaultApi",
				routeTemplate: "api/{controller}/{id}",
				defaults: new { id = RouteParameter.Optional }
			);

			// Add custom action selector to mimic Google rest API (couldn't do it without implementing my own selector)
			//config.Services.Replace(typeof(IHttpActionSelector), new ElevationActionSelector(config));
		}
	}
}
