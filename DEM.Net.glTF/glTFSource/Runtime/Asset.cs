namespace AssetGenerator.Runtime
{
    /// <summary>
    /// Abstraction for asset in glTF
    /// </summary>
    public class Asset
    {
        /// <summary>
        /// Tool that generated this glTF model.  Useful for debugging
        /// </summary>
        public string Generator { get; set; }
        /// <summary>
        /// The glTF version that this asset targets
        /// </summary>
        public string Version { get; set; }
        /// <summary>
        /// Extra attribute data
        /// </summary>
        public Extras Extras { get; set; }
        /// <summary>
        /// A copyright message suitable for display to credit the content creator
        /// </summary>
        public string Copyright { get; set; }
        /// <summary>
        /// The minimum glTF version that this asset targets
        /// </summary>
        public string MinVersion { get; set; }

        /// <summary>
        /// Converts asset to schema
        /// </summary>
        /// <returns></returns>
        public glTFLoader.Schema.Asset ConvertToSchema()
        {
            glTFLoader.Schema.Extras extras = new glTFLoader.Schema.Extras();


            glTFLoader.Schema.Asset asset = new glTFLoader.Schema.Asset();
            if (Generator != null)
            {
                asset.Generator = Generator;
            }
            if (Version != null)
            {
                asset.Version = Version;
            }
            if (Extras != null)
            {
                asset.Extras = Extras;
            }
            if (Copyright != null)
            {
                asset.Copyright = Copyright;
            }
            if (MinVersion != null)
            {
                asset.MinVersion = MinVersion;
            }


            return asset;
        }
        
    }

}
