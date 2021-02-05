using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;

namespace DEM.Net.Core.Voronoi
{
	public class BeanTopologieIlot 
	{
		public int IlotId { get; set; }
		public Geometry IlotGeometry { get; set; }
		public List<BeanTopologieArc> IlotArcsBordiers { get; set; }
		public List<BeanTopologieArc> IlotArcsInclus { get; set; }
		public enumQualiteContoursIlot IlotQualificationContours { get; set; }

		public bool vf_noModifie { get; set; }
		
		public BeanTopologieIlot()
		{
			IlotArcsBordiers = new List<BeanTopologieArc>();
			IlotArcsInclus = new List<BeanTopologieArc>();

			vf_noModifie = false;
		}

		//public object Clone()
		//{
		//	BeanTopologieIlot v_beanTopologieIlot = (BeanTopologieIlot)this.MemberwiseClone();

		//	foreach (BeanTopologieArc v_beanArcsBordiers in IlotArcsBordiers)
		//	{
		//		v_beanTopologieIlot.IlotArcsBordiers.Add((BeanTopologieArc)v_beanArcsBordiers.Clone());
		//	}
		//	foreach (BeanTopologieArc v_beanArcsInclu in IlotArcsInclus)
		//	{
		//		v_beanTopologieIlot.IlotArcsInclus.Add((BeanTopologieArc)v_beanArcsInclu.Clone());
		//	}
			
		//	return v_beanTopologieIlot;
		//}
	}
}
