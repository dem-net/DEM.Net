//
// GpxSamples.cs
//
// Author:
//       Xavier Fischer
//
// Copyright (c) 2019 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using AssetGenerator;
using AssetGenerator.Runtime;
using DEM.Net.glTF;
using DEM.Net.Lib;
using DEM.Net.Lib.Imagery;
using DEM.Net.Lib.Services.Lab;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Samples
{
    class GpxSamples
    {
        private readonly string _gpxFile;
        private DEMDataSet _dataSet;
        private readonly string _outputDirectory;
        bool _withTexture = true;
        bool _generateTIN = false;
        int _outputSrid = Reprojection.SRID_PROJECTED_MERCATOR;
        float _Z_FACTOR = 2f;
        float _Z_TRANSLATE_GPX_TRACK_METERS = 5;
        float _trailWidthMeters = 5f;
        int _skipGpxPointsEvery = 1;
        public GpxSamples(string outputDirectory, string gpxFile)
        {
            _dataSet = DEMDataSet.AW3D30;
            _outputDirectory = outputDirectory;
            _gpxFile = gpxFile;
        }

        internal void Run(ServiceProvider serviceProvider)
        {
            IElevationService elevationService = serviceProvider.GetService<IElevationService>();
            IglTFService glTF = serviceProvider.GetService<IglTFService>();

            ImageryService imageryService = new ImageryService();
            List<MeshPrimitive> meshes = new List<MeshPrimitive>();
            string outputDir = Path.GetFullPath(Path.Combine(_outputDirectory, "glTF"));

            //=======================
            /// Line strip from GPX
            ///
            // Get GPX points
            var segments = GpxImport.ReadGPX_Segments(_gpxFile);
            var points = segments.SelectMany(seg => seg);
            var bbox = points.GetBoundingBox().Scale(4, 1.5);

            var gpxPointsElevated = elevationService.GetPointsElevation(points, _dataSet);

            //
            //=======================

            //=======================
            /// Height map (get dem elevation for bbox)
            ///
            HeightMap hMap = elevationService.GetHeightMap(bbox, _dataSet);
            hMap = hMap.ReprojectTo(4326, _outputSrid).CenterOnOrigin().ZScale(_Z_FACTOR).BakeCoordinates();
            //
            //=======================

            //=======================
            // Textures
            //
            PBRTexture pbrTexture = null;
            if (_withTexture)
            {


                Console.WriteLine("Download image tiles...");
                TileRange tiles = imageryService.DownloadTiles(bbox, ImageryProvider.MapBoxSatellite, 4);
                string fileName = Path.Combine(outputDir, "Texture.jpg");

                Console.WriteLine("Construct texture...");
                TextureInfo texInfo = imageryService.ConstructTexture(tiles, bbox, fileName, TextureImageFormat.image_jpeg);

                //
                //=======================

                //=======================
                // Normal map
                Console.WriteLine("Height map...");
                //float Z_FACTOR = 0.00002f;

                //hMap = hMap.CenterOnOrigin().ZScale(Z_FACTOR);
                var normalMap = imageryService.GenerateNormalMap(hMap, outputDir);

                pbrTexture = PBRTexture.Create(texInfo, normalMap);

                //hMap = hMap.CenterOnOrigin(Z_FACTOR);
                //
                //=======================
            }


            //=======================
            // MESH 3D terrain
            Console.WriteLine("Height map...");

            Console.WriteLine("GenerateTriangleMesh...");
            MeshPrimitive triangleMesh = null;
            //hMap = _elevationService.GetHeightMap(bbox, _dataSet);
            if (_generateTIN)
            {
                try
                {
                    triangleMesh = TINGeneration.GenerateTIN(hMap, 10d, glTF, pbrTexture, _outputSrid);
                }
                catch (Exception e)
                {
                    Logger.Error($"{e.Message}: {e.ToString()}");
                }

            }
            else
            {
                //hMap = hMap.CenterOnOrigin().ZScale(Z_FACTOR);
                // generate mesh with texture
                triangleMesh = glTF.GenerateTriangleMesh(hMap, null, pbrTexture);
            }
            meshes.Add(triangleMesh);

            // take 1 point evert nth

            gpxPointsElevated = gpxPointsElevated.Where((x, i) => (i + 1) % _skipGpxPointsEvery == 0);
            gpxPointsElevated = gpxPointsElevated.ZTranslate(_Z_TRANSLATE_GPX_TRACK_METERS)
                                                    .ReprojectTo(4326, _outputSrid)
                                                    .CenterOnOrigin()
                                                    .CenterOnOrigin(hMap.BoundingBox)
                                                    .ZScale(_Z_FACTOR);


            MeshPrimitive gpxLine = glTF.GenerateLine(gpxPointsElevated, new Vector4(1, 0, 0, 0.5f), _trailWidthMeters);
            meshes.Add(gpxLine);

            // model export
            Console.WriteLine("GenerateModel...");
            Model model = glTF.GenerateModel(meshes, this.GetType().Name);
            glTF.Export(model, outputDir, $"{GetType().Name} TIN{_generateTIN}", false, true);
        }


    }
}
