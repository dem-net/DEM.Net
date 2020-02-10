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

namespace DEM.Net.Extension.Osm.Buildings
{
    public static class BuildingValidator
    {

        static List<string> _importantKeys;

        static BuildingValidator()
        {
            _importantKeys = new List<string>();
            _importantKeys.Add("roof:orientation");
            _importantKeys.Add("roof:orientation:compass");
            _importantKeys.Add("roof:levels");
            _importantKeys.Add("roof:shape");
            _importantKeys.Add("roof:material");
            _importantKeys.Add("roof:direction");
            _importantKeys.Add("roof:height");
            _importantKeys.Add("roof:colour");
            _importantKeys.Add("roof:levels");
            _importantKeys.Add("max_level");
            _importantKeys.Add("min_level");
            _importantKeys.Add("wall");
            _importantKeys.Add("building:height");
            _importantKeys.Add("building:levels");
            _importantKeys.Add("building:level");
            _importantKeys.Add("building:part");
            _importantKeys.Add("building:facade:material");
            _importantKeys.Add("building:min_level");
            _importantKeys.Add("building:colour");
            _importantKeys.Add("building:roof:shape");
            _importantKeys.Add("building:material");
            _importantKeys.Add("building:architecture");
            _importantKeys.Add("building:levels:underground");
            _importantKeys.Add("levels");
            _importantKeys.Add("level");
            _importantKeys.Add("height");
            _importantKeys.Add("min_height");
            _importantKeys.Add("craft");
            _importantKeys.Add("source:building");
            _importantKeys.Add("tower:type");
            _importantKeys.Add("tower:construction");
        }

        public static void ValidateTags(List<BuildingModel> buildingModels)
        {
            using (StreamWriter sw = new StreamWriter("buildingsValidation.txt", false))
            {
                sw.WriteLine("id\t" + string.Join("\t", _importantKeys));

                List<string> values = new List<string>();
                foreach (var building in buildingModels)
                {
                    values.Add(building.Id);
                    foreach (var key in _importantKeys)
                    {
                        if (building.Properties.TryGetValue(key, out object value))
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
    }
}
