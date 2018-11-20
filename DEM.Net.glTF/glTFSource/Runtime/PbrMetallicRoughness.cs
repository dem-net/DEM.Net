using System.Numerics;

namespace AssetGenerator.Runtime
{
    /// <summary>
    /// GLTF Wrapper for glTF loader's MetallicRoughness
    /// </summary>
    public class PbrMetallicRoughness
    {
        /// <summary>
        /// The main texture that will be applied to the object.
        /// </summary>
        public Texture BaseColorTexture { get; set; }
        /// <summary>
        /// The scaling factors for the red, green, blue and alpha components of the color.
        /// </summary>
        public Vector4? BaseColorFactor { get; set; }
        /// <summary>
        /// Texture containing the metalness value in the "blue" color channel, and the roughness value in the "green" color channel.
        /// </summary>
        public Texture MetallicRoughnessTexture { get; set; }
        /// <summary>
        /// Scaling factor for the metalness component
        /// </summary>
        public float? MetallicFactor { get; set; }
        /// <summary>
        /// Scaling factor for the roughness component
        /// </summary>
        public float? RoughnessFactor { get; set; }
    }
}
