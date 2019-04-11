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
