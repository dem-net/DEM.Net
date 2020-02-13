using DEM.Net.Core;
using System;
using System.Linq;
using System.Collections.Generic;

namespace DEM.Net.Extension.Osm.Buildings
{
    public class BuildingModel
    {
        public List<GeoPoint> ExteriorRing { get; internal set; }

        public List<List<GeoPoint>> InteriorRings { get; internal set; }

        public IEnumerable<GeoPoint> Points
        {
            get
            {
                return ExteriorRing.Concat(this.InteriorRings == null ? Enumerable.Empty<GeoPoint>() : this.InteriorRings.SelectMany(r => r));
            }
        }

        public string Id { get; internal set; }
        public IDictionary<string, object> Tags { get; internal set; }

        public BuildingModel(List<GeoPoint> exteriorRingPoints, List<List<GeoPoint>> interiorRings, string id = null, IDictionary<string, object> tags = null)
        {
            this.ExteriorRing = exteriorRingPoints;
            this.InteriorRings = interiorRings ?? new List<List<GeoPoint>>();
            this.Id = id;
            this.Tags = tags;
        }


        // building:levels
        // height
        // min_height
        public int? Levels { get; set; }
        public double? MinHeight { get; set; }
        public double? Height { get; set; }


    }
}
