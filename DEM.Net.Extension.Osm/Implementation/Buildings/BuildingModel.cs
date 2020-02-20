using DEM.Net.Core;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;

namespace DEM.Net.Extension.Osm.Buildings
{
    public class BuildingModel : CommonModel
    {
        public List<BuildingModel> Parts { get; set; }
        public List<GeoPoint> ExteriorRing { get; internal set; }

        public List<List<GeoPoint>> InteriorRings { get; internal set; }

        public IEnumerable<GeoPoint> Points
        {
            get
            {
                return ExteriorRing.Concat(this.InteriorRings == null ? Enumerable.Empty<GeoPoint>() : this.InteriorRings.SelectMany(r => r));
            }
        }



        public BuildingModel(List<GeoPoint> exteriorRingPoints, List<List<GeoPoint>> interiorRings)
        {
            this.ExteriorRing = exteriorRingPoints;
            this.InteriorRings = interiorRings ?? new List<List<GeoPoint>>();
        }


        // building:levels
        // height
        // min_height
        public int? Levels { get; set; }
        public double? MinHeight { get; set; }
        public double? Height { get; set; }

        public double? ComputedFloorAltitude { get; set; }
        public double ComputedRoofAltitude { get; set; }
        public bool HasHeightInformation { get; set; }
        public bool IsPart { get; internal set; }
        public Vector4? Color { get; internal set; }
        public Vector4? RoofColor { get; internal set; }
    }
}
