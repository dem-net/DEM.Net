using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Services.Lab
{
    /// <summary>
    /// Décrit un point support de facette dans le cadre spécifique de cette facette
    /// </summary>
    public class BeanRelationPointToFacette
    {
        //=>Le point décrit:
       public BeanPoint_internal p11_pointRef { get; set; }
        //=>...dans le cadre de la facette:
       public BeanFacette_internal p12_facetteRef { get; set; }
        
       //=>On veut décrire si le point, DANS le CADRE de la FACETTE, est potentiellement:
       //'alimenté en eau', 'exutoire de facette', ou 'point de transit d'eau'
       public enum_qualificationHydro_pointDeFacette p21_qualifPointDansFacette { get; set; }

        //=>On va associer, s'IL y en a un inclus dans la facette, le vecteur de plus forte pente 'sortant'
        //(L'idée est décrire le trajet d'une goutte d'eau depuis ce point)
        //Même chose pour le vecteur de plus forte pente 'entrant'
        public double[] p31_vecteurPenteMaxiSurFacette_sortant { get; set; }
        public double[] p32_vecteurPenteMaxiSurFacette_entrant { get; set; }
        //
        public BeanRelationPointToFacette(BeanPoint_internal p_pointFacette, BeanFacette_internal p_facette)
        {
            p11_pointRef = p_pointFacette;
            p12_facetteRef = p_facette;
            //
            p21_qualifPointDansFacette = enum_qualificationHydro_pointDeFacette.indetermine;
            //[ces 2 vecteurs sont amenés à rester 'nuls' dans bcp de cas=>il ne faut pas les initialiser en double[3]
            
            p31_vecteurPenteMaxiSurFacette_sortant = null;
            p32_vecteurPenteMaxiSurFacette_entrant = null;
        }
    }

}
