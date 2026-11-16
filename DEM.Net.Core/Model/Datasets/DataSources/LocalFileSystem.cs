using System;
using System.Collections.Generic;
using System.Text;

namespace DEM.Net.Core.Datasets;

public class LocalFileSystem(string localDirectory) : IDEMDataSource
{
    private readonly string _localDirectory = localDirectory;

    public string IndexFilePath => _localDirectory;

    public DEMDataSourceType DataSourceType => DEMDataSourceType.LocalFileSystem;
}
