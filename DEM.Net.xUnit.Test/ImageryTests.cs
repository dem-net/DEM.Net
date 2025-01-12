using DEM.Net.Core;
using DEM.Net.Core.Imagery;
using DEM.Net.Test;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DEM.Net.xUnit.Test
{
    public class ImageryTests : IClassFixture<DemNetFixture>
    {
        readonly ImageryService _imageryService;

        private static MapTileInfo tileInfoFrance = new(x: 527, y: 374, zoom: 10, 256);
        private static MapTileInfo tileInfoSwiss = new(x: 534, y: 361, zoom: 10, 256);


        public ImageryTests(DemNetFixture fixture)
        {
            _imageryService = fixture.ServiceProvider.GetService<ImageryService>();
        }

        [Theory()]
        [MemberData(nameof(ImageryProviders))]
        public void ImageryProviderTest(ImageryProvider provider, MapTileInfo testTile)
        {
            Assert.NotNull(provider.UrlModel);
            testTile.TileSize = provider.TileSize;
            var result = _imageryService.DownloadTiles(new TileRange(testTile, testTile, provider.TileSize), provider);

            Assert.NotNull(result);
            Assert.Single(result.Tiles);
            Assert.True(result.Tiles[0].Image.Length > 0);
        }

        public static IEnumerable<object[]> ImageryProviders =>
        [
            new object[] {ImageryProvider.EsriWorldImagery, tileInfoFrance },
            new object[] {ImageryProvider.MapTilerSatellite, tileInfoFrance},
            new object[] {ImageryProvider.MapBoxSatellite, tileInfoFrance},
            new object[] {ImageryProvider.MapBoxSatelliteStreet, tileInfoFrance},
            new object[] {ImageryProvider.MapBoxStreets, tileInfoFrance},
            new object[] {ImageryProvider.MapBoxLight, tileInfoFrance},
            new object[] {ImageryProvider.MapBoxDark, tileInfoFrance},
            new object[] {ImageryProvider.StamenToner, tileInfoFrance},
            new object[] {ImageryProvider.StamenWaterColor, tileInfoFrance},
            new object[] {ImageryProvider.StamenTerrain, tileInfoFrance},
            new object[] {ImageryProvider.StadiaOutdoors, tileInfoFrance},
            new object[] {ImageryProvider.OpenTopoMap, tileInfoFrance},
            new object[] {ImageryProvider.ThunderForestOutdoors, tileInfoFrance},
            new object[] {ImageryProvider.ThunderForestLandscape, tileInfoFrance},
            new object[] {ImageryProvider.ThunderForestNeighbourhood, tileInfoFrance},
            new object[] {ImageryProvider.OrtoIGNes, tileInfoFrance},
            new object[] {ImageryProvider.SwissImage, tileInfoSwiss},
            new object[] {ImageryProvider.OrtoIGNfr, tileInfoFrance},
        ];

    }
}
