using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Http.Routing;

namespace DEM.Net.WebApi.Utility
{
	public class EnumConstraint : IHttpRouteConstraint
	{
		private static readonly ConcurrentDictionary<string, string[]> Cache = new ConcurrentDictionary<string, string[]>();
		private readonly string[] _validOptions;
		/// <summary>
		/// Create new <see cref="EnumConstraint"/>
		/// </summary>
		/// <param name="enumType"></param>
		public EnumConstraint(string enumType)
		{
			_validOptions = Cache.GetOrAdd(enumType, key =>
			{
				var type = Type.GetType(key);
				return type != null ? Enum.GetNames(type) : new string[0];
			});
		}

		public bool Match(HttpRequestMessage request, IHttpRoute route, string parameterName, IDictionary<string, object> values, HttpRouteDirection routeDirection)
		{
			object value;
			if (values.TryGetValue(parameterName, out value) && value != null)
			{
				return _validOptions.Contains(value.ToString(), StringComparer.OrdinalIgnoreCase);
			}
			return false;
		}
	}
}