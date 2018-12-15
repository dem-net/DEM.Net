using System;
using DEM.Net.Lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DEM.Net.Test
{
    [TestClass]
    public class ElevationTests
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
        [TestCategory("Elevation")]
        public void TestMethod1()
        {
        }
    }
}
