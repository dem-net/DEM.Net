
namespace DEM.Net.Core.Voronoi
{
	/// <summary>
	/// Objet décrivant un arc utilisé pour construire un ARC COMPOSITE
	/// et la manière dont il a été utilisé
	/// </summary>
	public class BeanTopologieArcComposant
	{
		public BeanTopologieArc Arc { get; set; }

		public int NoOrdreDepuisAmontDansArcComposite { get; set; }

		public string CodeOrdonneDansReseauComposite { get; set; }

		/// <summary>
		/// L'arc a t-il été utilisé de l'amont vers l'aval
		/// </summary>
		public bool SensNormal_VF { get; set; }
	}
} 
