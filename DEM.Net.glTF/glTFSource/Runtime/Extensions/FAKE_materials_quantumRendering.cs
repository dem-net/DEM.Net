using System.Numerics;


namespace AssetGenerator.Runtime.Extensions
{
    /// <summary>
    /// This is a fake extension. We've created it to verify the behavior of clients attempting
    /// to load a model which contains an extension that they do not support. By creating an
    /// extension which does not exist, we guarantee consistent results regardless of which
    /// client attempts to load a model requiring this extension.
    /// </summary>
    public class FAKE_materials_quantumRendering : Extension
    {
        /// <summary>
        /// The name of the extension
        /// </summary>
        public override string Name
        {
            get
            {
                return nameof(FAKE_materials_quantumRendering);
            }
        }

        /// <summary>
        /// The reflected diffuse factor of the material
        /// </summary>
        public Vector4? PlanckFactor { get; set; }
        /// <summary>
        /// The diffuse texture
        /// </summary>
        public Texture CopenhagenTexture { get; set; }
        /// <summary>
        /// The specular RGB color of the material
        /// </summary>
        public Vector3? EntanglementFactor { get; set; }
        /// <summary>
        /// The glossiness or smoothness of the material
        /// </summary>
        public float? ProbabilisticFactor { get; set; }
        /// <summary>
        /// The specular-glossiness texture
        /// </summary>
        public Texture SuperpositionCollapseTexture { get; set; }

    }
}
