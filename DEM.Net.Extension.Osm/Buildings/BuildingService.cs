using DEM.Net.Core;
using GeoJSON.Net;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using DEM.Net.Extension.Osm.OverpassAPI;

namespace DEM.Net.Extension.Osm.Buildings
{
    public class BuildingService
    {
        private readonly IElevationService _elevationService;
        private readonly ILogger<BuildingService> _logger;

        public BuildingService(IElevationService elevationService, ILogger<BuildingService> logger)
        {
            this._elevationService = elevationService;
            this._logger = logger;
        }

        public Triangulation GetBuildings3D(BoundingBox bbox, DEMDataSet dataSet, bool downloadMissingFiles)
        {
            try
            {
                using (TimeSpanBlock timeSpanBlock = new TimeSpanBlock(nameof(GetBuildings3D), _logger, LogLevel.Information))
                {
                    FeatureCollection buildings = this.GetBuildingsGeoJson(bbox);
                    Triangulation triangulation = this.Triangulate(buildings, dataSet, downloadMissingFiles);
                    return triangulation;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public FeatureCollection GetBuildingsGeoJson(BoundingBox bbox)
        {
            try
            {
                using (TimeSpanBlock timeSpanBlock = new TimeSpanBlock(nameof(GetBuildingsGeoJson), _logger, LogLevel.Debug))
                {
                    var task = new OverpassQuery(bbox)
                    .WithWays("building")
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

        public List<BuildingModel> ComputeElevations(FeatureCollection buildings, DEMDataSet dataset, bool downloadMissingFiles = true)
        {
            List<BuildingModel> polygonPoints = new List<BuildingModel>(buildings.Features.Count);

            using (TimeSpanBlock timeSpanBlock = new TimeSpanBlock(nameof(ComputeElevations), _logger, LogLevel.Debug))
            {
                int totalPoints = 0;
                foreach (var building in buildings.Features)
                {
                    LineString lineString = null;
                    switch (building.Geometry.Type)
                    {
                        case GeoJSONObjectType.Polygon:

                            Polygon poly = (Polygon)building.Geometry;
                            lineString = poly.Coordinates.Single();
                            break;


                        default:
                            lineString = null;
                            _logger.LogWarning($"ComputeElevations: type {building.Geometry.Type} not supported.");
                            break;
                    }

                    if (lineString != null)
                    {
                        var elevatedPoints = _elevationService.GetPointsElevation(lineString.Coordinates.Select(c => new GeoPoint(c.Latitude, c.Longitude))
                            , dataset
                            , downloadMissingFiles: downloadMissingFiles);
                        totalPoints += lineString.Coordinates.Count;
                        BuildingModel model = new BuildingModel(elevatedPoints, building.Id, building.Properties);
                        polygonPoints.Add(model);
                    }

                }

                _logger.LogInformation($"{nameof(ComputeElevations)} done for {totalPoints} points.");
            }

            return polygonPoints;
        }

        public Triangulation Triangulate(FeatureCollection featureCollection, DEMDataSet dataset, bool downloadMissingFiles = true)
        {
            var polygonList = ComputeElevations(featureCollection, dataset, downloadMissingFiles);
            return null;
        }
    }
}
