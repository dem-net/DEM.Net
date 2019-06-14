using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spir.Commun.Service.Technical.Cartographie.Service.CourbesNiveau
{
	public class BeanParametresCalculCourbesNiveau
	{
		public enumModeCalculCourbe p00_modeCalculCourbes { get; set; }
		//
		public enumModeDeduplicationPoints p11_modaliteDeDeduplicationGeomDesPoints { get; set; }

		/// <summary>
		/// Attention: le setteur doit modifier le nbre décimales d'arrondi DuCodeVertex
		/// </summary>
		public double p12_pasDeDepublicationDesPointsEnM { get; internal set; }

		public enumModeAgregationDesPoints p13_modeAgregationDesPoints { get; set; }

		/// <summary>
		/// ATTENTION: il faut adapter cette valeur au pas de duplication: le niveau de précision doit être supérieur au pas de duplication 
		/// </summary>
		public int p14_nbreDecimalesDArrondiPourCodeVertex { get; internal set; }
		//
		public double p21_ecartEntreCourbes { get; set; }
		public double p22_valeurDeLaCourbe0 { get; set; }
	}
}
