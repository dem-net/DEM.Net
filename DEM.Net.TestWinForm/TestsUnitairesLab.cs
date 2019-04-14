using DEM.Net.Core.Services.Lab;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DEM.Net.TestWinForm
{
    public class TestsUnitairesLab : ITestsUnitairesLab
    {
        public Dictionary<string, bool> TestUnitairesLab(bool p_envoyerMessageSiKo_vf)
        {
            Dictionary<string, bool> v_resultats = new Dictionary<string, bool>();
            try
            {
                bool v_resultTestIntersection = TestIntersectionsDroitesXY_coherenceServicesParamVsServicesExplicites(p_envoyerMessageSiKo_vf);
                v_resultats.Add("Intersection droites XY", v_resultTestIntersection);
         
                //
                bool v_resultTestInverseMatrice2D = TestInversionMatrice2D(p_envoyerMessageSiKo_vf);
                v_resultats.Add("Inversion matrices 2D", v_resultTestInverseMatrice2D);
               
                //
                bool v_resultTestInverseMatrice3D = TestInversionMatrice3D(p_envoyerMessageSiKo_vf);
                v_resultats.Add("Inversion matrices 3D", v_resultTestInverseMatrice3D);
               
                //
                if (p_envoyerMessageSiKo_vf && v_resultats.Where(c=>!c.Value).Count()==0)
                {
                    MessageBox.Show("TESTS UNITAIRES LAB: pas d'anomalies détectées.");
                }
            }
            catch (Exception)
            {

                throw;
            }
            return v_resultats;
        }
        //
        internal bool TestIntersectionsDroitesXY_coherenceServicesParamVsServicesExplicites(bool p_envoyerMessageSiKo_vf)
        {
            bool v_resultats = true;
           
            ///
            double[] v_coordPtA;
            double[] v_coordPtB;
            double[] v_coordPtC;
            double[] v_coordPtD;

            ////Point quelconque:
            v_coordPtA = new double[2] { -4, 2 };
            v_coordPtB = new double[2] { 0, 4 };
            v_coordPtC = new double[2] { 4, 1 };
            v_coordPtD = new double[2] { 3, 3 };
            v_resultats=TestIntersectionDroitesXy(v_coordPtA, v_coordPtB, v_coordPtC, v_coordPtD, p_envoyerMessageSiKo_vf);
            if(!v_resultats)
            {
                return false;
            }

            //Droite 1 verticale
            v_coordPtA = new double[2] { 0, 2 };
            v_coordPtB = new double[2] { 0, 4 };
            v_coordPtC = new double[2] { 4, 1 };
            v_coordPtD = new double[2] { 3, 3 };
            v_resultats = TestIntersectionDroitesXy(v_coordPtA, v_coordPtB, v_coordPtC, v_coordPtD, p_envoyerMessageSiKo_vf);
            if (!v_resultats)
            {
                return false;
            }

            //Droite 2 verticale
            v_coordPtA = new double[2] { -4, 2 };
            v_coordPtB = new double[2] { 0, 4 };
            v_coordPtC = new double[2] { 4, 1 };
            v_coordPtD = new double[2] { 4, 3 };
            v_resultats = TestIntersectionDroitesXy(v_coordPtA, v_coordPtB, v_coordPtC, v_coordPtD, p_envoyerMessageSiKo_vf);
            if (!v_resultats)
            {
                return false;
            }
            ////Parallèles:
            v_coordPtA = new double[2] { 1, 1 };
            v_coordPtB = new double[2] { 3, 3 };
            v_coordPtC = new double[2] { 3, 1 };
            v_coordPtD = new double[2] { 5, 3 };
            v_resultats = TestIntersectionDroitesXy(v_coordPtA, v_coordPtB, v_coordPtC, v_coordPtD, p_envoyerMessageSiKo_vf);
            if (!v_resultats)
            {
                return false;
            }

            ////Fusion:
            v_coordPtA = new double[2] { -4, 2 };
            v_coordPtB = new double[2] { 0, 4 };
            v_coordPtC = new double[2] { -4, 2 };
            v_coordPtD = new double[2] { 0, 4 };
            v_resultats = TestIntersectionDroitesXy(v_coordPtA, v_coordPtB, v_coordPtC, v_coordPtD, p_envoyerMessageSiKo_vf);
            if (!v_resultats)
            {
                return false;
            }
            //
            return v_resultats;
        }
        internal bool TestInversionMatrice2D(bool p_envoyerMessageSiKo_vf)
        {
            bool v_resultat = true;
            try
            {
                //1-Matrice quelconque
                double[,] v_matriceSource;
                int param_seedRandomisateur;
                param_seedRandomisateur = 11;
                v_matriceSource = GetMatriceCarreeAleatoire(2, -100, 100, param_seedRandomisateur);
                double[,] v_matriceInverse;
                v_matriceInverse = FLabServices.createCalculLow().GetMatriceInverse2x2(v_matriceSource);
                v_resultat = TestInversionMatrice(v_matriceSource, v_matriceInverse, p_envoyerMessageSiKo_vf);
                if(!v_resultat)
                {
                    return false;
                }
                

            }
            catch (Exception)
            {
                throw;
            }
            return v_resultat;
        }
        internal bool TestInversionMatrice3D(bool p_envoyerMessageSiKo_vf)
        {
            bool v_resultat = true;
            try
            {
                //1-Matrice quelconque
                double[,] v_matriceSource;
                int param_seedRandomisateur;
                param_seedRandomisateur = 11;
                v_matriceSource = GetMatriceCarreeAleatoire(3, -100, 100, param_seedRandomisateur);
                double[,] v_matriceInverse;
                v_matriceInverse = FLabServices.createCalculLow().GetMatriceInverse3x3(v_matriceSource);
                v_resultat = TestInversionMatrice(v_matriceSource, v_matriceInverse, p_envoyerMessageSiKo_vf);
                if (!v_resultat)
                {
                    return false;
                }


            }
            catch (Exception)
            {
                throw;
            }
            return v_resultat;
        }

        //
        private bool TestInversionMatrice(double[,] v_matriceSource, double[,] v_matriceInverse, bool p_envoyerMessageSiKo_vf)
        {
            bool v_resultat = true;
            try
            {
                int param_tolerance = 6;
                v_resultat=FLabServices.createCalculLow().IsInversionMatriceOk(v_matriceSource, v_matriceInverse, param_tolerance);
                if(p_envoyerMessageSiKo_vf && !v_resultat)
                {
                    string v_message = "Tests inversion matrice KO \n ";
                   
                    MessageBox.Show(v_message);
                }
            }
            catch (Exception)
            {

                throw;
            }
            return v_resultat;
        }
        private bool TestIntersectionDroitesXy(double[] v_coordPtA, double[] v_coordPtB, double[] v_coordPtC, double[] v_coordPtD, bool p_envoyerMessageSiKo_vf)
        {
            bool v_retour = true;
            double[] v_coordIntersectionParametrique;
            double[] v_coordIntersectionExplicite;

            v_coordIntersectionParametrique = FLabServices.createCalculLow().GetIntersectionDroites2DMethodeParametrique(v_coordPtA, v_coordPtB, v_coordPtC, v_coordPtD);
            v_coordIntersectionExplicite = FLabServices.createCalculLow().GetIntersectionDroites2D(v_coordPtA, v_coordPtB, v_coordPtC, v_coordPtD);

            if (v_coordIntersectionParametrique == null && v_coordIntersectionExplicite == null)
            {
                return true;
            }

            if (v_coordIntersectionParametrique[0] != v_coordIntersectionExplicite[0] || v_coordIntersectionParametrique[1] != v_coordIntersectionExplicite[1])
            {
                if (p_envoyerMessageSiKo_vf)
                {
                    string v_message = "Tests intersection ";
                    v_message += " KO\n";
                    v_message += "Parametriques " + v_coordIntersectionParametrique[0] + " / " + v_coordIntersectionParametrique[1] + "\n";
                    v_message += "Explicites " + v_coordIntersectionExplicite[0] + " / " + v_coordIntersectionExplicite[1] + "\n";
                    MessageBox.Show(v_message);
                }


                return false;
            }
            return v_retour;
        }
        //
        internal double[,] GetMatriceCarreeAleatoire(int p_dimensions, double p_valeurMin, double p_valeurMax, int p_seedMoins1sinon)
        {
            double[,] v_matriceColLigne = new double[p_dimensions, p_dimensions];
            try
            {
                Random v_random;
                if (p_seedMoins1sinon < 0)
                {
                    v_random = new Random();
                }
                else
                {
                    v_random = new Random(p_seedMoins1sinon);
                }
                //
                double v_ecart = p_valeurMax - p_valeurMin;
                for(int v_indiceLigne=0; v_indiceLigne< p_dimensions; v_indiceLigne++)
                {
                    for (int v_indiceCol = 0; v_indiceCol < p_dimensions; v_indiceCol++)
                    {
                        v_matriceColLigne[v_indiceLigne, v_indiceCol]=(v_random.Next(0, 1000)/1000d * v_ecart) + p_valeurMin;
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
            return v_matriceColLigne;
        }
        internal Dictionary<int, double[]> GetVecteursAleatoires(int p_dimensions,int p_nbreVecteurs,double p_valeurMin, double p_valeurMax, int p_seedMoins1sinon)
        {
            Dictionary<int, double[]> v_vecteursOut = new Dictionary<int, double[]>();
            try
            {
                Random v_random;
                if (p_seedMoins1sinon  < 0)
                {
                    v_random = new Random();
                }
                else
                {
                    v_random = new Random(p_seedMoins1sinon );
                }
                //
                double v_ecart = p_valeurMax - p_valeurMin;
                double[] v_coord;
                for (int v_indice = 0; v_indice < p_nbreVecteurs; v_indice++)
                {
                    v_coord = new double[p_dimensions];
                    for (int v_dim = 0; v_dim < p_dimensions; v_dim++)
                    {
                        v_coord[v_dim] = (v_random.Next(0, 1) * v_ecart) + p_valeurMin;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return v_vecteursOut;
        }
    }
}
