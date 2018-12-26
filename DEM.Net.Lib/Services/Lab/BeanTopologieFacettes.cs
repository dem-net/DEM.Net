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
        public Dictionary<string, BeanArc_internal> p12_arcsByCode { get; set; }
        public Dictionary<int, BeanFacette_internal> p13_facettesById { get; set; }
        
        public BeanTopologieFacettes()
        {
            p11_pointsFacettes = new List<BeanPoint_internal>();
            p12_arcsByCode = new Dictionary<string, BeanArc_internal>();
            p13_facettesById = new Dictionary<int, BeanFacette_internal>();
        }
        public BeanTopologieFacettes(List<BeanPoint_internal> v_pointsSources):this()
        {
            p00_pointsSources = v_pointsSources;
        }

    }
}
