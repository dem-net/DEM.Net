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
        public int p00_idArc { get; }
        public string p01_hcodeArc { get; }
        //
        public BeanPoint_internal p11_pointDbt { get; }
        public BeanPoint_internal p12_pointFin { get; }
        //
        public enumStatutArc p20_statutArc { get; set; }
        public BeanFacette_internal p21_facetteGauche { get; set; }
        public BeanFacette_internal p22_facetteDroite { get; set; }

        //(Pour la construction des TINs)
        public List<BeanPoint_internal> p31_pointsAssocies { get; set; }

        //(Pour les services hydro/morpho
        //[Attention: le sens est défini par référence pt début -> pt fin de l'arc
        private enumSensPenteArc p41_sensPenteDeLArc;
        private double? p42_valeurPente;
        private enum_qualificationMorpho_arc p43_qualifMorphoDeLArc;

        public enumSensPenteArc getSensPenteArc()
        {
            if (p41_sensPenteDeLArc == enumSensPenteArc.indetermine)
            {
                if (p11_pointDbt.p10_coord[2] == p12_pointFin.p10_coord[2])
                {
                    p41_sensPenteDeLArc = enumSensPenteArc.flat;
                }
                else
                {
                    if (p11_pointDbt.p10_coord[2] > p12_pointFin.p10_coord[2])
                    {
                        p41_sensPenteDeLArc = enumSensPenteArc.descendant;
                    }
                    else
                    {
                        p41_sensPenteDeLArc = enumSensPenteArc.montant;
                    }
                }
            }
            return p41_sensPenteDeLArc;
        }
        public double getValeurPente()
        {
            if (p42_valeurPente == null)
            {
                p42_valeurPente = FLabServices.createCalculLow().GetPenteFromPoints3D(p11_pointDbt.p10_coord, p12_pointFin.p10_coord);
            }
            return (double)p42_valeurPente;
        }
        public enum_qualificationMorpho_arc getQualifMorphoDeLArc()
        {
            if(p43_qualifMorphoDeLArc== enum_qualificationMorpho_arc.indetermine)
            {
                p43_qualifMorphoDeLArc = FLabServices.createGeomorphoServices().GetQualificationMorphoDeLArc(this);
            }
            return p43_qualifMorphoDeLArc;
        }


        #region CONSTRUCTEURS
        public BeanArc_internal(BeanPoint_internal p_pointDbt, BeanPoint_internal p_pointFin)
        {
            p00_idArc=_dernierIdArc++;
            p11_pointDbt = p_pointDbt;
            p12_pointFin = p_pointFin;
            //
            p01_hcodeArc = FLabServices.createUtilitaires().GethCodeGeogSegment(p11_pointDbt.p10_coord, p12_pointFin.p10_coord);

            //
            p41_sensPenteDeLArc = enumSensPenteArc.indetermine;
            p42_valeurPente = null;
            //
            p43_qualifMorphoDeLArc = enum_qualificationMorpho_arc.indetermine;
        }
        public BeanArc_internal(BeanPoint_internal p_pointDbt, BeanPoint_internal p_pointFin, List<BeanPoint_internal> p_pointsAssocies):this(p_pointDbt, p_pointFin)
        {
            p31_pointsAssocies = p_pointsAssocies;
        }
        #endregion CONSTRUCTEURS
    }
}
