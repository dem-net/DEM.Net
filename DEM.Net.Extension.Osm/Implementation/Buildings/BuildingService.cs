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

        public ModelRoot GetBuildings3DModel(List<BuildingModel> buildings, DEMDataSet dataSet, bool downloadMissingFiles, float zScale)
        {
            try
            {
                TriangulationNormals triangulation = this.GetBuildings3DTriangulation(buildings, null, dataSet, downloadMissingFiles, zScale);

                var model = _gltfService.AddMesh(null, new SharpGltfService.IndexedTriangulation(triangulation), null, null, doubleSided: true);

                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(GetBuildings3DModel)} error: {ex.Message}");
                throw;
            }
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
        public (List<BuildingModel> Buildings, int TotalPoints) GetBuildingsModel(BoundingBox bbox)
        {
            try
            {
                // Download buildings and convert them to GeoJson
                //FeatureCollection buildings = _osmService.GetOsmDataAsGeoJson(bbox, q => q
                //.WithWays("building")
                //.WithWays("building:part")
                //.WithRelations("type=building")
                //.WithRelations("building"));

                FeatureCollection buildings = _osmService.GetOsmDataAsGeoJson(bbox,
                    @"(way[""building""] ({{bbox}});
                        way[""building:part""] ({{bbox}});
                        //relation[type=building] ({{bbox}});
                        //relation[""building""] ({{bbox}});
                       );");

                // Create internal building model
                var buildingValidator = new BuildingValidator(_logger);
                (List<BuildingModel> Buildings, int TotalPoints) parsedBuildings = _osmService.CreateModelsFromGeoJson(buildings, buildingValidator);

                return parsedBuildings;
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(GetBuildingsModel)} error: {ex.Message}");
                throw;
            }
        }
        public TriangulationNormals GetBuildings3DTriangulation(BoundingBox bbox, DEMDataSet dataSet, bool downloadMissingFiles, float zScale)
        {
            try
            {

                // Download elevation data if missing
                if (downloadMissingFiles) _elevationService.DownloadMissingFiles(dataSet, bbox);

                (List<BuildingModel> Buildings, int TotalPoints) parsedBuildings = GetBuildingsModel(bbox);

                return GetBuildings3DTriangulation(parsedBuildings.Buildings, parsedBuildings.TotalPoints, dataSet, downloadMissingFiles, zScale);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(GetBuildings3DModel)} error: {ex.Message}");
                throw;
            }
        }
        public TriangulationNormals GetBuildings3DTriangulation(List<BuildingModel> buildings, int? count, DEMDataSet dataSet, bool downloadMissingFiles, float zScale)
        {

            // Compute elevations (faster elevation when point count is known in advance)
            buildings = this.ComputeElevations(buildings, count ?? buildings.Sum(b => b.Points.Count()), dataSet, downloadMissingFiles, zScale);

            TriangulationNormals triangulation = this.Triangulate(buildings);
            return triangulation;

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
            List<Vector4> colors = new List<Vector4>();
            List<int> indices = new List<int>();
            List<Vector3> normals = new List<Vector3>();

            using (TimeSpanBlock timer = new TimeSpanBlock(nameof(Triangulate), _logger, LogLevel.Information))
            {
                // Get highest base point
                // Retrieve building size
                int numWithHeight = 0;
                int numWithColor = 0;

                foreach (var building in buildingModels)
                {
                    numWithHeight += building.HasHeightInformation ? 1 : 0;
                    numWithColor += (building.Color.HasValue || building.RoofColor.HasValue) ? 1 : 0;

                    var triangulation = this.Triangulate(building);

                    var positionsVec3 = triangulation.Positions.ToVector3().ToList();
                    var buildingNormals = _meshService.ComputeMeshNormals(positionsVec3, triangulation.Indices);

                    int initialPositionsCount = positions.Count;

                    positions.AddRange(positionsVec3);
                    indices.AddRange(triangulation.Indices.Select(i => i + initialPositionsCount).ToList());
                    colors.AddRange(triangulation.Colors);
                    normals.AddRange(buildingNormals);
                }

                _logger.LogInformation($"Building heights: {numWithHeight}/{buildingModels.Count} ({numWithHeight / (float)buildingModels.Count:P}) with height information.");
                _logger.LogInformation($"Building colors: {numWithColor}/{buildingModels.Count} ({numWithColor / (float)buildingModels.Count:P}) with color information.");
            }


            return new TriangulationNormals(positions, indices, normals, colors);
        }
        public TriangulationList<GeoPoint> Triangulate(BuildingModel building)
        {
            int totalPoints = building.ExteriorRing.Count - 1 + building.InteriorRings.Sum(r => r.Count - 1);

            //==========================
            // Footprint triangulation
            //
            var footPrintOutline = building.ExteriorRing.Skip(1); // In GeoJson, ring last point == first point, we must filter the first point out
            var footPrintInnerRingsFlattened = building.InteriorRings == null ? null : building.InteriorRings.Select(r => r.Skip(1));

            TriangulationList<GeoPoint> triangulation = _meshService.Tesselate(footPrintOutline, footPrintInnerRingsFlattened);
            int numFootPrintIndices = triangulation.Indices.Count;
            /////

            // Now extrude it (build the sides)
            // Algo
            // First triangulate the foot print (with inner rings if existing)
            // This triangulation is the roof top if building is flat
            building = this.ComputeBuildingHeightMeters(building);

            // Triangulate wall for each ring
            // (We add floor indices before copying the vertices, they will be duplicated and z shifted later on)
            List<int> numVerticesPerRing = new List<int>();
            numVerticesPerRing.Add(building.ExteriorRing.Count - 1);
            numVerticesPerRing.AddRange(building.InteriorRings.Select(r => r.Count - 1));
            triangulation = this.TriangulateRingsWalls(triangulation, numVerticesPerRing, totalPoints);

            // Roof
            // Building has real elevations

            // Create floor vertices by copying roof vertices and setting their z min elevation (floor or min height)
            var floorVertices = triangulation.Positions.Select(pt => pt.Clone(building.ComputedFloorAltitude)).ToList();
            triangulation.Positions.AddRange(floorVertices);

            // Take the first vertices and z shift them
            foreach (var pt in triangulation.Positions.Take(totalPoints))
            {
                pt.Elevation = building.ComputedRoofAltitude;
            }

            //==========================
            // Colors: if walls and roof color is the same, all vertices can have the same color
            // otherwise we must duplicate vertices to ensure consistent triangles color (avoid unrealistic shades)
            // AND shift the roof triangulation indices
            // Before:
            //      Vertices: <roof_wallcolor_0..i> / <floor_wallcolor_i..j>
            //      Indices: <roof_triangulation_0..i> / <roof_wall_triangulation_0..j>
            // After:
            //      Vertices: <roof_wallcolor_0..i> / <floor_wallcolor_i..j> // <roof_roofcolor_j..k>
            //      Indices: <roof_triangulation_j..k> / <roof_wall_triangulation_0..j>
            Vector4 DefaultColor = Vector4.One;
            bool mustCopyVerticesForRoof = (building.Color ?? DefaultColor) != (building.RoofColor ?? DefaultColor);
            // assign wall or default color to all vertices
            triangulation.Colors = triangulation.Positions.Select(p => building.Color ?? DefaultColor).ToList();

            if (mustCopyVerticesForRoof)
            {
                triangulation.Positions.AddRange(triangulation.Positions.Take(totalPoints));
                triangulation.Colors.AddRange(Enumerable.Range(1, totalPoints).Select(_ => building.RoofColor ?? DefaultColor));

                // shift roof triangulation indices
                for (int i = 0; i < numFootPrintIndices; i++)
                {
                    triangulation.Indices[i] += (triangulation.Positions.Count - totalPoints);
                }

            }

            Debug.Assert(triangulation.Colors.Count == 0 || triangulation.Colors.Count == triangulation.Positions.Count);

            return triangulation;

        }

        private TriangulationList<GeoPoint> TriangulateRingsWalls(TriangulationList<GeoPoint> triangulation, List<int> numVerticesPerRing, int totalPoints)
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

                // Connect last vertices to start vertices
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
