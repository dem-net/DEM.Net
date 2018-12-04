using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Services.Lab
{
    public class CalculServices_Low : ICalculServices_Low
    {
        public double[] GetVectorFromTwoPoints(double[] p_pointOrigine, double[] p_point2, bool p_normaliser_vf)
        {
            double[] v_vector = new double[p_pointOrigine.Count()];
            try
            {
               AreVectorsSameDimension(p_pointOrigine, p_point2, true);
                for (int v_ind = 0; v_ind < p_pointOrigine.Count(); v_ind++)
                {
                    v_vector[v_ind] = p_point2[v_ind] - p_pointOrigine[v_ind];
                }
                //
                if (p_normaliser_vf)
                {
                    v_vector = GetNormalisationVecteur(v_vector);
                }
            }
            catch (Exception)
            {

                throw;
            }
            return v_vector;
        }
        //
        public double GetNormeVecteur(double[] p_vector)
        {
            double v_sommeDesCarres = 0;
            try
            {
                for (int v_ind = 0; v_ind < p_vector.Count(); v_ind++)
                {
                    v_sommeDesCarres += (p_vector[v_ind] * p_vector[v_ind]);
                }
                return Math.Sqrt(v_sommeDesCarres);
            }
            catch (Exception)
            {
                throw;
            }
        }
        public double[] GetNormalisationVecteur(double[] p_vector)
        {
            double[] p_vectorN = new double[2];
            try
            {
                double v_norme = GetNormeVecteur(p_vector);
                if (v_norme > 0)
                {
                    for (int v_ind = 0; v_ind < p_vector.Count(); v_ind++)
                    {
                        p_vectorN[v_ind] /= v_norme;
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
            return p_vectorN;
        }
        //
        public double GetProduitScalaire(double[] p_vector1, double[] p_vector2)
        {
            double v_produit;
            try
            {
                AreVectorsSameDimension(p_vector1, p_vector2, true);
                v_produit = 0;
                for (int v_index = 0; v_index < p_vector1.Count(); v_index++)
                {
                    v_produit += p_vector1[v_index] * p_vector2[v_index];
                }
            }
            catch (Exception)
            {

                throw;
            }
            return v_produit;
        }
        public bool AreVectorsSameDimension(double[] p_vector1, double[] p_vector2, bool p_exceptionSiFalse)
        {
            bool v_memeDimension = (p_vector1.Count() == p_vector2.Count());
            if(!v_memeDimension && p_exceptionSiFalse)
            {
                throw new Exception("Les deux vecteurs ne sont pas de même dimension.");
            }
            return v_memeDimension;
        }
        public bool AreVecteursColineaires(double[] p_vector1, double[] p_vector2)
        {
            try
            {
                AreVectorsSameDimension(p_vector1, p_vector2, true);
                //(Moins coûteux comme cela si dimension 2)
                if(p_vector1.Count()==2)
                {
                    return ((p_vector1[0] * p_vector2[1]) - (p_vector1[1] * p_vector2[0]) == 0);
                }
                //(Produit scalaire=produit des normes)
                return (GetProduitScalaire(p_vector1, p_vector2) == GetNormeVecteur(p_vector1) * GetNormeVecteur(p_vector2));
            }
            catch (Exception)
            {
                throw;
            }
          }
        //
        public double[,] GetInverseMatrice2_2(double[,] p_matriceToInverse)
        {
            double[,] v_matriceInverse = new double[2,2];
            try
            {
                double v_delta = (p_matriceToInverse[0,0] * p_matriceToInverse[1,1]) - (p_matriceToInverse[1,0] * p_matriceToInverse[0,1]);
                v_matriceInverse[0,0] = p_matriceToInverse[1,1]/ v_delta; //a
                v_matriceInverse[0, 1] = p_matriceToInverse[0, 1] * -1 / v_delta; //b
                v_matriceInverse[1, 0] = p_matriceToInverse[1, 0] * -1 / v_delta; //c
                v_matriceInverse[1, 1] = p_matriceToInverse[0, 0] / v_delta; //d
            }
            catch (Exception)
            {

                throw;
            }
            return v_matriceInverse;
        }
        public double[] GetProduitMatriceParVector(double[,] p_matrice, double[] p_vector)
        {
            double[] v_vectorOut = new double[p_vector.Count()];
            try
            {
                for (int v_indiceLigne = 0; v_indiceLigne < p_vector.Count(); v_indiceLigne++)
                {
                    v_vectorOut[v_indiceLigne] = 0;
                    for (int v_indiceColonne = 0; v_indiceColonne < p_vector.Count(); v_indiceColonne++)
                    {
                        v_vectorOut[v_indiceLigne] += p_matrice[v_indiceLigne, v_indiceColonne] * p_vector[v_indiceColonne];
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
            return v_vectorOut;
        }
        public double[] GetInverseVector2(double[,] p_matrice, double[] p_vectorToInverse)
        {
            double[] v_vectorInverse = new double[2];
            try
            {
                double[,] v_matriceInverse = GetInverseMatrice2_2(p_matrice);
                v_vectorInverse = GetProduitMatriceParVector(v_matriceInverse, p_vectorToInverse);
            }
            catch (Exception)
            {
                throw;
            }
            return v_vectorInverse;
        }
        //
        public double[] GetCoeffDroite2D(double[] p_Droite1_pt1, double[] p_Droite1_pt2)
        {
            double[] v_coeff = new double[2];
            try
            {
                double[] v_vectorPt1 = GetVectorFromTwoPoints(p_Droite1_pt1, p_Droite1_pt2, false);
                double[] v_vectorPt2 = GetVectorFromTwoPoints(p_Droite1_pt2, p_Droite1_pt2, false);
                //
                double v_delta = v_vectorPt1[0] - v_vectorPt2[0];
                if (v_delta == 0)
                {
                    return v_coeff;
                }
                //
                double v_a = (v_vectorPt1[1] - v_vectorPt2[1]) / v_delta;
                double v_b = ((v_vectorPt1[0] * v_vectorPt2[1]) - (v_vectorPt2[0] * v_vectorPt1[1])) / v_delta;
                v_coeff[0] = v_a;
                v_coeff[1] = v_b;
            }
            catch (Exception)
            {

                throw;
            }
            return v_coeff;
        }
        public double[] GetIntersectionDroites2D(double[] p_coeffDroite1, double[] p_coeffDroite2)
        {
            double[] v_coord = null;
            try
            {
                double v_delta = p_coeffDroite2[0] - p_coeffDroite1[0];
                if (v_delta == 0)
                {
                    return null;
                }
                double v_x = (p_coeffDroite1[1] - p_coeffDroite2[1]) / v_delta;
                double v_y = ((p_coeffDroite2[0] * p_coeffDroite1[1]) - (p_coeffDroite1[0] * p_coeffDroite2[1])) / v_delta;
                v_coord = new double[2];
                v_coord[0] = v_x;
                v_coord[1] = v_y;
            }
            catch (Exception)
            {

                throw;
            }
            return v_coord;
        }
        public double[] GetIntersectionDroites2D(double[] p_Droite1_pt1, double[] p_Droite1_pt2, double[] p_Droite2_pt1, double[] p_Droite2_pt2)
        {
            double[] v_coord = null;
            try
            {
                double[] p_coeffDroite1 = GetCoeffDroite2D(p_Droite1_pt1, p_Droite1_pt2);
                double[] p_coeffDroite2 = GetIntersectionDroites2D(p_Droite2_pt1, p_Droite2_pt2);
                v_coord = GetIntersectionDroites2D(p_coeffDroite1, p_coeffDroite2);
            }
            catch (Exception)
            {

                throw;
            }
            return v_coord;
        }
        //
        public double[] GetNormaleDuPlan(double[] p_point3D_1, double[] p_point3D_2, double[] p_point3D_3)
        {
            double[] v_normale = new double[3];
            try
            {
                if(p_point3D_1.Count()!=3  ||  p_point3D_2.Count()!=3 || p_point3D_3.Count()!=3)
                {
                    throw new Exception("La dimension des vecteurs n'est pas conforme.");
                }
                double[] u = GetVectorFromTwoPoints(p_point3D_1, p_point3D_2,false);
                double[] w = GetVectorFromTwoPoints(p_point3D_1, p_point3D_3, false);
                //Contrôle de colinéarité dans le repère xy
               if ((u[0] * w[1]) - (u[1] * w[0]) == 0)
                {
                    throw new Exception("Les points ne doivent pas être alignés.");
                }
                double B = ((u[0] * w[2]) - (w[0] * u[2])) / ((w[0] * u[1]) - (u[0] * w[1]));
                double c = 1;
                double b = c * B;
                double a;
                if(u[0]!=0)
                {
                    a = (-1) * c * ((B * u[1]) + u[2]) / u[0];
                }
                else
                {
                    a = (-1) * c * ((B * w[1]) + w[2]) / w[0];
                }
                v_normale[0] = a;
                v_normale[1] = b;
                v_normale[2] = c;
                //
                v_normale=GetNormalisationVecteur(v_normale);
            }
            catch (Exception)
            {

                throw;
            }
            return v_normale;
        }
        public double[] GetEquationDuPlan(double[] p_normaleAuPlan, double[] p_point3DDuPlan)
        {
            double[] v_coeff = new double[3];
            try
            {
                v_coeff[0] = p_normaleAuPlan[0];
                v_coeff[1] = p_normaleAuPlan[1];
                v_coeff[2] = p_normaleAuPlan[2];
                v_coeff[3]=(-1) * GetProduitScalaire(p_normaleAuPlan, p_point3DDuPlan);
            }
            catch (Exception)
            {
                throw;
            }
            return v_coeff;
        }
        public double[] GetEquationDuPlan(double[] p_point3D_1, double[] p_point3D_2, double[] p_point3D_3)
        {
            double[] v_coeff = null;
            try
            {
                double[] v_normale = GetNormaleDuPlan(p_point3D_1, p_point3D_2, p_point3D_3);
                v_coeff = GetEquationDuPlan(v_normale, p_point3D_1);
            }
            catch (Exception)
            {
                throw;
            }
            return v_coeff;
        }
        //
        public double[,] GetMatriceChangementDeRepere2D(double[] p_coordPoint0, double[] p_coordPoint2Abs, double[] p_coordPoint3Ord_orthoSiNull=null, bool p_normaliser_vf=true)
        {
            double[,] v_matriceLigne = new double[2, 2];
            try
            {
                double[] v_vecteur_e1 = GetVectorFromTwoPoints(p_coordPoint0, p_coordPoint2Abs, p_normaliser_vf);
                double[] v_vecteur_e2;
                if(p_coordPoint3Ord_orthoSiNull==null) //Si le second vecteur n'est pas proposé=>on créé un vecteur orthogonal de même norme.
                {
                    v_vecteur_e2 = new double[2] { (-1) * v_vecteur_e1[1], v_vecteur_e1[0] };
                }
                else
                {
                    v_vecteur_e2 = GetVectorFromTwoPoints(p_coordPoint0, p_coordPoint3Ord_orthoSiNull, p_normaliser_vf);
                }
                //
                double v_delta = (v_vecteur_e1[0] * v_vecteur_e2[1]) - (v_vecteur_e1[1] * v_vecteur_e2[0]);
                v_matriceLigne[0, 0] = v_vecteur_e2[1] / v_delta;
                v_matriceLigne[0, 1] = v_vecteur_e1[0] * -1 / v_delta;
                v_matriceLigne[1, 0] = v_vecteur_e2[1] * -1 / v_delta;
                v_matriceLigne[1, 1] = v_vecteur_e1[0] / v_delta;
            }
            catch (Exception)
            {

                throw;
            }
            return v_matriceLigne;
        }
        public double[] GetCoordDansNewRepere2D(double[,] p_matriceNewRepere, double[] p_pointToTranslate)
        {
            double[] v_coord = new double[2];
            try
            {
                v_coord[0] = (p_pointToTranslate[0] * p_matriceNewRepere[0, 0]) + (p_pointToTranslate[1] * p_matriceNewRepere[1, 0]);
                v_coord[1] = (p_pointToTranslate[0] * p_matriceNewRepere[0, 1]) + (p_pointToTranslate[1] * p_matriceNewRepere[1, 1]);
            }
            catch (Exception)
            {

                throw;
            }
            return v_coord;
        }
        public double[] GetCoordDansNewRepere2D(double[] p_pointsAReferencer, double[] p_coordPoint0, double[] p_coordPoint2Abs, double[] p_coordPoint3Ord_orthoSiNull = null)
        {
            double[] v_coord = null;
            try
            {
                ICalculServices_Low v_calcul = new CalculServices_Low();
                bool v_normaliser_vf = true;
                double[,] v_matriceDeConversion;
                v_matriceDeConversion = v_calcul.GetMatriceChangementDeRepere2D(p_coordPoint0, p_coordPoint2Abs, p_coordPoint3Ord_orthoSiNull, v_normaliser_vf);
                //
                v_coord = v_calcul.GetCoordDansNewRepere2D(v_matriceDeConversion, p_pointsAReferencer);
            }
            catch (Exception)
            {

                throw;
            }
            return v_coord;
        }
        //
        public double GetDistanceEuclidienneCarree2D(double[] v_point1, double[] v_point2)
        {
         return (v_point2[0] - v_point1[0]) * (v_point2[0] - v_point1[0]) + (v_point2[1] - v_point1[1]) * (v_point2[1] - v_point1[1]);  
        }
        public double GetDistanceEuclidienne2D(double[] v_point1, double[] v_point2)
        {
            return Math.Sqrt(GetDistanceEuclidienneCarree2D(v_point1, v_point2));
        }
        //
    }
}
