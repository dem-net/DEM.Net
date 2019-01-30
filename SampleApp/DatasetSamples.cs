using DEM.Net.Lib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleApp
{
     class DatasetSamples
    {

        public DatasetSamples()
        {
        }

        public static void Run()
        {

            RasterService rasterService = new RasterService();
            string sampleName = "DatasetSamples";
            Logger.StartPerf(sampleName);
            Logger.Info("============================");
            Logger.Info($"= {sampleName}");


            Logger.Info($"Local data directory : {rasterService.LocalDirectory}");

            // Get report for downloaded files
            foreach (DEMDataSet dataset in DEMDataSet.RegisteredDatasets)
            {
                Dictionary<string, DemFileReport> report = rasterService.GenerateReport(dataset);
                int totalFiles = report.Count;
                int downloadedCount = report.Count(kvp => kvp.Value.IsExistingLocally);
                int isMetadataGeneratedCount = report.Count(kvp => kvp.Value.IsMetadataGenerated);
                int isnotMetadataGeneratedCount = report.Count(kvp => !kvp.Value.IsMetadataGenerated);

                var fileSizeBytes = new DirectoryInfo(rasterService.GetLocalDEMPath(dataset)).EnumerateFileSystemInfos("*", SearchOption.AllDirectories)
                    .Select(fi=>new FileInfo(fi.FullName))
                    .Where(fi=> fi.Exists)
                    .Sum(fi=>fi.Length);

                var fileSizeMB = fileSizeBytes / 1024f / 1024f;

                Logger.Info($"Dataset : {dataset.Name} report :");
                Logger.Info($"> {totalFiles} file(s) in dataset ({fileSizeMB:F2} MB total)");
                Logger.Info($"> {downloadedCount} file(s) dowloaded.");
                Logger.Info($"> {isMetadataGeneratedCount} file(s) with DEM.Net metadata.");
            }

            Logger.StopPerf(sampleName);
        }


    }
}
