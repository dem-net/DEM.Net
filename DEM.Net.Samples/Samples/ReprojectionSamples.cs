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
