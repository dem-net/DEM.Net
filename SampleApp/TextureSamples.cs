using AssetGenerator;
using AssetGenerator.Runtime;
using DEM.Net.glTF;
using DEM.Net.Lib;
using DEM.Net.Lib.Imagery;
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
            _bboxWkt = "POLYGON ((5.192413330078125 44.12209907358672, 5.3015899658203125 44.12209907358672, 5.3015899658203125 44.201897151875094, 5.192413330078125 44.201897151875094, 5.192413330078125 44.12209907358672))";
            // duranne
            //_bboxWkt = "POLYGON ((5.303306579589844 43.45478810195138, 5.379180908203125 43.45478810195138, 5.379180908203125 43.51394981739109, 5.303306579589844 43.51394981739109, 5.303306579589844 43.45478810195138))";
            _dataSet = DEMDataSet.AW3D30;
            _outputDirectory = outputDirectory;
        }

        internal void Run(bool withTexture)
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

            TextureInfo baseColor = new TextureInfo(Path.Combine(outputDir, "heightmap.jpg"), ImageFormat.Jpeg, hMap.Width, hMap.Height);
            TextureInfo normal = new TextureInfo(Path.Combine(outputDir, "normalmap.jpg"), ImageFormat.Jpeg, hMap.Width, hMap.Height);

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
