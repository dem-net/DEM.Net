using DEM.Net.Lib.Services.VisualisationServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Services.Lab
{
    public class CalculServices_Low : ICalculServices_Low
    {
        public double[] GetVectorBrutFromTwoPoints(double[] p_pointOrigine, double[] p_point2)
        {
            double[] v_vector = new double[p_pointOrigine.Count()];
            try
            {
               AreVectorsSameDimension(p_pointOrigine, p_point2, true);
                for (int v_ind = 0; v_ind < p_pointOrigine.Count(); v_ind++)
                {
                    v_vector[v_ind] = p_point2[v_ind] - p_pointOrigine[v_ind];
                }
            }
            catch (Exception)
            {

                throw;
            }
            return v_vector;
        }
        //
        public double GetNormeVecteurXY(double[] p_vector)
        {
            return GetNormeVecteur(p_vector, 2);
        }
        public double GetNormeVecteurXYZ(double[] p_vector)
        {
            return GetNormeVecteur(p_vector, 3);
        }
        private double GetNormeVecteur(double[] p_vector, int p_dimension)
        {
            double v_sommeDesCarres = 0;
            try
            {
                for (int v_ind = 0; v_ind < p_dimension; v_ind++)
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
        //
        public double[] GetNormalisationVecteurXY(double[] p_vector)
        {
            return GetNormalisationVecteur(p_vector, 2);
        }
        public double[] GetNormalisationVecteurXYZ(double[] p_vector)
        {
            return GetNormalisationVecteur(p_vector, 3);
        }
        private double[] GetNormalisationVecteur(double[] p_vector, int p_dimension)
        {
            double[] p_vectorN = new double[p_vector.Length];
            try
            {
                double v_norme = GetNormeVecteur(p_vector, p_dimension);
                if (v_norme <= 0) //On ne retourne pas directement un vecteur null.=>la dim demandée peut être inf à la dim réelle+on veut une copie 
                {
                    v_norme = 1;
                }
                    for (int v_ind = 0; v_ind < p_vector.Length; v_ind++)
                    {
                        if(v_ind> p_dimension-1) //(On veut qd même conserver les dimensions supp si la dimension de normalisation est inf à la dim réelle
                        {
                            v_norme = 1;
                        }
                        p_vectorN[v_ind] = p_vector[v_ind] / v_norme;
                    }
                   
            }
            catch (Exception)
            {

                throw;
            }
            return p_vectorN;
        }
        //
        public double GetProduitScalaireXY(double[] p_vector1, double[] p_vector2)
        {
            return GetProduitScalaire(p_vector1, p_vector2, 2);
        }
        public double GetProduitScalaireXYZ(double[] p_vector1, double[] p_vector2)
        {
            return GetProduitScalaire(p_vector1, p_vector2, 3);
        }
        private double GetProduitScalaire(double[] p_vector1, double[] p_vector2, int p_dimension)
        {
            double v_produit;
            try
            {
                v_produit = 0;
                for (int v_index = 0; v_index < p_dimension; v_index++)
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

        public bool AreVecteursColineairesXY(double[] p_vector1, double[] p_vector2)
        {
             return ((p_vector1[0] * p_vector2[1]) - (p_vector1[1] * p_vector2[0]) == 0);
        }
        public bool AreVecteursColineairesXYZ(double[] p_vector1, double[] p_vector2)
        {
            try
            {
                //(Produit scalaire=produit des normes)
                return (GetProduitScalaireXYZ(p_vector1, p_vector2) == GetNormeVecteurXYZ(p_vector1) * GetNormeVecteurXYZ(p_vector2));
            }
            catch (Exception)
            {
                throw;
            }
        }
        //
        public double[,] GetInverseMatrice2x2(double[,] p_matriceToInverse)
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
        
        public double[] GetInverseVector2(double[,] p_matrice, double[] p_vectorToInverse)
        {
            double[] v_vectorInverse = new double[2];
            try
            {
                double[,] v_matriceInverse = GetInverseMatrice2x2(p_matrice);
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
                double[] v_vectorPt1 = GetVectorBrutFromTwoPoints(p_Droite1_pt1, p_Droite1_pt2);
                v_vectorPt1 = GetNormalisationVecteurXY(v_vectorPt1);
                double[] v_vectorPt2 = GetVectorBrutFromTwoPoints(p_Droite1_pt2, p_Droite1_pt2);
                v_vectorPt2 = GetNormalisationVecteurXY(v_vectorPt2);
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
        public bool AreVectorsSameDimension(double[] p_vector1, double[] p_vector2, bool p_exceptionSiFalse)
        {
            bool v_memeDimension = (p_vector1.Count() == p_vector2.Count());
            if (!v_memeDimension && p_exceptionSiFalse)
            {
                throw new Exception("Les deux vecteurs ne sont pas de même dimension.");
            }
            return v_memeDimension;
        }
    
        public double[] GetProduitMatriceParVector(double[,] p_matrice, double[] p_vector)
        {
            double[] v_vectorOut = new double[p_vector.Count()];
            try
            {
                //v_indiceLigne
                for (int v_indiceColonne = 0; v_indiceColonne < p_vector.Count(); v_indiceColonne++)
                {
                    v_vectorOut[v_indiceColonne] = 0;
                    for (int v_indiceLigne = 0; v_indiceLigne < p_vector.Count(); v_indiceLigne++)
                    {
                        v_vectorOut[v_indiceColonne] += p_matrice[v_indiceColonne, v_indiceLigne] * p_vector[v_indiceLigne];
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
            return v_vectorOut;
        }
        public double[,] GetProduitMatricesAxB(double[,] v_matriceA, double[,] v_matriceB)
        {
            double[,] v_produitAxB = null;
            try
            {
                double[] v_vectorIn;
                double[] v_vectorOut;
                int v_nbreLignesColonnes = v_matriceA.GetLength(0);
                v_produitAxB = new double[v_nbreLignesColonnes, v_nbreLignesColonnes];
                for(int v_indexLigne = 0; v_indexLigne < v_nbreLignesColonnes; v_indexLigne++)
                {
                    v_vectorIn = new double[v_nbreLignesColonnes];
                       
                    for (int v_indexColonne = 0; v_indexColonne < v_nbreLignesColonnes; v_indexColonne++)
                    {
                        v_vectorIn[v_indexColonne] = v_matriceB[v_indexColonne, v_indexLigne];
                    }
                    v_vectorOut=GetProduitMatriceParVector(v_matriceA, v_vectorIn);
                    for (int v_indexColonne = 0; v_indexColonne < v_nbreLignesColonnes; v_indexColonne++)
                    {
                        v_produitAxB[v_indexLigne, v_indexColonne] = v_vectorOut[v_indexColonne];
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
            return v_produitAxB;
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
                double[] u = GetVectorBrutFromTwoPoints(p_point3D_1, p_point3D_2);
                u = GetNormalisationVecteurXYZ(u);
                double[] w = GetVectorBrutFromTwoPoints(p_point3D_1, p_point3D_3);
                w = GetNormalisationVecteurXYZ(w);
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
                v_normale=GetNormalisationVecteurXYZ(v_normale);
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
                v_coeff[3]=(-1) * GetProduitScalaireXYZ(p_normaleAuPlan, p_point3DDuPlan);
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
        public double[,] GetMatrice3x3FromCoord(double[] p_coordPointOrigine, double[] p_coordPoint1, double[] p_coordPoint2, double[] p_coordPoint3, bool p_normaliser_vf)
        {
            double[,] v_matrice = new double[3, 3];
            try
            {
                double[] p_vector1 = GetVectorBrutFromTwoPoints(p_coordPointOrigine, p_coordPoint1);
                double[] p_vector2 = GetVectorBrutFromTwoPoints(p_coordPointOrigine, p_coordPoint2);
                double[] p_vector3 = GetVectorBrutFromTwoPoints(p_coordPointOrigine, p_coordPoint3);
                if(p_normaliser_vf)
                {
                    p_vector1 = GetNormalisationVecteurXYZ(p_vector1);
                    p_vector2 = GetNormalisationVecteurXYZ(p_vector2);
                    p_vector3 = GetNormalisationVecteurXYZ(p_vector3);
                }
                v_matrice = GetMatrice3x3FromVectors(p_vector1, p_vector2, p_vector3);
            }
            catch (Exception)
            {

                throw;
            }
            return v_matrice;
        }
        public double[,] GetMatrice3x3FromVectors(double[] p_vector1, double[] p_vector2, double[] p_vector3)
        {
            double[,] v_matrice = new double[3, 3];
            try
            {
                //[colonne puis ligne
                v_matrice[0, 0] = p_vector1[0];
                v_matrice[1, 0] = p_vector1[1];
                v_matrice[2, 0] = p_vector1[2];
                //
                v_matrice[0, 1] = p_vector2[0];
                v_matrice[1, 1] = p_vector2[1];
                v_matrice[2, 1] = p_vector2[2];
                //
                v_matrice[0, 2] = p_vector3[0];
                v_matrice[1, 2] = p_vector3[1];
                v_matrice[2, 2] = p_vector3[2];
            }
            catch (Exception)
            {

                throw;
            }
            return v_matrice;
        }
        public double[,] GetMatriceInverse3x3(double[,] p_matriceAInverser)
        {
            double[,] v_matriceInverse = new double[3, 3];
            try
            {
                double a = p_matriceAInverser[0, 0];
                double b = p_matriceAInverser[1, 0];
                double c = p_matriceAInverser[2, 0];
                double d = p_matriceAInverser[0, 1];
                double e = p_matriceAInverser[1, 1];
                double f = p_matriceAInverser[2, 1];
                double g = p_matriceAInverser[0, 2];
                double h = p_matriceAInverser[1, 2];
                double i = p_matriceAInverser[2, 2];
                //
                v_matriceInverse = GetMatriceInverse3x3(a, b, c, d, e, f, g, h, i);
            }
            catch (Exception)
            {

                throw;
            }
            return v_matriceInverse;
        }
        public double[,] GetMatriceInverse3x3(double[] p_vector1, double[] p_vector2, double[] p_vector3)
        {
            double[,] v_matriceInverse = new double[3, 3];
            try
            {
                double a = p_vector1[0];
                double b = p_vector1[1];
                double c = p_vector1[2];
                //
                double d = p_vector2[0];
                double e = p_vector2[1];
                double f = p_vector2[2];
                //
                double g = p_vector3[0];
                double h = p_vector3[1];
                double i = p_vector3[2];
                //
                v_matriceInverse = GetMatriceInverse3x3(a, b, c, d, e, f, g, h, i);
            }
            catch (Exception)
            {

                throw;
            }
            return v_matriceInverse;
        }
        public double[,] GetMatriceInverse3x3(double p_v1_a, double p_v1_b, double p_v1_c, double p_v2_d, double p_v2_e, double p_v2_f, double p_v3_g, double p_v3_h, double p_v3_i)
        {
            double[,] v_matriceInverse = new double[3, 3];
            try
            {
                double v_determinant;
                v_determinant = (p_v1_a * p_v2_e * p_v3_i) - (p_v1_a * p_v3_h * p_v2_f) + (p_v1_b * p_v2_f * p_v3_g) - (p_v1_b * p_v2_d * p_v3_i) + (p_v1_c * p_v2_d * p_v3_h) - (p_v1_c * p_v2_e * p_v3_g);
               if(v_determinant==0)
                {
                    return null;
                }
                
                //
                v_matriceInverse[0, 0] = ((p_v2_e * p_v3_i) - (p_v2_f * p_v3_h)) / v_determinant;
                v_matriceInverse[1, 0] = ((p_v1_c * p_v3_h) - (p_v1_b * p_v3_i)) / v_determinant;
                v_matriceInverse[2, 0] = ((p_v1_b * p_v2_f) - (p_v1_c * p_v2_e)) / v_determinant;
                //
                v_matriceInverse[0, 1] = ((p_v2_f * p_v3_g) - (p_v2_d * p_v3_i)) / v_determinant;
                v_matriceInverse[1, 1] = ((p_v1_a * p_v3_i) - (p_v1_c * p_v3_g)) / v_determinant;
                v_matriceInverse[2, 1] = ((p_v1_c * p_v2_d) - (p_v1_a * p_v2_f)) / v_determinant;
                //
                v_matriceInverse[0, 2] = ((p_v2_d * p_v3_h) - (p_v2_e * p_v3_g)) / v_determinant;
                v_matriceInverse[1, 2] = ((p_v1_b * p_v3_g) - (p_v1_a * p_v3_h)) / v_determinant;
                v_matriceInverse[2, 2] = ((p_v1_a * p_v2_e) - (p_v1_b * p_v2_d)) / v_determinant;
            }
            catch (Exception)
            {

                throw;
            }
            return v_matriceInverse;
        }
       public bool IsInversionMatriceOk(double[,] p_matriceAInverser, double[,] p_matriceInverse, int p_toleranceDArrondi)
        {
            bool v_isInversionOk = false;
            try
            {
                double[,] v_matriceIdentiteAttendue=GetProduitMatricesAxB(p_matriceAInverser, p_matriceInverse);
                v_isInversionOk = IsMatriceIdentite(v_matriceIdentiteAttendue, p_toleranceDArrondi);
            }
            catch (Exception)
            {

                throw;
            }
            return v_isInversionOk;
        }
   
        public bool IsMatriceIdentite(double[,] p_matriceIdentiteAttendue, int p_toleranceDArrondi)
        {
            bool isMatriceIdentite = true;
            try
            {
                int p_dimension = p_matriceIdentiteAttendue.GetLength(0);
                for (int v_indiceDiagonal=0; v_indiceDiagonal< p_dimension; v_indiceDiagonal++)
                {
                    if(Math.Round(p_matriceIdentiteAttendue[v_indiceDiagonal, v_indiceDiagonal], p_toleranceDArrondi)!=1)
                    {
                        return false;
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
            return isMatriceIdentite;
        }
        
        public double[,] GetMatriceChangementDeRepereXY(double[] p_coordPoint0, double[] p_coordPoint2Abs, double[] p_coordPoint3Ord_orthoSiNull=null, bool p_normaliser_vf=true)
        {
            double[,] v_matriceLigne = new double[2, 2];
            try
            {
                double[] v_vecteur_e1 = GetVectorBrutFromTwoPoints(p_coordPoint0, p_coordPoint2Abs);
                v_vecteur_e1 = GetNormalisationVecteurXY(v_vecteur_e1);
                //
                double[] v_vecteur_e2;
                if(p_coordPoint3Ord_orthoSiNull==null) //Si le second vecteur n'est pas proposé=>on créé un vecteur orthogonal de même norme.
                {
                    v_vecteur_e2 = new double[2] { (-1) * v_vecteur_e1[1], v_vecteur_e1[0] };
                }
                else
                {
                    v_vecteur_e2 = GetVectorBrutFromTwoPoints(p_coordPoint0, p_coordPoint3Ord_orthoSiNull);
                    v_vecteur_e2 = GetNormalisationVecteurXY(v_vecteur_e2);
                }
              
                double v_delta = (v_vecteur_e1[0] * v_vecteur_e2[1]) - (v_vecteur_e1[1] * v_vecteur_e2[0]);
                //[Colonne puis ligne
                v_matriceLigne[0, 0] = v_vecteur_e2[1] / v_delta;
                v_matriceLigne[1, 1] = v_vecteur_e1[0] / v_delta;
                //
                v_matriceLigne[1, 0] = v_vecteur_e1[1] * -1 / v_delta;
                v_matriceLigne[0, 1] = v_vecteur_e2[0] * -1 / v_delta;
            }
            catch (Exception)
            {

                throw;
            }
            return v_matriceLigne;
        }

        public double[] GetCoordDansNewRepereXY(double[,] p_matriceNewRepere,double[] p_origineDuRepere, double[] p_pointToTranslate)
        {
            double[] v_coord = new double[2];
            try
            {
                double[] v_vecteurToTranslate = GetVectorBrutFromTwoPoints(p_origineDuRepere, p_pointToTranslate);
                //v_vecteurToTranslate = GetNormalisationVecteurXY(v_vecteurToTranslate); //=>Ne pas normaliser!!
                
                v_coord[0] = (v_vecteurToTranslate[0] * p_matriceNewRepere[0, 0]) + (v_vecteurToTranslate[1] * p_matriceNewRepere[0,1]);
                v_coord[1] = (v_vecteurToTranslate[0] * p_matriceNewRepere[1,0]) + (v_vecteurToTranslate[1] * p_matriceNewRepere[1, 1]);
            }
            catch (Exception)
            {

                throw;
            }
            return v_coord;
        }
        public double[] GetCoordDansNewRepereXY(double[] p_pointsAReferencer, double[] p_coordPoint0, double[] p_coordPoint2Abs, double[] p_coordPoint3Ord_orthoSiNull = null)
        {
            double[] v_coord = null;
            try
            {
                bool v_normaliser_vf = true;
                double[,] v_matriceDeConversion;
                v_matriceDeConversion = GetMatriceChangementDeRepereXY(p_coordPoint0, p_coordPoint2Abs, p_coordPoint3Ord_orthoSiNull, v_normaliser_vf);
                //
                v_coord = GetCoordDansNewRepereXY(v_matriceDeConversion, p_coordPoint0, p_pointsAReferencer);
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
