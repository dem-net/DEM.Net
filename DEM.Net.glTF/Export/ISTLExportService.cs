using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DEM.Net.Core;
using SharpGLTF.Schema2;

namespace DEM.Net.glTF.Export
{
    public interface ISTLExportService
    {
        /// <summary>
        /// Writes the triangles of <paramref name="mesh"/> to an STL file (through a temporary file: never a partial file)
        /// </summary>
        /// <param name="ascii">ASCII STL, binary otherwise</param>
        /// <param name="attributions">Listed in the solid name of ASCII files</param>
        Task STLExportAsync(MeshPrimitive mesh, string fileName, bool ascii = true, IEnumerable<Attribution>? attributions = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Writes the triangles of <paramref name="mesh"/> as STL to <paramref name="stream"/>, as they are read from the mesh
        /// </summary>
        Task WriteSTLAsync(MeshPrimitive mesh, Stream stream, bool ascii = true, IEnumerable<Attribution>? attributions = null, CancellationToken cancellationToken = default);
    }
}
