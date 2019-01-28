using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Imagery
{
    /// <summary>
    /// Physical Based Rendering (PBR) set of textures
    /// - Base color texture
    /// - Normal map texture
    /// 
    /// not supported or used in DEM.net
    /// - Ambient occlusion texture
    /// - Emissive texture
    /// </summary>
    public class PBRTexture
    {
        public TextureInfo BaseColorTexture { get; set; }
        public TextureInfo NormalTexture { get; set; }
        public IEnumerable<Vector2> TextureCoordSets { get; set; }

        public static PBRTexture Create(TextureInfo baseColorTexture, TextureInfo normalMapTexture, IEnumerable<Vector2> textureUVMap = null)
        {
            return new PBRTexture() {
                BaseColorTexture = baseColorTexture,
                NormalTexture = normalMapTexture,
                TextureCoordSets = textureUVMap
            };
        }

        public static PBRTexture Create(TextureInfo baseColorTexture)
        {
            return Create(baseColorTexture, null, null);
        }

    }
}
