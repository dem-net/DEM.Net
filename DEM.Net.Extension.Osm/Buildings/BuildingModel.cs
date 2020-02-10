using DEM.Net.Core;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

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
        public IDictionary<string, object> Properties { get; internal set; }

        public BuildingModel(List<GeoPoint> exteriorRingPoints, List<List<GeoPoint>> interiorRings, string id = null, IDictionary<string, object> properties = null)
        {
            this.ExteriorRing = exteriorRingPoints;
            this.InteriorRings = interiorRings ?? new List<List<GeoPoint>>();
            this.Id = id;
            this.Properties = properties;
        }

        #region Cached computed properties (late binding)

        // building:levels
        // height
        // min_height
        bool levelsRetrieved = false;
        int? levels;
        public int? Levels
        {
            get
            {
                if (!levelsRetrieved)
                {
                    if (Properties.TryGetValue("building:levels", out object val))
                    {
                        levels = int.Parse(val.ToString(), CultureInfo.InvariantCulture);
                    }
                    levelsRetrieved = true;
                }
                return levels;
            }
        }

        bool minHeightRetrieved = false;
        double? minHeight;
        public double? MinHeight
        {
            get
            {
                if (!minHeightRetrieved)
                {
                    if (Properties.TryGetValue("min_height", out object val))
                    {
                        minHeight = double.Parse(val.ToString(), CultureInfo.InvariantCulture);
                    }
                    minHeightRetrieved = true;
                }
                return minHeight;
            }
        }


        bool heightRetrieved = false;
        double? height;
        public double? Height
        {
            get
            {
                if (!heightRetrieved)
                {
                    if (Properties.TryGetValue("height", out object val))
                    {
                        height = double.Parse(val.ToString(), CultureInfo.InvariantCulture);
                    }
                    heightRetrieved = true;
                }
                return height;
            }
        }
        #endregion


    }
}
