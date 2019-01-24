using AssetGenerator;
using AssetGenerator.Runtime;
using DEM.Net.glTF;
using DEM.Net.Lib;
using DEM.Net.Lib.Imagery;
using DEM.Net.Lib.Services.Lab;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SampleApp
{
    class TextureSamples
    {
        private readonly IElevationService _elevationService;
        private readonly string _bboxWkt;
        private DEMDataSet _dataSet;
        private readonly string _outputDirectory;

        public TextureSamples(IElevationService elevationService, string outputDirectory)
        {
            _elevationService = elevationService;
            // sugiton
            //_bboxWkt = "POLYGON ((5.42201042175293 43.20023317388979, 5.459775924682617 43.20023317388979, 5.459775924682617 43.22594305473314, 5.42201042175293 43.22594305473314, 5.42201042175293 43.20023317388979))";
            // ste victoire
            //_bboxWkt = "POLYGON((5.424004809009261 43.68472756348281, 5.884057299243636 43.68472756348281, 5.884057299243636 43.40402056297321, 5.424004809009261 43.40402056297321, 5.424004809009261 43.68472756348281))";
            // ventoux
            // _bboxWkt = "POLYGON ((5.192413330078125 44.12209907358672, 5.3015899658203125 44.12209907358672, 5.3015899658203125 44.201897151875094, 5.192413330078125 44.201897151875094, 5.192413330078125 44.12209907358672))";
            // duranne
            //_bboxWkt = "POLYGON ((5.303306579589844 43.45478810195138, 5.379180908203125 43.45478810195138, 5.379180908203125 43.51394981739109, 5.303306579589844 43.51394981739109, 5.303306579589844 43.45478810195138))";
            // ventoux debug
            //_bboxWkt = "POLYGON ((5.1340484619140625 44.17580225275465, 5.2700042724609375 44.17580225275465, 5.2700042724609375 44.21986144948162, 5.1340484619140625 44.21986144948162, 5.1340484619140625 44.17580225275465))";
            // zoom ste victoire
            //_bboxWkt = "POLYGON ((5.533332824707031 43.51668853502906, 5.582771301269531 43.51668853502906, 5.582771301269531 43.550289946081115, 5.533332824707031 43.550289946081115, 5.533332824707031 43.51668853502906))";
            // santiago
            //_bboxWkt = "POLYGON ((-70.8673095703125 -33.612331963363914, -70.04745483398438 -33.612331963363914, -70.04745483398438 -33.05586750447235, -70.8673095703125 -33.05586750447235, -70.8673095703125 -33.612331963363914))";

            // valgo
            _bboxWkt = "POLYGON ((6.373444 44.913277, 5.971403 44.913277, 5.971403 44.73893, 6.373444 44.73893, 6.373444 44.913277))";
            _dataSet = DEMDataSet.AW3D30;
            _outputDirectory = outputDirectory;
        }

        internal void Run()
        {
            glTFService glTF = new glTFService();
            string outputDir = Path.GetFullPath(Path.Combine(_outputDirectory, "glTF"));

            // Get GPX points
            var bbox = GeometryService.GetBoundingBox(_bboxWkt);

            //=======================
            // Textures
            //
            TextureInfo texInfo = null;


            ImageryService imageryService = new ImageryService();

            Console.WriteLine("Download image tiles...");
            TileRange tiles = imageryService.DownloadTiles(bbox, ImageryProvider.MapBoxSatellite, 10);

            Console.WriteLine("Construct texture...");
            string fileName = Path.Combine(outputDir, "Texture.jpg");
            texInfo = imageryService.ConstructTexture(tiles, bbox, fileName, TextureImageFormat.image_jpeg);

            //
            //=======================

            //=======================
            // Normal map
            Console.WriteLine("Height map...");
            float Z_FACTOR = 1f;
            HeightMap hMapNormal = _elevationService.GetHeightMap(bbox, _dataSet);

            hMapNormal = hMapNormal.ReprojectTo(4326, 2154);
            hMapNormal = hMapNormal.CenterOnOrigin(1f);

            HeightMap hMap = _elevationService.GetHeightMap(bbox, DEMDataSet.AW3D30);

            hMap = hMap.ReprojectTo(4326, 2154);
            hMap = hMap.CenterOnOrigin(Z_FACTOR);
            //
            //=======================

            //=======================
            // Normal map
            Console.WriteLine("Generate normal map...");
            HeightMap hMap4NormalMap = hMapNormal;
            List<Vector3> normals = glTF.ComputeNormals(hMap4NormalMap);

            bool debugBMP = false;
            if (debugBMP)
            {
                using (var dbm = new DirectBitmap(hMap.Width, hMap.Height))
                using (var dbmHeight = new DirectBitmap(hMap.Width, hMap.Height))
                {
                    // for debug only
                    List<Vector3> coordinates = hMap.Coordinates.Select(c => new Vector3((float)c.Longitude, (float)c.Latitude, (float)c.Elevation)).ToList();
                    float maxHeight = (float)hMap.Coordinates.Max(c => c.Elevation.GetValueOrDefault(0));

                    for (int j = 0; j < hMap.Height; j++)
                        for (int i = 0; i < hMap.Width; i++)
                        {
                            int index = i + (j * hMap.Width);
                            Vector3 norm = normals[index];
                            Color color = FromVec3NormalToColor(norm);
                            dbm.SetPixel(i, j, color);
                            dbmHeight.SetPixel(i, j, FromVec3ToHeightColor(coordinates[index], maxHeight));
                        }

                    dbm.Bitmap.Save(Path.Combine(outputDir, "normalmap.jpg"), ImageFormat.Jpeg);
                    dbmHeight.Bitmap.Save(Path.Combine(outputDir, "heightmap.jpg"), ImageFormat.Jpeg);
                }
            }
            else
            {
                using (var dbm = new DirectBitmap(hMap4NormalMap.Width, hMap4NormalMap.Height))
                {

                    for (int j = 0; j < hMap4NormalMap.Height; j++)
                        for (int i = 0; i < hMap4NormalMap.Width; i++)
                        {
                            int index = i + (j * hMap4NormalMap.Width);
                            Vector3 norm = normals[index];
                            Color color = FromVec3NormalToColor(norm);
                            dbm.SetPixel(i, j, color);
                        }

                    dbm.Bitmap.Save(Path.Combine(outputDir, "normalmap.jpg"), ImageFormat.Jpeg);
                }
            }

            TextureInfo normal = new TextureInfo(Path.Combine(outputDir, "normalmap.jpg"), TextureImageFormat.image_jpeg, hMap4NormalMap.Width, hMap4NormalMap.Height);
            //
            //=======================


            //=======================
            // MESH 3D terrain


            List<MeshPrimitive> meshes = new List<MeshPrimitive>();
            // generate mesh with texture
            bool useTIN = true;
            MeshPrimitive triangleMesh;
            if (useTIN)
            {
                Console.WriteLine("Create TIN...");
                //triangleMesh = GenerateTIN(hMapTIN, glTF, PBRTexture.Create(texInfo, normal));
                triangleMesh = GenerateTIN(hMap, glTF, PBRTexture.Create(texInfo, normal));
            }
            else
            {
                Console.WriteLine("GenerateTriangleMesh...");
                triangleMesh = glTF.GenerateTriangleMesh(hMap, null, PBRTexture.Create(texInfo, normal));

            }
            meshes.Add(triangleMesh);

            // model export
            Console.WriteLine("GenerateModel...");
            Model model = glTF.GenerateModel(meshes, this.GetType().Name);
            glTF.Export(model, outputDir, $"{GetType().Name} NONormal", false, true);
        }

        private MeshPrimitive GenerateTIN(HeightMap hMap, IglTFService gltf, PBRTexture textures)
        {
            var v_pointsToTest = GetGeoPointsByHMap(hMap, 2154);


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

            FLabServices.createCalculMedium().AugmenteDetailsTinByRef_v2(ref _topolFacettes, _paramTin);


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
            p00_geoPoint = p00_geoPoint.CenterOnOrigin(1f).ToList();


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
        private List<BeanPoint_internal> GetGeoPointsByHMap(HeightMap p_hMap, int p_srid)
        {
            return p_hMap.Coordinates.Select(c => GetPointInternalFromGeoPoint(c, p_srid)).ToList();
        }
        private BeanPoint_internal GetPointInternalFromGeoPoint(GeoPoint p_geoPoint, int p_srid)
        {
            BeanPoint_internal v_ptInternal = new BeanPoint_internal(p_geoPoint.Longitude, p_geoPoint.Latitude, p_geoPoint.Elevation.GetValueOrDefault(0), p_srid);
            return v_ptInternal;
        }

        internal void RunImagery(bool withTexture)
        {
            glTFService glTF = new glTFService();
            List<MeshPrimitive> meshes = new List<MeshPrimitive>();
            string outputDir = Path.GetFullPath(Path.Combine(_outputDirectory, "glTF"));

            // Get GPX points
            var bbox = GeometryService.GetBoundingBox(_bboxWkt);

            //=======================
            // Textures
            //
            TextureInfo texInfo = null;
            if (withTexture)
            {

                ImageryService imageryService = new ImageryService();

                Console.WriteLine("Download image tiles...");
                TileRange tiles = imageryService.DownloadTiles(bbox, ImageryProvider.MapBoxSatellite, 16);
                string fileName = Path.Combine(outputDir, "Texture.png");

                Console.WriteLine("Construct texture...");
                texInfo = imageryService.ConstructTexture(tiles, bbox, fileName, TextureImageFormat.image_jpeg);
            }
            //
            //=======================

            //=======================
            // MESH 3D terrain

            Console.WriteLine("Height map...");
            HeightMap hMap = _elevationService.GetHeightMap(bbox, _dataSet);

            //hMap = hMap.ReprojectTo(4326, 2154);
            hMap = hMap.CenterOnOrigin(0.00002f);

            Console.WriteLine("GenerateTriangleMesh...");
            // generate mesh with texture
            MeshPrimitive triangleMesh = glTF.GenerateTriangleMesh(hMap, null, PBRTexture.Create(texInfo));
            meshes.Add(triangleMesh);

            // model export
            Console.WriteLine("GenerateModel...");
            Model model = glTF.GenerateModel(meshes, this.GetType().Name);
            glTF.Export(model, outputDir, $"{GetType().Name} Packed", false, true);
        }

        internal void RunNormalMapGeneration()
        {
            glTFService glTF = new glTFService();
            List<MeshPrimitive> meshes = new List<MeshPrimitive>();
            string outputDir = Path.GetFullPath(Path.Combine(_outputDirectory, "glTF"));

            // Get GPX points
            var bbox = GeometryService.GetBoundingBox(_bboxWkt);


            //=======================
            // MESH 3D terrain

            Console.WriteLine("Height map...");
            HeightMap hMap = _elevationService.GetHeightMap(bbox, _dataSet);
            hMap = hMap.ReprojectTo(4326, 2154);


            List<Vector3> coordinates = hMap.Coordinates.Select(c => new Vector3((float)c.Longitude, (float)c.Latitude, (float)c.Elevation)).ToList();
            List<Vector3> normals = glTF.ComputeNormals(hMap);
            float maxHeight = (float)hMap.Coordinates.Max(c => c.Elevation.GetValueOrDefault(0));
            //Vector3 avg, min, max;
            //normals.GetStats(out avg, out min, out max);
            //coordinates.GetStats(out avg, out min, out max);

            using (var dbm = new DirectBitmap(hMap.Width, hMap.Height))
            using (var dbmHeight = new DirectBitmap(hMap.Width, hMap.Height))
            {
                for (int j = 0; j < hMap.Height; j++)
                    for (int i = 0; i < hMap.Width; i++)
                    {
                        int index = i + (j * hMap.Width);
                        Vector3 norm = normals[index];
                        Color color = FromVec3NormalToColor(norm);
                        dbm.SetPixel(i, j, color);
                        dbmHeight.SetPixel(i, j, FromVec3ToHeightColor(coordinates[index], maxHeight));
                    }

                dbm.Bitmap.Save(Path.Combine(outputDir, "normalmap.jpg"), ImageFormat.Jpeg);
                dbmHeight.Bitmap.Save(Path.Combine(outputDir, "heightmap.jpg"), ImageFormat.Jpeg);
            }

            TextureInfo baseColor = new TextureInfo(Path.Combine(outputDir, "heightmap.jpg"), TextureImageFormat.image_jpeg, hMap.Width, hMap.Height);
            TextureInfo normal = new TextureInfo(Path.Combine(outputDir, "normalmap.jpg"), TextureImageFormat.image_jpeg, hMap.Width, hMap.Height);

            MeshPrimitive triangleMesh = glTF.GenerateTriangleMesh(hMap, null, PBRTexture.Create(baseColor, normal));
            meshes.Add(triangleMesh);
            Model model = glTF.GenerateModel(meshes, this.GetType().Name);
            glTF.Export(model, outputDir, $"{GetType().Name} NormalMap", false, true);


        }

        private Color FromVec3ToHeightColor(Vector3 vector3, float maxHeight)
        {
            int height = (int)Math.Round(MathHelper.Map(0, maxHeight, 0, 255, vector3.Z, true), 0);
            return Color.FromArgb(height, height, height);
        }

        public Color FromVec3NormalToColor(Vector3 normal)
        {
            return Color.FromArgb((int)Math.Round(MathHelper.Map(-1, 1, 0, 255, normal.X, true), 0),
                (int)Math.Round(MathHelper.Map(-1, 1, 0, 255, normal.Y, true), 0),
                (int)Math.Round(MathHelper.Map(0, -1, 128, 255, -normal.Z, true), 0));
        }
    }
}
