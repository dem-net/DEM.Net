namespace DEM.Net.Lib.Services.Lab
{
    public interface ICalculServices_Low
    {
        double[] GetVectorFromTwoPoints(double[] p_pointOrigine, double[] p_point2, bool p_normaliser_vf);
        //
        double GetNormeVecteur(double[] p_vector);
        double[] GetNormalisationVecteur(double[] p_vector);
        //
        double GetProduitScalaire(double[] p_vector1, double[] p_vector2);
        bool AreVectorsSameDimension(double[] p_vector1, double[] p_vector2, bool p_exceptionSiFalse);
        bool AreVecteursColineaires(double[] p_vector1, double[] p_vector2);
        //
        double[,] GetInverseMatrice2_2(double[,] p_matriceToInverse);
        double[] GetProduitMatriceParVector(double[,] p_matrice, double[] p_vector);
        double[] GetInverseVector2(double[,] p_matrice, double[] p_vectorToInverse);
        //
        double[] GetCoeffDroite2D(double[] p_Droite1_pt1, double[] p_Droite1_pt2);
        double[] GetIntersectionDroites2D(double[] p_coeffDroite1, double[] p_coeffDroite2);
        double[] GetIntersectionDroites2D(double[] p_Droite1_pt1, double[] p_Droite1_pt2, double[] p_Droite2_pt1, double[] p_Droite2_pt2);
        //
        double[] GetNormaleDuPlan(double[] p_point3D_1, double[] p_point3D_2, double[] p_point3D_3);
        double[] GetEquationDuPlan(double[] p_normaleAuPlan, double[] p_point3DDuPlan);
        double[] GetEquationDuPlan(double[] p_point3D_1, double[] p_point3D_2, double[] p_point3D_3);
        //
        double[,] GetMatriceChangementDeRepere2D(double[] p_coordPoint0, double[] p_coordPoint2Abs, double[] p_coordPoint3Ord_orthoSiNull = null, bool p_normaliser_vf = true);
        double[] GetCoordDansNewRepere2D(double[,] p_matriceNewRepere, double[] p_pointToTranslate);
        double[] GetCoordDansNewRepere2D(double[] p_pointsAReferencer, double[] p_coordPoint0, double[] p_coordPoint2Abs, double[] p_coordPoint3Ord_orthoSiNull = null);

        //
        double GetDistanceEuclidienneCarree2D(double[] v_point1, double[] v_point2);
        double GetDistanceEuclidienne2D(double[] v_point1, double[] v_point2);
    }
}