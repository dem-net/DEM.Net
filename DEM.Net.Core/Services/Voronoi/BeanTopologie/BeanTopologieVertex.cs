using System;
using System.Collections.Generic;

namespace DEM.Net.Core.FortuneVoronoi
{
	public class BeanTopologieVertex: ICloneable
	{
		public string VER_code_XY { get; set; }

		public double VER_CoordX { get; set; }
		public double VER_CoordY { get; set; }

		public List<BeanTopologieArc> VER_ListeArcs { get; set; }

		public BeanTopologieVertex()
		{
			VER_ListeArcs = new List<BeanTopologieArc>();
		}

		public object Clone()
		{
			BeanTopologieVertex v_topologieVertexClone = new BeanTopologieVertex();
			v_topologieVertexClone = (BeanTopologieVertex)this.MemberwiseClone();

			List<BeanTopologieArc> v_ListeBeanTopologieArc = new List<BeanTopologieArc>();
			BeanTopologieArc v_beanTopoArc = new BeanTopologieArc();
			foreach(BeanTopologieArc v_beanTopologieArc in this. VER_ListeArcs)
			{
				v_beanTopoArc = (BeanTopologieArc) v_beanTopologieArc.Clone();
					v_ListeBeanTopologieArc.Add(v_beanTopoArc); 
			}
			v_topologieVertexClone.VER_ListeArcs = v_ListeBeanTopologieArc;
			return v_topologieVertexClone;
		}
	}
}
