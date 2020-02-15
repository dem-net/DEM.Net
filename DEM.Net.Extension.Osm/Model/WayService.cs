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
    public class WayService
    {
        private readonly IElevationService _elevationService;
        private readonly SharpGltfService _gltfService;
        private readonly IMeshService _meshService;
        private readonly ILogger<BuildingService> _logger;

        const double FloorHeightMeters = 2.5;


        public WayService(IElevationService elevationService
            , SharpGltfService gltfService
            , IMeshService meshService
            , ILogger<BuildingService> logger)
        {
            this._elevationService = elevationService;
            this._gltfService = gltfService;
            this._meshService = meshService;
            this._logger = logger;
        }

        public ModelRoot GetWays3DModel(BoundingBox bbox, string wayTag, DEMDataSet dataSet, bool downloadMissingFiles, float zScale)
        {
            try
            {
                // Download buildings and convert them to GeoJson
                FeatureCollection skiPistes = this.GetOsmDataAsGeoJson(bbox, wayTag);

                // Download elevation data if missing
                if (downloadMissingFiles) _elevationService.DownloadMissingFiles(dataSet, bbox);

                // Create internal building model
                var validator = new SkiPisteValidator(_logger);
                (List<PisteModel> Models, int TotalPoints) parsed = this.CreateSkiPistesFromGeoJson(skiPistes, validator);

                // Compute elevations (faster elevation when point count is known in advance)
                parsed.Models = this.ComputeElevations(parsed.Models, parsed.TotalPoints, dataSet, downloadMissingFiles, zScale);

                ModelRoot gltfModel = _gltfService.CreateNewModel();
                foreach (var m in parsed.Models)
                {
                    gltfModel = _gltfService.AddLine(gltfModel, m.LineString, m.ColorVec4, 30);
                }

                return gltfModel;
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(GetWays3DModel)} error: {ex.Message}");
                throw;
            }
        }


        public FeatureCollection GetOsmDataAsGeoJson(BoundingBox bbox, string wayTag)
        {
            try
            {
                using (TimeSpanBlock timeSpanBlock = new TimeSpanBlock(nameof(GetOsmDataAsGeoJson), _logger, LogLevel.Debug))
                {
                    var task = new OverpassQuery(bbox)
                    .WithWays(wayTag)
                    .WithRelations(wayTag)
                    .ToGeoJSON();

                    FeatureCollection ways = task.GetAwaiter().GetResult();

                    _logger.LogInformation($"{ways?.Features?.Count} ways downloaded");

                    return ways;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(GetBuildingsGeoJson)} error: {ex.Message}");
                throw;
            }

        }
        public FeatureCollection GetBuildingsGeoJson(int wayId)
        {
            try
            {
                using (TimeSpanBlock timeSpanBlock = new TimeSpanBlock(nameof(GetBuildingsGeoJson), _logger, LogLevel.Debug))
                {
                    var task = new OverpassQuery()
                    .WithWays("id", wayId.ToString())
                    .ToGeoJSON();

                    FeatureCollection buildings = task.GetAwaiter().GetResult();

                    return buildings;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(GetBuildingsGeoJson)} error: {ex.Message}");
                throw;
            }

        }

        public (List<PisteModel> models, int totalPoints) CreateSkiPistesFromGeoJson(FeatureCollection features, SkiPisteValidator validator)
        {
            int geoPointIdCounter = 0;

            List<PisteModel> models = new List<PisteModel>(features.Features.Count);
            StopwatchLog parseTimer = new StopwatchLog(_logger);
            using (TimeSpanBlock timeSpanBlock = new TimeSpanBlock(nameof(CreateSkiPistesFromGeoJson), _logger, LogLevel.Debug))
            {
                foreach (var feature in features.Features)
                {
                    PisteModel pisteModel = null;
                    switch (feature.Geometry.Type)
                    {
                        case GeoJSONObjectType.LineString:

                            LineString line = (LineString)feature.Geometry;
                            pisteModel = ConvertBuildingGeometry(line, ref geoPointIdCounter);
                            pisteModel.Id = feature.Id;
                            pisteModel.Tags = feature.Properties;

                            break;


                        default:
                            pisteModel = null;
                            _logger.LogWarning($"{nameof(CreateSkiPistesFromGeoJson)}: type {feature.Geometry.Type} not supported.");
                            break;
                    }

                    if (pisteModel != null)
                    {
                        parseTimer.Start();
                        validator.ParseTags(pisteModel);
                        parseTimer.Stop();
                        models.Add(pisteModel);
                    }
                }
            }

            parseTimer.LogTime("ParseTags");

            //BuildingValidator.ValidateTags(models);

            _logger.LogInformation($"{nameof(CreateSkiPistesFromGeoJson)} done for {geoPointIdCounter} points.");

            return (models, geoPointIdCounter);

        }

        private PisteModel ConvertBuildingGeometry(LineString geom, ref int geoPointIdCounter)
        {
            // Can't do it with a linq + lambda because of ref int param
            List<GeoPoint> outerRingGeoPoints = ConvertLineString(geom, ref geoPointIdCounter);


            var model = new PisteModel(outerRingGeoPoints);

            return model;
        }

        private List<GeoPoint> ConvertLineString(LineString lineString, ref int geoPointIdCounter)
        {
            // Can't do it with a linq + lambda because of ref int param
            List<GeoPoint> geoPoints = new List<GeoPoint>(lineString.Coordinates.Count);
            foreach (var pt in lineString.Coordinates)
            {
                geoPoints.Add(new GeoPoint(++geoPointIdCounter, pt.Latitude, pt.Longitude));
            }
            return geoPoints;
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
