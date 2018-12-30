using System.Collections.Generic;

namespace DEM.Net.Lib.Services.Lab
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
        Dictionary<int, double[]> GetCoordDansNewRepereXY(Dictionary<int, double[]> p_pointsAReferencer, double[] p_coordPoint0, double[] p_coordPoint2Abs, double[] p_coordPoint3Ord_orthoSiNull = null);
        double[] GetCoordDansNewRepereXY(double[,] p_matriceNewRepere, double[] p_origineDuRepere, double[] p_pointToTranslate);
        double[] GetCoordDansNewRepereXY(double[] p_pointAReferencer, double[] p_coordPoint0, double[] p_coordPoint2Abs, double[] p_coordPoint3Ord_orthoSiNull = null);
        double GetDistanceEuclidienneCarreeXY(double[] v_point1, double[] v_point2);
        double GetDistanceEuclidienneCarreeXYZ(double[] v_point1, double[] v_point2);
        double GetDistanceEuclidienneXY(double[] v_point1, double[] v_point2);
        double GetDistanceEuclidienneXYZ(double[] v_point1, double[] v_point2);
        double[] GetEquationDuPlan(double[] p_normaleAuPlan, double[] p_point3DDuPlan);
        double[] GetEquationDuPlan(double[] p_point3D_1, double[] p_point3D_2, double[] p_point3D_3);
        double[] GetIntersectionDroites2D(double[] p_coeffDroite1, double[] p_coeffDroite2);
        double[] GetIntersectionDroites2D(double[] p_Droite1_pt1, double[] p_Droite1_pt2, double[] p_Droite2_pt1, double[] p_Droite2_pt2);
        double[,] GetInverseMatrice2x2(double[,] p_matriceToInverse);
        double[] GetInverseVector2(double[,] p_matrice, double[] p_vectorToInverse);
        double[,] GetMatrice3x3FromCoord(double[] p_coordPointOrigine, double[] p_coordPoint1, double[] p_coordPoint2, double[] p_coordPoint3, bool p_normaliser_vf);
        double[,] GetMatrice3x3FromVectors(double[] p_vector1, double[] p_vector2, double[] p_vector3);
        double[,] GetMatriceChangementDeRepereXY(double[] p_coordPoint0, double[] p_coordPoint2Abs, double[] p_coordPoint3Ord_orthoSiNull = null, bool p_normaliser_vf = true);
        double[,] GetMatriceInverse3x3(double p_v1_a, double p_v1_b, double p_v1_c, double p_v2_d, double p_v2_e, double p_v2_f, double p_v3_g, double p_v3_h, double p_v3_i);
        double[,] GetMatriceInverse3x3(double[,] p_matriceAInverser);
        double[,] GetMatriceInverse3x3(double[] p_vector1, double[] p_vector2, double[] p_vector3);
        double[] GetNormaleDuPlan(double[] p_point3D_1, double[] p_point3D_2, double[] p_point3D_3);
        double[] GetNormalisationVecteurXY(double[] p_vector);
        double[] GetNormalisationVecteurXYZ(double[] p_vector);
        double GetNormeVecteurXY(double[] p_vector);
        double GetNormeVecteurXYZ(double[] p_vector);
        List<int> GetOrdonnancement(Dictionary<int, double[]> p_pointsATester, bool p_renvoyerNullSiColineaires_vf, bool p_horaireSinonAntohoraire_vf);
        int GetPointLePlusEloigneDePoint0(Dictionary<int, double[]> p_pointsATester, double[] p_coordPoint0);
        int GetPointLePlusExcentreXY(Dictionary<int, double[]> p_pointsATester);
        double[] GetProduitMatriceParVector(double[,] p_matrice, double[] p_vector);
        double[,] GetProduitMatricesAxB(double[,] v_matriceA, double[,] v_matriceB);
        double GetProduitScalaireXY(double[] p_vector1, double[] p_vector2);
        double GetProduitScalaireXYZ(double[] p_vector1, double[] p_vector2);
        double[] GetVectorBrutFromTwoPoints(double[] p_pointOrigine, double[] p_point2);
        bool IsInversionMatriceOk(double[,] p_matriceAInverser, double[,] p_matriceInverse, int p_toleranceDArrondi);
        bool IsMatriceIdentite(double[,] p_matriceIdentiteAttendue, int p_toleranceDArrondi);
        bool IsPointDDansCercleCirconscritAuTriangleByMatrice(Dictionary<int, double[]> p_pointsTriangle, double[] p_coordPtD);
    }
}