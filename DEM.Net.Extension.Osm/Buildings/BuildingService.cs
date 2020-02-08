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

        public ModelRoot GetBuildings3DModel(BoundingBox bbox, DEMDataSet dataSet, bool downloadMissingFiles, float zScale)
        {
            try
            {
                var triangulation = this.GetBuildings3DTriangulation(bbox, dataSet, downloadMissingFiles, zScale);
                var model = _gltfService.AddMesh(null, new SharpGltfService.IndexedTriangulation(triangulation), null, null, doubleSided: true);

                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(GetBuildings3DModel)} error: {ex.Message}");
                throw;
            }
        }
        public TriangulationNormals GetBuildings3DTriangulation(BoundingBox bbox, DEMDataSet dataSet, bool downloadMissingFiles, float zScale)
        {
            try
            {
                using (TimeSpanBlock timeSpanBlock = new TimeSpanBlock(nameof(GetBuildings3DTriangulation), _logger, LogLevel.Information))
                {
                    FeatureCollection buildings = this.GetBuildingsGeoJson(bbox);
                    _logger.LogInformation($"{buildings?.Features?.Count} buildings downloaded");


                    if (downloadMissingFiles)
                    {
                        _elevationService.DownloadMissingFiles(dataSet, bbox);
                    }
                    TriangulationNormals triangulation = this.Triangulate(buildings, dataSet, false, zScale);
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

        public List<BuildingModel> ComputeElevations(FeatureCollection buildings, DEMDataSet dataset, bool downloadMissingFiles = true, float zScale = 1f)
        {
            // TODO: compute elevations in one batch. Could easily retrieve points by doing a dual enumeration from features

            int geoPointIdCounter = 0;

            List<BuildingModel> buildingModels = new List<BuildingModel>(buildings.Features.Count);
            Stopwatch swElevation = new Stopwatch();
            Stopwatch swReproj = new Stopwatch();
            Stopwatch swGeoJson = new Stopwatch();
            using (TimeSpanBlock timeSpanBlock = new TimeSpanBlock(nameof(ComputeElevations), _logger, LogLevel.Debug))
            {
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
                        List<GeoPoint> buildingGeoPoints = lineString.Coordinates.Select(c => new GeoPoint(++geoPointIdCounter, c.Latitude, c.Longitude))
                                                            .ToList();

                        buildingModels.Add(new BuildingModel(buildingGeoPoints, building.Id, building.Properties));

                        swElevation.Stop();
                    }
                }


                swElevation.Start();
                var allPoints = buildingModels.SelectMany(b => b.ElevatedPoints);
                var elevatedPoints = _elevationService.GetPointsElevation(allPoints
                        , dataset
                        , downloadMissingFiles: downloadMissingFiles);
                Dictionary<int, GeoPoint> elevationResult = elevatedPoints
                                        .ZScale(zScale)
                                        .ReprojectGeodeticToCartesian(geoPointIdCounter)
                                        .ToDictionary(p => p.Id.Value, p => p);

                foreach (var buiding in buildingModels)
                {
                    foreach (var point in buiding.ElevatedPoints)
                    {
                        var newPoint = elevationResult[point.Id.Value];
                        point.Latitude = newPoint.Latitude;
                        point.Longitude = newPoint.Longitude;
                        point.Elevation = newPoint.Elevation;
                    }
                }
                elevationResult.Clear();
                elevationResult = null;

                swElevation.Stop();



                //if (lineString != null)
                //{
                //    swElevation.Start();
                //    var elevatedPoints = _elevationService.GetPointsElevation(lineString.Coordinates.Select(c => new GeoPoint(c.Latitude, c.Longitude))
                //        , dataset
                //        , downloadMissingFiles: downloadMissingFiles);
                //    elevatedPoints = elevatedPoints.ZScale(zScale).ToList();
                //    swElevation.Stop();
                //    totalPoints += lineString.Coordinates.Count;
                //    // Reproject
                //    swReproj.Start();
                //    BuildingModel model = new BuildingModel(elevatedPoints.ReprojectGeodeticToCartesian(lineString.Coordinates.Count).ToList(), building.Id, building.Properties);
                //    swReproj.Stop();
                //    polygonPoints.Add(model);
                //}

            }

            _logger.LogInformation($"{nameof(ComputeElevations)} done for {geoPointIdCounter} points. (Elevations: {swElevation.Elapsed:g}, Reproj: {swReproj.Elapsed:g}, Json: {swGeoJson.Elapsed:g})");

            return buildingModels;

        }

        public TriangulationNormals Triangulate(FeatureCollection featureCollection, DEMDataSet dataset, bool downloadMissingFiles = true, float zScale = 1f)
        {
            List<BuildingModel> buildingModels = ComputeElevations(featureCollection, dataset, downloadMissingFiles, zScale);

            var tags = new HashSet<string>(buildingModels.SelectMany(b => b.Properties.Keys));

            List<Vector3> positions = new List<Vector3>();
            List<int> indices = new List<int>();
            List<Vector3> normals = new List<Vector3>();

            using (TimeSpanBlock timer = new TimeSpanBlock("Triangulation", _logger))
            {
                // Get highest base point
                // Retrieve building size
                foreach (var building in buildingModels)
                //foreach (var building in buildingModels.Take(1))
                {
                    var triangulation = this.Triangulate(building);
                    var positionsVec3 = triangulation.Positions.ToVector3().ToList();
                    var buildingNormals = _meshService.ComputeMeshNormals(positionsVec3, triangulation.Indices);
                    int initialPositionsCount = positions.Count;
                    positions.AddRange(positionsVec3);
                    indices.AddRange(triangulation.Indices.Select(i => i + initialPositionsCount).ToList());
                    normals.AddRange(buildingNormals);
                }
            }

            return new TriangulationNormals(positions, indices, normals);
        }
        public TriangulationList<GeoPoint> Triangulate(BuildingModel building)
        {

            List<GeoPoint> positions = new List<GeoPoint>();
            List<int> indices = new List<int>();
            int outlinePointCount = building.ElevatedPoints.Count;

            double highestElevation = building.ElevatedPoints.OrderByDescending(p => p.Elevation ?? 0).First().Elevation ?? 0;
            double buildingHeight = this.GetBuildingHeightMeters(building);
            double buildingTop = highestElevation + buildingHeight;


            // sides
            for (int i = 0; i < outlinePointCount - 1; i++) // -2 because last point == first point
            {
                var pos = building.ElevatedPoints[i];
                if (building.MinHeight.HasValue)
                {
                    var posBottom = pos.Clone(building.MinHeight);
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

            //--------------------
            // Rooftop
            // We have a triangulation from original vertices, but now they are interleaved between bottom and top
            // we need to remap indices on top vertices only, ie: newIndex = index * 2 + 1
            var roofOutline = building.ElevatedPoints.Take(outlinePointCount - 1)
                                .Select(p => p.Clone(buildingTop))
                                .ToList();
            TriangulationList<GeoPoint> roofTopFlat = _meshService.Tesselate(roofOutline, buildingTop);
            var topIndices = roofTopFlat.Indices.Select(i => i * 2 + 1);
            indices.AddRange(topIndices);

            return new TriangulationList<GeoPoint>(positions, indices);
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
