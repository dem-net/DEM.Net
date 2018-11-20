using System.Collections.Generic;
using System.Numerics;

namespace AssetGenerator.Runtime
{
    /// <summary>
    /// Wrapper for glTF loader's Material
    /// </summary>
    public class Material
    {
        /// <summary>
        /// The user-defined name of this object
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// A set of parameter values that are used to define the metallic-roughness material model from Physically-Based Rendering methodology
        /// </summary>
        public PbrMetallicRoughness MetallicRoughnessMaterial { get; set; }
        /// <summary>
        /// Texture that contains tangent-space normal information
        /// </summary>
        public Texture NormalTexture { get; set; }
        /// <summary>
        /// Scaling factor for the normal texture
        /// </summary>
        public float? NormalScale { get; set; }
        /// <summary>
        /// Texture that defines areas of the surface that are occluded from light, and thus rendered darker.  This information is contained in the "red" channel.
        /// </summary>
        public Texture OcclusionTexture { get; set; }
        /// <summary>
        /// Scaling factor for the occlusion texture
        /// </summary>
        public float? OcclusionStrength { get; set; }
        /// <summary>
        /// Texture that may be used to illuminate parts of the object surface. It defines the color of the light that is emitted from the surface
        /// </summary>
        public Texture EmissiveTexture { get; set; }
        /// <summary>
        /// Contains scaling factors for the "red", "green" and "blue" components of the emissive texture
        /// </summary>
        public Vector3? EmissiveFactor { get; set; }

        /// <summary>
        /// Specifies whether the material is float sided
        /// </summary>
        public bool? DoubleSided { get; set; }

        /// <summary>
        /// The alpha rendering mode of the material
        /// </summary>
        public glTFLoader.Schema.Material.AlphaModeEnum? AlphaMode { get; set; }
        /// <summary>
        /// The alpha cutoff value of the material
        /// </summary>
        public float? AlphaCutoff { get; set; }

        public IEnumerable<Runtime.Extensions.Extension> Extensions { get; set; }

    }
}
