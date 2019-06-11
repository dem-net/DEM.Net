using DEM.Net.Core.Services.Lab;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spir.Commun.Service.Technical.Cartographie.Service.CourbesNiveau
{
	public class BeanCourbes
	{
		public List<BeanPoint_internal> p00_pointsAlti { get; set; }
		public BeanParametresCalculCourbesNiveau p01_parametresCalculDesCourbes { get; set; }
		//
        public Dictionary<int, string> p10_labelClassesParIndexClasse { get; set; }
        public Dictionary<string, int> p11_indexClasseParLabelClasse { get; set; }
        public Dictionary<string, Dictionary<string, BeanFractionCourbe>> p12_tousSegmentsCourbesParNiveau { get; set; }
		public List<BeanArc_internal> p13_arcsPeripherieToClosePolygones { get; set; }

        public Dictionary<string, Dictionary<int, List<double[]>>> p14_courbesAssembleesCoordParNiveau { get; set; }
        public BeanCourbes()
		{
			p00_pointsAlti = new List<BeanPoint_internal>();
            p10_labelClassesParIndexClasse = new Dictionary<int, string>();
            p11_indexClasseParLabelClasse = new Dictionary<string, int>();
            p12_tousSegmentsCourbesParNiveau = new Dictionary<string, Dictionary<string, BeanFractionCourbe>>();
			p13_arcsPeripherieToClosePolygones = new List<BeanArc_internal>();
            p14_courbesAssembleesCoordParNiveau = new Dictionary<string, Dictionary<int, List<double[]>>>();

        }

	}

}
