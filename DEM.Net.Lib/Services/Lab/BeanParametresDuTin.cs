using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Services.Lab
{
    public class BeanParametresDuTin
    {
        public enumModeDelimitationFrontiere p11_initialisation_determinationFrontieres { get; set; }
        public double p12_extensionSupplementaireMboEnM { get; set; }
        public enumModeCalculZ p13_modeCalculZParDefaut { get; set; }
        public double p14_altitudeParDefaut { get; set; }
        public BeanParametresChoixDuPointCentral p15_initialisation_modeChoixDuPointCentral { get; set; }
        public BeanParametresChoixDuPointCentral p21_enrichissement_modeChoixDuPointCentral { get; set; }

        public int p31_nbreIterationsMaxi { get; set; }

        public BeanParametresDuTin()
         {
            p15_initialisation_modeChoixDuPointCentral = new BeanParametresChoixDuPointCentral();
            p21_enrichissement_modeChoixDuPointCentral = new BeanParametresChoixDuPointCentral();
        }
    }
}
