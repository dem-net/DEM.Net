namespace DEM.Net.Lib.Services.Lab
{
    public interface ICalculServices_Low
    {
        bool AreVecteursColineairesXY(double[] p_vector1, double[] p_vector2);
        bool AreVecteursColineairesXYZ(double[] p_vector1, double[] p_vector2);
        bool AreVectorsSameDimension(double[] p_vector1, double[] p_vector2, bool p_exceptionSiFalse);
        double[] GetCoeffDroite2D(double[] p_Droite1_pt1, double[] p_Droite1_pt2);
        double[] GetCoordDansNewRepereXY(double[,] p_matriceNewRepere, double[] p_origineDuRepere, double[] p_pointToTranslate);
        double[] GetCoordDansNewRepereXY(double[] p_pointsAReferencer, double[] p_coordPoint0, double[] p_coordPoint2Abs, double[] p_coordPoint3Ord_orthoSiNull = null);
        double GetDistanceEuclidienne2D(double[] v_point1, double[] v_point2);
        double GetDistanceEuclidienneCarree2D(double[] v_point1, double[] v_point2);
        double[] GetEquationDuPlan(double[] p_normaleAuPlan, double[] p_point3DDuPlan);
        double[] GetEquationDuPlan(double[] p_point3D_1, double[] p_point3D_2, double[] p_point3D_3);
        double[] GetIntersectionDroites2D(double[] p_coeffDroite1, double[] p_coeffDroite2);
        double[] GetIntersectionDroites2D(double[] p_Droite1_pt1, double[] p_Droite1_pt2, double[] p_Droite2_pt1, double[] p_Droite2_pt2);
        double[,] GetInverseMatrice2x2(double[,] p_matriceToInverse);
        double[] GetInverseVector2(double[,] p_matrice, double[] p_vectorToInverse);
        double[,] GetMatrice3x3FromCoord(double[] p_coordPointOrigine, double[] p_coordPoint1, double[] p_coordPoint2, double[] p_coordPoint3, bool p_normaliser_vf);
        double[,] GetMatrice3x3FromVectors(double[] p_vector1, double[] p_vector2, double[] p_vector3);
        double[,] GetMatriceChangementDeRepereXY(double[] p_coordPoint0, double[] p_coordPoint2Abs, double[] p_coordPoint3Ord_orthoSiNull = null, bool p_normaliser_vf = true);
        double[,] GetMatriceInverse3x3(double[,] p_matriceAInverser);
        double[,] GetMatriceInverse3x3(double[] p_vector1, double[] p_vector2, double[] p_vector3);
        double[,] GetProduitMatricesAxB(double[,] v_matriceA, double[,] v_matriceB);
        double[] GetNormaleDuPlan(double[] p_point3D_1, double[] p_point3D_2, double[] p_point3D_3);
        double[] GetNormalisationVecteurXY(double[] p_vector);
        double[] GetNormalisationVecteurXYZ(double[] p_vector);
        double GetNormeVecteurXY(double[] p_vector);
        double GetNormeVecteurXYZ(double[] p_vector);
        double[] GetProduitMatriceParVector(double[,] p_matrice, double[] p_vector);
        double GetProduitScalaireXY(double[] p_vector1, double[] p_vector2);
        double GetProduitScalaireXYZ(double[] p_vector1, double[] p_vector2);
        double[] GetVectorBrutFromTwoPoints(double[] p_pointOrigine, double[] p_point2);
        bool IsInversionMatriceOk(double[,] p_matriceAInverser, double[,] p_matriceInverse, int p_toleranceDArrondi = 5);
        bool IsMatriceIdentite(double[,] p_matriceIdentiteAttendue, int p_toleranceDArrondi = 5);
    }
}