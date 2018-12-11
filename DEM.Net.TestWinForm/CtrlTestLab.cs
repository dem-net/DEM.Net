using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DEM.Net.Lib.Services.Lab;
using System.IO;
using SqlServerSpatial.Toolkit;
using DEM.Net.glTF;
using System.Numerics;
using AssetGenerator.Runtime;
using AssetGenerator;
using DEM.Net.Lib;

namespace DEM.Net.TestWinForm
{
    public partial class CtrlTestLab : UserControl
    {
        private static BeanParamGenerationAutoDePointsTests _paramGenerationPoints;
        private static List<BeanPoint_internal> _dataPointsTests;

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
            lb_modeGenerationXY.SelectedIndex = 0;
            //
            foreach (string v_modeGenerationZ in Enum.GetNames(typeof(enumMethodeGenerationValeursEnZ)))
            {
                lb_modeGenerationZ.Items.Add(v_modeGenerationZ);
            }
            lb_modeGenerationZ.SelectedIndex = 1;
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
            Dictionary<string, List<BeanPoint_internal>> v_classifDesPoints;
            v_classifDesPoints = FServicesApplicatifs.createStatsPopServices().GetPointsParClasseOrdonnees(_dataPointsTests, 10, enumModeSeuillage.memeNombreDIndividus);
            Dictionary<string, Color> v_tableCouleurs;
            v_tableCouleurs = FServicesApplicatifs.createVisuSpatialTrace().GetTableCouleursDegradees(v_classifDesPoints.Keys.ToList(), enumProgressionCouleurs.greenVersRed, 120, true);
            FServicesApplicatifs.createVisuSpatialTrace().VisuPoints(v_classifDesPoints, v_tableCouleurs);
            //
            MessageBox.Show("Visualisez dans SpatialTrace.");
            SpatialTrace.ShowDialog();
        }

        private void btnTestPoints_Click(object sender, EventArgs e)
        {
            IglTFService glTFService = new glTFService();


            MeshPrimitive pointMesh = glTFService.GeneratePointMesh(FromBeanPoint_internalToGeoPoint(_dataPointsTests), new Vector3(1, 0, 0), 0);
            Model model = glTFService.GenerateModel(pointMesh, "Test Points");
            glTFService.Export(model, "testpoints.glb", "Test points", false, true);
        }

        private IEnumerable<GeoPoint> FromBeanPoint_internalToGeoPoint(List<BeanPoint_internal> dataPointsTests)
        {
            return dataPointsTests.Select(ptIn => new GeoPoint(ptIn.p10_coord[1], ptIn.p10_coord[0], (float)ptIn.p10_coord[2], 0, 0));
            //foreach (BeanPoint_internal ptIn in dataPointsTests)
            //{
            //    yield return new GeoPoint(ptIn.p10_coord[1], ptIn.p10_coord[0], (float)ptIn.p10_coord[2], 0, 0);
            //}
        }
    }
}
