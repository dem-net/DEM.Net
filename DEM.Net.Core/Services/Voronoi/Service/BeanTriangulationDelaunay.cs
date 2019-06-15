using GeoAPI.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core.Voronoi
{
	public class BeanTriangulationDelaunay
	{
		public Dictionary<int, IGeometry> p00_PointIGeometrySources { get; set; }
		public List<BeanArcDelaunay> p01_arcsDelaunay { get; set; }

	public BeanTriangulationDelaunay()
		{
			p00_PointIGeometrySources = new Dictionary<int, IGeometry>();
			p01_arcsDelaunay = new List<BeanArcDelaunay>();
		}

	}
}
