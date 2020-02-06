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
using System.Numerics;

namespace DEM.Net.Extension.Osm.Buildings
{
    public class BuildingService
    {
        private readonly IElevationService _elevationService;
        private readonly SharpGltfService _gltfService;
        private readonly IMeshService _meshService;
        private readonly ILogger<BuildingService> _logger;

        const double FloorHeightMeters = 2.5;


        public BuildingService(IElevationService elevationService
            , SharpGltfService gltfService
            , IMeshService meshService
            , ILogger<BuildingService> logger)
        {
            this._elevationService = elevationService;
            this._gltfService = gltfService;
            this._meshService = meshService;
            this._logger = logger;
        }

        public ModelRoot GetBuildings3DModel(BoundingBox bbox, DEMDataSet dataSet, bool downloadMissingFiles)
        {
            try
            {
                var triangulation = this.GetBuildings3DTriangulation(bbox, dataSet, downloadMissingFiles);
                var model = _gltfService.CreateNewModel();
                _gltfService.AddTerrainMesh(model, new SharpGltfService.IndexedTriangulation(triangulation), triangulation.Normals, null, doubleSided: true);

                return model;

                //using (TimeSpanBlock timeSpanBlock = new TimeSpanBlock(nameof(GetBuildings3DTriangulation), _logger, LogLevel.Information))
                //{
                //    FeatureCollection buildings = this.GetBuildingsGeoJson(bbox);

                //    if (downloadMissingFiles)
                //    {
                //        _elevationService.DownloadMissingFiles(dataSet, bbox);
                //    }
                //   List<BuildingModel> buildingModels = ComputeElevations(buildings, dataSet, downloadMissingFiles);

                //    foreach(var building in buildingModels)
                //    {
                //        try
                //        {
                //            var triangulation = this.Triangulate(building);
                //            var normals =  _meshService.ComputeMeshNormals(triangulation.Positions.ToVector3().ToList(), triangulation.Indices.ToList());
                //            var model = _gltfService.CreateTerrainMesh(triangulation, null, doubleSided: false);
                //        }
                //        catch (Exception ex)
                //        {
                //            _logger.LogWarning($"Error: {ex.Message}");
                //        }

                //    }

                //}
                //return null;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public TriangulationNormals GetBuildings3DTriangulation(BoundingBox bbox, DEMDataSet dataSet, bool downloadMissingFiles)
        {
            try
            {
                using (TimeSpanBlock timeSpanBlock = new TimeSpanBlock(nameof(GetBuildings3DTriangulation), _logger, LogLevel.Information))
                {
                    FeatureCollection buildings = this.GetBuildingsGeoJson(bbox);

                    if (downloadMissingFiles)
                    {
                        _elevationService.DownloadMissingFiles(dataSet, bbox);
                    }
                    TriangulationNormals triangulation = this.Triangulate(buildings, dataSet, false);
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
            Stopwatch swElevation = new Stopwatch();
            Stopwatch swReproj = new Stopwatch();
            Stopwatch swGeoJson = new Stopwatch();
            using (TimeSpanBlock timeSpanBlock = new TimeSpanBlock(nameof(ComputeElevations), _logger, LogLevel.Debug))
            {
                int totalPoints = 0;
                foreach (var building in buildings.Features)
                {
                    swGeoJson.Start();
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
                    swGeoJson.Stop();

                    if (lineString != null)
                    {
                        swElevation.Start();
                        var elevatedPoints = _elevationService.GetPointsElevation(lineString.Coordinates.Select(c => new GeoPoint(c.Latitude, c.Longitude))
                            , dataset
                            , downloadMissingFiles: downloadMissingFiles);
                        swElevation.Stop();
                        totalPoints += lineString.Coordinates.Count;
                        // Reproject
                        swReproj.Start();
                        BuildingModel model = new BuildingModel(elevatedPoints.ReprojectGeodeticToCartesian(lineString.Coordinates.Count).ToList(), building.Id, building.Properties);
                        swReproj.Stop();
                        polygonPoints.Add(model);
                    }

                }

                _logger.LogInformation($"{nameof(ComputeElevations)} done for {totalPoints} points. (Elevations: {swElevation.Elapsed:g}, Reproj: {swReproj.Elapsed:g}, Json: {swGeoJson.Elapsed:g})");
            }

            return polygonPoints;
        }

        public TriangulationNormals Triangulate(FeatureCollection featureCollection, DEMDataSet dataset, bool downloadMissingFiles = true)
        {
            List<BuildingModel> buildingModels = ComputeElevations(featureCollection, dataset, downloadMissingFiles);

            List<Vector3> positions = new List<Vector3>();
            List<int> indices = new List<int>();
            List<Vector3> normals = new List<Vector3>();

            // Get highest base point
            // Retrieve building size
            foreach (var building in buildingModels)
            //foreach (var building in buildingModels.Take(2))
            {
                var triangulation = Triangulate(building);
                var positionsVec3 = triangulation.Positions.ToVector3().ToList();
                var buildingNormals = _meshService.ComputeMeshNormals(positionsVec3, triangulation.Indices);
                int initialPositionsCount = positions.Count;
                positions.AddRange(positionsVec3);
                indices.AddRange(triangulation.Indices.Select(i => i + initialPositionsCount).ToList());
                normals.AddRange(buildingNormals);
            }

            return new TriangulationNormals(positions, indices, normals);
        }
        public (List<GeoPoint> Positions, List<int> Indices) Triangulate(BuildingModel building)
        {

            List<GeoPoint> positions = new List<GeoPoint>();
            List<int> indices = new List<int>();

            double highestElevation = building.ElevatedPoints.OrderByDescending(p => p.Elevation ?? 0).First().Elevation ?? 0;
            double buildingHeight = this.GetBuildingHeightMeters(building);
            double buildingTop = highestElevation + buildingHeight;


            // sides
            for (int i = 0; i < building.ElevatedPoints.Count - 1; i++) // -2 because last point == first point
            {
                var pos = building.ElevatedPoints[i];
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

                var posTop = pos.Clone();
                posTop.Elevation = buildingTop;
                positions.Add(posTop);

                if (i > 0)
                {
                    indices.Add(i * 2 - 2);
                    indices.Add(i * 2 - 1);
                    indices.Add(i * 2);


                    indices.Add(i * 2);
                    indices.Add(i * 2 - 1);
                    indices.Add(i * 2 + 1);
                }
            }

            // connect last vertex to first
            int index = positions.Count;
            indices.Add(index - 2);
            indices.Add(index - 1);
            indices.Add(0);


            indices.Add(0);
            indices.Add(index - 1);
            indices.Add(1);

            return (positions, indices);
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
