using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetGenerator.Runtime.ExtensionMethods
{
    public static class SamplerExtensionMethods
    {
        /// <summary>
        /// Function which determines if two Sampler objects have equal values
        /// </summary>
        /// <param name="s1"></param>
        /// <param name="s2"></param>
        /// <returns></returns>
        public static bool SamplersEqual(this glTFLoader.Schema.Sampler s1, glTFLoader.Schema.Sampler s2)
        {
            return ((s1.MagFilter == s2.MagFilter) && (s1.MinFilter == s2.MinFilter) && (s1.Name == s2.Name) && (s1.WrapS == s2.WrapS) && (s1.WrapT == s2.WrapT));

        }
    }
}
