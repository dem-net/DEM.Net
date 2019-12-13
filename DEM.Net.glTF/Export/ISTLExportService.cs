using System.Collections.Generic;
using DEM.Net.Core;
using SharpGLTF.Schema2;

namespace DEM.Net.glTF.Export
{
    public interface ISTLExportService
    {
        void STLExport(MeshPrimitive mesh, string fileName, bool ascii = true, IEnumerable<Attribution> attributions = null);
    }
}