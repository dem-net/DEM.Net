using DEM.Net.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace DEM.Net.Extension.Osm.Buildings
{
    public class BuildingModel
    {
        public IEnumerable<GeoPoint> ElevatedPoints { get; private set; }
        public string Id { get; private set; }
        public IDictionary<string, object> Properties { get; private set; }

        public BuildingModel(IEnumerable<GeoPoint> elevatedPoints, string id, IDictionary<string, object> properties)
        {
            this.ElevatedPoints = elevatedPoints;
            this.Id = id;
            this.Properties = properties;
        }
    }
}
