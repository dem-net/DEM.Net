//
// Program.cs
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
using System.IO;
using System;
using System.Diagnostics;

namespace DEM.Net.Samples
{
    class Program
    {
        static string _OutputDataDirectory = @"..\..\..\..\Data".Replace('\\', Path.DirectorySeparatorChar);

        static void Main(string[] args)
        {
            //Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));

            Logger.StartPerf("Main cold start");

            DatasetSamples.Run();


            STLSamples.Run(Path.Combine(_OutputDataDirectory, "glTF"), DEMDataSet.AW3D30);

            GpxSamples gpxSamples = new GpxSamples(_OutputDataDirectory, Path.Combine(_OutputDataDirectory, "GPX", "venturiers.gpx"));
            gpxSamples.Run();


            ElevationSamples.Run();

            TextureSamples textureSamples = new TextureSamples(_OutputDataDirectory);
            textureSamples.Run();



            ReprojectionSamples reprojSamples = new ReprojectionSamples("POLYGON ((-69.647827 -33.767732, -69.647827 -32.953368, -70.751202 -32.953368, -70.751202 -33.767732, -69.647827 -33.767732))");
            reprojSamples.Run();







            //OldSamples oldSamples = new OldSamples( _OutputDataDirectory);
            //oldSamples.Run();


            Logger.StopPerf("Main cold start", true);
            Console.Write("Press any key to exit...");
            Console.ReadLine();
        }
    }
}
