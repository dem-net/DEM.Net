using System;
using DEM.Net.Core;
using Xunit;

namespace DEM.Net.Test
{
    public class ElevationTests
    {
        IRasterService _rasterService;
        IElevationService _elevationService;

        public ElevationTests()
        {
            _rasterService = new RasterService();
            _elevationService = new ElevationService(_rasterService);
        }

        [Fact]
        public void TestMethod1()
        {
        }
    }
}
