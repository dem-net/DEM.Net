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

                FeatureCollection buildings = this.GetBuildingsGeoJson(bbox);
                if (downloadMissingFiles)
                {
                    _elevationService.DownloadMissingFiles(dataSet, bbox);
                }
                var triangulation = this.GetBuildings3DTriangulation(buildings, dataSet, downloadMissingFiles, zScale);
                var model = _gltfService.AddMesh(null, new SharpGltfService.IndexedTriangulation(triangulation), null, null, doubleSided: true);

                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(GetBuildings3DModel)} error: {ex.Message}");
                throw;
            }
        }
        public TriangulationNormals GetBuildings3DTriangulation(FeatureCollection buildings, DEMDataSet dataSet, bool downloadMissingFiles, float zScale)
        {
            try
            {
                using (TimeSpanBlock timeSpanBlock = new TimeSpanBlock(nameof(GetBuildings3DTriangulation), _logger, LogLevel.Information))
                {

                    _logger.LogInformation($"{buildings?.Features?.Count} buildings downloaded");


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
                    .WithRelations("building")
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

        public (List<BuildingModel> buildings, int totalPoints) CreateBuildingsFromGeoJson(FeatureCollection buildings)
        {
            int geoPointIdCounter = 0;

            List<BuildingModel> buildingModels = new List<BuildingModel>(buildings.Features.Count);
            using (TimeSpanBlock timeSpanBlock = new TimeSpanBlock(nameof(CreateBuildingsFromGeoJson), _logger, LogLevel.Debug))
            {
                foreach (var building in buildings.Features)
                {
                    BuildingModel buildingModel = null;
                    switch (building.Geometry.Type)
                    {
                        case GeoJSONObjectType.Polygon:

                            Polygon poly = (Polygon)building.Geometry;
                            buildingModel = ConvertBuildingGeometry(poly, ref geoPointIdCounter);
                            buildingModel.Id = building.Id;
                            buildingModel.Properties = building.Properties;

                            break;


                        default:
                            buildingModel = null;
                            _logger.LogWarning($"CreateBuildings: type {building.Geometry.Type} not supported.");
                            break;
                    }

                    if (buildingModel != null)
                    {
                        buildingModels.Add(buildingModel);
                    }
                }



            }

            BuildingValidator.ValidateTags(buildingModels);

            _logger.LogInformation($"{nameof(CreateBuildingsFromGeoJson)} done for {geoPointIdCounter} points.");

            return (buildingModels, geoPointIdCounter);

        }

        private BuildingModel ConvertBuildingGeometry(Polygon poly, ref int geoPointIdCounter)
        {
            // Can't do it with a linq + lambda because of ref int param
            List<GeoPoint> outerRingGeoPoints = ConvertBuildingLineString(poly.Coordinates.First(), ref geoPointIdCounter);

            List<List<GeoPoint>> interiorRings = null;
            if (poly.Coordinates.Count > 1)
            {
                interiorRings = new List<List<GeoPoint>>();
                foreach (LineString innerRing in poly.Coordinates.Skip(1))
                {
                    interiorRings.Add(ConvertBuildingLineString(innerRing, ref geoPointIdCounter));
                }
            }

            var buildingModel = new BuildingModel(outerRingGeoPoints, interiorRings);

            return buildingModel;
        }

        private List<GeoPoint> ConvertBuildingLineString(LineString lineString, ref int geoPointIdCounter)
        {
            // Can't do it with a linq + lambda because of ref int param
            List<GeoPoint> geoPoints = new List<GeoPoint>(lineString.Coordinates.Count);
            foreach (var pt in lineString.Coordinates)
            {
                geoPoints.Add(new GeoPoint(++geoPointIdCounter, pt.Latitude, pt.Longitude));
            }
            return geoPoints;
        }

        public List<BuildingModel> ComputeElevations(List<BuildingModel> buildingModels, int pointCount, DEMDataSet dataset, bool downloadMissingFiles = true, float zScale = 1f)
        {
            Dictionary<int, GeoPoint> reprojectedPointsById = null;

            using (TimeSpanBlock timeSpanBlock = new TimeSpanBlock("Elevations+Reprojection", _logger, LogLevel.Debug))
            {
                // Select all points (outer ring) + (inner rings)
                // They all have an Id, so we can lookup in which building they should be mapped after
                var allBuildingPoints = buildingModels
                    .SelectMany(b => b.Points);

                // Compute elevations
                reprojectedPointsById = _elevationService.GetPointsElevation(allBuildingPoints
                                                                    , dataset
                                                                    , downloadMissingFiles: downloadMissingFiles)
                                        .ZScale(zScale)
                                        .ReprojectGeodeticToCartesian(pointCount)
                                        .ToDictionary(p => p.Id.Value, p => p);
            }

            using (TimeSpanBlock timeSpanBlock = new TimeSpanBlock("Remap points", _logger, LogLevel.Debug))
            {
                int checksum = 0;
                foreach (var buiding in buildingModels)
                {
                    foreach (var point in buiding.Points)
                    {
                        var newPoint = reprojectedPointsById[point.Id.Value];
                        point.Latitude = newPoint.Latitude;
                        point.Longitude = newPoint.Longitude;
                        point.Elevation = newPoint.Elevation;
                        checksum++;
                    }
                }
                Debug.Assert(checksum == reprojectedPointsById.Count);
                reprojectedPointsById.Clear();
                reprojectedPointsById = null;
            }

            return buildingModels;

        }

        public TriangulationNormals Triangulate(FeatureCollection featureCollection, DEMDataSet dataset, bool downloadMissingFiles = true, float zScale = 1f)
        {
            (List<BuildingModel> Buildings, int TotalPoints) parsedBuildings = this.CreateBuildingsFromGeoJson(featureCollection);
            var buildingModels = parsedBuildings.Buildings;
            // Faster elevation when point count is known in advance
            buildingModels = this.ComputeElevations(buildingModels, parsedBuildings.TotalPoints, dataset, downloadMissingFiles, zScale);

            var tags = new HashSet<string>(buildingModels.SelectMany(b => b.Properties.Keys));

            List<Vector3> positions = new List<Vector3>();
            List<int> indices = new List<int>();
            List<Vector3> normals = new List<Vector3>();

            StopwatchLog sw = StopwatchLog.StartNew(_logger);
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
            sw.LogTime("Buildings triangulation");

            return new TriangulationNormals(positions, indices, normals);
        }
        public TriangulationList<GeoPoint> Triangulate(BuildingModel building)
        {

            // Algo
            // First triangulate the foot print (with inner rings if existing)
            // This triangulation is the roof top if building is flat
            double highestElevation = building.Points.OrderByDescending(p => p.Elevation ?? 0).First().Elevation ?? 0;
            double buildingHeight = this.GetBuildingHeightMeters(building);
            double buildingTop = highestElevation + buildingHeight;


            //--------------------
            // Footprint
            // In GeoJson, ring last point == first point, we must filter the first point out
            var footPrintOutline = building.ExteriorRing.Skip(1);
            var footPrintInnerRingsFlattened = building.InteriorRings == null ? null : building.InteriorRings.Select(r => r.Skip(1));
            TriangulationList<GeoPoint> triangulation = _meshService.Tesselate(footPrintOutline, footPrintInnerRingsFlattened);

            // Now extrude it (build the sides)

            // sides / exterior
            int startIndexOffset = 0;
            triangulation = AppendRingWallTriangulation(triangulation, building.ExteriorRing, startIndexOffset, buildingTop, building.MinHeight);
            startIndexOffset += building.ExteriorRing.Count;

            // sides / interiors
            foreach (var interiorRing in building.InteriorRings)
            {
                triangulation = AppendRingWallTriangulation(triangulation, interiorRing, startIndexOffset, buildingTop, building.MinHeight);
                startIndexOffset += interiorRing.Count;
            }
            return triangulation;

        }

        public TriangulationList<GeoPoint> AppendRingWallTriangulation(TriangulationList<GeoPoint> triangulation, List<GeoPoint> buildingRing, int indexOffset, double buildingTop, double? minHeight)
        {
            Dictionary<int, GeoPoint> currentPointIndex = triangulation.Positions.ToDictionary(p => p.Id.Value, p => p);
            int maxIndexInitial = triangulation.Positions.Count - 1;

            // walls
            // Initial elevations are onto terrain
            // We must add the top vertices
            //
            for (int i = 0; i < buildingRing.Count - 1; i++) // -2 because last point == first point
            {
                var posFloor = buildingRing[i];
                if (minHeight.HasValue)
                {
                    // Set floor elevation up for minHeight tags
                    currentPointIndex[posFloor.Id.Value].Elevation = minHeight.Value;
                }

                var posTop = posFloor.Clone();
                posTop.Elevation = buildingTop;
                triangulation.Positions.Add(posTop);

                if (i > 0)
                {
                    triangulation.Indices.Add(indexOffset + i - 1);
                    triangulation.Indices.Add(maxIndexInitial + i - 3);
                    triangulation.Indices.Add(indexOffset + i);


                    triangulation.Indices.Add(maxIndexInitial + i - 3);
                    triangulation.Indices.Add(maxIndexInitial + i - 2);
                    triangulation.Indices.Add(indexOffset + i);
                }
            }

            // connect last vertex to first
            int index = triangulation.Positions.Count;
            triangulation.Indices.Add(maxIndexInitial);
            triangulation.Indices.Add(index - 1);
            triangulation.Indices.Add(indexOffset + 0);


            triangulation.Indices.Add(index - 1);
            triangulation.Indices.Add(maxIndexInitial + 1);
            triangulation.Indices.Add(indexOffset + 0);



            return triangulation;

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
