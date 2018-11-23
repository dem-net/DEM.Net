using System.Collections.Generic;

namespace AssetGenerator.Runtime
{
    /// <summary>
    /// Wrapper for glTF loader's Mesh
    /// </summary>
    public class Mesh
    {
        /// <summary>
        /// The user-defined name of this mesh.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// List of mesh primitives in the mesh
        /// </summary>
        public IEnumerable<MeshPrimitive> MeshPrimitives { get; set; }

    }
}
