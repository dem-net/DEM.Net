using System;
using DEM.Net.Core;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace DEM.Net.Test
{
    public class ElevationTests : IClassFixture<DemNetFixture>
    {
        IRasterService _rasterService;
        IElevationService _elevationService;

        public ElevationTests(DemNetFixture fixture)
        {

            _rasterService = fixture.ServiceProvider.GetService<IRasterService>();
            _elevationService = fixture.ServiceProvider.GetService<IElevationService>();
        }

        [Fact]
        public void TestMethod1()
        {
        }
    }
}
