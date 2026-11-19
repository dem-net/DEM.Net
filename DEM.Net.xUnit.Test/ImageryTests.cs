using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DEM.Net.Core;
using DEM.Net.Core.Imagery;
using DEM.Net.Test;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DEM.Net.xUnit.Test;

// Downloads datasets / tiles: excluded from CI (dotnet test --filter Category!=Network)
[Trait("Category", "Network")]
public class ImageryTests(DemNetFixture fixture) : IClassFixture<DemNetFixture>
{
    readonly ImageryService _imageryService = fixture.ServiceProvider.GetRequiredService<ImageryService>();

    private static MapTileInfo tileInfoFrance = new(x: 527, y: 374, zoom: 10, 256);
    private static MapTileInfo tileInfoSwiss = new(x: 534, y: 361, zoom: 10, 256);

    [Theory()]
    [MemberData(nameof(ImageryProviders))]
    public async Task ImageryProviderTest(ImageryProvider provider, MapTileInfo testTile)
    {
        Assert.NotNull(provider.UrlModel);
        testTile.TileSize = provider.TileSize;
        var result = await _imageryService.DownloadTilesAsync(new TileRange(testTile, testTile, provider.TileSize), provider);

        Assert.NotNull(result);
        Assert.Single(result.Tiles);
        Assert.True(result.Tiles[0].Image.Length > 0);
    }

    public static IEnumerable<object[]> ImageryProviders =>
    [
        [ImageryProvider.MapTilerSatellite, tileInfoFrance],
        [ImageryProvider.MapBoxSatellite, tileInfoFrance],
        [ImageryProvider.MapBoxSatelliteStreet, tileInfoFrance],
        [ImageryProvider.MapBoxStreets, tileInfoFrance],
        [ImageryProvider.MapBoxLight, tileInfoFrance],
        [ImageryProvider.MapBoxDark, tileInfoFrance],
        [ImageryProvider.StamenToner, tileInfoFrance],
        [ImageryProvider.StamenWaterColor, tileInfoFrance],
        [ImageryProvider.StamenTerrain, tileInfoFrance],
        [ImageryProvider.StadiaOutdoors, tileInfoFrance],
        [ImageryProvider.OpenTopoMap, tileInfoFrance],
        [ImageryProvider.ThunderForestOutdoors, tileInfoFrance],
        [ImageryProvider.ThunderForestLandscape, tileInfoFrance],
        [ImageryProvider.ThunderForestNeighbourhood, tileInfoFrance],
        [ImageryProvider.OrtoIGNes, tileInfoFrance],
        [ImageryProvider.SwissImage, tileInfoSwiss],
        [ImageryProvider.OrtoIGNfr, tileInfoFrance],
    ];

}
