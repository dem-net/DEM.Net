using System.Collections.Generic;

namespace AssetGenerator.Runtime
{
    /// <summary>
    /// Wrapper class for abstracting the glTF Loader API
    /// </summary>
    public class GLTF
    {
        /// <summary>
        /// List of scenes in the gltf wrapper
        /// </summary>
        public IEnumerable<Scene> Scenes { get; set; }
        /// <summary>
        /// index of the main scene
        /// </summary>
        public int? Scene { get; set; }

        public IEnumerable<Animation> Animations { get; set; }

        public IEnumerable<string> ExtensionsUsed { get; set; }
        public IEnumerable<string> ExtensionsRequired { get; set; }

        /// <summary>
        /// Initializes the gltf wrapper
        /// </summary>
        public GLTF()
        {
            Scenes = new List<Scene>();
            Animations = new List<Animation>();
        }
        /// <summary>
        /// Holds the Asset data
        /// </summary>
        public Asset Asset { get; set; }
    }
}
