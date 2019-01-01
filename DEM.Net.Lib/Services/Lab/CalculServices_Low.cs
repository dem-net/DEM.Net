using DEM.Net.Lib.Services.VisualisationServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Services.Lab
{
    public class CalculServices_Low : ICalculServices_Low, ICalculServicesLow_testsDivers
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
                if (v_norme == 0) //On ne retourne pas directement un vecteur null.=>la dim demandée peut être inf à la dim réelle+on veut une copie 
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
        public bool AreDroitesParallelesXY(double[] p_Droite1_pt1, double[] p_Droite1_pt2, double[] p_Droite2_pt1, double[] p_Droite2_pt2)
        {
            double[] v_vector1=GetVectorBrutFromTwoPoints(p_Droite1_pt1, p_Droite1_pt2);
            double[] v_vector2 = GetVectorBrutFromTwoPoints(p_Droite2_pt1, p_Droite2_pt2);
            return AreVecteursColineairesXY(v_vector1, v_vector2);
        }
        //
        public double[,] GetMatriceInverse2x2(double[,] p_matriceToInverse)
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
                double[,] v_matriceInverse = GetMatriceInverse2x2(p_matrice);
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
                if ((p_Droite1_pt2[0] - p_Droite1_pt1[0])==0)
                {
                    return null;
                }
               
                v_coeff[0] = (p_Droite1_pt2[1]- p_Droite1_pt1[1])/ (p_Droite1_pt2[0] - p_Droite1_pt1[0]);
                v_coeff[1] = p_Droite1_pt2[1] - (v_coeff[0] * p_Droite1_pt2[0]);
            }
            catch (Exception)
            {
                throw;
            }
            return v_coeff;
        }
        public double[] GetIntersectionDroites2DCoeffExplicites(double[] p_coeffDroite1, double[] p_coeffDroite2)
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
               if (AreDroitesParallelesXY(p_Droite1_pt1, p_Droite1_pt2, p_Droite2_pt1, p_Droite2_pt2))
                {
                    return null;
                }
                v_coord = new double[2];
                double[] p_coeffDroite1 = GetCoeffDroite2D(p_Droite1_pt1, p_Droite1_pt2);
                double[] p_coeffDroite2 = GetCoeffDroite2D(p_Droite2_pt1, p_Droite2_pt2);
                //Si une des droites est verticale
                if(p_coeffDroite1==null)
                {
                    v_coord[0] = p_Droite1_pt1[0];
                    v_coord[1] = (p_coeffDroite2[0] * p_Droite1_pt1[0]) + p_coeffDroite2[1];
                    return v_coord;
                }
                if (p_coeffDroite2 == null)
                {
                    v_coord[0] = p_Droite2_pt1[0];
                    v_coord[1] = (p_coeffDroite1[0] * p_Droite2_pt1[0]) + p_coeffDroite1[1];
                    return v_coord;
                }
                //Sinon:
                v_coord = GetIntersectionDroites2DCoeffExplicites(p_coeffDroite1, p_coeffDroite2);
            }
            catch (Exception)
            {

                throw;
            }
            return v_coord;
        }
        public bool AreSegmentsSequants(double[] p_Segment1_pt1, double[] p_Segment1_pt2, double[] p_Segment2_pt1, double[] p_Segment2_pt2)
        {
            bool v_out = true;
            try
            {
                double[] v_pointDIntersection;
                v_pointDIntersection = GetIntersectionDroites2D(p_Segment1_pt1, p_Segment1_pt2, p_Segment2_pt1, p_Segment2_pt2);
                //Existe-t-il un point d'intersection entre les 2 droites?
                if (v_pointDIntersection==null)
                {
                    return false;
                }
               //Si oui, le point peut être sur le segment OU en amont OU en aval. 
               //Dans ces deux derniers cas, la distance entre le point et l'un ou/et l'autre point doit être plus grande que la longueur du segment.
                double v_longueurSeg1 = GetDistanceEuclidienneCarreeXY(p_Segment1_pt1, p_Segment1_pt2);
                if (GetDistanceEuclidienneCarreeXY(p_Segment1_pt1, v_pointDIntersection)> v_longueurSeg1 || GetDistanceEuclidienneCarreeXY(p_Segment1_pt2, v_pointDIntersection) > v_longueurSeg1)
                {
                    return false;
                }
                double v_longueurSeg2 = GetDistanceEuclidienneCarreeXY(p_Segment2_pt1, p_Segment2_pt2);
                if (GetDistanceEuclidienneCarreeXY(p_Segment2_pt1, v_pointDIntersection) > v_longueurSeg2 || GetDistanceEuclidienneCarreeXY(p_Segment2_pt2, v_pointDIntersection) > v_longueurSeg2)
                {
                    return false;
                }
            }
            catch (Exception)
            {

                throw;
            }
            return v_out;
        }

        /// <summary>
        /// On renvoie une matrice [ligne,colonne] telle que:
        /// colonne 1 contient les paramètres des x
        /// colonne 2 contient les paramètres des y
        /// ligne 1: coefficient en x et en y
        /// </summary>
        /// <param name="p_Droite1_pt1"></param>
        /// <param name="p_Droite1_pt2"></param>
        /// <returns></returns>
        public double[,] GetEquationParametriqueDroite2D(double[] p_Droite1_pt1, double[] p_Droite1_pt2, bool p_normaliser_vf)
        {
            double[,] v_matriceDesCoeff = new double[2, 2];
            try
            {
                double[] v_vector1 = GetVectorBrutFromTwoPoints(p_Droite1_pt1, p_Droite1_pt2);
                if(p_normaliser_vf)
                {
                    v_vector1 = GetNormalisationVecteurXY(v_vector1);
                }
                v_matriceDesCoeff[0, 0] = v_vector1[0];
                v_matriceDesCoeff[0, 1] = v_vector1[1];
                v_matriceDesCoeff[1, 0] = p_Droite1_pt1[0];
                v_matriceDesCoeff[1, 1] = p_Droite1_pt1[1];
            }
            catch (Exception)
            {

                throw;
            }
            return v_matriceDesCoeff;
        }
        public double[] GetIntersectionDroites2DCoeffParametriques(double[,] p_parametresDroite1, double[,] p_parametresDroite2)
        {
            double[] v_pointIntersection = null;
            try
            {
                double a, b, c, d, e, f, g, h;
                a = p_parametresDroite1[0, 0];
                b = p_parametresDroite1[0, 1];
                c = p_parametresDroite1[1, 0];
                d = p_parametresDroite1[1, 1];
                //
                e = p_parametresDroite2[0, 0];
                f = p_parametresDroite2[0, 1];
                g = p_parametresDroite2[1, 0];
                h = p_parametresDroite2[1, 1];

                //On calcule les coefficients à appliquer pour que les 2 droites s'intersectent
                double v_delta = (b * e) - (f * a);
                if(v_delta==0)
                {
                    return null;
                }
                
                //Coeff à appliquer pour la droite 2
                double v_n = ((h * a) - (d * a) - (g * b) + (c * b)) / v_delta;

                //On peut calculer celui pour la droite mais...ce n'est pas nécessaire!
                //double v_m = ((e * v_n) + g - c) / a;

                //On applique le coeff calculé pour les paramètres de la droite 2 et on déduit x et y
                double v_x = (p_parametresDroite2[0, 0] * v_n) + p_parametresDroite2[1, 0];
                double v_y = (p_parametresDroite2[0, 1] * v_n) + p_parametresDroite2[1, 1];

                v_pointIntersection = new double[2] { v_x, v_y };

            }
            catch (Exception)
            {

                throw;
            }
            return v_pointIntersection;
        }
        public double[] GetIntersectionDroites2DMethodeParametrique(double[] p_Droite1_pt1, double[] p_Droite1_pt2, double[] p_Droite2_pt1, double[] p_Droite2_pt2)
        {
            double[] v_coord = null;
            try
            {
                bool v_normaliser_vf = false;
                double[,] v_paramDroite1 = GetEquationParametriqueDroite2D(p_Droite1_pt1, p_Droite1_pt2, v_normaliser_vf);
                double[,] v_paramDroite2 = GetEquationParametriqueDroite2D(p_Droite2_pt1, p_Droite2_pt2, v_normaliser_vf);
                //
                v_coord = GetIntersectionDroites2DCoeffParametriques(v_paramDroite1, v_paramDroite2);
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

        public double GetDeterminantMatrice3x3(double[] p_vector1, double[] p_vector2, double[] p_vector3)
        {
            double v_determinant = new double();
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
                v_determinant = GetDeterminantMatrice3x3(a, b, c, d, e, f, g, h, i);
            }
            catch (Exception)
            {

                throw;
            }
            return v_determinant;
        }
        public double GetDeterminantMatrice3x3(double p_v1_a, double p_v1_b, double p_v1_c, double p_v2_d, double p_v2_e, double p_v2_f, double p_v3_g, double p_v3_h, double p_v3_i)
        {
            double v_determinant;
            v_determinant = (p_v1_a * p_v2_e * p_v3_i) - (p_v1_a * p_v3_h * p_v2_f) + (p_v1_b * p_v2_f * p_v3_g) - (p_v1_b * p_v2_d * p_v3_i) + (p_v1_c * p_v2_d * p_v3_h) - (p_v1_c * p_v2_e * p_v3_g);
            return v_determinant;
        }
       
        
        /// <summary>
        /// (Attention:
        /// - le principe de l'algo n'est pas clair 
        /// - + une 'pustule' inversant le résulat a été nécessaire
        /// </summary>
        /// <param name="p_pointsTriangle"></param>
        /// <param name="p_coordPtD"></param>
        /// <returns></returns>
        public bool IsPointDDansCercleCirconscritAuTriangleByMatrice(List<double[]> p_pointsTriangle, double[] p_coordPtD)
        {
            bool v_out = false;
            try
            {
                //On ordonne les points du triangle A,B,C dans le sens anti-horaire
                int v_indice = 0;
                Dictionary<int, double[]> p_dicoPointsTriangle = p_pointsTriangle.ToDictionary(t => v_indice++,  t=>t);
                List<int> v_pointsOrdonnances;
                bool v_sensHoraire_vf = false;
                v_pointsOrdonnances=GetOrdonnancement(p_dicoPointsTriangle, false, v_sensHoraire_vf);

                //Coeff de la matrice
                //(4x4 mais d,h,l et p sont égaux à 1)
                double[] v_coordPtA_ord= p_dicoPointsTriangle[v_pointsOrdonnances[0]];
                double[] v_coordPtB_ord = p_dicoPointsTriangle[v_pointsOrdonnances[1]];
                double[] v_coordPtC_ord = p_dicoPointsTriangle[v_pointsOrdonnances[2]];
               
                double a, b, c, e, f, g, i, j, k, m, n, o;
               
                a = v_coordPtA_ord[0];
                e = v_coordPtB_ord[0];
                i = v_coordPtC_ord[0];
                m = p_coordPtD[0];
                //
                b = v_coordPtA_ord[1];
                f = v_coordPtB_ord[1];
                j = v_coordPtC_ord[1];
                n = p_coordPtD[1];
                //
                c = (v_coordPtA_ord[0] * v_coordPtA_ord[0]) + (v_coordPtA_ord[1] * v_coordPtA_ord[1]);
                g = (v_coordPtB_ord[0] * v_coordPtB_ord[0]) + (v_coordPtB_ord[1] * v_coordPtB_ord[1]);
                k = (v_coordPtC_ord[0] * v_coordPtC_ord[0]) + (v_coordPtC_ord[1] * v_coordPtC_ord[1]);
                o = (p_coordPtD[0] * p_coordPtD[0]) + (p_coordPtD[1] * p_coordPtD[1]);
                //
                double v_delta;

                ////(Déterminant complet de la matrice 4x4
                //double d, h, l, p;
                //d = 1; h = 1; l = 1; p = 1;
                //v_delta = a*((f*k*p)-(f*l*o)-(g*j*p) + (g*l*n) + (h*j*o)-(h*k*n));
                //v_delta += -b * ((e * k * p) - (e * l * o) - (g * i * p) + (g * l * m) + (h * i * o) - (h * k * m));
                //v_delta += +c * ((e * j * p) - (e * l * n) - (f * i * p) + (f * l * m) + (h * i * n) - (h * j * m));
                //v_delta += -d*((e*j*o)-(e*k*n)-(f*i*o) + (f*k*m) + (g*i*n)-(g*j*m));

                //Déterminant réduit
                v_delta = a * ((f * k) - (f * o) - (g * j) + (g * n) + (j * o) - (k * n));
                v_delta += -b * ((e * k) - (e * o) - (g * i) + (g * m) + (i * o) - (k * m));
                v_delta += +c * ((e * j) - (e * n) - (f * i) + (f * m) + (i * n) - (j * m));
                v_delta += (e * j * o) - (e * k * n) - (f * i * o) + (f * k * m) + (g * i * n) - (g * j * m);
                
                //Si le déterminant est positif, alors le point est dans le cercle circonscrit
                //NON Bizarre!!! Semble marcher à l'inverse de l'attendu?? (cf article wikipedia 'triangulation delaunay
                //if(v_delta>0)
                if (v_delta <= 0) //PUSTULE!
                {
                    v_out = true;
                }
                else
                {
                    v_out = false;
                }

            }
            catch (Exception)
            {

                throw;
            }
            return v_out;
        }
        //EN COURS
        public bool IsPointDDansCercleCirconscritAuTriangleExplicite(List<double[]> p_pointsTriangle, double[] p_pointToTest)
        {
            bool v_retour = true;
            try
            {
                double[] v_centreDuCercle;
                v_centreDuCercle = GetCoordonneesCercleCirconscritAuTriangle(p_pointsTriangle);
                //Si null=>traduit le fait qu'il y a alignement
                if(v_centreDuCercle==null)
                {
                    return true;
                }
                double v_rayon = GetDistanceEuclidienneCarreeXY(v_centreDuCercle, p_pointsTriangle.First());
                //
                double v_ecartAuCentre = GetDistanceEuclidienneCarreeXY(v_centreDuCercle, p_pointToTest);
                if (v_ecartAuCentre> v_rayon)
                {
                    v_retour = false;
                }
            }
            catch (Exception)
            {

                throw;
            }
            return v_retour;
        }
        public double[] GetCoordonneesCercleCirconscritAuTriangle(List<double[]> p_pointsTriangle)
        {
            double[] v_coordOut = new double[2];
            try
            {
                double[,] p_parametrePremiereMediatrice = GetMediatriceByParametres(p_pointsTriangle[0], p_pointsTriangle[1]);
                double[,] p_parametreSecondeMediatrice = GetMediatriceByParametres(p_pointsTriangle[1], p_pointsTriangle[2]);
                v_coordOut=GetIntersectionDroites2DCoeffParametriques(p_parametrePremiereMediatrice, p_parametreSecondeMediatrice);
            }
            catch (Exception)
            {

                throw;
            }
            return v_coordOut;
        }
        public double[,] GetMediatriceByParametres(double[] p_point0, double[] p_point1)
        {
            double[,] v_parametres = new double[2, 2];
            try
            {
                //(on définit paramétriquement une droite passant le milieu du segment et orthogonal à ce segment
                double[] v_vecteurSource = GetVectorBrutFromTwoPoints(p_point0, p_point1);
                double[] v_vecteurCoeff = new double[2] { -1 * v_vecteurSource[1], v_vecteurSource[0] };
                double[] v_vecteurPointAppui = new double[2] { p_point0[0] + (v_vecteurSource[0] / 2), p_point0[1] + (v_vecteurSource[1] / 2) };
                v_parametres[0, 0] = v_vecteurCoeff[0];
                v_parametres[0, 1] = v_vecteurCoeff[1];
                v_parametres[1, 0] = v_vecteurPointAppui[0];
                v_parametres[1, 1] = v_vecteurPointAppui[1];
            }
            catch (Exception)
            {

                throw;
            }
            return v_parametres;
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
                double v_valeur;
                for (int v_indiceLigne=0; v_indiceLigne< p_dimension; v_indiceLigne++)
                {
                    for (int v_indiceColonne = 0; v_indiceColonne < p_dimension; v_indiceColonne++)
                    {
                        v_valeur = Math.Round(p_matriceIdentiteAttendue[v_indiceLigne, v_indiceColonne], p_toleranceDArrondi);
                        if(v_indiceLigne== v_indiceColonne)
                        {
                            if(v_valeur!=1)
                            {
                                return false;
                            }
                        }
                        else
                        {
                            if (v_valeur != 0)
                            {
                                return false;
                            }
                        }
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
        public double[] GetCoordDansNewRepereXY(double[] p_pointAReferencer, double[] p_coordPoint0, double[] p_coordPoint2Abs, double[] p_coordPoint3Ord_orthoSiNull = null)
        {
            double[] v_coord = null;
            try
            {
                bool v_normaliser_vf = true;
                double[,] v_matriceDeConversion;
                v_matriceDeConversion = GetMatriceChangementDeRepereXY(p_coordPoint0, p_coordPoint2Abs, p_coordPoint3Ord_orthoSiNull, v_normaliser_vf);
                //
                v_coord = GetCoordDansNewRepereXY(v_matriceDeConversion, p_coordPoint0, p_pointAReferencer);
            }
            catch (Exception)
            {

                throw;
            }
            return v_coord;
        }
        public Dictionary<int, double[]> GetCoordDansNewRepereXY(Dictionary<int,double[]> p_pointsAReferencer, double[] p_coordPoint0, double[] p_coordPoint2Abs, double[] p_coordPoint3Ord_orthoSiNull = null)
        {
            Dictionary<int, double[]> v_coordsDesPoints = new Dictionary<int, double[]>();
            try
            {
                bool v_normaliser_vf = true;
                double[,] v_matriceDeConversion;
                v_matriceDeConversion = GetMatriceChangementDeRepereXY(p_coordPoint0, p_coordPoint2Abs, p_coordPoint3Ord_orthoSiNull, v_normaliser_vf);
                //
                double[] v_coord;
                foreach (KeyValuePair < int,double[]> v_pointToTest in p_pointsAReferencer)
                {
                    v_coord = GetCoordDansNewRepereXY(v_matriceDeConversion, p_coordPoint0, v_pointToTest.Value);
                    v_coordsDesPoints.Add(v_pointToTest.Key, v_coord);
                }
            }
            catch (Exception)
            {
                throw;
            }
            return v_coordsDesPoints;
        }

        //
        public double GetDistanceEuclidienneCarreeXY(double[] v_point1, double[] v_point2)
        {
         return (v_point2[0] - v_point1[0]) * (v_point2[0] - v_point1[0]) + (v_point2[1] - v_point1[1]) * (v_point2[1] - v_point1[1]);  
        }
        public double GetDistanceEuclidienneXY(double[] v_point1, double[] v_point2)
        {
            return Math.Sqrt(GetDistanceEuclidienneCarreeXY(v_point1, v_point2));
        }
        //
        public double GetDistanceEuclidienneCarreeXYZ(double[] v_point1, double[] v_point2)
        {
            return (v_point2[0] - v_point1[0]) * (v_point2[0] - v_point1[0]) + (v_point2[1] - v_point1[1]) * (v_point2[1] - v_point1[1]) + (v_point2[2] - v_point1[2]) * (v_point2[2] - v_point1[2]);
        }
        public double GetDistanceEuclidienneXYZ(double[] v_point1, double[] v_point2)
        {
           return Math.Sqrt(GetDistanceEuclidienneCarreeXYZ(v_point1, v_point2));
        }
        //
        /// <summary>
        /// Surtout adapté aux triangles)
        /// </summary>
        /// <param name="p_pointsATester"></param>
        /// <param name="p_renvoyerNullSiColineaires_vf"></param>
        /// <param name="p_horaireSinonAntohoraire_vf"></param>
        /// <returns></returns>
        public List<int> GetOrdonnancement(Dictionary<int, double[]> p_pointsATester, bool p_renvoyerNullSiColineaires_vf, bool p_horaireSinonAntohoraire_vf)
        {
            List<int> v_pointsOrdonnances = new List<int>();
            try
            {
                int v_pt1 = GetPointLePlusExcentreXY(p_pointsATester);
                int v_pt2 = GetPointLePlusEloigneDePoint0(p_pointsATester, p_pointsATester[v_pt1]);
                //
                Dictionary<int, double[]> v_coord = GetCoordDansNewRepereXY(p_pointsATester, p_pointsATester[v_pt1], p_pointsATester[v_pt2]);
               
                if (p_renvoyerNullSiColineaires_vf)
                {
                    int v_nbrePoints = v_coord.Count;
                    if (v_coord.Where(c => c.Value[1] == 0).Count()== v_nbrePoints)
                    {
                        return null;
                    }
                 }
                //
                v_pointsOrdonnances.AddRange(v_coord.Where(c => c.Value[1] >= 0).OrderBy(c => c.Value[0]).Select(c => c.Key));
                v_pointsOrdonnances.AddRange(v_coord.Where(c => c.Value[1] < 0).OrderByDescending(c => c.Value[0]).Select(c => c.Key));
                if(!p_horaireSinonAntohoraire_vf)
                {
                    v_pointsOrdonnances.Reverse();
                }
            }
            catch (Exception)
            {

                throw;
            }
            return v_pointsOrdonnances;
        }
        public double[] GetCentroide(List<double[]> p_coordPoints)
        {
            int v_dimension = p_coordPoints.First().Length;
            double[] v_coordCentroide = new double[v_dimension];
            for(int i=0;i< v_dimension;i++)
            {
                v_coordCentroide[i] = p_coordPoints.Average(c => c[i]);
            }
            //
            return v_coordCentroide;
        }
        public int GetPointLePlusExcentreXY(Dictionary<int,double[]> p_pointsATester)
        {
            double[] v_centroide = GetCentroide(p_pointsATester.Select(c => c.Value).ToList());
            return GetPointLePlusEloigneDePoint0(p_pointsATester, v_centroide);
        }
        public int GetPointLePlusEloigneDePoint0(Dictionary<int, double[]> p_pointsATester, double[] p_coordPoint0)
        {
            return p_pointsATester.OrderByDescending(c => GetDistanceEuclidienneCarreeXY(c.Value, p_coordPoint0)).Select(c => c.Key).First();
        }
    }
}
