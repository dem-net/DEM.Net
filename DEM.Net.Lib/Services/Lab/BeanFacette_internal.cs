using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Services.Lab
{
    public class BeanFacette_internal
    {
        private static int _dernierId = 0;
        //
        public int p00_idFacette { get; }
        //
        public List<BeanPoint_internal> p01_pointsDeFacette { get; set; }
        public List<BeanArc_internal> p02_arcs { get; set; }
        //
        public List<BeanPoint_internal> p10_pointsInclus { get; set; }
        //  
        public double p21_plusGrandEcartAbsolu { get; set; }
        public BeanPoint_internal p22_pointPlusGrandEcart { get; set; }
        //
        public BeanFacette_internal p23_facetteEcartSup { get; set; }
        public BeanFacette_internal p24_facetteEcartInf { get; set; }

        #region PROPRIETES PRIVEES
        private double[] p31_normaleDuPlan;
        private double[] p32_vecteurPenteMaxi;
        #endregion PROPRIETES PRIVEES

        #region METHODES
        public double[] getNormaleDuPlan()
        {
           if(p31_normaleDuPlan==null)
            {
                p31_normaleDuPlan= FLabServices.createCalculLow().GetNormaleDuPlan(p01_pointsDeFacette[0].p10_coord, p01_pointsDeFacette[2].p10_coord, p01_pointsDeFacette[1].p10_coord);
            }
            return p31_normaleDuPlan;
        }
        public double[] getVecteurPenteMaxi()
        {
            if(p32_vecteurPenteMaxi==null)
            {
                bool v_normaliser_vf = true;
                p32_vecteurPenteMaxi=FLabServices.createCalculLow().GetVecteurPenteMaxi(getNormaleDuPlan(), v_normaliser_vf);
            }
            return p32_vecteurPenteMaxi;
        }
        #endregion METHODES

        #region CONSTRUCTEURS
        public BeanFacette_internal()
        {
            p00_idFacette= _dernierId++;
            p01_pointsDeFacette = new List<BeanPoint_internal>();
            p02_arcs = new List<BeanArc_internal>();
            p10_pointsInclus = new List<BeanPoint_internal>();
            //
            p31_normaleDuPlan = null;
            p32_vecteurPenteMaxi = null;
        }
        #endregion CONSTRUCTEURS
    }
}
