namespace AssetGenerator.Runtime
{
    /// <summary>
    /// Wrapper for glTF loader's Sampler.  The sampler descibe the wrapping and scaling of textures.
    /// </summary>
    public class Sampler
    {
        /// <summary>
        /// Magnification filter
        /// </summary>
        public glTFLoader.Schema.Sampler.MagFilterEnum? MagFilter { get; set; }
        /// <summary>
        /// Minification filter
        /// </summary>
        public glTFLoader.Schema.Sampler.MinFilterEnum? MinFilter { get; set; }
        /// <summary>
        /// S wrapping mode
        /// </summary>
        public glTFLoader.Schema.Sampler.WrapSEnum? WrapS { get; set; }
        /// <summary>
        /// T wrapping mode
        /// </summary>
        public glTFLoader.Schema.Sampler.WrapTEnum? WrapT { get; set; }
        /// <summary>
        /// User-defined name of the sampler
        /// </summary>
        public string Name { get; set; }

    }
}
