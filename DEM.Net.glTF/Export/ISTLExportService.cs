using System.Collections.Generic;
using AssetGenerator.Runtime;
using DEM.Net.Core;

namespace DEM.Net.glTF.Export
{
    public interface ISTLExportService
    {
        void STLExport(MeshPrimitive mesh, string fileName, bool ascii = true, IEnumerable<Attribution> attributions = null);
    }
}