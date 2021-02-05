using DEM.Net.Core.Services.Lab;
using DEM.Net.Core.Services.VisualisationServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using NetTopologySuite.Geometries;

namespace DEM.Net.TestWinForm
{
    public class TestsDiversServices : ITestsDiversServices
    {
        public void TestChangementReferentiel2D(IEnumerable<BeanPoint_internal> p_points, double[] p_vecteurDeDecalage)
        {
            Color v_couleurObjet;
            string v_label;

            //POINTS REF
            double[] v_coordCentroide =FServicesApplicatifs.createCalculServicesMedium_testDivers().GetCentroide(p_points);
            BeanPoint_internal v_centroide = new BeanPoint_internal(v_coordCentroide, 2154);
            //
            double[] p_coordPointX = new double[3];
            p_coordPointX[0] = v_coordCentroide[0] + p_vecteurDeDecalage[0];
            p_coordPointX[1] = v_coordCentroide[1] + p_vecteurDeDecalage[1];
            p_coordPointX[2] = 0;
            BeanPoint_internal p_pointX = new BeanPoint_internal(p_coordPointX, 2154);

            //VISU POINTS REF
            v_couleurObjet = Color.FromArgb(255, 0, 0, 255);
            FVisualisationServices.createVisualisationSpatialTraceServices().GetVisuPoint2D(v_centroide, "Pt0", v_couleurObjet, 20);
            v_couleurObjet = Color.FromArgb(255, 0, 255, 0);
            FVisualisationServices.createVisualisationSpatialTraceServices().GetVisuPoint2D(p_pointX, "Pt1", v_couleurObjet, 20);
            //FIN VISU POINTS REF



            Dictionary<int, double[]> v_coordDansRef;
            v_coordDansRef = FServicesApplicatifs.createCalculServicesMedium_testDivers().GetCoordonneesDansNewReferentiel2D(p_points, v_coordCentroide, p_coordPointX, null);



            Dictionary<int, BeanPoint_internal> v_dicoPoints = p_points.ToDictionary(c => c.p00_id, c => c);

            //VISU COORD
            int param_arrondi = 2;
            v_couleurObjet = Color.FromArgb(255, 0, 0, 255);
            List<int> v_idPointsOrdonnes = v_coordDansRef.OrderBy(c => c.Value[1]).Select(c => c.Key).ToList();
            foreach (int v_id in v_idPointsOrdonnes)
            {
                v_label = "Pt: " + v_id + " => " + Math.Round(v_coordDansRef[v_id][0], param_arrondi) + " / " + Math.Round(v_coordDansRef[v_id][1], param_arrondi);
                FVisualisationServices.createVisualisationSpatialTraceServices().GetVisuPoint2D(v_dicoPoints[v_id], v_label, v_couleurObjet, 10);
            }
            FVisualisationServices.createVisualisationSpatialTraceServices().AfficheVisu();
            //FIN VISU COORD

        }
        public void TestIsInCercleCirconscrit()
        {
            double v_xMin = 1500000;
            double v_yMin = 6000000;
            //
           
            List<double[]> v_pointsTriangle = new List<double[]>();
            double[] v_pointA;
            double[] v_pointB;
            double[] v_pointC;
            double[] v_pointToTest;
            string p_message;
            //Triangle 1
            v_pointA = new double[2] { v_xMin+50, v_yMin + 100 };
            v_pointB = new double[2] { v_xMin, v_yMin };
            v_pointC = new double[2] { v_xMin + 100, v_yMin };
            v_pointsTriangle.Add(v_pointA);
            v_pointsTriangle.Add(v_pointB);
            v_pointsTriangle.Add(v_pointC);
            //
            v_pointToTest = new double[2] { v_xMin + 90, v_yMin + 90 };
            p_message = "Test 1 (point n'est pas dans le cercle)=> FALSE";
            TestIsInCercleCirconscrit(v_pointsTriangle, v_pointToTest, p_message);

            v_pointToTest = new double[2] { v_xMin + 50, v_yMin + 50 };
            p_message = "Test 2 (point est dans le cercle)=> TRUE";
            TestIsInCercleCirconscrit(v_pointsTriangle, v_pointToTest, p_message);

            v_pointToTest = new double[2] { v_xMin + 100, v_yMin };
            p_message = "Test 3 (point est sur le cercle)=> TRUE";
            TestIsInCercleCirconscrit(v_pointsTriangle, v_pointToTest, p_message);

            ////SpatialTrace.ShowDialog();
        }
        public void TestCercleCirconscritAuTriangle()
        {
            double v_xMin = 1500000;
            double v_yMin = 6000000;
            //
            double[] v_pointA;
            double[] v_pointB;
            double[] v_pointC;


            //Triangle 1
            v_pointA = new double[2] { v_xMin + 50, v_yMin + 100 };
            v_pointB = new double[2] { v_xMin, v_yMin };
            v_pointC = new double[2] { v_xMin + 100, v_yMin };
            VisuCercleCirconscritAuTriangle(v_pointA, v_pointB, v_pointC,"Triangle 1");

            //Triangle 2
            v_pointA = new double[2] { v_xMin, v_yMin + 100 };
            v_pointB = new double[2] { v_xMin, v_yMin };
            v_pointC = new double[2] { v_xMin + 100, v_yMin };
            VisuCercleCirconscritAuTriangle(v_pointA, v_pointB, v_pointC, "Triangle 2");

            //Triangle 3
            v_pointA = new double[2] { v_xMin, v_yMin + 100 };
            v_pointB = new double[2] { v_xMin, v_yMin };
            v_pointC = new double[2] { v_xMin + 300, v_yMin-100 };
            VisuCercleCirconscritAuTriangle(v_pointA, v_pointB, v_pointC, "Triangle 3");

            ////SpatialTrace.ShowDialog();

        }
        public void TestOrdonnancement()
        {
            double v_xMin = 1500000;
            double v_yMin = 6000000;
            //
            //double[] v_point1;
            //double[] v_point2;
            //double[] v_point3;
            Dictionary<int, double[]> v_points = new Dictionary<int, double[]>();

            bool v_renvoyerNullSiColineaire_vf;
            bool v_horaire_vf;

            ////Triangle 1
            //v_point1 = new double[2] { v_xMin + 50, v_yMin + 100 };
            //v_point2 = new double[2] { v_xMin, v_yMin };
            //v_point3 = new double[2] { v_xMin + 100, v_yMin};

            ////Triangle 2
            //v_point1 = new double[2] { v_xMin + 50, v_yMin + 100 };
            //v_point2 = new double[2] { v_xMin, v_yMin };
            //v_point3 = new double[2] { v_xMin + 100, v_yMin-50 };

            ////Triangle 3
            //v_point1 = new double[2] { v_xMin + 50, v_yMin + 100 };
            //v_point2 = new double[2] { v_xMin, v_yMin };
            //v_point3 = new double[2] { v_xMin + 100, v_yMin+500 };

            //v_points.Add(1, v_point1);
            //v_points.Add(2, v_point2);
            //v_points.Add(3, v_point3);

            //Groupe de points:

            BeanParamGenerationAutoDePointsTests v_paramGenerationPoints = new BeanParamGenerationAutoDePointsTests();
            v_paramGenerationPoints.p51_hauteurRefEnM = 100;
            v_paramGenerationPoints.p31_nbrePoints = 5;
            v_paramGenerationPoints.p11_pointBasGaucheX = v_xMin;
            v_paramGenerationPoints.p12_pointBasGaucheY = v_yMin;
            v_paramGenerationPoints.p13_pointHautDroitX = v_xMin+ v_paramGenerationPoints.p51_hauteurRefEnM;
            v_paramGenerationPoints.p14_pointHautDroitY = v_yMin + v_paramGenerationPoints.p51_hauteurRefEnM;
            v_paramGenerationPoints.p32_pasEntrePointsEnM = 6;
            v_paramGenerationPoints.p32_seed = 13;
            List<BeanPoint_internal> v_pointsTests;
            v_pointsTests=FServicesApplicatifs.createEchantillonsTestsServices().GetPointsTests(v_paramGenerationPoints);
            int v_indice= 0;
            v_points = v_pointsTests.ToDictionary(c => v_indice++, c => c.p10_coord);
             //
             v_renvoyerNullSiColineaire_vf = true;
            v_horaire_vf = false;


            List<int> v_pointsOrdonnances;
            v_pointsOrdonnances=FServicesApplicatifs.createCalculServicesLow_testDivers().GetOrdonnancement(v_points, v_renvoyerNullSiColineaire_vf, v_horaire_vf);
            string v_sens = "hor.";
            if(!v_horaire_vf)
            {
                v_sens = "antih.";
            }

            int v_no = 1;
            foreach (int v_id in v_pointsOrdonnances)
            {
                VisuPointSuppl(v_points[v_id], "no: " + v_no+" (pt "+ v_id+" "+ v_sens + ")", 2);
                v_no++;
            }
            ////SpatialTrace.ShowDialog();
        }

        //
        private void TestIsInCercleCirconscrit(List<double[]> p_pointsTriangle, double[] p_pointToTest, string p_messageResultatAttendu)
        {
            bool v_estInclusDansCercleExplicite;
            bool v_estInclusDansCercleMatrice;
            v_estInclusDansCercleExplicite = FServicesApplicatifs.createCalculServicesLow_testDivers().IsPointDDansCercleCirconscritAuTriangleExplicite(p_pointsTriangle, p_pointToTest);
            v_estInclusDansCercleMatrice = FServicesApplicatifs.createCalculServicesLow_testDivers().IsPointDDansCercleCirconscritAuTriangleByMatrice(p_pointsTriangle, p_pointToTest);
            //
            string v_label;
            v_label = p_messageResultatAttendu+"\n";
            v_label += "  Résultats calcul";
            v_label += " =>'Explicite': " + v_estInclusDansCercleExplicite.ToString();
            v_label += " =>'Matrice':" + v_estInclusDansCercleMatrice.ToString();
            VisuCercleCirconscritAuTriangle(p_pointsTriangle[0], p_pointsTriangle[1], p_pointsTriangle[2], v_label);
            VisuPointSuppl(p_pointToTest, "Pt à tester", 3);
        }
        private void VisuCercleCirconscritAuTriangle(double[] v_pointA,double[] v_pointB,double[] v_pointC, string p_message)
        {
            List<double[]> v_pointsTriangle;
            v_pointsTriangle = new List<double[]>();
            v_pointsTriangle.Add(v_pointA);
            v_pointsTriangle.Add(v_pointB);
            v_pointsTriangle.Add(v_pointC);
            //
            //SpatialTrace.Enable();
            //SpatialTrace.TraceText(p_message);
            Color param_couleurContour = Color.FromArgb(125, 125, 125);
            //SpatialTrace.SetLineColor(param_couleurContour);

            Color p_couleurCourante;
            int p_taillePoint;
            Geometry v_pointGeom;
            double[] v_pointCentre;
            double v_rayon;
            v_pointCentre = FServicesApplicatifs.createCalculServicesLow_testDivers().GetCoordonneesCercleCirconscritAuTriangle(v_pointsTriangle);
            v_rayon = Math.Sqrt(Math.Pow((v_pointA[0] - v_pointCentre[0]), 2) + Math.Pow((v_pointA[1] - v_pointCentre[1]), 2));
            //Cercle
            p_couleurCourante = Color.FromArgb(50, 50, 50);
            v_pointGeom = FLabServices.createUtilitaires().ConstructPoint(v_pointCentre[0], v_pointCentre[1], 2154).Buffer(v_rayon);
            //SpatialTrace.TraceGeometry(v_pointGeom, "Cercle", "Cercle");

            //Points du triangle
            p_taillePoint = 5;
            p_couleurCourante = Color.FromArgb(255, 0, 0);
            //SpatialTrace.SetFillColor(p_couleurCourante);
            v_pointGeom = FLabServices.createUtilitaires().ConstructPoint(v_pointA[0], v_pointA[1], 2154).Buffer(p_taillePoint);
            //SpatialTrace.TraceGeometry(v_pointGeom, "Pt A", "Pt A");
            v_pointGeom = FLabServices.createUtilitaires().ConstructPoint(v_pointB[0], v_pointB[1], 2154).Buffer(p_taillePoint);
            //SpatialTrace.TraceGeometry(v_pointGeom, "Pt B", "Pt B");
            v_pointGeom = FLabServices.createUtilitaires().ConstructPoint(v_pointC[0], v_pointC[1], 2154).Buffer(p_taillePoint);
            //SpatialTrace.TraceGeometry(v_pointGeom, "Pt C", "Pt C");
          
            //Cercle
            p_taillePoint = 8;
            p_couleurCourante = Color.FromArgb(0, 255, 0);
            //SpatialTrace.SetFillColor(p_couleurCourante);
            v_pointGeom = FLabServices.createUtilitaires().ConstructPoint(v_pointCentre[0], v_pointCentre[1], 2154).Buffer(p_taillePoint);
            //SpatialTrace.TraceGeometry(v_pointGeom, "Centre", "Centre");

            //SpatialTrace.Disable();
            //
        }
        private void VisuPointSuppl(double[] p_point, string p_message, int p_tailleDuPoint)
        {
            //SpatialTrace.Enable();
          
            Color param_couleurContour = Color.FromArgb(125, 125, 125);
            //SpatialTrace.SetLineColor(param_couleurContour);
            Color p_couleurCourante;
            Geometry v_pointGeom;
            p_couleurCourante = Color.FromArgb(50, 50, 50);
            //SpatialTrace.SetFillColor(p_couleurCourante);
           v_pointGeom = FLabServices.createUtilitaires().ConstructPoint(p_point[0], p_point[1], 2154).Buffer(p_tailleDuPoint);
            //SpatialTrace.TraceGeometry(v_pointGeom, p_message, p_message);
            //SpatialTrace.Disable();
        }
       
    }
    
}
