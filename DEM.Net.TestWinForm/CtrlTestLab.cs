using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DEM.Net.Core.Services.Lab;
using System.IO;
using DEM.Net.glTF;
using System.Numerics;
using DEM.Net.Core;
using DEM.Net.Core.Services.VisualisationServices;
//using NetTopologySuite.Diagnostics.Tracing;
using Microsoft.Extensions.Logging;
using DEM.Net.glTF.SharpglTF;
using NetTopologySuite.Geometries;

namespace DEM.Net.TestWinForm
{
    public partial class CtrlTestLab : UserControl
    {
        private static BeanParamGenerationAutoDePointsTests _paramGenerationPoints;
        BeanParametresDuTin _paramTin;
        private static List<BeanPoint_internal> _dataPointsTests;
        private static BeanTopologieFacettes _topolFacettes;

        public CtrlTestLab()
        {
            InitializeComponent();
            //
            lb_srid.Items.Add(enumSrid.Lambert93.ToString());
            lb_srid.SelectedIndex = 0;
            //
            foreach (string v_modeGenerationXY in Enum.GetNames(typeof(enumMethodeGenerationPtsEnXetY)))
            {
                lb_modeGenerationXY.Items.Add(v_modeGenerationXY);
            }
            lb_modeGenerationXY.SelectedIndex = 1;
            //
            foreach (string v_modeGenerationZ in Enum.GetNames(typeof(enumMethodeGenerationValeursEnZ)))
            {
                lb_modeGenerationZ.Items.Add(v_modeGenerationZ);
            }
            lb_modeGenerationZ.SelectedIndex = 3;
        }

        private void RemonteParametres()
        {
            _paramGenerationPoints = new BeanParamGenerationAutoDePointsTests();
            _paramGenerationPoints.p51_hauteurRefEnM = Convert.ToDouble(tb_hauteurMinEnM.Text);
            _paramGenerationPoints.p31_nbrePoints = Convert.ToInt32(tb_NbrePoints.Text);
            _paramGenerationPoints.p11_pointBasGaucheX = Convert.ToDouble(tb_pointBasGaucheX.Text);
            _paramGenerationPoints.p12_pointBasGaucheY = Convert.ToDouble(tb_pointBasGaucheY.Text);
            _paramGenerationPoints.p13_pointHautDroitX = Convert.ToDouble(tb_pointHautDroitX.Text);
            _paramGenerationPoints.p14_pointHautDroitY = Convert.ToDouble(tb_pointHautDroitY.Text);
            _paramGenerationPoints.p32_pasEntrePointsEnM = Convert.ToDouble(tb_pasSeparationEntrePoints.Text);
            _paramGenerationPoints.p32_seed = Convert.ToInt32(tb_seed.Text);
            //
            _paramGenerationPoints.p01_modeGenerationXY = (enumMethodeGenerationPtsEnXetY)Enum.Parse(typeof(enumMethodeGenerationPtsEnXetY), lb_modeGenerationXY.SelectedItem.ToString(), true);
            //
            _paramGenerationPoints.p02_modeGenerationEnZ = (enumMethodeGenerationValeursEnZ)Enum.Parse(typeof(enumMethodeGenerationValeursEnZ), lb_modeGenerationZ.SelectedItem.ToString(), true);
            //
            if (lb_srid.SelectedItem.ToString() == enumSrid.Lambert93.ToString())
            {
                _paramGenerationPoints.p10_srid = 2154;
            }
            else
            {
                throw new Exception("Méthode " + lb_srid.SelectedItem.ToString() + " non implémentée");
            }
            //
            _paramGenerationPoints.p41_recalageMinX = Convert.ToDouble(tb_recalageMinX.Text);
            _paramGenerationPoints.p42_recalageMaxX = Convert.ToDouble(tb_recalageMaxX.Text);
            _paramGenerationPoints.p43_recalageMinY = Convert.ToDouble(tb_recalageMinY.Text);
            _paramGenerationPoints.p44_recalageMaxY = Convert.ToDouble(tb_recalageMaxY.Text);
            //
            _paramGenerationPoints.p52_coeff_X = Convert.ToDouble(tb_coeffX.Text);
            _paramGenerationPoints.p53_coeff_Y = Convert.ToDouble(tb_coeffY.Text);
        }

        private void btn_genererPoints_Click(object sender, EventArgs e)
        {
            RemonteParametres();
            _dataPointsTests = FServicesApplicatifs.createEchantillonsTestsServices().GetPointsTests(_paramGenerationPoints);
            MessageBox.Show("Génération points tests terminée.");
        }

        private void btn_visualisationSpatialTrace_Click(object sender, EventArgs e)
        {
            if (_dataPointsTests == null || _dataPointsTests.Count == 0)
            {
                MessageBox.Show("Générez d'abord les points.");
                return;
            }
            FServicesApplicatifs.createVisuSpatialTrace().GetVisuPointsAlti(_dataPointsTests);
            FServicesApplicatifs.createVisuSpatialTrace().AfficheVisu();

        }




        private IEnumerable<GeoPoint> FromBeanPoint_internalToGeoPoint(List<BeanPoint_internal> dataPointsTests)
        {
            return dataPointsTests.Select(ptIn => new GeoPoint(ptIn.p10_coord[1], ptIn.p10_coord[0], (float)ptIn.p10_coord[2]));
            //foreach (BeanPoint_internal ptIn in dataPointsTests)
            //{
            //    yield return new GeoPoint(ptIn.p10_coord[1], ptIn.p10_coord[0], (float)ptIn.p10_coord[2], 0, 0);
            //}
        }

        private void btn_testsDivers_Click(object sender, EventArgs e)
        {
            ////TEST MATRICES 2D et changement référentiel
            //double[] v_decalage = new double[2];
            //v_decalage[0] += 10000;
            //v_decalage[1] += -10000;
            //FServicesApplicatifs.createTestsDivers().TestChangementReferentiel2D(_dataPointsTests, v_decalage);

            //
            //FServicesApplicatifs.createTestsDivers().TestCercleCirconscritAuTriangle();
            //FServicesApplicatifs.createTestsDivers().TestIsInCercleCirconscrit();
            FServicesApplicatifs.createTestsDivers().TestOrdonnancement();
        }

        private void btn_testCH_Click(object sender, EventArgs e)
        {
            List<BeanPoint_internal> v_pointConvexHull;
            v_pointConvexHull = FLabServices.createCalculMedium().GetConvexHull2D(_dataPointsTests);

            //
            string v_message = "ConvexHull calculé";
            v_message += " (" + v_pointConvexHull.Count + " pts sur " + _dataPointsTests.Count + ").";
            v_message += ". Affichez dans SpatialTrace ?";
            if (MessageBox.Show(v_message, "Calcul convexHull", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
            {
                return;
            }

            string v_label = "";
            Color v_couleurBase = Color.FromArgb(255, 0, 255, 255);
            FServicesApplicatifs.createVisuSpatialTrace().GetVisuPoints2D(_dataPointsTests, v_label, v_couleurBase, -1);
            v_label = "CH";
            FServicesApplicatifs.createVisuSpatialTrace().GetVisuPoints2D(v_pointConvexHull, v_label, 10);

            //SpatialTrace.Enable();
            Geometry v_ligneCh;
            for (int v_indexPt = 0; v_indexPt < v_pointConvexHull.Count - 1; v_indexPt++)
            {
                v_ligneCh = FLabServices.createUtilitaires().GetGeometryLine(v_pointConvexHull[v_indexPt].p10_coord, v_pointConvexHull[v_indexPt + 1].p10_coord, 2154, true);
                //SpatialTrace.TraceGeometry(v_ligneCh, "CH arc: " + v_indexPt, "CH arc: " + v_indexPt);
            }
            v_ligneCh = FLabServices.createUtilitaires().GetGeometryLine(v_pointConvexHull[v_pointConvexHull.Count - 1].p10_coord, v_pointConvexHull[0].p10_coord, 2154, true);
            //SpatialTrace.TraceGeometry(v_ligneCh, "CH arc: " + (v_pointConvexHull.Count - 1), "CH arc: " + (v_pointConvexHull.Count - 1));
            //SpatialTrace.Disable();
            FVisualisationServices.createVisualisationSpatialTraceServices().AfficheVisu();
            //
            MessageBox.Show("Traitement terminé.");
        }

        private void btn_testTin_Click(object sender, EventArgs e)
        {

            _paramTin = FLabServices.createCalculMedium().GetParametresDuTinParDefaut();
           // _paramTin.p11_initialisation_determinationFrontieres = enumModeDelimitationFrontiere.pointsProchesDuMbo;
            _paramTin.p11_initialisation_determinationFrontieres = enumModeDelimitationFrontiere.pointsProchesDuMbo;
            //_paramTin.p12_extensionSupplementaireMboEnM = 1000;
            _paramTin.p12_extensionSupplementaireMboEnM = 0;
            _paramTin.p13_modeCalculZParDefaut = enumModeCalculZ.alti_0;
            _paramTin.p14_altitudeParDefaut = -200;
            _paramTin.p15_nbrePointsSupplMultiples4 = 0;
            _paramTin.p16_initialisation_modeChoixDuPointCentral.p01_excentrationMinimum = 0;
            _paramTin.p21_enrichissement_modeChoixDuPointCentral.p01_excentrationMinimum = Convert.ToDouble(tb_precisionEnM.Text);

            //
            _topolFacettes = FLabServices.createCalculMedium().GetInitialisationTin(_dataPointsTests, _paramTin);
            FLabServices.createCalculMedium().AugmenteDetailsTinByRef(ref _topolFacettes, _paramTin);
            //
            bool v_visuSpatialTrace_vf = false;
            if (v_visuSpatialTrace_vf)
            {
                FVisualisationServices.createVisualisationSpatialTraceServices().GetVisuTopologieFacettes(_topolFacettes, false, false);
                FVisualisationServices.createVisualisationSpatialTraceServices().AfficheVisu();
            }
            MessageBox.Show("Traitement terminé.");
        }

        private void btn_clearSpatialTrace_Click(object sender, EventArgs e)
        {
            //SpatialTrace.Enable();
            //SpatialTrace.Clear();
            //SpatialTrace.Disable();
        }

        private void btn_genererPointsReels_Click(object sender, EventArgs e)
        {
            string v_bbox="";
            string v_sainteVictoire= "POLYGON((5.523314005345696 43.576096090257955, 5.722441202611321 43.576096090257955, 5.722441202611321 43.46456490270913, 5.523314005345696 43.46456490270913, 5.523314005345696 43.576096090257955))";
            string v_eyger= "Polygon((8.12951188622090193 46.634254667789655, 7.8854960299327308 46.63327193616965616, 7.89909222133881617 46.4319282954101098, 8.13595218741325965 46.43143509785498679, 8.12951188622090193 46.634254667789655))";
            string v_gorges="Polygon ((6.14901771150602894 43.8582708438193265, 6.30590241369230409 43.8575166880815317, 6.32080646040000005 43.74636314919661828, 6.14561854295865828 43.74579647280887684, 6.14901771150602894 43.8582708438193265))";
           
            if(rb_example_SteVictoire.Checked)
            {
                v_bbox = v_sainteVictoire;
            }
            if (rb_example_eyger.Checked)
            {
                v_bbox = v_eyger;
            }
            if (rb_example_Verdon.Checked)
            {
                v_bbox = v_gorges;
            }
            if (rb_example_WKT.Checked)
            {
                v_bbox = tb_wkt.Text;
            }


            DEMDataSet dataSet = null;
            if (rdSRTMGL3.Checked)
            {
                dataSet = DEMDataSet.SRTM_GL3;
            }
            else if (rdSRTMGL1.Checked)
            {
                dataSet = DEMDataSet.SRTM_GL1;
            }
            else
            {
                dataSet = DEMDataSet.AW3D30;
            }
            _dataPointsTests = FServicesApplicatifs.createEchantillonsTestsServices().GetPointsTestsByBBox(v_bbox, dataSet, int.Parse(txtSRID.Text));

            //Dictionary<string, int> v_doublons;
            //v_doublons=FLabServices.createCalculMedium().GetEtComptePointsDoublonnes(_dataPointsTests);
            MessageBox.Show("Remontée des points terminée (" + _dataPointsTests.Count + " points).");
        }

        private void btnTestFacettes_Click(object sender, EventArgs e)
        {
            BeanFacettesToVisu3D v_beanToVisu3d;
            v_beanToVisu3d = new BeanFacettesToVisu3D();

            Dictionary<int, int> v_indiceParIdPoint = new Dictionary<int, int>();
            int v_indice = 0;
            GeoPoint v_geoPoint;

            foreach (BeanPoint_internal v_point in _topolFacettes.p11_pointsFacettesByIdPoint.Values)
            {
                v_geoPoint = new GeoPoint(v_point.p10_coord[1], v_point.p10_coord[0], (float)v_point.p10_coord[2]);
                v_beanToVisu3d.p00_geoPoint.Add(v_geoPoint);
                v_indiceParIdPoint.Add(v_point.p00_id, v_indice);
                v_indice++;
            }
            //Création des listes d'indices et normalisation du sens des points favettes
            List<int> v_listeIndices;
            bool v_renvoyerNullSiPointsColineaires_vf = true;
            bool v_normalisationSensHoraireSinonAntihoraire = false;

            foreach (BeanFacette_internal v_facette in _topolFacettes.p13_facettesById.Values)
            {
                List<BeanPoint_internal> v_normalisationDuSens = FLabServices.createCalculMedium().GetOrdonnancementPointsFacette(v_facette.p01_pointsDeFacette, v_renvoyerNullSiPointsColineaires_vf, v_normalisationSensHoraireSinonAntihoraire);
                if (v_normalisationDuSens != null)
                {
                    v_listeIndices = new List<int>();
                    foreach (BeanPoint_internal v_ptFacette in v_normalisationDuSens)
                    {
                        v_listeIndices.Add(v_indiceParIdPoint[v_ptFacette.p00_id]);
                    }
                    v_beanToVisu3d.p01_listeIndexPointsfacettes.Add(v_listeIndices);
                }
            }
            //
            SharpGltfService glTFService = new SharpGltfService(new MeshService());
            var model = glTFService.GenerateTriangleMesh(v_beanToVisu3d.p00_geoPoint, v_beanToVisu3d.p01_listeIndexPointsfacettes.SelectMany(c => c).ToList(), null);

            
            string v_nomFichierOut = "TIN_";
            if (rdSRTMGL3.Checked)
            {
                v_nomFichierOut += "SRTM_GL3";
            }
            else if (rdSRTMGL1.Checked)
            {
                v_nomFichierOut += "SRTM_GL1";
            }
            else
            {
                v_nomFichierOut += "AW3D30";
            }
            v_nomFichierOut += "_p" + tb_precisionEnM.Text;
            model.SaveGLB(Path.Combine("Test3D", v_nomFichierOut + ".glb"));
            MessageBox.Show("Traitement terminé =>"+ v_nomFichierOut);
        }

        private void btn_testUnitaire_Click(object sender, EventArgs e)
        {
            bool v_afficherMessageSiko_vf = true;
            //
            FServicesApplicatifs.createTestsUnitairesLab().TestUnitairesLab(v_afficherMessageSiko_vf);
        }


        private void btn_creteEtTalwegTin_visu_Click(object sender, EventArgs e)
        {
            HashSet<enum_qualificationMorpho_arc> v_exclureDeLaVisu = new HashSet<enum_qualificationMorpho_arc>();
            v_exclureDeLaVisu.Add(enum_qualificationMorpho_arc.autre);
            //v_exclureDeLaVisu.Add(enum_qualificationMorpho_arc.talweg);
            //v_exclureDeLaVisu.Add(enum_qualificationMorpho_arc.crete);

            FServicesApplicatifs.createVisuSpatialTrace().GetVisuCreteEtTalweg(_topolFacettes, v_exclureDeLaVisu);
            FServicesApplicatifs.createVisuSpatialTrace().AfficheVisu();
            MessageBox.Show("Traitement terminé.");
        }

        private void btn_pentesTin_visuST_Click(object sender, EventArgs e)
        {
            int p_nbreClasses = 7;
            FVisualisationServices.createVisualisationSpatialTraceServices().GetVisuPentesFacettes(_topolFacettes, p_nbreClasses);
            FServicesApplicatifs.createVisuSpatialTrace().AfficheVisu();
            MessageBox.Show("Traitement terminé.");
        }

        private void btn_testVoronoi_Click(object sender, EventArgs e)
        {
            //
            int param_srid = 2154;
            if (_dataPointsTests == null || _dataPointsTests.Count == 0)
            {
                MessageBox.Show("Générez d'abord les points.");
                return;
            }
            //Il est impératif que tous les points soient distincts:
            Dictionary<string, List<BeanPoint_internal>> v_pourDeduplication;
            v_pourDeduplication=FLabServices.createUtilitaires().GetPointsRegroupesParHCode(_dataPointsTests);
            List<BeanPoint_internal> v_pointsDedupliques = v_pourDeduplication.SelectMany(c => c.Value).ToList();
            //
            BeanTopologieFacettes v_topolFacettes;
            v_topolFacettes=FLabServices.createVoronoiServices().GetTopologieVoronoi(v_pointsDedupliques, param_srid);
            FServicesApplicatifs.createVisuSpatialTrace().GetVisuArcsTopologie(v_topolFacettes, Color.Red, "Vor");
            FServicesApplicatifs.createVisuSpatialTrace().GetVisuIlots(v_topolFacettes, Color.Cyan, "Ilot V");
            FServicesApplicatifs.createVisuSpatialTrace().GetVisuPoints(v_pointsDedupliques, Color.Green, 3, "Pt");
        }
    }
}
