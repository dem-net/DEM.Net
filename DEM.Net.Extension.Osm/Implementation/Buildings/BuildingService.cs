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
        private readonly OsmService _osmService;
        private readonly ILogger<BuildingService> _logger;

        const double FloorHeightMeters = 2.5;


        public BuildingService(IElevationService elevationService
            , SharpGltfService gltfService
            , IMeshService meshService
            , OsmService osmService
            , ILogger<BuildingService> logger)
        {
            this._elevationService = elevationService;
            this._gltfService = gltfService;
            this._meshService = meshService;
            this._osmService = osmService;
            this._logger = logger;
        }

        public ModelRoot GetBuildings3DModel(BoundingBox bbox, DEMDataSet dataSet, bool downloadMissingFiles, float zScale)
        {
            try
            {
                TriangulationNormals triangulation = this.GetBuildings3DTriangulation(bbox, dataSet, downloadMissingFiles, zScale);

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
                // Download buildings and convert them to GeoJson
                //FeatureCollection buildings = _osmService.GetOsmDataAsGeoJson(bbox, q => q
                //.WithWays("building")
                //.WithWays("building:part")
                //.WithRelations("type=building")
                //.WithRelations("building"));

                FeatureCollection buildings = _osmService.GetOsmDataAsGeoJson(bbox, "(way[\"building\"]({{bbox}});(._;<;);(._;>;);relation[\"building\"]({{bbox}});(._;>;););");



                // Download elevation data if missing
                if (downloadMissingFiles) _elevationService.DownloadMissingFiles(dataSet, bbox);

                // Create internal building model
                var buildingValidator = new BuildingValidator(_logger);
                (List<BuildingModel> Buildings, int TotalPoints) parsedBuildings = _osmService.CreateModelsFromGeoJson(buildings, buildingValidator);

                // Compute elevations (faster elevation when point count is known in advance)
                parsedBuildings.Buildings = this.ComputeElevations(parsedBuildings.Buildings, parsedBuildings.TotalPoints, dataSet, downloadMissingFiles, zScale);

                TriangulationNormals triangulation = this.Triangulate(parsedBuildings.Buildings);
                return triangulation;
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(GetBuildings3DModel)} error: {ex.Message}");
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

        public TriangulationNormals Triangulate(List<BuildingModel> buildingModels)
        {

            List<Vector3> positions = new List<Vector3>();
            List<int> indices = new List<int>();
            List<Vector3> normals = new List<Vector3>();

            StopwatchLog sw = StopwatchLog.StartNew(_logger);
            StopwatchLog swTri = new StopwatchLog(_logger);
            StopwatchLog swNormals = new StopwatchLog(_logger);
            StopwatchLog swOther = new StopwatchLog(_logger);
            // Get highest base point
            // Retrieve building size
            foreach (var building in buildingModels)
            {
                swTri.Start();
                   var triangulation = this.Triangulate(building);
                swTri.Stop();
                swNormals.Start();
                var positionsVec3 = triangulation.Positions.ToVector3().ToList();
                var buildingNormals = _meshService.ComputeMeshNormals(positionsVec3, triangulation.Indices);
                swNormals.Stop();
                swOther.Start();
                int initialPositionsCount = positions.Count;
                positions.AddRange(positionsVec3);
                indices.AddRange(triangulation.Indices.Select(i => i + initialPositionsCount).ToList());
                normals.AddRange(buildingNormals);
                swOther.Stop();
            }
            swTri.LogTime("Triangulation");
            swNormals.LogTime("Normals");
            swOther.LogTime("Other");
            sw.LogTime("Buildings triangulation");

            int numBuildingsWithHeightInfo = buildingModels.Count(b => b.HasHeightInformation);
            _logger.LogInformation($"Building heights: {numBuildingsWithHeightInfo}/{buildingModels.Count} ({numBuildingsWithHeightInfo / (float)buildingModels.Count:P}) with height information.");

            return new TriangulationNormals(positions, indices, normals);
        }
        public TriangulationList<GeoPoint> Triangulate(BuildingModel building)
        {
            int totalPoints = building.ExteriorRing.Count - 1 + building.InteriorRings.Sum(r => r.Count - 1);

            //--------------------
            // Footprint
            // In GeoJson, ring last point == first point, we must filter the first point out
            var footPrintOutline = building.ExteriorRing.Skip(1);
            var footPrintInnerRingsFlattened = building.InteriorRings == null ? null : building.InteriorRings.Select(r => r.Skip(1));
            TriangulationList<GeoPoint> triangulation = _meshService.Tesselate(footPrintOutline, footPrintInnerRingsFlattened);

            // Now extrude it (build the sides)
            // Algo
            // First triangulate the foot print (with inner rings if existing)
            // This triangulation is the roof top if building is flat
            building = this.ComputeBuildingHeightMeters(building);

            // Triangulate wall for each ring
            List<int> numVerticesPerRing = new List<int>();
            numVerticesPerRing.Add(building.ExteriorRing.Count - 1);
            numVerticesPerRing.AddRange(building.InteriorRings.Select(r => r.Count - 1));

            triangulation = this.TriangulateRingsWall(triangulation, numVerticesPerRing, totalPoints);

            // Building has real elevations
            if (building.ComputedFloorAltitude.HasValue)
            {
                // Create floor vertices by copying roof vertices and setting their z min elevation (floor or min height)
                var floorVertices = triangulation.Positions.Select(pt => pt.Clone(building.ComputedFloorAltitude)).ToList();
                triangulation.Positions.AddRange(floorVertices);

                foreach (var pt in triangulation.Positions.Take(totalPoints))
                {
                    pt.Elevation = building.ComputedRoofAltitude;
                }
            }
            else
            {
                // Create floor vertices by copying roof vertices and setting their z min elevation (floor or min height)
                var floorVertices = triangulation.Positions.Select(pt => pt.Clone(null)).ToList();
                triangulation.Positions.AddRange(floorVertices);

                foreach (var pt in triangulation.Positions.Take(totalPoints))
                {
                    pt.Elevation = building.ComputedRoofAltitude;
                }
            }




            return triangulation;

        }

        private TriangulationList<GeoPoint> TriangulateRingsWall(TriangulationList<GeoPoint> triangulation, List<int> numVerticesPerRing, int totalPoints)
        {
            int offset = numVerticesPerRing.Sum();

            Debug.Assert(totalPoints == offset);

            int ringOffset = 0;
            foreach (var numRingVertices in numVerticesPerRing)
            {
                int i = 0;
                do
                {
                    triangulation.Indices.Add(ringOffset + i);
                    triangulation.Indices.Add(ringOffset + i + offset);
                    triangulation.Indices.Add(ringOffset + i + 1);

                    triangulation.Indices.Add(ringOffset + i + offset);
                    triangulation.Indices.Add(ringOffset + i + offset + 1);
                    triangulation.Indices.Add(ringOffset + i + 1);

                    i++;
                }
                while (i < numRingVertices - 1);

                triangulation.Indices.Add(ringOffset + i);
                triangulation.Indices.Add(ringOffset + i + offset);
                triangulation.Indices.Add(ringOffset + 0);

                triangulation.Indices.Add(ringOffset + i + offset);
                triangulation.Indices.Add(ringOffset + 0 + offset);
                triangulation.Indices.Add(ringOffset + 0);

                ringOffset += numRingVertices;

            }
            return triangulation;
        }


        private BuildingModel ComputeBuildingHeightMeters(BuildingModel building)
        {
            building.HasHeightInformation = building.Levels.HasValue || building.Height.HasValue || building.MinHeight.HasValue;

            if (building.Levels.HasValue && (building.Height.HasValue || building.MinHeight.HasValue))
            {
                _logger.LogWarning("Inchoerent height info (Levels and Height), choosing Height.");
            }

            double highestFloorElevation = building.Points.OrderByDescending(p => p.Elevation ?? 0).First().Elevation ?? 0;

            double computedHeight = building.Height ?? (building.Levels ?? 3) * FloorHeightMeters;
            double roofElevation = computedHeight + highestFloorElevation;

            double? computedMinHeight = null;
            if (building.MinHeight.HasValue)
                computedMinHeight = roofElevation - building.MinHeight.Value;

            building.ComputedRoofAltitude = roofElevation;
            building.ComputedFloorAltitude = computedMinHeight;

            return building;
        }
    }
}
