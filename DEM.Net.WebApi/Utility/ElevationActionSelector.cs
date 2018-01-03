using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace DEM.Net.WebApi
{

	/// <summary>
	/// Custom action selector. This one uses the default ApiControllerActionSelector()
	/// for routes other than "api/elevation"
	/// For this specific route, it tries to match the correct action between locations and path requests
	/// to mimic Google Elevation API
	/// </summary>
	internal class ElevationActionSelector : IHttpActionSelector
	{
		private readonly ApiControllerActionSelector _defaultSelector;
		private HttpConfiguration config;
		

		public ElevationActionSelector(HttpConfiguration config)
		{
			this.config = config;
			_defaultSelector = new ApiControllerActionSelector();
		}

		public ILookup<string, HttpActionDescriptor> GetActionMapping(HttpControllerDescriptor controllerDescriptor)
		{
			return _defaultSelector.GetActionMapping(controllerDescriptor); ;
		}

		public HttpActionDescriptor SelectAction(HttpControllerContext controllerContext)
		{
			Uri requestUri = controllerContext.Request.RequestUri;
			if (requestUri.LocalPath == "/api/elevation")
			{
				// possible actions on controller
				var actions = _defaultSelector.GetActionMapping(controllerContext.ControllerDescriptor);

				// querystring keys
				HashSet<string> keys = new HashSet<string>(HttpUtility.ParseQueryString(requestUri.Query).AllKeys);
				if (keys.Contains("path"))
				{
					return FindActionByParamName("path", actions);
				}
				else if (keys.Contains("locations"))
				{
					return FindActionByParamName("locations", actions);
				}
				throw new Exception("No action found. Check parameters.");
			}

			return _defaultSelector.SelectAction(controllerContext);
		}

		private HttpActionDescriptor FindActionByParamName(string paramName, ILookup<string, HttpActionDescriptor> actions)
		{
			foreach (var action in actions)
			{
				var actionFound = action.FirstOrDefault(a => a.GetParameters().Any(p => p.ParameterName == paramName));
				if (actionFound != null)
				{
					return actionFound;
				}

			}
			return null;
		}
	}
}