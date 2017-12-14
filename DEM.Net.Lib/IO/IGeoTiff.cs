using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib
{
    public interface IGeoTiff : IDisposable
    {
        float ParseGeoDataAtPoint(FileMetadata metadata, int x, int y);
        FileMetadata ParseMetaData();
    }
}
