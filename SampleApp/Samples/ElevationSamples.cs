using DEM.Net.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleApp
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
            Logger.Info($"= {sampleName} : Point elevation");

            double lat1 = -33.466479;
            double lon1 = -70.660565;
           

            foreach (var dataSet in DEMDataSet.RegisteredDatasets)
            {
                elevationService.DownloadMissingFiles(dataSet, lat1, lon1);
                var elevation = elevationService.GetPointElevation(lat1, lon1, dataSet);
                Logger.Info($"{dataSet.Name} elevation: {elevation.Elevation}");
            }
            Logger.StopPerf(sampleName);

            //double lat2 = -32.901011;
            //double lont2 = -68.814862;

            //var elevationLine = GeometryService.ParseGeoPointAsGeometryLine(new List<GeoPoint>
            //{  new GeoPoint(lat1,lon1)
            //, new GeoPoint(lat2,lont2)});
            //elevationService.DownloadMissingFiles(DEMDataSet.SRTM_GL1, elevationLine.GetBoundingBox());
            //var demNetFull = elevationService.GetLineGeometryElevation(elevationLine, DEMDataSet.SRTM_GL1, InterpolationMode.Bilinear);

            //var googlePoints = ParseGoogleElevationResponse(@"..\..\..\Data\elevationResultGoogle.json");
            //var demNetPoints = googlePoints.Select(p => new GeoPoint(p.Latitude, p.Longitude)).ToList();
            //var demNetPointsResult = elevationService.GetPointsElevation(demNetPoints, DEMDataSet.SRTM_GL1);
            //demNetPointsResult = elevationService.GetPointsElevation(demNetPoints, DEMDataSet.SRTM_GL3);
            //demNetPointsResult = elevationService.GetPointsElevation(demNetPoints, DEMDataSet.AW3D30);
            //File.WriteAllText(@"..\..\..\Data\elevationResultGoogleSCL.tsv", elevationService.ExportElevationTable(googlePoints));
            //File.WriteAllText(@"..\..\..\Data\elevationResultDemnetSCL.tsv", elevationService.ExportElevationTable(demNetPointsResult.ToList()));

            //Logger.Info($"Local data directory : {rasterService.LocalDirectory}");

            //Logger.RestartPerf(sampleName);

            //Logger.Info(rasterService.GenerateReportAsString());

            //Logger.StopPerf(sampleName);

            Logger.Info("=");
            Logger.Info("============================");
           
        }
    }
}
