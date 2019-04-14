//
// DatasetSamples.cs
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

using DEM.Net.Core;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Samples
{
    class DatasetSamples
    {
        
        public static void Run(ServiceProvider serviceProvider)
        {
            IRasterService rasterService = serviceProvider.GetService<IRasterService>();
            string sampleName = nameof(DatasetSamples);

            Logger.Info("============================");
            Logger.Info($"= {sampleName}");
            Logger.Info("============================");
            Logger.Info($"= {sampleName} : Datadirectory report");


            Logger.Info($"Local data directory : {rasterService.LocalDirectory}");

            Logger.RestartPerf(sampleName);

            Logger.Info(rasterService.GenerateReportAsString());

            Logger.StopPerf(sampleName);

            Logger.Info("=");
            Logger.Info("============================");



            //rasterService.GenerateDirectoryMetadata(DEMDataSet.AW3D30, false, true, true);
            //rasterService.GenerateDirectoryMetadata(DEMDataSet.SRTM_GL3, false, true, true);

        }


    }
}
