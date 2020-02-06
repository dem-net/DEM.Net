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
using DEM.Net.glTF.SharpglTF;
using SharpGLTF.Schema2;

namespace DEM.Net.Extension.Osm.Buildings
{
    public class BuildingService
    {
        private readonly IElevationService _elevationService;
        private readonly SharpGltfService _gltfService;
        private readonly ILogger<BuildingService> _logger;

        const double FloorHeightMeters = 2.5;

        public BuildingService(IElevationService elevationService
            , SharpGltfService gltfService
            , ILogger<BuildingService> logger)
        {
            this._elevationService = elevationService;
            this._gltfService = gltfService;
            this._logger = logger;
        }

        public ModelRoot GetBuildings3DModel(BoundingBox bbox, DEMDataSet dataSet, bool downloadMissingFiles)
        {
            try
            {
                var triangulation = this.GetBuildings3DTriangulation(bbox, dataSet, downloadMissingFiles);
                var model = _gltfService.CreateTerrainMesh(triangulation, null, doubleSided: false);
                return model;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public Triangulation GetBuildings3DTriangulation(BoundingBox bbox, DEMDataSet dataSet, bool downloadMissingFiles)
        {
            try
            {
                using (TimeSpanBlock timeSpanBlock = new TimeSpanBlock(nameof(GetBuildings3DTriangulation), _logger, LogLevel.Information))
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
                        // Reproject
                        BuildingModel model = new BuildingModel(elevatedPoints.ReprojectGeodeticToCartesian().ToList(), building.Id, building.Properties);
                        polygonPoints.Add(model);
                    }

                }

                _logger.LogInformation($"{nameof(ComputeElevations)} done for {totalPoints} points.");
            }

            return polygonPoints;
        }

        public Triangulation Triangulate(FeatureCollection featureCollection, DEMDataSet dataset, bool downloadMissingFiles = true)
        {
            List<BuildingModel> buildingModels = ComputeElevations(featureCollection, dataset, downloadMissingFiles);

            List<GeoPoint> positions = new List<GeoPoint>();
            List<int> indices = new List<int>();

            // Get highest base point
            // Retrieve building size
            foreach (var building in buildingModels)
            {
                double highestElevation = building.ElevatedPoints.OrderByDescending(p => p.Elevation ?? 0).First().Elevation ?? 0;
                double buildingHeight = this.GetBuildingHeightMeters(building);
                double buildingTop = highestElevation + buildingHeight;

                // sides (TODO check winding)
                int i = 0;
                foreach (var pos in building.ElevatedPoints)
                {
                    if (building.MinHeight.HasValue)
                    {
                        var posBottom = pos.Clone();
                        posBottom.Elevation += building.MinHeight ?? 0d;
                        positions.Add(posBottom);
                    }
                    else
                    {
                        positions.Add(pos);
                    }
                    Debug.Assert(!double.IsNaN(pos.Elevation.Value));
                    Debug.Assert(!double.IsInfinity(pos.Elevation.Value));

                    var posTop = pos.Clone();
                    posTop.Elevation = buildingTop;
                    positions.Add(posTop);

                    Debug.Assert(!double.IsNaN(posTop.Elevation.Value));
                    Debug.Assert(!double.IsInfinity(posTop.Elevation.Value));
                    if (i > 0)
                    {
                        indices.Add(i - 2);
                        indices.Add(i - 1);
                        indices.Add(i);


                        indices.Add(i);
                        indices.Add(i - 1);
                        indices.Add(i + 1);
                    }
                    i += 2;
                }
            }

            return new Triangulation(positions, indices);
        }

        private double GetBuildingHeightMeters(BuildingModel building)
        {
            if (building.Height.HasValue && building.Levels.HasValue)
            {
                _logger.LogWarning("Inchoerent height info.");
            }
            if (building.Levels.HasValue && (building.Height.HasValue || building.MinHeight.HasValue))
            {
                _logger.LogWarning("Inchoerent height info.");
            }


            double computedHeight = (building.Levels ?? 3) * FloorHeightMeters;

            double height = (building.Height ?? computedHeight) - (building.MinHeight ?? 0d);
            return height;
        }
    }
}
