using DEM.Net.Core;
using DEM.Net.Extension.Osm.OverpassAPI;
using DEM.Net.Extension.Osm.Ski;
using DEM.Net.glTF.SharpglTF;
using GeoJSON.Net;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Microsoft.Extensions.Logging;
using SharpGLTF.Schema2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Extension.Osm.Buildings
{
    public class PisteSkiService
    {
        private readonly IElevationService _elevationService;
        private readonly SharpGltfService _gltfService;
        private readonly IMeshService _meshService;
        private readonly OsmService _osmService;
        private readonly ILogger<PisteSkiService> _logger;

        public PisteSkiService(IElevationService elevationService
            , SharpGltfService gltfService
            , IMeshService meshService
            , OsmService osmService
            , ILogger<PisteSkiService> logger)
        {
            this._elevationService = elevationService;
            this._gltfService = gltfService;
            this._meshService = meshService;
            this._osmService = osmService;
            this._logger = logger;
        }

        public ModelRoot GetPiste3DModel(BoundingBox bbox, string wayTag, DEMDataSet dataSet, bool downloadMissingFiles, float zScale)
        {
            try
            {

                List<PisteModel> models = GetPisteModels(bbox, wayTag, dataSet, downloadMissingFiles, zScale);

                ModelRoot gltfModel = _gltfService.CreateNewModel();
                foreach (var m in models)
                {
                    gltfModel = _gltfService.AddLine(gltfModel, m.LineString, m.ColorVec4, 30);
                }

                return gltfModel;
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(GetPiste3DModel)} error: {ex.Message}");
                throw;
            }
        }

        public List<PisteModel> GetPisteModels(BoundingBox bbox, string wayTag, DEMDataSet dataSet, bool downloadMissingFiles, float zScale)
        {
            try
            {
                // Download buildings and convert them to GeoJson
                FeatureCollection skiPistes = _osmService.GetOsmDataAsGeoJson(bbox, q => q
                                                                                       .WithWays(wayTag)
                                                                                       .WithWays(wayTag)
                                                                              );

                // Download elevation data if missing
                if (downloadMissingFiles) _elevationService.DownloadMissingFiles(dataSet, bbox);

                // Create internal building model
                var validator = new SkiPisteValidator(_logger);
                (List<PisteModel> Models, int TotalPoints) parsed = _osmService.CreateModelsFromGeoJson<PisteModel>(skiPistes, validator);

                _logger.LogInformation($"Computing elevations ({parsed.Models.Count} lines, {parsed.TotalPoints} total points)...");
                // Compute elevations (faster elevation when point count is known in advance)
                parsed.Models = this.ComputeElevations(parsed.Models, parsed.TotalPoints, dataSet, downloadMissingFiles, zScale);

                return parsed.Models;
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(GetPisteModels)} error: {ex.Message}");
                throw;
            }
        }
        public ModelRoot GetPiste3DModel(List<PisteModel> models)
        {
            try
            {

                ModelRoot gltfModel = _gltfService.CreateNewModel();
                foreach (var m in models)
                {
                    gltfModel = _gltfService.AddLine(gltfModel, m.LineString, m.ColorVec4, 30);
                }

                return gltfModel;
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(GetPiste3DModel)} error: {ex.Message}");
                throw;
            }
        }

        public List<PisteModel> ComputeElevations(List<PisteModel> models, int pointCount, DEMDataSet dataset, bool downloadMissingFiles = true, float zScale = 1f)
        {
            using (TimeSpanBlock timeSpanBlock = new TimeSpanBlock("Elevations+Reprojection", _logger, LogLevel.Debug))
            {
                //foreach(var model in models)
                Parallel.ForEach(models, model =>
                {
                    model.LineString = _elevationService.GetLineGeometryElevation(model.LineString, dataset)
                                         .ZScale(zScale)
                                         .ZTranslate(10)
                                         .ReprojectGeodeticToCartesian()
                                         .ToList();
                }
                );

            }

            return models;

        }
    }
}
