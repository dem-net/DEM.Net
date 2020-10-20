using System;
using System.Collections.Generic;
using System.Text;

namespace DEM.Net.Core.Datasets
{
    public class LocalFileSystem : IDEMDataSource
    {
        private readonly string _localDirectory;

        public LocalFileSystem(string localDirectory)
        {
            _localDirectory = localDirectory;
        }
        public string IndexFilePath => _localDirectory;

        public DEMDataSourceType DataSourceType => DEMDataSourceType.LocalFileSystem;
    }
}
