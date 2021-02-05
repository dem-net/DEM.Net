using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core.Voronoi
{
	public class BeanArcDelaunay
	{
		public string p00_codeArcDelaunay { get; set; }
		public int p11_idPoint1 { get; set; }
		public int p12_idPoint2 { get; set; }
		public double[] p21_coordPoint1 { get; set; }
		public double[] p22_coordPoint2 { get; set; }

		public Geometry p30_arcDelaunay { get; set; }

		public BeanArcDelaunay()
		{
			p21_coordPoint1 = new double[2];
			p22_coordPoint2 = new double[2];
		}

	}
}
