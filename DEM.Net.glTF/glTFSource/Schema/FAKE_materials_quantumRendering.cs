namespace glTFLoader.Schema
{
    using System.Linq;
    using System.Runtime.Serialization;


    public class FAKE_materials_quantumRendering
    {

        /// <summary>
        /// Backing field for planckFactor.
        /// </summary>
        private float[] m_planckFactor = new float[] {
                1F,
                1F,
                1F,
                1F};

        /// <summary>
        /// Backing field for copenhagenTexture.
        /// </summary>
        private TextureInfo m_copenhagenTexture;

        /// <summary>
        /// Backing field for entanglementFactor.
        /// </summary>
        private float[] m_entanglementFactor = new float[] {
                1F,
                1F,
                1F};

        /// <summary>
        /// Backing field for probabilisticFactor.
        /// </summary>
        private float m_probabilisticFactor = 1F;

        /// <summary>
        /// Backing field for superpositionCollapseTexture.
        /// </summary>
        private TextureInfo m_superpositionCollapseTexture;

        /// <summary>
        /// Backing field for Extensions.
        /// </summary>
        private System.Collections.Generic.Dictionary<string, object> m_extensions;

        /// <summary>
        /// Backing field for Extras.
        /// </summary>
        private Extras m_extras;

        /// <summary>
        /// The reflected planck factor of the material.
        /// </summary>
        [Newtonsoft.Json.JsonConverterAttribute(typeof(glTFLoader.Shared.ArrayConverter))]
        [Newtonsoft.Json.JsonPropertyAttribute("planckFactor")]
        public float[] PlanckFactor
        {
            get
            {
                return this.m_planckFactor;
            }
            set
            {
                if ((value.Length < 4u))
                {
                    throw new System.ArgumentException("Array not long enough");
                }
                if ((value.Length > 4u))
                {
                    throw new System.ArgumentException("Array too long");
                }
                int index = 0;
                for (index = 0; (index < value.Length); index = (index + 1))
                {
                    if ((value[index] < 0D))
                    {
                        throw new System.ArgumentOutOfRangeException();
                    }
                }
                for (index = 0; (index < value.Length); index = (index + 1))
                {
                    if ((value[index] > 1D))
                    {
                        throw new System.ArgumentOutOfRangeException();
                    }
                }
                this.m_planckFactor = value;
            }
        }

        /// <summary>
        /// The copenhagen texture.
        /// </summary>
        [Newtonsoft.Json.JsonPropertyAttribute("copenhagenTexture")]
        public TextureInfo CopenhagenTexture
        {
            get
            {
                return this.m_copenhagenTexture;
            }
            set
            {
                this.m_copenhagenTexture = value;
            }
        }

        /// <summary>
        /// The entanglement factor of the material.
        /// </summary>
        [Newtonsoft.Json.JsonConverterAttribute(typeof(glTFLoader.Shared.ArrayConverter))]
        [Newtonsoft.Json.JsonPropertyAttribute("entanglementFactor")]
        public float[] EntanglementFactor
        {
            get
            {
                return this.m_entanglementFactor;
            }
            set
            {
                if ((value.Length < 3u))
                {
                    throw new System.ArgumentException("Array not long enough");
                }
                if ((value.Length > 3u))
                {
                    throw new System.ArgumentException("Array too long");
                }
                int index = 0;
                for (index = 0; (index < value.Length); index = (index + 1))
                {
                    if ((value[index] < 0D))
                    {
                        throw new System.ArgumentOutOfRangeException();
                    }
                }
                for (index = 0; (index < value.Length); index = (index + 1))
                {
                    if ((value[index] > 1D))
                    {
                        throw new System.ArgumentOutOfRangeException();
                    }
                }
                this.m_entanglementFactor = value;
            }
        }

        /// <summary>
        /// The probabilistic Factor of the material.
        /// </summary>
        [Newtonsoft.Json.JsonPropertyAttribute("probabilisticFactor")]
        public float ProbabilisticFactor
        {
            get
            {
                return this.m_probabilisticFactor;
            }
            set
            {
                if ((value < 0D))
                {
                    throw new System.ArgumentOutOfRangeException("ProbabilisticFactor", value, "Expected value to be greater than or equal to 0");
                }
                if ((value > 1D))
                {
                    throw new System.ArgumentOutOfRangeException("ProbabilisticFactor", value, "Expected value to be less than or equal to 1");
                }
                this.m_probabilisticFactor = value;
            }
        }

        /// <summary>
        /// The specular-glossiness texture.
        /// </summary>
        [Newtonsoft.Json.JsonPropertyAttribute("superpositionCollapseTexture")]
        public TextureInfo SuperpositionCollapseTexture
        {
            get
            {
                return this.m_superpositionCollapseTexture;
            }
            set
            {
                this.m_superpositionCollapseTexture = value;
            }
        }

        /// <summary>
        /// Dictionary object with extension-specific objects.
        /// </summary>
        [Newtonsoft.Json.JsonPropertyAttribute("extensions")]
        public System.Collections.Generic.Dictionary<string, object> Extensions
        {
            get
            {
                return this.m_extensions;
            }
            set
            {
                this.m_extensions = value;
            }
        }

        /// <summary>
        /// Application-specific data.
        /// </summary>
        [Newtonsoft.Json.JsonPropertyAttribute("extras")]
        public Extras Extras
        {
            get
            {
                return this.m_extras;
            }
            set
            {
                this.m_extras = value;
            }
        }

        public bool ShouldSerializeDiffuseFactor()
        {
            return (m_planckFactor.SequenceEqual(new float[] {
                        1F,
                        1F,
                        1F,
                        1F}) == false);
        }

        public bool ShouldSerializeDiffuseTexture()
        {
            return ((m_copenhagenTexture == null)
                        == false);
        }

        public bool ShouldSerializeSpecularFactor()
        {
            return (m_entanglementFactor.SequenceEqual(new float[] {
                        1F,
                        1F,
                        1F}) == false);
        }

        public bool ShouldSerializeGlossinessFactor()
        {
            return ((m_probabilisticFactor == 1F)
                        == false);
        }

        public bool ShouldSerializeSpecularGlossinessTexture()
        {
            return ((m_superpositionCollapseTexture == null)
                        == false);
        }

        public bool ShouldSerializeExtensions()
        {
            return ((m_extensions == null)
                        == false);
        }

        public bool ShouldSerializeExtras()
        {
            return ((m_extras == null)
                        == false);
        }
    }
}
