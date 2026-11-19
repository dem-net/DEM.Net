using System;
using System.IO;
using System.Linq;
using DEM.Net.Core;
using DEM.Net.Core.Configuration;
using DEM.Net.xUnit.Test;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace DEM.Net.Test;

public class ConfigurationTests(DemNetFixture fixture) : IClassFixture<DemNetFixture>
{
    readonly RasterService _rasterService = fixture.ServiceProvider.GetRequiredService<RasterService>();
    private readonly IOptions<DEMNetOptions> _options = fixture.ServiceProvider.GetRequiredService<IOptions<DEMNetOptions>>();
    readonly RasterIndexServiceResolver _indexServiceResolver = fixture.ServiceProvider.GetRequiredService<RasterIndexServiceResolver>();

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
