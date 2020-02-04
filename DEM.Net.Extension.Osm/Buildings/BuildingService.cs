using DEM.Net.Core;
using GeoJSON.Net;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DEM.Net.Extension.Osm.Buildings
{
    public class BuildingService
    {
        private readonly IElevationService _elevationService;

        public BuildingService(IElevationService elevationService)
        {
            this._elevationService = elevationService;
        }
        public List<BuildingModel> ComputeElevations(FeatureCollection buildings, DEMDataSet dataset)
        {
            List<BuildingModel> polygonPoints = new List<BuildingModel>(buildings.Features.Count);
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
                        Trace.TraceWarning($"ComputeElevations: type {building.Geometry.Type} not supported.");
                        break;
                }

                if (lineString != null)
                {
                    var elevatedPoints = _elevationService.GetPointsElevation(lineString.Coordinates.Select(c => new GeoPoint(c.Latitude, c.Longitude)), dataset);
                    BuildingModel model = new BuildingModel(elevatedPoints, building.Id, building.Properties);
                    polygonPoints.Add(model);
                }

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
