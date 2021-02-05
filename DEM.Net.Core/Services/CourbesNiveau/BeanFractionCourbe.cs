
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Core.CourbesNiveau
{
	public class BeanFractionCourbe
	{
		public string p00_codeFractionCourbe { get; set; }
		public BeanPointDecoup_internal p01_point_1 { get; set; }
		public BeanPointDecoup_internal p02_point_2 { get; set; }
		//public Geometry p10_geom { get; set; }
		public string p20_valeurCourbe { get; set; }
	public bool p11_estLigneSinonPoint_vf { get; set; }
	}
}
