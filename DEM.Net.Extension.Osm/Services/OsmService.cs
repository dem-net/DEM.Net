using DEM.Net.Core;
using DEM.Net.Extension.Osm.OverpassAPI;
using GeoJSON.Net;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DEM.Net.Extension.Osm
{
    public class OsmService
    {
        private readonly ILogger<OsmService> _logger;

        public OsmService(ILogger<OsmService> logger)
        {
            this._logger = logger;
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

                    _logger.LogInformation($"{ways?.Features?.Count} features downloaded");

                    return ways;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(GetOsmDataAsGeoJson)} error: {ex.Message}");
                throw;
            }

        }

        public (List<T> models, int totalPoints) CreateModelsFromGeoJson<T>(FeatureCollection features, OsmModelFactory<T> validator)
            where T : class
        {

            List<T> models = new List<T>(features.Features.Count);
            using (TimeSpanBlock timeSpanBlock = new TimeSpanBlock(nameof(CreateModelsFromGeoJson), _logger, LogLevel.Debug))
            {
                foreach (var feature in features.Features)
                {
                    validator.RegisterTags(feature);
                    T model = validator.CreateModel(feature);

                    if (model == null)
                    {
                        _logger.LogWarning($"{nameof(CreateModelsFromGeoJson)}: {feature.Id}, type {feature.Geometry.Type} not supported.");
                        break;
                    }
                    else
                    {
                        validator.ParseTags(model);
                        models.Add(model);
                    }
                }
            }

#if DEBUG
            File.WriteAllText($"{typeof(T).Name}_osm_tag_report_{DateTime.Now:yyyyMMdd_HHmmss}.txt", validator.GetTagsReport(), Encoding.UTF8);
#endif

            _logger.LogInformation($"{nameof(CreateModelsFromGeoJson)} done for {validator.TotalPoints} points.");

            return (models, validator.TotalPoints);

        }
    }
}
