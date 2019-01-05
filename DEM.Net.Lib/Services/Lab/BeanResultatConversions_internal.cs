using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Services.Lab
{
    public class BeanResultatConversions_internal
    {
        public bool p00_modif_vf { get; set; }
        public List<BeanFacette_internal> p01_newFacettes { get; set; }
        public List<BeanArc_internal> p02_arcsCandidatsOut { get; set; }
        public List<BeanArc_internal> p03_arcsAExclureOut { get; set; }

        public BeanResultatConversions_internal()
        {
            p01_newFacettes = new List<BeanFacette_internal>();
            p02_arcsCandidatsOut = new List<BeanArc_internal>();
            p03_arcsAExclureOut = new List<BeanArc_internal>();
        }
    }
}
