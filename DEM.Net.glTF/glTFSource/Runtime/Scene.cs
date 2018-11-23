using System.Collections.Generic;

namespace AssetGenerator.Runtime
{
    /// <summary>
    /// Wrapper for glTF loader's Scene
    /// </summary>
    public class Scene
    {
        /// <summary>
        /// List of nodes in the scene
        /// </summary>
        public IEnumerable<Node> Nodes { get; set; }

        /// <summary>
        /// The user-defined name of the scene
        /// </summary>
        public string Name { get; set; }
    }
}
