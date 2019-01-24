using AssetGenerator;
using AssetGenerator.Runtime;
using DEM.Net.glTF;
using DEM.Net.Lib;
using DEM.Net.Lib.Imagery;
using DEM.Net.Lib.Services.Lab;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SampleApp
{
    class GpxSamples
    {
        private readonly IElevationService _elevationService;
        private readonly string _gpxFile;
        private DEMDataSet _dataSet;
        private readonly string _outputDirectory;

        public GpxSamples(IElevationService elevationService, string outputDirectory, string gpxFile)
        {
            _elevationService = elevationService;
            _dataSet = DEMDataSet.AW3D30;
            _outputDirectory = outputDirectory;
            _gpxFile = gpxFile;
        }

        internal void Run()
        {
            bool withTexture = true;
            bool generateTIN = true;
            glTFService glTF = new glTFService();
            List<MeshPrimitive> meshes = new List<MeshPrimitive>();
            string outputDir = Path.GetFullPath(Path.Combine(_outputDirectory, "glTF"));


            //=======================
            /// Line strip from GPX
            ///
            // Get GPX points
            var segments = GpxImport.ReadGPX_Segments(_gpxFile);
            var points = segments.SelectMany(seg => seg);
            var bbox = points.GetBoundingBox().Scale(1.1);

            var gpxPointsElevated = _elevationService.GetPointsElevation(points, _dataSet);

            //
            //=======================

            //=======================
            // Textures
            //
            TextureInfo texInfo = null;
            if (withTexture)
            {

                ImageryService imageryService = new ImageryService();

                Console.WriteLine("Download image tiles...");
                TileRange tiles = imageryService.DownloadTiles(bbox, ImageryProvider.MapBoxSatellite, 8);
                string fileName = Path.Combine(outputDir, "Texture.jpg");

                Console.WriteLine("Construct texture...");
                texInfo = imageryService.ConstructTexture(tiles, bbox, fileName, TextureImageFormat.image_jpeg);
            }
            //
            //=======================

            //=======================
            // MESH 3D terrain
            Console.WriteLine("Height map...");
            HeightMap hMap = _elevationService.GetHeightMap(bbox, _dataSet);

            Console.WriteLine("GenerateTriangleMesh...");
            MeshPrimitive triangleMesh = null;
            if (generateTIN)
            {
                triangleMesh = GenerateTIN(hMap, glTF, PBRTexture.Create(texInfo));
            }
            else
            {
                hMap = hMap.CenterOnOrigin(0.00002f);

                // generate mesh with texture
                triangleMesh = glTF.GenerateTriangleMesh(hMap, null, PBRTexture.Create(texInfo));
            }
            meshes.Add(triangleMesh);

            // take 1 point evert nth
            int nSkip = 2;
            gpxPointsElevated = gpxPointsElevated.Where((x, i) => (i + 1) % nSkip == 0);
            gpxPointsElevated = gpxPointsElevated.Select(pt => { pt.Elevation += 5; return pt; });
            gpxPointsElevated = gpxPointsElevated.CenterOnOrigin(hMap.BoundingBox, 0.00002f);
            MeshPrimitive gpxLine = glTF.GenerateLine(gpxPointsElevated, new Vector4(1, 0, 0, 0.5f), 0.00015f);
            meshes.Add(gpxLine);

            // model export
            Console.WriteLine("GenerateModel...");
            Model model = glTF.GenerateModel(meshes, this.GetType().Name);
            glTF.Export(model, outputDir, $"{GetType().Name} Packed", false, true);
        }

        private MeshPrimitive GenerateTIN(HeightMap hMap, IglTFService gltf, PBRTexture textures)
        {
            int v_sridCible = 2154;
            hMap = hMap.ReprojectTo(4326, v_sridCible);
            var v_pointsToTest = GetGeoPointsByHMap(hMap, v_sridCible);


            var _paramTin = FLabServices.createCalculMedium().GetParametresDuTinParDefaut();
            _paramTin.p11_initialisation_determinationFrontieres = enumModeDelimitationFrontiere.pointsProchesDuMbo;
            _paramTin.p12_extensionSupplementaireMboEnM = 1000;
            _paramTin.p13_modeCalculZParDefaut = enumModeCalculZ.alti_0;
            _paramTin.p14_altitudeParDefaut = -200;
            _paramTin.p15_nbrePointsSupplMultiples4 = 4;
            _paramTin.p16_initialisation_modeChoixDuPointCentral.p01_excentrationMinimum = 0;
            _paramTin.p21_enrichissement_modeChoixDuPointCentral.p01_excentrationMinimum = 10;
            _paramTin.p31_nbreIterationsMaxi = 10;
            var _topolFacettes = FLabServices.createCalculMedium().GetInitialisationTin(v_pointsToTest, _paramTin);

            FLabServices.createCalculMedium().AugmenteDetailsTinByRef(ref _topolFacettes, _paramTin);


            Dictionary<int, int> v_indiceParIdPoint = new Dictionary<int, int>();
            int v_indice = 0;
            GeoPoint v_geoPoint;
            List<GeoPoint> p00_geoPoint = new List<GeoPoint>(_topolFacettes.p11_pointsFacettesByIdPoint.Count);
            List<List<int>> p01_listeIndexPointsfacettes = new List<List<int>>(_topolFacettes.p13_facettesById.Count);

            foreach (BeanPoint_internal v_point in _topolFacettes.p11_pointsFacettesByIdPoint.Values)
            {
                v_geoPoint = new GeoPoint(v_point.p10_coord[1], v_point.p10_coord[0], (float)v_point.p10_coord[2], 0, 0);
                p00_geoPoint.Add(v_geoPoint);
                v_indiceParIdPoint.Add(v_point.p00_id, v_indice);
                v_indice++;
            }
            p00_geoPoint = p00_geoPoint.ReprojectTo(2154, 4326).CenterOnOrigin(0.00002f).ToList();


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
                    p01_listeIndexPointsfacettes.Add(v_listeIndices);
                }
            }
            MeshPrimitive v_trianglesMesh = gltf.GenerateTriangleMesh(p00_geoPoint, p01_listeIndexPointsfacettes.SelectMany(c => c).ToList(), null, textures);

            return v_trianglesMesh;

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
    }
}
