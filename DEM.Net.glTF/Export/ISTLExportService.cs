using AssetGenerator.Runtime;

namespace DEM.Net.glTF.Export
{
    public interface ISTLExportService
    {
        void STLExport(MeshPrimitive mesh, string fileName, bool ascii = true);
    }
}