//
// BuildingValidator.cs
//
// Author:
//       Xavier Fischer 2020-2
//
// Copyright (c) 2020 Xavier Fischer
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using System.Diagnostics;
using DEM.Net.Core;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using GeoJSON.Net;

namespace DEM.Net.Extension.Osm.Buildings
{
    public class BuildingValidator : OsmModelFactory<BuildingModel>
    {
        // Will capture 4 items : inputval / sign / value / unit
        const string ValueAndUnitRegex = @"([+\-])?((?:\d+\/|(?:\d+|^|\s)\.)?\d+)\s*([^\s\d+\-.,:;^\/]+(?:\^\d+(?:$|(?=[\s:;\/])))?(?:\/[^\s\d+\-.,:;^\/]+(?:\^\d+(?:$|(?=[\s:;\/])))?)*)?";

        public BuildingValidator(ILogger logger)
        {
            this._logger = logger;
        }

        private readonly ILogger _logger;

        private static HashSet<string> GetBuildingTagKeys()
        {
            var tagKeys = new HashSet<string>();
            tagKeys.Add("roof:orientation");
            tagKeys.Add("roof:orientation:compass");
            tagKeys.Add("roof:levels");
            tagKeys.Add("roof:shape");
            tagKeys.Add("roof:material");
            tagKeys.Add("roof:direction");
            tagKeys.Add("roof:height");
            tagKeys.Add("roof:colour");
            tagKeys.Add("roof:levels");
            tagKeys.Add("max_level");
            tagKeys.Add("min_level");
            tagKeys.Add("wall");
            tagKeys.Add("building:height");
            tagKeys.Add("building:levels");
            tagKeys.Add("building:level");
            tagKeys.Add("building:part");
            tagKeys.Add("building:facade:material");
            tagKeys.Add("building:min_level");
            tagKeys.Add("building:colour");
            tagKeys.Add("building:roof:shape");
            tagKeys.Add("building:material");
            tagKeys.Add("building:architecture");
            tagKeys.Add("building:levels:underground");
            tagKeys.Add("levels");
            tagKeys.Add("level");
            tagKeys.Add("height");
            tagKeys.Add("min_height");
            tagKeys.Add("craft");
            tagKeys.Add("source:building");
            tagKeys.Add("tower:type");
            tagKeys.Add("tower:construction");
            return tagKeys;
        }

        public static void ValidateTags(List<BuildingModel> buildingModels)
        {
            var keys = GetBuildingTagKeys();
            using (StreamWriter sw = new StreamWriter("buildingsValidation.txt", false))
            {
                sw.WriteLine("id\t" + string.Join("\t", keys));

                List<string> values = new List<string>();
                foreach (var building in buildingModels)
                {
                    values.Add(building.Id);
                    foreach (var key in keys)
                    {
                        if (building.Tags.TryGetValue(key, out object value))
                        {
                            values.Add(value.ToString());
                        }
                        else
                        {
                            values.Add(string.Empty);
                        }
                    }

                    sw.WriteLine(string.Join("\t", values));
                    values.Clear();

                }
            }
        }

        public override void ParseTags(BuildingModel model)
        {
            ParseTag<int>(model, "buildings:levels", v => model.Levels = v);
            ParseLengthTag(model, "min_height", v => model.MinHeight = v);
            ParseLengthTag(model, "height", v => model.Height = v);
            ParseLengthTag(model, "building:height", v =>
            {
                if (model.Height != null)
                {
                    _logger.LogWarning($"Height is passed as height and building:height, got value {v}");
                }
                model.Height = v;
            });
        }

        private void ParseTag<T>(BuildingModel model, string tagName, Action<T> updateAction)
        {
            if (model.Tags.TryGetValue(tagName, out object val))
            {
                try
                {
                    T typedVal = (T)Convert.ChangeType(val, typeof(T), CultureInfo.InvariantCulture);
                    updateAction(typedVal);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Cannot convert tag value {tagName}, got value {val}. {ex.Message}");
                }
            }
        }
        // Parse with unit conversion to meters
        private void ParseLengthTag(BuildingModel model, string tagName, Action<double> updateAction)
        {
            if (model.Tags.TryGetValue(tagName, out object val))
            {
                try
                {
                    // Will capture 4 items : inputval / sign / value / unit
                    var match = Regex.Match(val.ToString(), ValueAndUnitRegex, RegexOptions.Singleline | RegexOptions.Compiled);
                    if (match.Success)
                    {
                        var groups = match.Groups[0];
                        int sign = match.Groups[1].Value == "-" ? -1 : 1;
                        string valueStr = match.Groups[2].Value;
                        double typedVal = sign * double.Parse(valueStr, CultureInfo.InvariantCulture);

                        string unit = match.Groups[3].Value;
                        double factor = 1d;
                        switch (unit.ToLower())
                        {
                            case "ft":
                            case "feet":
                            case "foot":
                            case "'":
                                factor = 0.3048009193;
                                break;

                            case "":
                                factor = 1d;
                                break;
                            case "m":
                            case "meters":
                                factor = 1d;
                                break;
                            default:
                                throw new NotSupportedException($"Length unit {unit} conversion is not supported.");
                        }

                        typedVal *= factor;

                        updateAction(typedVal);
                    }
                    else
                    {
                        _logger.LogWarning($"Cannot convert tag {tagName} length value, got value {val}.");
                    }


                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Cannot convert tag value {tagName}, got value {val}. {ex.Message}");
                }
            }
        }

        public override BuildingModel CreateModel(Feature feature)
        {
            if (feature == null) return null;

            BuildingModel model = null;
            switch (feature.Geometry.Type)
            {
                case GeoJSON.Net.GeoJSONObjectType.Polygon:
                    model = ConvertBuildingGeometry((Polygon)feature.Geometry, ref base._totalPoints);
                    break;
                default:
                    _logger.LogDebug($"{feature.Geometry.Type} not supported for {nameof(BuildingModel)} {feature.Id}.");
                    break;
            }

            if (model != null)
            {
                model.Id = feature.Id;
                model.Tags = feature.Properties;
            }


            return model;
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

    }
}

