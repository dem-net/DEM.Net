using System;

namespace DEM.Net.Core.Datasets;

/// <summary>
/// Data source made of one remote raster file with a known extent, e.g. the GEBCO global grid as a single
/// cloud optimized GeoTIFF on Source Cooperative. The file is downloaded once, on first use, into the dataset directory.
/// </summary>
public class RemoteFileDataSource : IDEMDataSource
{
    /// <summary>URL of the raster file.</summary>
    public string Url { get; }

    /// <summary>Local file name, stored in the dataset directory. Defaults to the last segment of <see cref="Url"/>.</summary>
    public string FileName { get; }

    /// <summary>Extent of the file, in the dataset SRID: requests outside of it need no download.</summary>
    public BoundingBox Extent { get; }

    public string? IndexFilePath => null;

    public DEMDataSourceType DataSourceType => DEMDataSourceType.RemoteFile;

    public RemoteFileDataSource(string url, BoundingBox extent, string? fileName = null)
    {
        Url = url ?? throw new ArgumentNullException(nameof(url));
        Extent = extent ?? throw new ArgumentNullException(nameof(extent));
        FileName = fileName ?? System.IO.Path.GetFileName(new Uri(url).LocalPath);
        if (string.IsNullOrWhiteSpace(FileName))
            throw new ArgumentException($"No file name in {url}: specify {nameof(fileName)}.", nameof(fileName));
    }
}
