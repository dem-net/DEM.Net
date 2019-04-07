using DEM.Net.Lib;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Net;
using System.IO.Compression;
using System.Xml;
using System.Diagnostics;
using System.Windows.Media;
using System.Configuration;
using DEM.Net.glTF;
using AssetGenerator.Runtime;
using AssetGenerator;
using System.Numerics;
using Newtonsoft.Json;
using GeoAPI.Geometries;

namespace SampleApp
{
    class Program
    {
        static string _OutputDataDirectory = @"..\..\..\Data";

        [STAThread]
        static void Main(string[] args)
        {

            Logger.StartPerf("Main cold start");

            //DatasetSamples.Run();

            //ElevationSamples.Run();

            TextureSamples textureSamples = new TextureSamples(_OutputDataDirectory);
            textureSamples.Run();



            ReprojectionSamples reprojSamples = new ReprojectionSamples("POLYGON ((-69.647827 -33.767732, -69.647827 -32.953368, -70.751202 -32.953368, -70.751202 -33.767732, -69.647827 -33.767732))");
            reprojSamples.Run();



            string bboxTest = "POLYGON ((5.558267 43.538602, 5.557902 43.538602, 5.557902 43.538353, 5.558267 43.538353, 5.558267 43.538602))";
            STLSamples.Run(Path.Combine(_OutputDataDirectory, "glTF"), "Test", bboxTest, DEMDataSet.AW3D30);

            string bboxMtBlanc = "POLYGON ((6.944733 45.904438, 6.778999 45.904438, 6.778999 45.776144, 6.944733 45.776144, 6.944733 45.904438))";
            STLSamples.Run(Path.Combine(_OutputDataDirectory, "glTF"), "Mont Blanc", bboxMtBlanc, DEMDataSet.AW3D30);
            string steVictoire = "POLYGON((5.54888 43.519525, 5.61209 43.519525, 5.61209 43.565225, 5.54888 43.565225, 5.54888 43.519525))";
            STLSamples.Run(Path.Combine(_OutputDataDirectory, "glTF"), "Ste Victoire", steVictoire, DEMDataSet.AW3D30);
            string bboxSantiagoChile = "POLYGON ((-69.647827 -33.767732, -69.647827 -32.953368, -70.751202 -32.953368, -70.751202 -33.767732, -69.647827 -33.767732))";
            STLSamples.Run(Path.Combine(_OutputDataDirectory, "glTF"), "Santiago de Chile", bboxSantiagoChile, DEMDataSet.SRTM_GL3);
            //GpxSamples gpxSamples = new GpxSamples(_OutputDataDirectory, @"..\..\..\Data\GPX\venturiers.gpx");
            //gpxSamples.Run();





            //OldSamples oldSamples = new OldSamples( _OutputDataDirectory);
            //oldSamples.Run();


            Logger.StopPerf("Main cold start", true);
            Console.Write("Press any key to exit...");
            Console.ReadLine();

        }




    }
}