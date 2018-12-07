using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Services.Lab
{
    public class BeanTopologieFacettes
    {
        public List<BeanPoint_internal> p00_pointsSources { get; set; }
        public List<BeanPoint_internal> p11_pointsFacettes { get; set; }
        public List<BeanArc_internal> p12_arcs { get; set; }
        public List<BeanFacette_internal> p13_facettes { get; set; }
        public BeanTopologieFacettes(List<BeanPoint_internal> v_pointsSources)
        {
            p00_pointsSources = v_pointsSources;
            //
            p11_pointsFacettes = new List<BeanPoint_internal>();
            p12_arcs = new List<BeanArc_internal>();
            p13_facettes = new List<BeanFacette_internal>();
        }
    }
}
