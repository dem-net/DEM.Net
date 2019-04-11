//
// ElevationSamples.cs
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

using DEM.Net.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Samples
{
    public static class ElevationSamples
    {
        public static void Run()
        {
            RasterService rasterService = new RasterService();
            IElevationService elevationService = new ElevationService(rasterService);
            string sampleName = nameof(ElevationSamples);

            Logger.Info("============================");
            Logger.Info($"= {sampleName}");
            Logger.Info("============================");



            Logger.RestartPerf(sampleName);

            Logger.Info("============================");
            Logger.Info($"= {sampleName} : Point elevation");

            double lat1 = 45.179337;
            double lon1 = 5.721421;
            double lat2 = 45.212278;
            double lont2 = 5.468857;


            foreach (var dataSet in DEMDataSet.RegisteredDatasets)
            {
                elevationService.DownloadMissingFiles(dataSet, lat1, lon1);
                var elevation = elevationService.GetPointElevation(lat1, lon1, dataSet);
                Logger.Info($"{dataSet.Name} elevation: {elevation.Elevation}");
            }
            Logger.StopPerf(sampleName);

            Logger.Info("============================");

            Logger.Info("============================");
            Logger.Info($"= {sampleName} : Multiple point elevation");
            Logger.RestartPerf(sampleName);

            GeoPoint pt1 = new GeoPoint(lat1, lon1);
            GeoPoint pt2 = new GeoPoint(lat2, lont2);
            GeoPoint[] points = { pt1, pt2 };
            foreach (var dataSet in DEMDataSet.RegisteredDatasets)
            {
                var elevations = elevationService.GetPointsElevation(points, dataSet);
                Logger.Info($"{dataSet.Name} elevation: {string.Join(" / ", elevations.Select(e => e.Elevation))}");
            }
            Logger.StopPerf(sampleName);

            Logger.Info("=");
            Logger.Info("============================");


            Logger.Info("============================");
            Logger.Info($"= {sampleName} : Line elevation");
            Logger.RestartPerf(sampleName);
            var elevationLine = GeometryService.ParseGeoPointAsGeometryLine(new List<GeoPoint>
            {  new GeoPoint(lat1,lon1)
            , new GeoPoint(lat2,lont2)});
            foreach (var dataSet in DEMDataSet.RegisteredDatasets)
            {
                elevationService.DownloadMissingFiles(dataSet, elevationLine.GetBoundingBox());
                var elevations = elevationService.GetLineGeometryElevation(elevationLine, dataSet);
                var metrics = GeometryService.ComputeMetrics(elevations);
                //Logger.Info($"{dataSet.Name} elevation: {string.Join(", ", elevations.Select(e => e.Elevation))}");
                Logger.Info($"{dataSet.Name} metrics: {metrics}");
            }
            Logger.StopPerf(sampleName);

            Logger.Info("=");
            Logger.Info("============================");
            

        }

    }
}
