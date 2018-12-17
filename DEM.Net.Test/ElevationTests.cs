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
        [DeploymentItem(@"SqlServerTypes\x64\msvcr120.dll", @"SqlServerTypes\x64")]
        [DeploymentItem(@"SqlServerTypes\x64\SqlServerSpatial140.dll", @"SqlServerTypes\x64")]
        [DeploymentItem(@"SqlServerTypes\x86\msvcr120.dll", @"SqlServerTypes\x86")]
        [DeploymentItem(@"SqlServerTypes\x86\SqlServerSpatial140.dll", @"SqlServerTypes\x86")]
        public void Initialize()
        {
            _rasterService = new RasterService(".");
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
