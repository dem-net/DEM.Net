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
                FeatureCollection buildings = _osmService.GetOsmDataAsGeoJson(bbox, "building");

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
            // Get highest base point
            // Retrieve building size
            foreach (var building in buildingModels)
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
            (double? floor, double roof) computedHeight = this.GetBuildingHeightMeters(building);

            // Triangulate wall for each ring
            List<int> numVerticesPerRing = new List<int>();
            numVerticesPerRing.Add(building.ExteriorRing.Count - 1);
            numVerticesPerRing.AddRange(building.InteriorRings.Select(r => r.Count - 1));

            triangulation = this.TriangulateRingsWall(triangulation, numVerticesPerRing, totalPoints);

            // Building has real elevations
            if (computedHeight.floor.HasValue)
            {
                // Create floor vertices by copying roof vertices and setting their z min elevation (floor or min height)
                var floorVertices = triangulation.Positions.Select(pt => pt.Clone(computedHeight.floor)).ToList();
                triangulation.Positions.AddRange(floorVertices);

                foreach (var pt in triangulation.Positions.Take(totalPoints))
                {
                    pt.Elevation = computedHeight.roof;
                }
            }
            else
            {
                // Create floor vertices by copying roof vertices and setting their z min elevation (floor or min height)
                var floorVertices = triangulation.Positions.Select(pt => pt.Clone(null)).ToList();
                triangulation.Positions.AddRange(floorVertices);

                foreach (var pt in triangulation.Positions.Take(totalPoints))
                {
                    pt.Elevation = computedHeight.roof;
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


        private (double? minHeight, double maxHeight) GetBuildingHeightMeters(BuildingModel building)
        {
            if (building.Levels.HasValue && (building.Height.HasValue || building.MinHeight.HasValue))
            {
                _logger.LogWarning("Inchoerent height info (Levels and Height), choosing Height.");
            }

            double highestFloorElevation = building.Points.OrderByDescending(p => p.Elevation ?? 0).First().Elevation ?? 0;

            double computedHeight = building.Height ?? (building.Levels ?? 3) * FloorHeightMeters;
            double roofElevation = computedHeight + highestFloorElevation;

            double? computedMinHeight = building.MinHeight == null ? default(double?) : computedHeight - building.MinHeight.Value;

            return (computedMinHeight, roofElevation);
        }
    }
}
