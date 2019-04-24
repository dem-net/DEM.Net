// ICalculServices_Low.cs
//
// Author:
//       Frédéric Aubin
//
// Copyright (c) 2019 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Collections.Generic;

namespace DEM.Net.Core.Services.Lab
{
    public interface ICalculServices_Low
    {
        bool AreDroitesParallelesXY(double[] p_Droite1_pt1, double[] p_Droite1_pt2, double[] p_Droite2_pt1, double[] p_Droite2_pt2);
        bool AreSegmentsSequants(double[] p_Segment1_pt1, double[] p_Segment1_pt2, double[] p_Segment2_pt1, double[] p_Segment2_pt2);
        bool AreVecteursColineairesXY(double[] p_vector1, double[] p_vector2);
        bool AreVecteursColineairesXYZ(double[] p_vector1, double[] p_vector2);
        bool AreVectorsSameDimension(double[] p_vector1, double[] p_vector2, bool p_exceptionSiFalse);
        double[] GetCentroide(List<double[]> p_coordPoints);
        double[] GetCoeffDroite2D(double[] p_Droite1_pt1, double[] p_Droite1_pt2);
        Dictionary<int, double[]> GetCoordDansNewRepereXY(Dictionary<int, double[]> p_pointsAReferencer, double[] p_coordPoint0, double[] p_coordPoint2Abs, double[] p_coordPoint3Ord_orthoSiNull = null, bool p_normaliser_vf = false);
        double[] GetCoordDansNewRepereXY(double[,] p_matriceNewRepere, double[] p_origineDuRepere, double[] p_pointToTranslate, bool p_normaliser_vf=false);
        double[] GetCoordDansNewRepereXY(double[] p_pointAReferencer, double[] p_coordPoint0, double[] p_coordPoint2Abs, double[] p_coordPoint3Ord_orthoSiNull = null, bool p_normaliser_vf = false);
        double[] GetCoordonneesCercleCirconscritAuTriangle(List<double[]> p_pointsTriangle);
        double GetDeterminantMatrice3x3(double p_v1_a, double p_v1_b, double p_v1_c, double p_v2_d, double p_v2_e, double p_v2_f, double p_v3_g, double p_v3_h, double p_v3_i);
        double GetDeterminantMatrice3x3(double[] p_vector1, double[] p_vector2, double[] p_vector3);
        double GetDistanceEuclidienneCarreeXY(double[] v_point1, double[] v_point2);
        double GetDistanceEuclidienneCarreeXYZ(double[] v_point1, double[] v_point2);
        double GetDistanceEuclidienneXY(double[] v_point1, double[] v_point2);
        double GetDistanceEuclidienneXYZ(double[] v_point1, double[] v_point2);
        double[] GetEquationNormaleDuPlan(double[] p_normaleAuPlan, double[] p_point3DDuPlan);
        double[] GetEquationNormaleDuPlan(double[] p_point3D_1, double[] p_point3D_2, double[] p_point3D_3, bool p_exceptionSiVecteursColineaires_vecteur0Sinon_vf);
        double[,] GetEquationParametriqueDroite2D(double[] p_Droite1_pt1, double[] p_Droite1_pt2, bool p_normaliser_vf);
        double[] GetIntersectionDroites2D(double[] p_Droite1_pt1, double[] p_Droite1_pt2, double[] p_Droite2_pt1, double[] p_Droite2_pt2);
        double[] GetIntersectionDroites2DCoeffExplicites(double[] p_coeffDroite1, double[] p_coeffDroite2);
        double[] GetIntersectionDroites2DCoeffParametriques(double[,] p_parametresDroite1, double[,] p_parametresDroite2);
        double[] GetIntersectionDroites2DMethodeParametrique(double[] p_Droite1_pt1, double[] p_Droite1_pt2, double[] p_Droite2_pt1, double[] p_Droite2_pt2);
        double[] GetInverseVector2(double[,] p_matrice, double[] p_vectorToInverse);
        double[,] GetMatrice3x3FromCoord(double[] p_coordPointOrigine, double[] p_coordPoint1, double[] p_coordPoint2, double[] p_coordPoint3, bool p_normaliser_vf);
        double[,] GetMatrice3x3FromVectors(double[] p_vector1, double[] p_vector2, double[] p_vector3);
        double[,] GetMatriceChangementDeRepereXY(double[] p_coordPoint0, double[] p_coordPoint2Abs, double[] p_coordPoint3Ord_orthoSiNull = null, bool p_normaliser_vf = true);
        double[,] GetMatriceInverse2x2(double[,] p_matriceToInverse);
        double[,] GetMatriceInverse3x3(double p_v1_a, double p_v1_b, double p_v1_c, double p_v2_d, double p_v2_e, double p_v2_f, double p_v3_g, double p_v3_h, double p_v3_i);
        double[,] GetMatriceInverse3x3(double[,] p_matriceAInverser);
        double[,] GetMatriceInverse3x3(double[] p_vector1, double[] p_vector2, double[] p_vector3);
        double[,] GetMediatriceByParametres(double[] p_point0, double[] p_point1);
        double[] GetNormaleDuPlan(double[] p_vector_u, double[] p_vector_w, bool p_normaliserVecteursEnEntree_vf, bool p_exceptionSiVecteursColineaires_vecteur0Sinon_vf);
        double[] GetNormaleDuPlan(double[] p_point3D_1, double[] p_point3D_2, double[] p_point3D_3, bool p_exceptionSiVecteursColineaires_vecteur0Sinon_vf);
        double[] GetNormalisationVecteurXY(double[] p_vector);
        double[] GetNormalisationVecteurXYZ(double[] p_vector);
        double GetNormeVecteurXY(double[] p_vector);
        double GetNormeVecteurXYZ(double[] p_vector);
        List<int> GetOrdonnancement(Dictionary<int, double[]> p_pointsATester, double[] p_pointCentral, int p_idPremierPoint, bool p_horaireSinonAntihoraire_vf);
        List<int> GetOrdonnancement(Dictionary<int, double[]> p_pointsATester, bool p_renvoyerNullSiColineaires_vf, bool p_horaireSinonAntohoraire_vf);
        double GetPenteFromPoints3D(double[] p_pointDOrigine, double[] p_point2);
        double GetPente(double[] p_vecteur3D);
        int GetPointLePlusEloigneDePoint0XY(Dictionary<int, double[]> p_pointsATester, double[] p_coordPoint0);
        int GetPointLePlusExcentreXY(Dictionary<int, double[]> p_pointsATester);
        int GetPointLePlusProcheDePoint0XY(Dictionary<int, double[]> p_pointsATester, double[] p_coordPoint0);
        double[] GetProduitMatriceParVector(double[,] p_matrice, double[] p_vector);
        double[,] GetProduitMatricesAxB(double[,] v_matriceA, double[,] v_matriceB);
        double GetProduitScalaireXY(double[] p_vector1, double[] p_vector2);
        double GetProduitScalaireXYZ(double[] p_vector1, double[] p_vector2);
        double[] GetVecteurPenteMaxi(double[] p_normaleDuPlan, bool p_normaliserVecteurEnSortie_vf = true);
        double[] GetVectorBrutFromTwoPoints(double[] p_pointOrigine, double[] p_point2);
        bool IsInversionMatriceOk(double[,] p_matriceAInverser, double[,] p_matriceInverse, int p_toleranceDArrondi);
        bool IsMatriceIdentite(double[,] p_matriceIdentiteAttendue, int p_toleranceDArrondi);
        bool IsPointDDansCercleCirconscritAuTriangleByMatrice(List<double[]> p_pointsTriangle, double[] p_coordPtD);
        bool IsPointDDansCercleCirconscritAuTriangleExplicite(List<double[]> p_pointsTriangle, double[] p_pointToTest);
    }
}