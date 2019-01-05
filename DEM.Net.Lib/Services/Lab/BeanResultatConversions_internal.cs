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
        public List<int> p01_idFacettesSupprimees { get; set; }
        public List<BeanFacette_internal> p02_newFacettes { get; set; }
        public List<BeanArc_internal> p03_arcsCandidatsOut { get; set; }
        public List<BeanArc_internal> p04_arcsAExclureOut { get; set; }

        public BeanResultatConversions_internal()
        {
            p01_idFacettesSupprimees = new List<int>();
            p02_newFacettes = new List<BeanFacette_internal>();
            p03_arcsCandidatsOut = new List<BeanArc_internal>();
            p04_arcsAExclureOut = new List<BeanArc_internal>();
        }
    }
}
