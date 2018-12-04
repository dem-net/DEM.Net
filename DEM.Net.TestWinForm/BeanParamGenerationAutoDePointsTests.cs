using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.TestWinForm
{


    public class BeanParamGenerationAutoDePointsTests
    {
        public enumMethodeGenerationPtsEnXetY p01_modeGenerationXY { get; set; }
        public enumMethodeGenerationValeursEnZ p02_modeGenerationEnZ { get; set; }
        public int p10_srid { get; set; }
        public double p11_pointBasGaucheX { get; set; }
        public double p12_pointBasGaucheY { get; set; }
        public double p13_pointHautDroitX { get; set; }
        public double p14_pointHautDroitY { get; set; }
        //
        public int p31_nbrePoints { get; set; }
        public int p32_seed { get; set; }
        public double p32_pasEntrePointsEnM { get; set; }
        //
        public double p41_recalageMinX { get; set; }
        public double p42_recalageMaxX { get; set; }
        public double p43_recalageMinY { get; set; }
        public double p44_recalageMaxY { get; set; }
        //
        public double p51_hauteurRefEnM { get; set; }
        public double p52_coeff_X { get; set; }
        public double p53_coeff_Y { get; set; }
    }
}
