using DEM.Net.Core;
using DEM.Net.xUnit.Test;
using System;
using System.IO;
using System.Linq;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using DEM.Net.Core.Configuration;

namespace DEM.Net.Test
{
    public class ConfigurationTests : IClassFixture<DemNetFixture>
    {
        readonly RasterService _rasterService;
        private readonly IOptions<DEMNetOptions> _options;
        readonly RasterIndexServiceResolver _indexServiceResolver;


        public ConfigurationTests(DemNetFixture fixture)
        {
            _rasterService = fixture.ServiceProvider.GetService<RasterService>();
            _options = fixture.ServiceProvider.GetService<IOptions<DEMNetOptions>>();
            _indexServiceResolver = fixture.ServiceProvider.GetService<RasterIndexServiceResolver>();
        }


        [Fact]
        public void LocalDirectoryTest()
        {   
            if (_options?.Value?.LocalDirectory == null)
            {
                Assert.Equal(_rasterService.LocalDirectory, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), RasterService.APP_NAME));
            }
            else
            {
                Assert.Equal(_options.Value.LocalDirectory, _rasterService.LocalDirectory);
            }
            
        }

        

    }
}
