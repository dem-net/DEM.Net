//
// IOsmTagsParser.cs
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
using GeoJSON.Net;
using GeoJSON.Net.Feature;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DEM.Net.Extension.Osm
{
    public abstract class OsmModelFactory<TModel>
    {
        public TagRegistry TagRegistry { get; private set; } = new TagRegistry();
        internal int _totalPoints;

        public abstract void ParseTags(TModel model);
        public abstract TModel CreateModel(Feature feature);

        public virtual void RegisterTags(Feature feature)
        {
            TagRegistry.RegisterTags(feature);
        }

        internal string GetTagsReport()
        {
            return TagRegistry.GetReport();
        }
    }
    public class TagRegistry
    {
        const string Separator = "\t";
        Dictionary<string, int> _tagsOccurences = new Dictionary<string, int>();
        Dictionary<GeoJSONObjectType, int> _geomTypes = new Dictionary<GeoJSONObjectType, int>();
        Dictionary<string, Dictionary<object, int>> _tagsValuesOccurences = new Dictionary<string, Dictionary<object, int>>();

        internal string GetReport()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Join(Separator, "GeometryType", "Tag", "Value", "Occurences"));
            foreach (var occur in _geomTypes.OrderBy(k=>k.Key))
            {
                sb.AppendLine(string.Join(Separator, occur.Key, "", "", occur.Value));
            }
            foreach (var occur in _tagsOccurences.OrderBy(k => k.Key))
            {
                sb.AppendLine(string.Join(Separator, "", occur.Key, "", occur.Value));
            }
            foreach (var occur in _tagsValuesOccurences.OrderBy(k => k.Key))
            {
                if (occur.Key == "@id") continue;

                foreach (var valOccur in occur.Value.OrderBy(k => k.Key))
                {
                    sb.AppendLine(string.Join(Separator, "", occur.Key, valOccur.Key, valOccur.Value));
                }
            }
            return sb.ToString();
        }

        internal void RegisterTags(Feature feature)
        {
            if (!_geomTypes.ContainsKey(feature.Geometry.Type))
            {
                _geomTypes.Add(feature.Geometry.Type, 0);
            }
            _geomTypes[feature.Geometry.Type]++;

            foreach (var prop in feature.Properties)
            {
                if (!_tagsOccurences.ContainsKey(prop.Key))
                {
                    _tagsOccurences.Add(prop.Key, 0);
                    _tagsValuesOccurences.Add(prop.Key, new Dictionary<object, int>());
                }
                if (!_tagsValuesOccurences[prop.Key].ContainsKey(prop.Value))
                {
                    _tagsValuesOccurences[prop.Key].Add(prop.Value, 0);
                }


                _tagsOccurences[prop.Key]++;
                _tagsValuesOccurences[prop.Key][prop.Value]++;

            }
        }
    }
}