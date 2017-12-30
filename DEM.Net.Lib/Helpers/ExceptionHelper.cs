using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib
{
	public static class ExceptionHelper
	{
		public static Exception GetInnerMostException(this AggregateException ex)
		{
			Exception e = ex;
			while (e.InnerException != null)
			{
				e = e.InnerException;
			}
			return e;
		}
	}
}
