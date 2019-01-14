using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Services.Lab
{
   public class BeanArc_internal
    {
        private static int _dernierIdArc = 0;
        //
        public int p00_idArc { get;  }
        public string p01_hcodeArc { get; }
        //
        public BeanPoint_internal p11_pointDbt { get; }
        public BeanPoint_internal p12_pointFin { get;}
        //
        public enumStatutArc p20_statutArc { get; set; }
        public BeanFacette_internal p21_facetteGauche { get; set; }
        public BeanFacette_internal p22_facetteDroite { get; set; }
        //
        public List<BeanPoint_internal> p31_pointsAssocies { get; set; }
        //
        public enumTypeArcReseau p41_natureArcDansLeReseau { get; set; }
        //
        public BeanArc_internal(BeanPoint_internal p_pointDbt, BeanPoint_internal p_pointFin)
        {
            p00_idArc=_dernierIdArc++;
            p11_pointDbt = p_pointDbt;
            p12_pointFin = p_pointFin;
            //
            p01_hcodeArc = FLabServices.createUtilitaires().GethCodeGeogSegment(p11_pointDbt.p10_coord, p12_pointFin.p10_coord);
            //
            p41_natureArcDansLeReseau = enumTypeArcReseau.indetermine;
        }
        public BeanArc_internal(BeanPoint_internal p_pointDbt, BeanPoint_internal p_pointFin, List<BeanPoint_internal> p_pointsAssocies):this(p_pointDbt, p_pointFin)
        {
            p31_pointsAssocies = p_pointsAssocies;
        }
      
    }
}
