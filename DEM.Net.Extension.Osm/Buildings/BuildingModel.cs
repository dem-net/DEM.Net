using DEM.Net.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DEM.Net.Extension.Osm.Buildings
{
    public class BuildingModel
    {
        public List<GeoPoint> ElevatedPoints { get; internal set; }
        public string Id { get; private set; }
        public IDictionary<string, object> Properties { get; private set; }

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

        public BuildingModel(List<GeoPoint> elevatedPoints, string id, IDictionary<string, object> properties)
        {
            this.ElevatedPoints = elevatedPoints;
            this.Id = id;
            this.Properties = properties;
        }
    }
}
