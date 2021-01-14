using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace DEM.Net.Core.Voronoi
{
	/// <summary>
	/// Arc géométriquement composé de l'assemblage de plusieurs arcs transmis
	/// </summary>
	public class BeanTopologieArcComposite
	{
		public int ArcsAssembles_id { get; set; }
		public Geometry ArcsAssembles_Geometry { get; set; }

		public enumModeAssemblageDesArcs TypeReseau { get; set; }

		
		//public Dictionary<int,BeanTopologieArcComposant> DicoDesArcsSources { get; set; }
		public List<BeanTopologieArcComposant> ListeOrdonneeDesArcsSources { get; set; }

		//
		public BeanTopologieArcComposite()
			{
				//DicoDesArcsSources = new Dictionary<int, BeanTopologieArcComposant>();
				ListeOrdonneeDesArcsSources = new List<BeanTopologieArcComposant>();
			}
	}		
}
