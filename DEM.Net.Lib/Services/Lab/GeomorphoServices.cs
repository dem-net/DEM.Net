using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Services.Lab
{
    public class GeomorphoServices : IGeomorphoServices
    {
        public enum_qualificationMorpho_arc GetQualificationMorphoDeLArc(BeanArc_internal p_arc)
        {
            enum_qualificationMorpho_arc v_result;
            try
            {
                //L'arc sépare 2 facettes (sauf en frontière)
                //On va exprimer les 2 points n'appartenant pas à l'arc dans le plan de plus forte pente 
                //Si les 2 points sont 'au-dessus' alors on considère que l'arc est 'talweg'
                //Si les 2 points sont 'au-dessous' alors on considère que l'arc est 'ligne de crète'
                //Les autres cas correspondent, au plus, à des ruptures de pente

                //-On détermine le plan.
                //Ce plan doit être tel que:
                //-sa pente est celle de l'arc
                //-si on translate le vecteur correspondant selon un vecteur de même élévation (=selon la courbe de niveau),
                //alors ce vecteur doit être inclus dans ce plan.
                //Du coup, pour déterminer ce plan, on détermine on utilise le vecteur de l'arc et un vecteur normal en xy à ce vecteur
                double[] v_vecteurArc = FLabServices.createCalculLow().GetVectorBrutFromTwoPoints(p_arc.p11_pointDbt.p10_coord, p_arc.p12_pointFin.p10_coord);
                double[] v_vecteurNormalXy = new double[3] { -1 * v_vecteurArc[1], v_vecteurArc[0], 0 };
                double[] v_pointNormal = new double[3] { v_vecteurNormalXy[0] + p_arc.p11_pointDbt.p10_coord[0], v_vecteurNormalXy[1] + p_arc.p11_pointDbt.p10_coord[1], p_arc.p11_pointDbt.p10_coord[2] };

                double[] v_vecteurNormalAuPlanDePenteMaxi;
                bool p_exceptionSiNotAPlanElseVecteur000_vf = false;
                v_vecteurNormalAuPlanDePenteMaxi = FLabServices.createCalculLow().GetNormaleDuPlan(p_arc.p11_pointDbt.p10_coord, p_arc.p12_pointFin.p10_coord, v_pointNormal, p_exceptionSiNotAPlanElseVecteur000_vf);

                //=>On calcule la matrice inverse...
                v_vecteurArc = FLabServices.createCalculLow().GetNormalisationVecteurXYZ(v_vecteurArc);
                v_vecteurNormalXy = FLabServices.createCalculLow().GetNormalisationVecteurXYZ(v_vecteurNormalXy);
                double[,] v_matriceDeRotation = FLabServices.createCalculLow().GetMatriceInverse3x3(v_vecteurArc, v_vecteurNormalXy, v_vecteurNormalAuPlanDePenteMaxi);

                //...on l'applique sur les 2 points
                List<double[]> v_coordonnees = new List<double[]>();
                double[] v_coordDansLeRepereDuPlan;
                BeanPoint_internal v_pointATester;
                double[] v_vectorATester;
                if (p_arc.p21_facetteGauche != null)
                {
                    v_pointATester = p_arc.p21_facetteGauche.p01_pointsDeFacette.Where(c => c.p00_id != p_arc.p11_pointDbt.p00_id && c.p00_id != p_arc.p12_pointFin.p00_id).First();
                    v_vectorATester = FLabServices.createCalculLow().GetVectorBrutFromTwoPoints(p_arc.p11_pointDbt.p10_coord, v_pointATester.p10_coord);
                    v_coordDansLeRepereDuPlan = FLabServices.createCalculLow().GetProduitMatriceParVector(v_matriceDeRotation, v_vectorATester);
                    v_coordonnees.Add(v_coordDansLeRepereDuPlan);
                }
                if (p_arc.p22_facetteDroite != null)
                {
                    v_pointATester = p_arc.p22_facetteDroite.p01_pointsDeFacette.Where(c => c.p00_id != p_arc.p11_pointDbt.p00_id && c.p00_id != p_arc.p12_pointFin.p00_id).First();
                    v_vectorATester = FLabServices.createCalculLow().GetVectorBrutFromTwoPoints(p_arc.p11_pointDbt.p10_coord, v_pointATester.p10_coord);
                    v_coordDansLeRepereDuPlan = FLabServices.createCalculLow().GetProduitMatriceParVector(v_matriceDeRotation, v_vectorATester);
                    v_coordonnees.Add(v_coordDansLeRepereDuPlan);
                }
                //On exploite l'info:
                //(pour l'instant, on ne traite pas les arcs frontières)
               
                if (v_coordonnees.Where(c => c[2] > 0).Count() > 1)
                {
                    v_result = enum_qualificationMorpho_arc.talweg;
                    return v_result;
                }
                if (v_coordonnees.Where(c => c[2] < 0).Count() > 1)
                {
                    v_result = enum_qualificationMorpho_arc.crete;
                    return v_result;
                }
                return enum_qualificationMorpho_arc.autre; ;
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
