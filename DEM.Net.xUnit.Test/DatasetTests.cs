using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DEM.Net.Core;
using DEM.Net.xUnit.Test;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DEM.Net.Test;

// Downloads datasets / tiles: excluded from CI (dotnet test --filter Category!=Network)
[Trait("Category", "Network")]
public class DatasetTests(DemNetFixture fixture) : IClassFixture<DemNetFixture>
{
    readonly RasterService _rasterService = fixture.ServiceProvider.GetRequiredService<RasterService>();
    readonly ElevationService _elevationService = fixture.ServiceProvider.GetRequiredService<ElevationService>();
    readonly RasterIndexServiceResolver _indexServiceResolver = fixture.ServiceProvider.GetRequiredService<RasterIndexServiceResolver>();

    [Theory(), TestPriority(1)]
    [InlineData(nameof(DEMDataSet.SRTM_GL3))]
    [InlineData(nameof(DEMDataSet.AW3D30))]
    [InlineData(nameof(DEMDataSet.NASADEM))]
    public async Task DatasetTest(string datasetName)
    {
        var datasets = DEMDataSet.RegisteredDatasets;

        Assert.True(datasets.Any(), "No datasets found");


        DEMDataSet dataset = datasets.First(d => d.Name == datasetName);
        Assert.NotNull(dataset);

        var indexService = this._indexServiceResolver(dataset.DataSource.DataSourceType);
        Assert.NotNull(indexService);
        await indexService.SetupAsync(dataset, _rasterService.GetLocalDEMPath(dataset));

        Assert.True(indexService.GetFileSources(dataset).Any());
    }

    [Fact]
    public async Task DownloadTile_Location()
    {

        double lat = 43.537854;
        double lon = 5.429993;

        DEMDataSet dataset = DEMDataSet.SRTM_GL3;
        await _elevationService.DownloadMissingFilesAsync(dataset, lat, lon);

        var report = await _rasterService.GenerateReportForLocationAsync(dataset, lat, lon);

        Assert.NotNull(report);
        Assert.NotEmpty(report);
        Assert.True(report.Count == 1);
        Assert.True(report.First().IsExistingLocally);
    }

    [Theory()]
    [InlineData(nameof(DEMDataSet.SRTM_GL3))]
    [InlineData(nameof(DEMDataSet.AW3D30))]
    public async Task DownloadTile_BBox(string datasetName)
    {
        var datasets = DEMDataSet.RegisteredDatasets;
        Assert.True(datasets.Any(), "No datasets found");

        DEMDataSet dataset = datasets.First(d => d.Name == datasetName);
        Assert.NotNull(dataset);

        const string WKT_BBOX_AIX_PUYRICARD = "POLYGON ((5.429993 43.537854, 5.459132 43.537854, 5.459132 43.58151, 5.429993 43.58151, 5.429993 43.537854))";

        BoundingBox bbox = GeometryService.GetBoundingBox(WKT_BBOX_AIX_PUYRICARD);

        await _elevationService.DownloadMissingFilesAsync(dataset, bbox);
        var report = await _rasterService.GenerateReportAsync(dataset, bbox);

        Assert.NotNull(report);
        Assert.True(report.Count > 0);
        Assert.True(report.First().IsExistingLocally);

    }

    [Fact]
    public async Task GDALVrtPerDataset_Test()
    {
        double lat = 46.537854;
        double lon = 10.429993;

        DEMDataSet dataset = DEMDataSet.SRTM_GL3;
        var report_SRTM_GL3 = await _rasterService.GenerateReportForLocationAsync(dataset, lat, lon);

        // now change the dataset
        dataset = DEMDataSet.AW3D30;
        var report_AW3D30 = await _rasterService.GenerateReportForLocationAsync(dataset, lat, lon);

        Assert.NotNull(report_SRTM_GL3);
        Assert.True(report_SRTM_GL3.Count == 1);
        Assert.NotNull(report_AW3D30);
        Assert.True(report_AW3D30.Count == 1);
        Assert.NotEqual(report_AW3D30.First().LocalName
                            , report_SRTM_GL3.First().LocalName);
    }





}
