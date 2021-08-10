using DEM.Net.Core.Services.Lab;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DEM.Net.Core;
using DEM.Net.Core.Datasets;

namespace DEM.Net.TestWinForm
{
    public class EchantillonsTestsServices : IServicesApplicatifs
    {
        public List<BeanPoint_internal> GetPointsTestsByBBox(string p_bbox, DEMDataSet dataset, int sridCible)
        {
            List<BeanPoint_internal> v_pointsToTest = new List<BeanPoint_internal>();
            try
            {
                // fix issue #86 to work with opentopography files without proper DI injection
                RasterIndexServiceResolver rasterIndexServiceResolver = dataSourceType =>
                {
                    switch (dataSourceType)
                    {
                        case DEMDataSourceType.GDALVrt:
                            return new GDALVRTFileService(null, null);
                        default:
                            throw new KeyNotFoundException(); // or maybe return null, up to you
                    }
                };
                RasterService v_rasterService = new RasterService(rasterIndexServiceResolver, null);
                ElevationService v_elevationService = new ElevationService(v_rasterService, null);
                BoundingBox v_bbox = GeometryService.GetBoundingBox(p_bbox);
                v_elevationService.DownloadMissingFiles(dataset, v_bbox);
                //
                HeightMap v_hMap;
                v_hMap = v_elevationService.GetHeightMap(ref v_bbox, dataset);


                v_hMap = v_hMap.ReprojectTo(4326, sridCible);
                v_pointsToTest = GetGeoPointsByHMap(v_hMap, sridCible);
            }
            catch (Exception)
            {

                throw;
            }
            return v_pointsToTest;
        }
        public List<BeanPoint_internal> GetGeoPointsByHMap(HeightMap p_hMap, int p_srid)
        {
            return p_hMap.Coordinates.Select(c => GetPointInternalFromGeoPoint(c, p_srid)).ToList();
        }
        public BeanPoint_internal GetPointInternalFromGeoPoint(GeoPoint p_geoPoint, int p_srid)
        {
            BeanPoint_internal v_ptInternal = new BeanPoint_internal(p_geoPoint.Longitude, p_geoPoint.Latitude, p_geoPoint.Elevation.GetValueOrDefault(0), p_srid);
            return v_ptInternal;
        }
        public List<BeanPoint_internal> GetPointsTests(BeanParamGenerationAutoDePointsTests p_paramGenerationPointsTest)
        {
            List<BeanPoint_internal> v_pointsToTest = new List<BeanPoint_internal>();
            try
            {
                v_pointsToTest = GetPointsTestsXY(p_paramGenerationPointsTest);
                UpdatePointsTests_Z(ref v_pointsToTest, p_paramGenerationPointsTest);
            }
            catch (Exception)
            {
                throw;
            }
            return v_pointsToTest;
        }
        //Couverture XY
        internal List<BeanPoint_internal> GetPointsTestsXY(BeanParamGenerationAutoDePointsTests p_paramGenerationPointsTest)
        {
            List<BeanPoint_internal> v_pointsToTest = new List<BeanPoint_internal>();
            try
            {
                switch (p_paramGenerationPointsTest.p01_modeGenerationXY)
                {
                    case enumMethodeGenerationPtsEnXetY.repartitionAleatoireUniforme:
                        v_pointsToTest = GetPointsTestsXY_RepartitionUniforme(p_paramGenerationPointsTest);
                        break;
                    case enumMethodeGenerationPtsEnXetY.carroyageRegulierParPas:
                        v_pointsToTest = GetPointsTestsXY_RepartitionReguliere(p_paramGenerationPointsTest);
                        break;
                    default:
                        throw new Exception("Méthode " + p_paramGenerationPointsTest.p01_modeGenerationXY + " non implémentée");
                }

            }
            catch (Exception)
            {

                throw;
            }
            return v_pointsToTest;
        }


        internal List<BeanPoint_internal> GetPointsTestsXY_RepartitionUniforme(BeanParamGenerationAutoDePointsTests p_paramGenerationPointsTest)
        {
            List<BeanPoint_internal> v_pointsToTest = new List<BeanPoint_internal>();
            try
            {
                Random v_random = new Random(p_paramGenerationPointsTest.p32_seed);
                double[] v_coord;
                //[Attention: nécessité de passer en int)
                int v_minX = Convert.ToInt32(Math.Round(p_paramGenerationPointsTest.p11_pointBasGaucheX, 0));
                int v_maxX = Convert.ToInt32(Math.Round(p_paramGenerationPointsTest.p13_pointHautDroitX, 0));
                int v_minY = Convert.ToInt32(Math.Round(p_paramGenerationPointsTest.p12_pointBasGaucheY, 0));
                int v_maxY = Convert.ToInt32(Math.Round(p_paramGenerationPointsTest.p14_pointHautDroitY, 0));
                //
                HashSet<string> p_codes = new HashSet<string>();
                string p_code;
                int v_no = 0;
                for (v_no = 1; v_no <= p_paramGenerationPointsTest.p31_nbrePoints;)
                {
                    v_coord = new double[3];
                    v_coord[0] = v_random.Next(v_minX, v_maxX);
                    v_coord[1] = v_random.Next(v_minY, v_maxY);
                    v_coord[2] = 0;
                    //
                    BeanPoint_internal v_point = new BeanPoint_internal(v_coord, p_paramGenerationPointsTest.p10_srid);
                    // p_code = FLabServices.createUtilitaires().GethCodeGeogPoint(v_coord);
                    p_code = FLabServices.createUtilitaires().GetHCodeGeogPoint(v_coord);
                    //(On évite les doublons)
                    if (!p_codes.Contains(p_code))
                    {
                        p_codes.Add(p_code);
                        v_pointsToTest.Add(v_point);
                        v_no++;
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
            return v_pointsToTest;
        }
        internal List<BeanPoint_internal> GetPointsTestsXY_RepartitionReguliere(BeanParamGenerationAutoDePointsTests p_paramGenerationPointsTest)
        {
            List<BeanPoint_internal> v_pointsToTest = new List<BeanPoint_internal>();
            try
            {
                BeanPoint_internal v_point;
                double[] v_coord;

                double v_coordX = p_paramGenerationPointsTest.p11_pointBasGaucheX;
                double v_coordY = p_paramGenerationPointsTest.p12_pointBasGaucheY;
                while (v_coordX < p_paramGenerationPointsTest.p13_pointHautDroitX)
                {
                    v_coordY = p_paramGenerationPointsTest.p12_pointBasGaucheY;
                    while (v_coordY < p_paramGenerationPointsTest.p14_pointHautDroitY)
                    {
                        v_coord = new double[3];
                        v_coord[0] = v_coordX;
                        v_coord[1] = v_coordY;
                        v_point = new BeanPoint_internal(v_coord, p_paramGenerationPointsTest.p10_srid);
                        v_pointsToTest.Add(v_point);
                        //
                        v_coordY += p_paramGenerationPointsTest.p32_pasEntrePointsEnM;
                    }
                    v_coordX += p_paramGenerationPointsTest.p32_pasEntrePointsEnM;
                    v_coordY = p_paramGenerationPointsTest.p12_pointBasGaucheY;
                }
            }
            catch (Exception)
            {
                throw;
            }
            return v_pointsToTest;
        }

        //Mise à jour Z
        internal void UpdatePointsTests_Z(ref List<BeanPoint_internal> p_points, BeanParamGenerationAutoDePointsTests p_paramGenerationPointsTest)
        {
            try
            {
                switch (p_paramGenerationPointsTest.p02_modeGenerationEnZ)
                {
                    case enumMethodeGenerationValeursEnZ.altitudeConstante:
                        UpdatePointsTests_Z_constante(ref p_points, p_paramGenerationPointsTest);
                        break;
                    case enumMethodeGenerationValeursEnZ.plan:
                        UpdatePointsTests_Z_NonConstante(ref p_points, p_paramGenerationPointsTest);
                        break;
                    case enumMethodeGenerationValeursEnZ.paraboloideElliptique:
                        UpdatePointsTests_Z_NonConstante(ref p_points, p_paramGenerationPointsTest);
                        break;
                    case enumMethodeGenerationValeursEnZ.paraboloideHyperbolique:
                        UpdatePointsTests_Z_NonConstante(ref p_points, p_paramGenerationPointsTest);
                        break;
                    default:
                        throw new Exception("Méthode " + p_paramGenerationPointsTest.p02_modeGenerationEnZ + " non implémentée");
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
        void UpdatePointsTests_Z_constante(ref List<BeanPoint_internal> p_points, BeanParamGenerationAutoDePointsTests p_paramGenerationPointsTest)
        {
            try
            {
                foreach (BeanPoint_internal v_point in p_points)
                {
                    v_point.p10_coord[2] = p_paramGenerationPointsTest.p51_hauteurRefEnM;
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
        void UpdatePointsTests_Z_NonConstante(ref List<BeanPoint_internal> p_points, BeanParamGenerationAutoDePointsTests p_paramGenerationPointsTest)
        {
            try
            {
                Dictionary<enumCoeffRecalage, double> v_coeffRecalage = RenormalisationDuPlanXY(p_paramGenerationPointsTest);
                //
                double[] v_coordRecalees;
                foreach (BeanPoint_internal v_point in p_points)
                {
                    v_coordRecalees = GetValeurXYNormalisees(v_point.p10_coord, v_coeffRecalage);
                    switch (p_paramGenerationPointsTest.p02_modeGenerationEnZ)
                    {
                        case enumMethodeGenerationValeursEnZ.plan:
                            v_point.p10_coord[2] = (v_coordRecalees[0] * p_paramGenerationPointsTest.p52_coeff_X) + (v_coordRecalees[1] * p_paramGenerationPointsTest.p53_coeff_Y) + p_paramGenerationPointsTest.p51_hauteurRefEnM;
                            break;
                        case enumMethodeGenerationValeursEnZ.paraboloideElliptique:
                            v_point.p10_coord[2] = (v_coordRecalees[0] * v_coordRecalees[0] * p_paramGenerationPointsTest.p52_coeff_X) + (v_coordRecalees[1] * v_coordRecalees[1] * p_paramGenerationPointsTest.p53_coeff_Y) + p_paramGenerationPointsTest.p51_hauteurRefEnM;
                            break;
                        case enumMethodeGenerationValeursEnZ.paraboloideHyperbolique:
                            v_point.p10_coord[2] =
                                ((
                                ((v_coordRecalees[0] / p_paramGenerationPointsTest.p52_coeff_X) * (v_coordRecalees[0] / p_paramGenerationPointsTest.p52_coeff_X))
                                + ((v_coordRecalees[1] / p_paramGenerationPointsTest.p53_coeff_Y) * (v_coordRecalees[1] / p_paramGenerationPointsTest.p53_coeff_Y))
                                ) * -1)
                                + p_paramGenerationPointsTest.p51_hauteurRefEnM;
                            break;
                        default:
                            throw new Exception("Méthode " + p_paramGenerationPointsTest.p02_modeGenerationEnZ + " non implémentée.");
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
        private double[] GetValeurXYNormalisees(double[] p_point, Dictionary<enumCoeffRecalage, double> p_parametresDeNormalisation)
        {
            double[] v_resultat = new double[2];
            v_resultat[0] = (p_point[0] - p_parametresDeNormalisation[enumCoeffRecalage.centreX]) * p_parametresDeNormalisation[enumCoeffRecalage.coeffRecalageX];
            v_resultat[1] = (p_point[1] - p_parametresDeNormalisation[enumCoeffRecalage.centreY]) * p_parametresDeNormalisation[enumCoeffRecalage.coeffRecalageY];
            return v_resultat;
        }
        private Dictionary<enumCoeffRecalage, double> RenormalisationDuPlanXY(BeanParamGenerationAutoDePointsTests p_paramGenerationPointsTest)
        {
            Dictionary<enumCoeffRecalage, double> v_coeff = new Dictionary<enumCoeffRecalage, double>();
            try
            {
                double v_origineX = p_paramGenerationPointsTest.p11_pointBasGaucheX;
                double v_origineY = p_paramGenerationPointsTest.p12_pointBasGaucheY;

                double v_centreX = ((p_paramGenerationPointsTest.p13_pointHautDroitX - p_paramGenerationPointsTest.p11_pointBasGaucheX) / 2) + p_paramGenerationPointsTest.p11_pointBasGaucheX;
                double v_centreY = ((p_paramGenerationPointsTest.p14_pointHautDroitY - p_paramGenerationPointsTest.p12_pointBasGaucheY) / 2) + p_paramGenerationPointsTest.p12_pointBasGaucheY;
                //
                double v_extensionX = Math.Abs(p_paramGenerationPointsTest.p13_pointHautDroitX - p_paramGenerationPointsTest.p11_pointBasGaucheX);
                double v_extensionY = Math.Abs(p_paramGenerationPointsTest.p14_pointHautDroitY - p_paramGenerationPointsTest.p12_pointBasGaucheY);

                double v_coeff_X = (p_paramGenerationPointsTest.p42_recalageMaxX - p_paramGenerationPointsTest.p41_recalageMinX) / v_extensionX;
                double v_coeff_Y = (p_paramGenerationPointsTest.p44_recalageMaxY - p_paramGenerationPointsTest.p43_recalageMinY) / v_extensionY;
                //
                v_coeff.Add(enumCoeffRecalage.centreX, v_centreX);
                v_coeff.Add(enumCoeffRecalage.centreY, v_centreY);
                v_coeff.Add(enumCoeffRecalage.origineX, v_origineX);
                v_coeff.Add(enumCoeffRecalage.origineY, v_origineY);
                //
                v_coeff.Add(enumCoeffRecalage.coeffRecalageY, v_coeff_Y);
                v_coeff.Add(enumCoeffRecalage.coeffRecalageX, v_coeff_X);
            }
            catch (Exception)
            {
                throw;
            }
            return v_coeff;
        }
        //

    }
}
