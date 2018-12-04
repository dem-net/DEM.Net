namespace AssetGenerator.Runtime
{
    /// <summary>
    /// Wrapper for glTF loader's Texture
    /// </summary>
    public class Texture
    {
        /// <summary>
        /// Image source for the texture
        /// </summary>
        public Image Source { get; set; }
        /// <summary>
        /// Texture coordinate index used for this texture
        /// </summary>
        public int? TexCoordIndex { get; set; }
        /// <summary>
        /// Sampler for this texture.
        /// </summary>
        public Sampler Sampler { get; set; }

        /// <summary>
        /// User defined name
        /// </summary>
        public string Name { get; set; }
    }
}
