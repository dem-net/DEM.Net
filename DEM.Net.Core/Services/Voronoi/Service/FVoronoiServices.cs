using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core.FortuneVoronoi
{
	public class FVoronoiServices
	{
		public static IVoronoiServices createVoronoiServices()
		{
			return new VoronoiServices();
		}
	}
}
