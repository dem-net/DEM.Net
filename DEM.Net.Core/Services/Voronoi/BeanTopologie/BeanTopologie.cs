using System;
using System.Collections.Generic;

namespace DEM.Net.Core.Voronoi
{
	public class BeanTopologie: ICloneable
	{
		public Dictionary<int, BeanTopologieArc> BT_ListeArcs { get; set; }
		public Dictionary<string, BeanTopologieVertex> BT_ListeVertex { get; set; }
		public Dictionary<int, BeanTopologieIlot> BT_ListeIlots { get; set; }

		public BeanTopologie()
		{
			BT_ListeArcs = new Dictionary<int, BeanTopologieArc>();
			BT_ListeVertex = new Dictionary<string, BeanTopologieVertex>();
			BT_ListeIlots = new Dictionary<int, BeanTopologieIlot>();
		}

		public object Clone()
		{
			BeanTopologie v_beanTopologieClone = new BeanTopologie();
			v_beanTopologieClone = (BeanTopologie)this.MemberwiseClone();

			//MODIF 2016 01 11 =>ATTENTION
			//J'ai rajouté un clone sur chaque topologie objet suite à un pb de suppression direct dans la liste des vertex.
			bool v_cloneTopologieObjet = true;
			//
			Dictionary<int, BeanTopologieArc> v_NewDicoArc= new Dictionary<int,BeanTopologieArc>();
			BeanTopologieArc v_arcClone;
			foreach (KeyValuePair<int, BeanTopologieArc> v_BeanArc in this.BT_ListeArcs)
			{
				if (v_cloneTopologieObjet)
				{
					v_arcClone = (BeanTopologieArc)v_BeanArc.Value.Clone();
					v_NewDicoArc.Add(v_BeanArc.Key, v_arcClone);
				}
				else
				{ v_NewDicoArc.Add(v_BeanArc.Key, v_BeanArc.Value); }
			}
			v_beanTopologieClone.BT_ListeArcs = v_NewDicoArc;

			//
			Dictionary<string, BeanTopologieVertex> v_NewDicoVertex = new Dictionary<string, BeanTopologieVertex>();
			BeanTopologieVertex v_vertexClone;
			foreach (KeyValuePair<string, BeanTopologieVertex> v_BeanVertex in this.BT_ListeVertex)
			{
				if (v_cloneTopologieObjet)
				{
					v_vertexClone = (BeanTopologieVertex)v_BeanVertex.Value.Clone();
					v_NewDicoVertex.Add(v_BeanVertex.Key, v_vertexClone);
				}
				else
				{
					v_NewDicoVertex.Add(v_BeanVertex.Key, v_BeanVertex.Value);
				}
			}
			v_beanTopologieClone.BT_ListeVertex = v_NewDicoVertex;

			//
			Dictionary<int, BeanTopologieIlot> v_NewDicoIlots = new Dictionary<int, BeanTopologieIlot>();
			//ATTENTION: clone ilot non disponible
			foreach (KeyValuePair<int, BeanTopologieIlot> v_BeanIlot in this.BT_ListeIlots)
			{
					v_NewDicoIlots.Add(v_BeanIlot.Key, v_BeanIlot.Value);
			}
			v_beanTopologieClone.BT_ListeIlots = v_NewDicoIlots;

			return v_beanTopologieClone;
		}
	}
}
