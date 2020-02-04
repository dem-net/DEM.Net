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
        public List<BuildingModel> ComputeElevations(FeatureCollection buildings, DEMDataSet dataset)
        {
            List<BuildingModel> polygonPoints = new List<BuildingModel>(buildings.Features.Count);

            using (TimeSpanBlock timeSpanBlock = new TimeSpanBlock(nameof(ComputeElevations), _logger))
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
                        var elevatedPoints = _elevationService.GetPointsElevation(lineString.Coordinates.Select(c => new GeoPoint(c.Latitude, c.Longitude)), dataset
                            , downloadMissingFiles: false);
                        totalPoints += lineString.Coordinates.Count;
                        BuildingModel model = new BuildingModel(elevatedPoints, building.Id, building.Properties);
                        polygonPoints.Add(model);
                    }

                }

                _logger.LogInformation($"{nameof(ComputeElevations)} done for {totalPoints} points.");
            }

            return polygonPoints;
        }

        public Triangulation Triangulate(FeatureCollection featureCollection, DEMDataSet dataset)
        {
            var polygonList = ComputeElevations(featureCollection, dataset);
            return null;
        }
    }
}
