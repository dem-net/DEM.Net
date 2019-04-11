//
// ReprojectionSamples.cs
//
// Author:
//       Xavier Fischer
//
// Copyright (c) 2019 
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

using AssetGenerator;
using AssetGenerator.Runtime;
using DEM.Net.glTF;
using DEM.Net.Lib;
using DEM.Net.Lib.Imagery;
using DEM.Net.Lib.Services.Lab;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Samples
{
    class ReprojectionSamples
    {
        private readonly string _bbox;
        private readonly IElevationService _elevationService;

        public ReprojectionSamples(string bbox)
        {
            _bbox = bbox;
            _elevationService = new ElevationService(new RasterService());
        }

        internal void Run()
        {
            //=======================
            // Normal map
            Console.WriteLine("Height map...");
            var bbox = GeometryService.GetBoundingBox(_bbox);
            HeightMap hMap = _elevationService.GetHeightMap(bbox, DEMDataSet.SRTM_GL3);

            var coords1 = hMap.Coordinates.ToList();
            var coords2 = hMap.Coordinates.ToList();
            Logger.RestartPerf("Projection with count");
            for (int i = 0; i < 5; i++)
            {
                coords2.ReprojectTo(4326, Reprojection.SRID_PROJECTED_MERCATOR, coords2.Count).ToList();
            }
            Logger.StopPerf("Projection with count");
            Logger.RestartPerf("Projection without count");
            for (int i = 0; i < 5; i++)
            {
                coords1.ReprojectTo(4326, Reprojection.SRID_PROJECTED_MERCATOR, null).ToList();
            }
            Logger.StopPerf("Projection without count");

           

        }


    }
}
