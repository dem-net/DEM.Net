using System;
using DEM.Net.Lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DEM.Net.Test
{
    [TestClass]
    public class RasterTests
    {
        IRasterService _rasterService;
        IElevationService _elevationService;

        [TestInitialize()]
        public void Initialize()
        {
            _rasterService = new RasterService("Temp");
            _elevationService = new ElevationService(_rasterService);

            SqlServerTypes.Utilities.LoadNativeAssemblies(AppDomain.CurrentDomain.BaseDirectory);
        }

        [TestMethod]
        [TestCategory("Raster")]
        [DeploymentItem()]
        public void TestMethod1()
        {
        }
    }
}
